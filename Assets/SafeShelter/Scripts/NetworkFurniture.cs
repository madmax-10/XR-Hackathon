using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Optional network adapter. The original local test prefab has no adapter.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(SharedFurniture))]
    public sealed class NetworkFurniture : NetworkBehaviour
    {
        readonly NetworkVariable<bool> present = new NetworkVariable<bool>(true,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        Renderer[] visuals;
        Collider[] colliders;
        bool[] visualDefaults;
        bool[] colliderDefaults;
        public bool IsPresent => present.Value;
        public bool CanEdit => IsSpawned && IsServer && NetworkManager.IsListening &&
            !NetworkManager.ShutdownInProgress && IsPresent;

        void Awake()
        {
            visuals = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            visualDefaults = new bool[visuals.Length];
            colliderDefaults = new bool[colliders.Length];
            for (int i = 0; i < visuals.Length; i++) visualDefaults[i] = visuals[i].enabled;
            for (int i = 0; i < colliders.Length; i++) colliderDefaults[i] = colliders[i].enabled;
        }

        public override void OnNetworkSpawn()
        {
            present.OnValueChanged += PresenceChanged;
            ApplyPresence(present.Value);
        }

        public override void OnNetworkDespawn() { present.OnValueChanged -= PresenceChanged; }
        void PresenceChanged(bool previous, bool current) { ApplyPresence(current); }

        void ApplyPresence(bool visible)
        {
            for (int i = 0; i < visuals.Length; i++) visuals[i].enabled = visible && visualDefaults[i];
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = visible && colliderDefaults[i];
        }

        public void SetPresent(bool value)
        {
            if (!IsSpawned || !IsServer || NetworkManager.ShutdownInProgress) return;
            // Leave the root active so NGO can keep synchronizing this scene object's state.
            present.Value = value;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (CanEdit) GetComponent<NetworkTransform>().Teleport(position, rotation, transform.localScale);
        }
    }
}
