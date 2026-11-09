using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SafeShelter
{
    [RequireComponent(typeof(NetworkObject), typeof(Unity.Netcode.Components.NetworkTransform), typeof(NavMeshAgent))]
    public sealed class NetworkOccupant : NetworkBehaviour
    {
        readonly NetworkVariable<NetworkObjectReference> directorReference = new NetworkVariable<NetworkObjectReference>();
        readonly NetworkVariable<int> look = new NetworkVariable<int>();
        readonly NetworkVariable<float> speed = new NetworkVariable<float>();
        NavMeshAgent agent;
        Animator animator;
        GameObject visual;
        public NavMeshAgent Agent => agent != null ? agent : agent = GetComponent<NavMeshAgent>();

        void Awake() { Agent.enabled = false; }
        public void Initialize(CrowdDirector director, int index)
        {
            directorReference.Value = director.NetworkObject;
            look.Value = index % 2;
        }
        public override void OnNetworkSpawn()
        {
            Agent.enabled = IsServer;
        }
        void Update()
        {
            if (!IsSpawned) return;
            if (visual == null && directorReference.Value.TryGet(out var obj))
            {
                var bindings = obj.GetComponent<CrowdDirector>().Bindings;
                visual = Instantiate(bindings.occupantVisuals[look.Value], transform);
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                animator = visual.GetComponent<Animator>();
                if (animator != null) animator.applyRootMotion = false;
            }
            if (IsServer && Agent.enabled)
            {
                float current = Agent.isStopped ? 0 : Agent.velocity.magnitude;
                if (Mathf.Abs(speed.Value - current) > .05f) speed.Value = current;
            }
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat("MoveSpeed", speed.Value, .12f, Time.deltaTime);
        }
        public override void OnNetworkDespawn()
        {
            Agent.enabled = false;
            if (visual != null) Destroy(visual);
        }
    }
}
