using System;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SafeShelter.Editor
{
    public static class CrowdValidation
    {
        static T[] All<T>(Scene scene) where T:Component =>
            scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();

        public static void Validate(Scene scene)
        {
            var directors = All<CrowdDirector>(scene);
            if (directors.Length != 1 || All<CrowdSceneBindings>(scene).Length != 1)
                throw new InvalidOperationException("Final crowd scene requires one CrowdDirector and CrowdSceneBindings.");
            var director = directors[0];
            var binding = new SerializedObject(director).FindProperty("bindings").objectReferenceValue as CrowdSceneBindings;
            if (binding == null || binding.gameObject != director.gameObject || director.GetComponent<NetworkObject>() == null)
                throw new InvalidOperationException("Wire the director and bindings on the same network scene root.");
            binding.ValidateBindings();
            var prefab = new SerializedObject(director).FindProperty("occupantPrefab").objectReferenceValue as GameObject;
            var manager = All<NetworkManager>(scene).Single();
            if (prefab == null || prefab.GetComponent<NetworkOccupant>() == null ||
                !manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Any(l=>l != null && l.PrefabList.Any(p=>p.Prefab==prefab)) ||
                new SerializedObject(prefab.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue == 0)
                throw new InvalidOperationException("Register the occupant network prefab with a persistent network identity.");
            var identities = manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Where(l => l != null)
                .SelectMany(l => l.PrefabList).Select(p => p.Prefab).Where(p => p != null).Distinct()
                .Select(p => p.GetComponent<NetworkObject>()).ToArray();
            if (identities.Any(n => n == null)) throw new InvalidOperationException("Every network prefab needs a root NetworkObject.");
            var hashes = identities.Select(n => new SerializedObject(n).FindProperty("GlobalObjectIdHash").longValue).ToArray();
            var transform = prefab.GetComponent<NetworkTransform>();
            var agent = prefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (hashes.Any(h => h == 0) || hashes.Distinct().Count() != hashes.Length ||
                prefab.GetComponentsInChildren<NetworkObject>(true).Length != 1 ||
                transform == null || !transform.Interpolate ||
                new SerializedObject(transform).FindProperty("AuthorityMode").enumValueIndex != 0 ||
                agent == null || agent.enabled || agent.agentTypeID != 0 ||
                Mathf.Abs(agent.radius - .28f) > .001f || prefab.GetComponent<SharedFurniture>() != null)
                throw new InvalidOperationException("Occupants require unique identity, server interpolation and a disabled 0.28 m navigation agent.");
            foreach(var visual in binding.occupantVisuals)
            {
                var animator = visual.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null || animator.applyRootMotion ||
                    !animator.parameters.Any(p=>p.name=="MoveSpeed" && p.type==AnimatorControllerParameterType.Float) ||
                    visual.GetComponentInChildren<Collider>(true) != null || visual.GetComponentInChildren<NetworkObject>(true) != null)
                    throw new InvalidOperationException("Invalid humanoid visual contract: "+visual.name);
            }
            var controls = All<CrowdDesktopControls>(scene);
            var viewers = All<CrowdViewerPresence>(scene);
            if (controls.Length != 1 || viewers.Length != 1 ||
                controls[0].GetComponent<FurnitureLayout>() == null ||
                viewers[0].GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
                throw new InvalidOperationException("Place crowd controls on DesktopRig and viewer presence under XR Origin.");
            foreach (var component in new Component[] {controls[0],viewers[0]})
                if (new SerializedObject(component).FindProperty("director").objectReferenceValue != director)
                    throw new InvalidOperationException("Assign the scene CrowdDirector to "+component.name);
            var head = new SerializedObject(viewers[0]).FindProperty("viewHead").objectReferenceValue as Transform;
            if (head == null || head.GetComponent<Camera>() == null ||
                head != viewers[0].GetComponentInParent<Unity.XR.CoreUtils.XROrigin>().Camera.transform)
                throw new InvalidOperationException("Assign the tracked XR camera to viewHead.");
            Debug.Log("CROWD INTEGRATION VALIDATION PASS");
        }
    }
}
