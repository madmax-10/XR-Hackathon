using System;
using Unity.Netcode;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Read-only keep-clear snapshot. The host replicates it as one value.</summary>
    public struct ZoneState : INetworkSerializable, IEquatable<ZoneState>
    {
        bool isActive;
        Vector3 cornerA;
        Vector3 cornerB;
        bool hasConflict;

        public ZoneState(Vector3 min, Vector3 max, bool conflict)
        {
            isActive = true;
            cornerA = min;
            cornerB = max;
            hasConflict = conflict;
        }

        public static ZoneState Inactive => default;
        public bool IsActive => isActive;
        /// <summary>Minimum X/Z corner at the configured floor height.</summary>
        public Vector3 CornerA => cornerA;
        /// <summary>Maximum X/Z corner at the configured floor height.</summary>
        public Vector3 CornerB => cornerB;
        public bool HasConflict => isActive && hasConflict;
        public Vector3 Center => (cornerA + cornerB) * 0.5f;
        public Vector2 SizeXZ => new Vector2(cornerB.x - cornerA.x, cornerB.z - cornerA.z);

        public ZoneState WithConflict(bool conflict) =>
            isActive ? new ZoneState(cornerA, cornerB, conflict) : Inactive;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref isActive);
            serializer.SerializeValue(ref cornerA);
            serializer.SerializeValue(ref cornerB);
            serializer.SerializeValue(ref hasConflict);
        }

        public bool Equals(ZoneState other) => isActive == other.isActive && hasConflict == other.hasConflict &&
            cornerA == other.cornerA && cornerB == other.cornerB;
        public override bool Equals(object obj) => obj is ZoneState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(isActive, cornerA, cornerB, hasConflict);
    }
}
