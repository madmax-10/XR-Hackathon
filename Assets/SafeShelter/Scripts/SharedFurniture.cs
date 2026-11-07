using System;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Furniture API shared by local testing and authoritative network furniture.</summary>
    [DisallowMultipleComponent]
    public sealed class SharedFurniture : MonoBehaviour
    {
        Vector3 initialPosition;
        Quaternion initialRotation;
        NetworkFurniture networkFurniture;
        public bool CanEdit => isActiveAndEnabled && (networkFurniture == null || networkFurniture.CanEdit);
        public event Action<SharedFurniture> PoseChanged;

        void Awake()
        {
            networkFurniture = GetComponent<NetworkFurniture>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void MoveTo(Vector3 position)
        {
            if (!CanEdit || !IsFinite(position)) return;
            // Preserve the initial height; planner requests contain world X/Z only.
            position.y = initialPosition.y;
            transform.position = position;
            PoseChanged?.Invoke(this);
        }

        public void Rotate90()
        {
            if (!CanEdit) return;
            transform.Rotate(Vector3.up, 90f, Space.World);
            PoseChanged?.Invoke(this);
        }

        public void ResetPose()
        {
            if (!CanEdit) return;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (networkFurniture != null) networkFurniture.Teleport(initialPosition, initialRotation);
            PoseChanged?.Invoke(this);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
