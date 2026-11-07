using System;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Local milestone implementation of the agreed furniture API.
    /// Add host authority at these mutation methods when networking is added.</summary>
    [DisallowMultipleComponent]
    public sealed class SharedFurniture : MonoBehaviour
    {
        Vector3 initialPosition;
        Quaternion initialRotation;
        public event Action<SharedFurniture> PoseChanged;

        void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void MoveTo(Vector3 position)
        {
            if (!isActiveAndEnabled || !IsFinite(position)) return;
            // Preserve the initial height; planner requests contain world X/Z only.
            position.y = initialPosition.y;
            transform.position = position;
            PoseChanged?.Invoke(this);
        }

        public void Rotate90()
        {
            if (!isActiveAndEnabled) return;
            transform.Rotate(Vector3.up, 90f, Space.World);
            PoseChanged?.Invoke(this);
        }

        public void ResetPose()
        {
            if (!isActiveAndEnabled) return;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            PoseChanged?.Invoke(this);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
