using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Host-authoritative keep-clear zone. Input components only submit requests.</summary>
    [DisallowMultipleComponent]
    public sealed class ConstraintManager : NetworkBehaviour
    {
        public const float MinimumSide = 0.1f;
        [SerializeField] Collider hallFloor;
        [SerializeField] FurnitureLayout layout;
        readonly NetworkVariable<ZoneState> zone = new NetworkVariable<ZoneState>(ZoneState.Inactive,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly List<Collider> colliders = new List<Collider>();

        public ZoneState CurrentZone => IsSpawned ? zone.Value : ZoneState.Inactive;
        /// <summary>Raised locally whenever the replicated snapshot changes, including on spawn/despawn.</summary>
        public event Action<ZoneState> ZoneChanged;
        public bool GetConflict() => CurrentZone.HasConflict;

        void Awake()
        {
            if (hallFloor == null || layout == null)
            {
                Debug.LogError("ConstraintManager requires the hall floor collider and the furniture layout.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            zone.OnValueChanged += Changed;
            ZoneChanged?.Invoke(zone.Value);
        }

        public override void OnNetworkDespawn()
        {
            zone.OnValueChanged -= Changed;
            ZoneChanged?.Invoke(ZoneState.Inactive);
        }

        void Changed(ZoneState previous, ZoneState current) { ZoneChanged?.Invoke(current); }

        /// <summary>Submits a rectangle from two floor points. The host may reject it.</summary>
        public void RequestZone(Vector3 cornerA, Vector3 cornerB)
        {
            if (!CanRequest()) return;
            RequestZoneRpc(cornerA, cornerB);
        }

        public void RequestClearZone()
        {
            if (!CanRequest()) return;
            RequestClearZoneRpc();
        }

        bool CanRequest()
        {
            if (enabled && IsSpawned) return true;
            Debug.LogWarning("Start or join a session before marking a keep-clear zone.", this);
            return false;
        }

        [Rpc(SendTo.Server)]
        void RequestZoneRpc(Vector3 cornerA, Vector3 cornerB, RpcParams rpcParams = default)
        {
            if (!enabled) return;
            if (!TryNormalize(cornerA, cornerB, out var min, out var max, out var reason))
            {
                Debug.LogWarning($"Rejected keep-clear zone from client {rpcParams.Receive.SenderClientId}: {reason}", this);
                return;
            }
            zone.Value = new ZoneState(min, max, Overlaps(min, max));
        }

        [Rpc(SendTo.Server)]
        void RequestClearZoneRpc() { if (enabled) zone.Value = ZoneState.Inactive; }

        void LateUpdate()
        {
            // Polling catches every layout change: drag, rotate, add, remove, reset, despawn.
            if (!IsSpawned || !IsServer) return;
            var current = zone.Value;
            if (!current.IsActive) return;
            bool conflict = Overlaps(current.CornerA, current.CornerB);
            if (conflict != current.HasConflict) zone.Value = current.WithConflict(conflict);
        }

        bool TryNormalize(Vector3 a, Vector3 b, out Vector3 min, out Vector3 max, out string reason)
        {
            min = max = default;
            if (!IsFinite(a) || !IsFinite(b)) { reason = "non-finite coordinates"; return false; }
            var hall = hallFloor.bounds;
            if (!Inside(a, hall) || !Inside(b, hall)) { reason = "a corner is outside the hall floor"; return false; }
            float floorY = hall.max.y;
            min = new Vector3(Mathf.Min(a.x, b.x), floorY, Mathf.Min(a.z, b.z));
            max = new Vector3(Mathf.Max(a.x, b.x), floorY, Mathf.Max(a.z, b.z));
            if (max.x - min.x < MinimumSide || max.z - min.z < MinimumSide)
            {
                reason = $"each side must be at least {MinimumSide} m";
                return false;
            }
            reason = null;
            return true;
        }

        bool Overlaps(Vector3 min, Vector3 max)
        {
            // Collider bounds lag transform writes until physics syncs.
            Physics.SyncTransforms();
            foreach (var item in layout.Furniture)
            {
                if (!item.IsPresent) continue;
                item.GetComponentsInChildren(colliders);
                foreach (var collider in colliders)
                {
                    if (!collider.enabled) continue;
                    var bounds = collider.bounds;
                    // Edge contact counts as conflict; vertical separation is ignored.
                    if (bounds.max.x >= min.x && bounds.min.x <= max.x &&
                        bounds.max.z >= min.z && bounds.min.z <= max.z) return true;
                }
            }
            return false;
        }

        static bool Inside(Vector3 point, Bounds hall) => point.x >= hall.min.x && point.x <= hall.max.x &&
            point.z >= hall.min.z && point.z <= hall.max.z;
        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
