using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SafeShelter
{
    public sealed class CrowdSceneBindings : MonoBehaviour
    {
        public BoxCollider hallFloor;
        public FurnitureLayout layout;
        public SharedFurniture blocker;
        public Transform clearBedPose;
        public Transform aisleGateSouth, aisleGateNorth;
        public Transform pantryPortal, pantryService;
        public Transform toiletPortal, toiletEntry, toiletService;
        public Transform[] residentStarts, transitQueue, pantryQueue, toiletQueue;
        public BoxCollider[] staticObstacles;
        public GameObject[] occupantVisuals;

        public IEnumerable<Transform> Anchors => new[] { aisleGateSouth, aisleGateNorth,
            pantryPortal, pantryService, toiletPortal, toiletEntry, toiletService }
            .Concat(residentStarts).Concat(transitQueue).Concat(pantryQueue).Concat(toiletQueue);

        public void ValidateBindings()
        {
            if (hallFloor == null || layout == null || blocker == null || clearBedPose == null)
                throw new InvalidOperationException("Crowd bindings require floor, layout, blocker and clear bed pose.");
            Require(residentStarts, 12, "ResidentStart");
            Require(transitQueue, 12, "TransitQueue");
            Require(pantryQueue, 8, "PantryQueue");
            Require(toiletQueue, 4, "ToiletQueue");
            if (Anchors.Any(t => t == null) || staticObstacles == null || staticObstacles.Length == 0 || staticObstacles.Any(c => c == null))
                throw new InvalidOperationException("Crowd anchors and static obstacles must be assigned.");
            if (occupantVisuals == null || occupantVisuals.Length != 2 || occupantVisuals.Any(v => v == null))
                throw new InvalidOperationException("Assign both occupant visual prefabs.");
        }

        static void Require(Transform[] values, int count, string field)
        {
            if (values == null || values.Length != count || values.Any(t => t == null) || values.Distinct().Count() != count)
                throw new InvalidOperationException(field + " requires " + count + " distinct transforms in order.");
            for (int i = 0; i < count; i++)
                if (values[i].name != $"{field}_{i + 1:00}")
                    throw new InvalidOperationException($"Expected {field}_{i + 1:00} at index {i}.");
        }
    }
}
