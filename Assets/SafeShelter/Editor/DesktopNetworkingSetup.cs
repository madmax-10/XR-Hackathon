using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeShelter.Editor
{
    public static class DesktopNetworkingSetup
    {
        const string LocalScene = "Assets/SafeShelter/Scenes/DesktopTest.unity";
        public const string NetworkScene = "Assets/SafeShelter/Scenes/DesktopNetworkTest.unity";
        const string NetworkPrefab = "Assets/SafeShelter/Prefabs/NetworkBed.prefab";
        const string PrefabList = "Assets/SafeShelter/Prefabs/SafeShelterNetworkPrefabs.asset";
        internal static bool IsBuildingNetworkTest { get; private set; }

        [MenuItem("Tools/SafeShelter/Open Networking Test")]
        public static void OpenNetworkingTest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before opening the networking test.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareNetworkTest();
        }

        public static void PrepareNetworkTest()
        {
            // A second invocation opens the existing assets instead of overwriting hand edits.
            if (File.Exists(NetworkScene))
            {
                EditorSceneManager.OpenScene(NetworkScene);
                EnsureBuildScene();
                EnsureConstraints(SceneManager.GetActiveScene());
                SaveValidatedNetworkAssets();
                ValidateNetworkTest();
                return;
            }
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SafeShelter/Prefabs/SharedBed.prefab");
            var source = (GameObject)PrefabUtility.InstantiatePrefab(template);
            source.AddComponent<NetworkFurniture>();
            var transformSync = source.GetComponent<NetworkTransform>();
            transformSync.InLocalSpace = false;
            transformSync.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, NetworkPrefab);
            UnityEngine.Object.DestroyImmediate(source);

            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            list.Add(new NetworkPrefab { Prefab = prefab });
            AssetDatabase.CreateAsset(list, PrefabList);

            var scene = EditorSceneManager.OpenScene(LocalScene, OpenSceneMode.Single);
            var layout = FindInScene<FurnitureLayout>(scene).Single();
            var planner = FindInScene<DesktopPlanner>(scene).Single();
            var oldBed = FindInScene<SharedFurniture>(scene).Single();
            var bed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            bed.transform.SetPositionAndRotation(oldBed.transform.position, oldBed.transform.rotation);
            UnityEngine.Object.DestroyImmediate(oldBed.gameObject);

            var managerObject = new GameObject("NetworkSession");
            var transport = managerObject.AddComponent<UnityTransport>();
            var manager = managerObject.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = null;
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            manager.NetworkConfig.ProtocolVersion = 1;
            var session = layout.gameObject.AddComponent<SessionController>();
            SetReference(session, "networkManager", manager);
            SetReference(session, "transport", transport);
            SetReference(layout, "session", session);
            SetReference(layout, "bedPrefab", prefab.GetComponent<SharedFurniture>());
            SetReference(planner, "session", session);
            var serializedLayout = new SerializedObject(layout);
            var initial = serializedLayout.FindProperty("initialFurniture");
            initial.arraySize = 1;
            initial.GetArrayElementAtIndex(0).objectReferenceValue = bed.GetComponent<SharedFurniture>();
            serializedLayout.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, NetworkScene);
            EnsureBuildScene();
            EnsureConstraints(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            SaveValidatedNetworkAssets();
            ValidateNetworkTest();
            Debug.Log("Networking test created. Press Play, then Start Host. Local DesktopTest is preserved.");
        }

        /// <summary>Adds the keep-clear zone objects once; existing hand-wired objects are left alone.</summary>
        static void EnsureConstraints(Scene scene)
        {
            if (FindInScene<ConstraintManager>(scene).Length > 0) return;
            var layout = FindInScene<FurnitureLayout>(scene).Single();
            var planner = FindInScene<DesktopPlanner>(scene).Single();
            var floor = new SerializedObject(planner).FindProperty("placementSurface").objectReferenceValue as Collider;
            if (floor == null) throw new InvalidOperationException("The planner needs its Placement Surface floor collider.");

            var constraintsObject = new GameObject("Constraints");
            SceneManager.MoveGameObjectToScene(constraintsObject, scene);
            constraintsObject.AddComponent<NetworkObject>();
            var constraints = constraintsObject.AddComponent<ConstraintManager>();
            SetReference(constraints, "hallFloor", floor);
            SetReference(constraints, "layout", layout);
            SetReference(planner, "constraints", constraints);

            var bed = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefab);
            var viewObject = new GameObject("ZoneDebugView");
            SceneManager.MoveGameObjectToScene(viewObject, scene);
            var view = viewObject.AddComponent<ZoneDebugView>();
            SetReference(view, "constraints", constraints);
            SetReference(view, "material", bed.GetComponentInChildren<Renderer>().sharedMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Added ConstraintManager and ZoneDebugView to the networking test.");
        }

        static T[] FindInScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static void EnsureBuildScene()
        {
            // NGO must resolve the active scene on the Editor host as well as built clients.
            var buildScenes = EditorBuildSettings.scenes.ToList();
            var existing = buildScenes.FindIndex(item => item.path == NetworkScene);
            if (existing < 0) buildScenes.Add(new EditorBuildSettingsScene(NetworkScene, true));
            else buildScenes[existing] = new EditorBuildSettingsScene(NetworkScene, true);
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        static void SaveValidatedNetworkAssets()
        {
            // A freshly added NetworkObject has no persistent asset ID until its first save.
            // Reimport after saving lets NGO's own OnValidate generate the prefab hash.
            AssetDatabase.ImportAsset(NetworkPrefab, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefab);
            var networkObject = prefab.GetComponent<NetworkObject>();
            // Invoke the pinned NGO version's editor validation after a persistent ID exists.
            // The package computes its own hash; we do not manufacture networking identities.
            networkObject.SendMessage("OnValidate", SendMessageOptions.RequireReceiver);
            EditorUtility.SetDirty(networkObject);
            foreach (var instance in FindInScene<NetworkObject>(SceneManager.GetActiveScene()))
                instance.SendMessage("OnValidate", SendMessageOptions.RequireReceiver);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var serialized = new SerializedObject(networkObject);
            if (serialized.FindProperty("GlobalObjectIdHash").longValue == 0)
                throw new InvalidOperationException("NGO has not generated the bed prefab hash. Reopen the networking test after import.");
        }

        static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ValidateNetworkTest()
        {
            var scene = SceneManager.GetActiveScene();
            if (FindInScene<NetworkManager>(scene).Length != 1 ||
                FindInScene<SessionController>(scene).Length != 1 ||
                FindInScene<NetworkFurniture>(scene).Length != 1)
                throw new InvalidOperationException("Network test needs exactly one manager, session, and starting bed.");
            var net = FindInScene<NetworkFurniture>(scene).Single();
            if (net.GetComponent<NetworkObject>() == null || net.GetComponent<NetworkTransform>() == null)
                throw new InvalidOperationException("The bed is missing networking components.");
            var constraints = FindInScene<ConstraintManager>(scene);
            if (constraints.Length != 1 || new SerializedObject(constraints[0].GetComponent<NetworkObject>())
                    .FindProperty("GlobalObjectIdHash").longValue == 0)
                throw new InvalidOperationException("Network test needs exactly one ConstraintManager with a scene NetworkObject hash.");
        }

        [MenuItem("Tools/SafeShelter/Build Windows Networking Test")]
        public static void BuildWindowsNetworkingTest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNetworkTestBatch();
        }

        public static void BuildNetworkTestBatch()
        {
            PrepareNetworkTest();
            var args = Environment.GetCommandLineArgs();
            string output = Path.GetFullPath("Builds/DesktopNetworkTest/SafeShelter.exe");
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--safeshelter-build") output = args[i + 1];
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var oldFullscreen = PlayerSettings.fullScreenMode;
            var standaloneTarget = UnityEditor.Build.NamedBuildTarget.Standalone;
            var oldBackend = PlayerSettings.GetScriptingBackend(standaloneTarget);
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget
                .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool oldXR = xr != null && xr.InitManagerOnStart;
            try
            {
                IsBuildingNetworkTest = true;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                // Fast laptop test build; do not require the optional Windows IL2CPP module.
                PlayerSettings.SetScriptingBackend(standaloneTarget, ScriptingImplementation.Mono2x);
                if (xr != null) xr.InitManagerOnStart = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { NetworkScene }, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Networking test build failed. See Console.");
                Debug.Log("SafeShelter networking test built: " + output);
            }
            finally
            {
                IsBuildingNetworkTest = false;
                PlayerSettings.fullScreenMode = oldFullscreen;
                PlayerSettings.SetScriptingBackend(standaloneTarget, oldBackend);
                if (xr != null) xr.InitManagerOnStart = oldXR;
            }
        }
    }
}
