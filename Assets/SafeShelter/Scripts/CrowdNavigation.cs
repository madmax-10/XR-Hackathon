using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SafeShelter
{
    internal sealed class CrowdNavigation : IDisposable
    {
        NavMeshData data;
        NavMeshDataInstance instance;
        public void Build(CrowdSceneBindings bindings)
        {
            Dispose();
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            AddBox(sources, bindings.hallFloor, 0);
            foreach (var box in bindings.staticObstacles) AddBox(sources, box, 1);
            foreach (var item in bindings.layout.Furniture)
            {
                if (!item.IsPresent) continue;
                foreach (var collider in item.GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled || collider.isTrigger) continue;
                    if (collider is BoxCollider box) AddBox(sources, box, 1);
                    else sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                        transform = Matrix4x4.TRS(collider.bounds.center, Quaternion.identity, Vector3.one),
                        size = collider.bounds.size, area = 1 });
                }
            }
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = .28f;
            settings.agentHeight = 1.75f;
            settings.agentClimb = .15f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .07f;
            settings.minRegionArea = .1f;
            var bounds = bindings.hallFloor.bounds;
            bounds.Expand(new Vector3(0, 8, 0));
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Navigation build produced no floor.");
            instance = NavMesh.AddNavMeshData(data);
            foreach (var anchor in bindings.Anchors) Point(anchor);
        }

        static void AddBox(List<NavMeshBuildSource> sources, BoxCollider box, int area)
        {
            if (!box.enabled || !box.gameObject.activeInHierarchy) return;
            sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                transform = box.transform.localToWorldMatrix * Matrix4x4.Translate(box.center),
                size = box.size, area = area });
        }

        public static Vector3 Point(Transform anchor)
        {
            if (!NavMesh.SamplePosition(anchor.position, out var hit, .35f, NavMesh.AllAreas) ||
                Mathf.Abs(hit.position.y - anchor.position.y) > .2f)
                throw new InvalidOperationException("Invalid navigation anchor " + anchor.name);
            return hit.position;
        }

        public bool CrossingOpen(CrowdSceneBindings bindings)
        {
            var path = new NavMeshPath();
            var south = Point(bindings.aisleGateSouth);
            var north = Point(bindings.aisleGateNorth);
            if (!NavMesh.CalculatePath(south, north, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                return false;
            foreach (var corner in path.corners)
                if (Mathf.Abs(corner.x) > 1.15f || corner.z < south.z - .35f || corner.z > north.z + .35f) return false;
            return true;
        }

        public void Dispose()
        {
            if (instance.valid) instance.Remove();
            if (data != null) UnityEngine.Object.Destroy(data);
            data = null;
        }
    }
}
