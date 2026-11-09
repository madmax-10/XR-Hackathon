using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace SafeShelter.Editor
{
    public static class IntegrationSetup
    {
        public const string ScenePath = "Assets/Scenes/SafeShelter_Integration.unity";
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("Tools/SafeShelter/Fix XR Floor Grounding")]
        public static void FixXRFloorGrounding()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var gravity = All<GravityProvider>(scene).Single();
            gravity.useGravity = false;
            gravity.sphereCastLayerMask |= 1 << 8;
            PrefabUtility.RecordPrefabInstancePropertyModifications(gravity);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Validate();
        }
        static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Tools/SafeShelter/Repair Integration Scene")]
        public static void Repair()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var layout = All<FurnitureLayout>(scene).Single();
            var planner = All<DesktopPlanner>(scene).Single();
            var rig = All<XROrigin>(scene).Single();
            var manager = All<NetworkManager>(scene).SingleOrDefault();
            if (manager == null) manager = new GameObject("NetworkSession").AddComponent<NetworkManager>();
            var transport = manager.GetComponent<UnityTransport>() ?? manager.gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.ProtocolVersion = 1;
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.PlayerPrefab = null;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/SafeShelter/Prefabs/SafeShelterNetworkPrefabs.asset"));
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            // Session stays active when the desktop rig is disabled on Quest.
            var oldSession = All<SessionController>(scene).SingleOrDefault();
            var session = manager.GetComponent<SessionController>() ?? manager.gameObject.AddComponent<SessionController>();
            if (oldSession != null && oldSession != session) UnityEngine.Object.DestroyImmediate(oldSession);
            Reference(session, "networkManager", manager);
            Reference(session, "transport", transport);
            var data = new SerializedObject(session);
            data.FindProperty("autoStartHost").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            Reference(layout, "session", session);
            Reference(planner, "session", session);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SafeShelter/Prefabs/NetworkBed.prefab");
            Reference(layout, "bedPrefab", prefab.GetComponent<SharedFurniture>());
            var beds = All<Transform>(scene).Where(t => t.name.StartsWith("Bed_", StringComparison.Ordinal)).ToArray();
            var retained = All<NetworkFurniture>(scene).Where(n => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(n.gameObject) == AssetDatabase.GetAssetPath(prefab)).ToArray();
            foreach (var bed in beds)
            {
                // Keep the existing network prefab; remove only a proven overlapping plain bed.
                if (retained.Any(n => Vector2.Distance(new Vector2(n.transform.position.x, n.transform.position.z), new Vector2(bed.position.x, bed.position.z)) < .05f))
                    UnityEngine.Object.DestroyImmediate(bed.gameObject);
                else if (bed.GetComponent<NetworkFurniture>() == null) bed.gameObject.AddComponent<NetworkFurniture>();
            }
            var table = All<Transform>(scene).Single(t => t.name == "SupplyTable");
            if (table.GetComponent<NetworkFurniture>() == null) table.gameObject.AddComponent<NetworkFurniture>();
            foreach (var item in All<NetworkFurniture>(scene))
            {
                item.GetComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Server;
                item.GetComponent<NetworkTransform>().InLocalSpace = false;
            }
            // The network bed geometry uses a floor-level root; align its lowest collider to the floor.
            var floor = All<Transform>(scene).Single(t => t.name == "HallFloor").GetComponent<Collider>();
            foreach (var item in retained)
            {
                float bottom = item.GetComponentsInChildren<Collider>().Min(c => c.bounds.min.y);
                item.transform.position += Vector3.up * (floor.bounds.max.y - bottom);
            }
            var center = All<Transform>(scene).Single(t => t.name == "HalllCenter" || t.name == "HallCenter");
            center.name = "HallCenter";
            Reference(planner, "floorPlane", center);
            Reference(planner, "placementSurface", floor);
            Reference(All<DesktopOrbitCamera>(scene).Single(), "focus", center);
            data = new SerializedObject(layout);
            var initial = data.FindProperty("initialFurniture");
            var furniture = All<SharedFurniture>(scene).OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
            initial.arraySize = furniture.Length;
            for (int i = 0; i < furniture.Length; i++) initial.GetArrayElementAtIndex(i).objectReferenceValue = furniture[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            var selector = manager.GetComponent<PlatformRigSelector>() ?? manager.gameObject.AddComponent<PlatformRigSelector>();
            Reference(selector, "desktopRig", layout.gameObject);
            Reference(selector, "xrRig", rig.gameObject);
            data = new SerializedObject(selector);
            var questObjects = scene.GetRootGameObjects().Where(o => o.name == "UI" || o.name == "Interactables" || o.name.Contains("Hands Permissions")).ToArray();
            var questOnly = data.FindProperty("questOnlyObjects");
            questOnly.arraySize = questObjects.Length;
            for (int i = 0; i < questObjects.Length; i++) questOnly.GetArrayElementAtIndex(i).objectReferenceValue = questObjects[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            var panel = manager.GetComponent<QuestSessionPanel>() ?? manager.gameObject.AddComponent<QuestSessionPanel>();
            Reference(panel, "session", session);
            Reference(panel, "xrCamera", rig.Camera);
            var standaloneXR = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standaloneXR != null) { standaloneXR.InitManagerOnStart = false; EditorUtility.SetDirty(standaloneXR); }
            PlayerSettings.Android.forceInternetPermission = true; // LAN sockets require INTERNET in the APK manifest.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            // NGO OnValidate runs naturally when components are added and the scene is saved.
            // Clear missing optional tutorial sprites without changing the original prefab assets.
            foreach (var component in All<Component>(scene))
            {
                if (!(component is UnityEngine.UI.Graphic) && !(component is AudioSource)) continue;
                var optional = new SerializedObject(component);
                foreach (var field in new[] { "m_Sprite", "m_Material", "m_Resource" })
                {
                    var value = optional.FindProperty(field);
                    if (value != null && value.propertyType == SerializedPropertyType.ObjectReference &&
                        value.objectReferenceValue == null && value.objectReferenceInstanceIDValue != 0)
                    {
                        Debug.LogWarning("Cleared missing optional UI/audio asset: " + component.name + "." + field);
                        value.objectReferenceValue = null;
                        optional.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
        }
        [MenuItem("Tools/SafeShelter/Validate Integration Scene")]
        public static void Validate()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try { ValidateScene(scene); }
            finally { if (openedForValidation) EditorSceneManager.CloseScene(scene, true); }
        }

        static T RequireOne<T>(Scene scene) where T : Component
        {
            var components = All<T>(scene);
            if (components.Length != 1)
                throw new InvalidOperationException($"Expected exactly one {typeof(T).Name}, found {components.Length}.");
            return components[0];
        }

        static UnityEngine.Object ReadReference(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        static void ValidateScene(Scene scene)
        {
            var manager = RequireOne<NetworkManager>(scene);
            var session = RequireOne<SessionController>(scene);
            var planner = RequireOne<DesktopPlanner>(scene);
            var rig = RequireOne<XROrigin>(scene);
            var layout = RequireOne<FurnitureLayout>(scene);
            var constraints = RequireOne<ConstraintManager>(scene);
            var view = RequireOne<ZoneDebugView>(scene);
            var marker = RequireOne<QuestKeepClearMarker>(scene);
            var floor = All<Collider>(scene).SingleOrDefault(c => c.name == "HallFloor");
            if (floor == null || floor.gameObject.layer != 8 || LayerMask.LayerToName(8) != "Floor")
                throw new InvalidOperationException("HallFloor requires a collider on the Floor layer (8).");
            var gravity = RequireOne<GravityProvider>(scene);
            if (gravity.useGravity || (gravity.sphereCastLayerMask.value & (1 << 8)) == 0)
                throw new InvalidOperationException("XR GravityProvider must have gravity off (flat hall) and include the Floor layer in its ground check.");
            var floorSize = floor.bounds.size;
            if (Mathf.Abs(floorSize.x - 24f) > 0.25f || Mathf.Abs(floorSize.z - 16f) > 0.25f)
            {
                Debug.LogWarning($"HallFloor is {floorSize.x:0.##} × {floorSize.z:0.##} m. Expected 24 × 16 m.", floor);
                throw new InvalidOperationException("HallFloor does not match the locked hall footprint.");
            }
            if (ReadReference(constraints, "hallFloor") != floor || ReadReference(constraints, "layout") != layout ||
                constraints.GetComponent<NetworkObject>() == null)
                throw new InvalidOperationException("ConstraintManager requires HallFloor, FurnitureLayout, and a NetworkObject.");
            if (ReadReference(planner, "constraints") != constraints || ReadReference(view, "constraints") != constraints ||
                ReadReference(view, "material") == null)
                throw new InvalidOperationException("Wire DesktopPlanner and ZoneDebugView to ConstraintManager, and assign the zone material.");
            var leftRay = ReadReference(marker, "leftRay") as XRRayInteractor;
            var rightRay = ReadReference(marker, "rightRay") as XRRayInteractor;
            if (ReadReference(marker, "constraints") != constraints || leftRay == null || rightRay == null ||
                leftRay == rightRay || !marker.transform.IsChildOf(rig.transform) ||
                new SerializedObject(marker).FindProperty("floorMask").intValue != (1 << 8))
                throw new InvalidOperationException("QuestKeepClearMarker requires the constraints, two distinct rays, and Floor-only mask under the XR origin.");

            var layoutData = new SerializedObject(layout);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SafeShelter/Prefabs/NetworkBed.prefab");
            if (prefab == null || layoutData.FindProperty("bedPrefab").objectReferenceValue != prefab.GetComponent<SharedFurniture>() ||
                !manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Any(list => list != null && list.PrefabList.Any(entry => entry.Prefab == prefab)))
                throw new Exception("NetworkBed must be both the layout prefab and a registered network prefab.");
            var initial = layoutData.FindProperty("initialFurniture");
            const int baselineCount = 22;
            if (initial.arraySize != baselineCount || All<SharedFurniture>(scene).Length != baselineCount)
                throw new InvalidOperationException("The scene and FurnitureLayout each require exactly 22 baseline furniture items.");
            var registered = new System.Collections.Generic.HashSet<SharedFurniture>();
            for (int i = 0; i < initial.arraySize; i++)
                if (!(initial.GetArrayElementAtIndex(i).objectReferenceValue is SharedFurniture item) ||
                    item.gameObject.scene != scene || !registered.Add(item) || item.GetComponent<NetworkFurniture>() == null ||
                    item.GetComponent<NetworkObject>() == null)
                    throw new InvalidOperationException($"Initial Furniture element {i} must reference a distinct scene item with NetworkFurniture and NetworkObject.");
            if (session.gameObject != manager.gameObject)
                throw new Exception("Session must remain active independently of DesktopRig.");
            if (manager.NetworkConfig.NetworkTransport != manager.GetComponent<UnityTransport>() || !manager.NetworkConfig.EnableSceneManagement)
                throw new Exception("Invalid scene-managed LAN transport configuration.");
            var hashes = All<NetworkObject>(scene).Select(n => new SerializedObject(n).FindProperty("GlobalObjectIdHash").longValue).ToArray();
            if (hashes.Any(h => h == 0) || hashes.Distinct().Count() != hashes.Length) throw new Exception("Invalid scene network identities.");
            int brokenReferences = 0;
            foreach (var component in All<Component>(scene))
            {
                if (component == null) throw new Exception("Missing script in integration scene.");
                var data = new SerializedObject(component);
                var property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var target = property.objectReferenceValue;
                    if (target == null && property.objectReferenceInstanceIDValue != 0) { Debug.LogWarning("Broken reference: " + component.name + "." + property.propertyPath); brokenReferences++; }
                    GameObject obj = target as GameObject;
                    if (target is Component targetComponent) obj = targetComponent.gameObject;
                    if (obj != null && !EditorUtility.IsPersistent(obj) && obj.scene != scene)
                        throw new Exception("Cross-scene reference: " + component.name + "." + property.propertyPath);
                }
            }
            if (brokenReferences > 0) throw new Exception("Broken references: " + brokenReferences);
            Debug.Log("INTEGRATION VALIDATION PASS: references, unique network identities; furniture=" + registered.Count);
            CrowdValidation.Validate(scene);
        }
        public static bool IsBuildingIntegration { get; private set; }
        public static void RepairAndBuildWindows() { Repair(); BuildWindows(); }
        public static void BuildWindows()
        {
            Validate();
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var fullscreen = PlayerSettings.fullScreenMode;
            var resizable = PlayerSettings.resizableWindow;
            var width = PlayerSettings.defaultScreenWidth;
            var height = PlayerSettings.defaultScreenHeight;
            try
            {
                IsBuildingIntegration = true;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.defaultScreenWidth = 1366;
                PlayerSettings.defaultScreenHeight = 768;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = BuildTarget.StandaloneWindows64,
                    locationPathName = "Builds/Integration/SafeShelter.exe", options = BuildOptions.Development });
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows integration build failed.");
            }
            finally
            {
                IsBuildingIntegration = false;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                PlayerSettings.fullScreenMode = fullscreen;
                PlayerSettings.resizableWindow = resizable;
                PlayerSettings.defaultScreenWidth = width;
                PlayerSettings.defaultScreenHeight = height;
            }
        }
        public static void BuildAndroid()
        {
            Validate();
            try
            {
                IsBuildingIntegration = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = BuildTarget.Android,
                    locationPathName = "Builds/Integration/SafeShelter.apk", options = BuildOptions.Development });
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android integration build failed.");
            }
            finally { IsBuildingIntegration = false; }
        }
    }
}
