using System;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SafeShelter.Editor
{
    public static class CrowdSetup
    {
        const string PrefabPath = "Assets/SafeShelter/Prefabs/NetworkOccupant.prefab";
        static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void PrepareAndBuildProof()
        {
            CreatePrefab();
            DesktopNetworkingSetup.PrepareNetworkTest();
            var scene = SceneManager.GetActiveScene();
            var old = GameObject.Find("CrowdDemo");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("CrowdDemo");
            var binding = root.AddComponent<CrowdSceneBindings>();
            binding.layout = UnityEngine.Object.FindFirstObjectByType<FurnitureLayout>();
            binding.blocker = UnityEngine.Object.FindFirstObjectByType<SharedFurniture>();
            binding.hallFloor = GameObject.Find("TestFloor").GetComponent<BoxCollider>();
            binding.hallFloor.transform.localScale = new Vector3(24, .2f, 16);
            binding.clearBedPose = Anchor(root, "ClearBedPose", 4, -2);
            binding.clearBedPose.position += Vector3.up * .225f;
            binding.aisleGateSouth = Anchor(root, "AisleGateSouth", 0, -1.15f);
            binding.aisleGateNorth = Anchor(root, "AisleGateNorth", 0, 1.15f);
            binding.pantryPortal = Anchor(root, "PantryPortal", -1.25f, 2.65f);
            binding.pantryService = Anchor(root, "PantryService", -9.05f, .9f);
            binding.toiletPortal = Anchor(root, "ToiletPortal", 1.25f, 3.6f);
            binding.toiletEntry = Anchor(root, "ToiletEntry", 9.15f, 6.85f);
            binding.toiletService = Anchor(root, "ToiletService", 10.35f, 6.85f);
            binding.pantryQueue = Enumerable.Range(0,8).Select(i => Anchor(root, $"PantryQueue_{i+1:00}", -8.25f+i*.8f,.9f)).ToArray();
            binding.toiletQueue = Enumerable.Range(0,4).Select(i => Anchor(root, $"ToiletQueue_{i+1:00}", 8.35f-i*.8f,6.85f)).ToArray();
            binding.transitQueue = Enumerable.Range(0,12).Select(i => Anchor(root, $"TransitQueue_{i+1:00}", i%2==0?-.6f:.6f,-1.7f-.88f*(i/2))).ToArray();
            var starts = new[] {new Vector2(-2.2f,-7.15f),new Vector2(2.2f,-7.15f),new Vector2(-3.4f,-7.15f),new Vector2(3.4f,-7.15f),
                new Vector2(-3.4f,-6.15f),new Vector2(3.4f,-6.15f),new Vector2(-4.6f,-7.15f),new Vector2(4.6f,-7.15f),
                new Vector2(-2.2f,-6.15f),new Vector2(2.2f,-6.15f),new Vector2(-4.6f,-6.15f),new Vector2(4.6f,-6.15f)};
            binding.residentStarts = starts.Select((p,i) => Anchor(root, $"ResidentStart_{i+1:00}",p.x,p.y)).ToArray();
            binding.staticObstacles = new[] {Wall(root,-1.25f,-2.275f,7.45f),Wall(root,-1.25f,4.425f,1.15f),
                Wall(root,1.25f,-1.8f,8.4f),Wall(root,1.25f,4.9f,.2f)};
            binding.occupantVisuals = new[] {ProofVisual(), ProofVisual()};
            root.AddComponent<NetworkObject>();
            var director = root.AddComponent<CrowdDirector>();
            Assign(director,"bindings",binding);
            Assign(director,"occupantPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var controls = binding.layout.GetComponent<CrowdDesktopControls>() ?? binding.layout.gameObject.AddComponent<CrowdDesktopControls>();
            Assign(controls,"director",director);
            EditorSceneManager.SaveScene(scene);
            root.GetComponent<NetworkObject>().SendMessage("OnValidate");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            DesktopNetworkingSetup.BuildNetworkTestBatch();
        }
        static Transform Anchor(GameObject root, string name, float x, float z)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(root.transform);
            obj.transform.position = new Vector3(x,0,z);
            return obj.transform;
        }
        static BoxCollider Wall(GameObject root,float x,float z,float length)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "ProofPartition";
            obj.transform.SetParent(root.transform);
            obj.transform.position = new Vector3(x,1,z);
            obj.transform.localScale = new Vector3(.2f,2,length);
            return obj.GetComponent<BoxCollider>();
        }
        static GameObject ProofVisual()
        {
            const string path = "Assets/SafeShelter/Tests/CrowdProofVisual.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            var root = new GameObject("CrowdProofVisual");
            obj.transform.SetParent(root.transform);
            obj.transform.localPosition = new Vector3(0,.875f,0);
            obj.transform.localScale = new Vector3(.5f,.875f,.5f);
            var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/SafeShelter/Tests/CrowdProof.controller");
            controller.AddParameter("MoveSpeed",AnimatorControllerParameterType.Float);
            root.AddComponent<Animator>().runtimeAnimatorController = controller;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
        [MenuItem("Tools/SafeShelter/Create Crowd Network Prefab")]
        public static void CreatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var obj = new GameObject("NetworkOccupant");
                obj.AddComponent<NetworkOccupant>();
                var agent = obj.GetComponent<NavMeshAgent>();
                agent.radius=.28f; agent.height=1.75f; agent.acceleration=5; agent.angularSpeed=360;
                agent.stoppingDistance=.1f; agent.enabled=false;
                var sync = obj.GetComponent<NetworkTransform>();
                sync.AuthorityMode=NetworkTransform.AuthorityModes.Server;
                sync.InLocalSpace=false; sync.Interpolate=true; sync.PositionThreshold=.01f;
                prefab = PrefabUtility.SaveAsPrefabAsset(obj,PrefabPath);
                UnityEngine.Object.DestroyImmediate(obj);
            }
            prefab.GetComponent<NetworkObject>().SendMessage("OnValidate");
            PrefabUtility.SavePrefabAsset(prefab);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/SafeShelter/Prefabs/SafeShelterNetworkPrefabs.asset");
            if (!list.PrefabList.Any(p=>p.Prefab==prefab)) list.Add(new NetworkPrefab {Prefab=prefab});
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
        }
    }
}
