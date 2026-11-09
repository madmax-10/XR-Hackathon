using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SafeShelter
{
    public enum CrowdProfile { Normal, MealRush }

    public struct CrowdResult : INetworkSerializable, IEquatable<CrowdResult>
    {
        public int Residents, Released, Trips, Pantry, Toilets, Queue, Peak;
        public float Elapsed, WaitSeconds;
        public bool Complete;
        public float MeanWait => Released == 0 ? 0 : WaitSeconds / Released;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Residents); s.SerializeValue(ref Released); s.SerializeValue(ref Trips);
            s.SerializeValue(ref Pantry); s.SerializeValue(ref Toilets); s.SerializeValue(ref Queue);
            s.SerializeValue(ref Peak); s.SerializeValue(ref Elapsed); s.SerializeValue(ref WaitSeconds);
            s.SerializeValue(ref Complete);
        }
        public bool Equals(CrowdResult other) => Residents == other.Residents && Released == other.Released &&
            Trips == other.Trips && Pantry == other.Pantry && Toilets == other.Toilets && Queue == other.Queue &&
            Peak == other.Peak && Elapsed == other.Elapsed && WaitSeconds == other.WaitSeconds && Complete == other.Complete;
    }

    public sealed class CrowdDirector : NetworkBehaviour
    {
        [SerializeField] CrowdSceneBindings bindings;
        [SerializeField] GameObject occupantPrefab;
        readonly NetworkVariable<CrowdResult> result = new NetworkVariable<CrowdResult>();
        readonly NetworkVariable<FixedString128Bytes> status = new NetworkVariable<FixedString128Bytes>("Ready");
        readonly NetworkVariable<bool> running = new NetworkVariable<bool>();
        readonly NetworkVariable<CrowdProfile> profile = new NetworkVariable<CrowdProfile>();
        readonly List<Resident> residents = new List<Resident>();
        readonly List<Resident> pantryLine = new List<Resident>(), toiletLine = new List<Resident>();
        readonly List<Resident> transitLine = new List<Resident>();
        readonly Dictionary<SharedFurniture, Pose> baseline = new Dictionary<SharedFurniture, Pose>();
        readonly Dictionary<SharedFurniture, (Vector3 position, Quaternion rotation, bool present)> snapshot =
            new Dictionary<SharedFurniture, (Vector3, Quaternion, bool)>();
        readonly CrowdNavigation navigation = new CrowdNavigation();
        readonly CrowdResult[,] completed = new CrowdResult[2, 2];
        Vector3 viewer;
        float viewerUpdated = -10, nextPublish;
        ulong viewerClient;
        bool crossingOpen;
        int layoutPreset;
        CrowdResult progress;
        public CrowdSceneBindings Bindings => bindings;
        public CrowdResult Result => result.Value;
        public string Status => status.Value.ToString();
        public bool Running => running.Value;
        public CrowdProfile ActiveProfile => profile.Value;
        public string LayoutLabel => !CanControl ? "Waiting for host" :
            IdentifyLayout() switch { 0 => "Blocked preset", 1 => "Clear preset", _ => "Manually edited" };
        public bool ViewerConnected => IsServer && NetworkManager != null &&
            NetworkManager.ConnectedClients.ContainsKey(viewerClient) && Time.unscaledTime - viewerUpdated < .5f;
        public CrowdResult Completed(CrowdProfile selected, bool clear) => completed[(int)selected, clear ? 1 : 0];
        public bool CanControl => IsSpawned && IsServer && NetworkManager.IsListening;

        enum Stage { Rest, ApproachTransit, Transit, North, Portal, Entry, Queue, Service, ExitEntry, ExitPortal, ReturnNorth, ReturnSouth, Home }
        sealed class Resident
        {
            public NetworkOccupant person;
            public int index;
            public bool toilet;
            public Stage stage;
            public float wake;
            public bool released;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer || bindings == null || bindings.layout == null) return;
            foreach (var item in bindings.layout.Furniture) baseline[item] = new Pose(item.transform.position, item.transform.rotation);
            bindings.layout.Changed += LayoutChanged;
        }
        public override void OnNetworkDespawn()
        {
            if (bindings != null && bindings.layout != null) bindings.layout.Changed -= LayoutChanged;
            Cleanup();
        }
        void LayoutChanged() { if (Running || residents.Count > 0) { StopRun(); status.Value = "Layout changed · restart run"; } }
        public void StopRun()
        {
            if (!CanControl) return;
            running.Value = false;
            Cleanup();
            result.Value = default;
            status.Value = "Stopped";
        }
        void Cleanup()
        {
            foreach (var resident in residents)
            {
                if (resident.person == null || !IsServer) continue;
                if (resident.person.IsSpawned) resident.person.NetworkObject.Despawn(true);
                else Destroy(resident.person.gameObject);
            }
            residents.Clear(); pantryLine.Clear(); toiletLine.Clear(); transitLine.Clear();
            navigation.Dispose();
        }
        public void ApplyBlockedLayout()
        {
            if (!CanControl) return;
            StopRun();
            if (bindings.blocker.IsPresent) bindings.blocker.ResetPose();
            status.Value = bindings.blocker.IsPresent ? "Blocked layout" : "Blocker removed · Reset Layout first";
        }
        public void ApplyClearLayout()
        {
            if (!CanControl) return;
            StopRun();
            if (bindings.blocker.IsPresent)
            {
                bindings.blocker.ResetPose();
                bindings.blocker.MoveTo(bindings.clearBedPose.position);
            }
            status.Value = bindings.blocker.IsPresent ? "Clear layout" : "Blocker removed · Reset Layout first";
        }
        public void StartRun(CrowdProfile selected)
        {
            if (!CanControl) return;
            StopRun();
            try
            {
                bindings.ValidateBindings();
                if (occupantPrefab == null) throw new InvalidOperationException("Assign NetworkOccupant prefab.");
                navigation.Build(bindings);
                crossingOpen = navigation.CrossingOpen(bindings);
                profile.Value = selected;
                progress = new CrowdResult { Residents = selected == CrowdProfile.Normal ? 6 : 12 };
                layoutPreset = IdentifyLayout();
                snapshot.Clear();
                foreach (var item in bindings.layout.Furniture)
                    snapshot[item] = (item.transform.position, item.transform.rotation, item.IsPresent);
                for (int i = 0; i < progress.Residents; i++)
                {
                    var obj = Instantiate(occupantPrefab, CrowdNavigation.Point(bindings.residentStarts[i]), Quaternion.identity);
                    var person = obj.GetComponent<NetworkOccupant>();
                    residents.Add(new Resident { person = person, index = i, toilet = i % 3 == 2,
                        wake = i * (selected == CrowdProfile.Normal ? 4f : .6f) });
                    person.NetworkObject.Spawn();
                    person.Initialize(this, i);
                    person.Agent.speed = 1f + (i % 4) * .1f;
                    person.Agent.avoidancePriority = 30 + i;
                }
                result.Value = progress;
                running.Value = true;
                status.Value = layoutPreset < 0 ? "Running · edited layout" : crossingOpen ? "Running · clear crossing" : "Running · blocked crossing";
            }
            catch (Exception error)
            {
                Cleanup();
                status.Value = new FixedString128Bytes(error.Message.Substring(0, Math.Min(110, error.Message.Length)));
                Debug.LogWarning("Crowd start rejected: " + error.Message, this);
            }
        }
        int IdentifyLayout()
        {
            if (baseline.Count != bindings.layout.Furniture.Count()) return -1;
            bool clear = Vector3.Distance(bindings.blocker.transform.position, bindings.clearBedPose.position) < .02f;
            foreach (var item in bindings.layout.Furniture)
            {
                if (!baseline.TryGetValue(item, out var pose) || !item.IsPresent) return -1;
                var expected = item == bindings.blocker && clear ? bindings.clearBedPose.position : pose.position;
                if (Vector3.Distance(item.transform.position, expected) > .02f ||
                    Quaternion.Angle(item.transform.rotation, pose.rotation) > .1f) return -1;
            }
            return clear ? 1 : 0;
        }
        bool LayoutMatches()
        {
            int count = 0;
            foreach (var item in bindings.layout.Furniture)
            {
                count++;
                if (!snapshot.TryGetValue(item, out var state) || state.present != item.IsPresent ||
                    Vector3.Distance(state.position, item.transform.position) > .001f ||
                    Quaternion.Angle(state.rotation, item.transform.rotation) > .01f) return false;
            }
            return count == snapshot.Count;
        }
        void Update()
        {
            if (!CanControl) return;
            if (residents.Count > 0 && !LayoutMatches()) { LayoutChanged(); return; }
            if (!Running) return;
            float dt = Mathf.Min(Time.deltaTime, 90f - progress.Elapsed);
            progress.Elapsed += dt;
            progress.Queue = 0;
            foreach (var resident in residents)
            {
                var agent = resident.person.Agent;
                bool yielding = ViewerConnected && Vector3.Distance(viewer, agent.transform.position) < .8f;
                agent.isStopped = yielding;
                try { Advance(resident); }
                catch (Exception error)
                {
                    StopRun();
                    status.Value = "Route failed · check navigation anchors";
                    Debug.LogWarning(error.Message, this);
                    return;
                }
                // Count stopped time at the aisle in either layout, including residents
                // still waiting at cutoff. Travel and destination-service time are excluded.
                bool atBlockedQueue = resident.stage == Stage.Transit && !crossingOpen && Arrived(resident);
                var position = agent.transform.position;
                bool atCrossing = crossingOpen && Mathf.Abs(position.x) < 1.15f &&
                    position.z >= bindings.aisleGateSouth.position.z - .6f &&
                    position.z <= bindings.aisleGateNorth.position.z + .6f &&
                    (resident.stage == Stage.Transit || resident.stage == Stage.North ||
                     resident.stage == Stage.ReturnNorth || resident.stage == Stage.ReturnSouth);
                if (atBlockedQueue || (atCrossing && agent.velocity.sqrMagnitude < .0225f))
                {
                    progress.Queue++;
                    progress.WaitSeconds += dt;
                }
            }
            progress.Peak = Mathf.Max(progress.Peak, progress.Queue);
            if (progress.Elapsed >= 90)
            {
                progress.Complete = true;
                if (layoutPreset >= 0) completed[(int)profile.Value, layoutPreset] = progress;
                running.Value = false;
                foreach (var resident in residents) resident.person.Agent.isStopped = true;
                status.Value = "Run complete";
            }
            if (!Running || Time.unscaledTime >= nextPublish)
            {
                result.Value = progress;
                nextPublish = Time.unscaledTime + .2f;
            }
        }
        bool Arrived(Resident resident) => !resident.person.Agent.pathPending &&
            Vector3.Distance(resident.person.transform.position, resident.person.Agent.destination) < .2f;
        void Go(Resident resident, Transform target, Stage next)
        {
            resident.stage = next;
            if (!resident.person.Agent.SetDestination(CrowdNavigation.Point(target)))
                throw new InvalidOperationException("Cannot set route to " + target.name);
        }
        void Advance(Resident r)
        {
            var line = r.toilet ? toiletLine : pantryLine;
            var slots = r.toilet ? bindings.toiletQueue : bindings.pantryQueue;
            var portal = r.toilet ? bindings.toiletPortal : bindings.pantryPortal;
            switch (r.stage)
            {
                case Stage.Rest:
                    if (progress.Elapsed < r.wake) return;
                    if (!r.released) { progress.Released++; r.released = true; }
                    Go(r, bindings.aisleGateSouth, crossingOpen ? Stage.Transit : Stage.ApproachTransit);
                    break;
                case Stage.ApproachTransit:
                    // Reserve the next slot when the resident reaches the aisle entrance,
                    // so faster walkers cannot inherit a later resident's occupied place.
                    var position = r.person.transform.position;
                    float entrance = bindings.transitQueue.Min(t => t.position.z) - .5f;
                    if (Mathf.Abs(position.x) < 1.05f && position.z >= entrance)
                    {
                        transitLine.Add(r);
                        Go(r, bindings.transitQueue[transitLine.Count - 1], Stage.Transit);
                    }
                    break;
                case Stage.Transit:
                    if (crossingOpen && Arrived(r)) Go(r, bindings.aisleGateNorth, Stage.North);
                    break;
                case Stage.North:
                    if (Arrived(r)) Go(r, portal, Stage.Portal);
                    break;
                case Stage.Portal:
                    if (!Arrived(r)) break;
                    if (r.toilet) Go(r, bindings.toiletEntry, Stage.Entry);
                    else JoinLine(r, line, slots);
                    break;
                case Stage.Entry:
                    if (Arrived(r)) JoinLine(r, line, slots);
                    break;
                case Stage.Queue:
                    int index = line.IndexOf(r);
                    if (index == 0 && Arrived(r))
                    {
                        Go(r, r.toilet ? bindings.toiletService : bindings.pantryService, Stage.Service);
                        r.wake = -1;
                    }
                    else if (index > 0 && (r.person.Agent.destination - slots[index].position).sqrMagnitude > .05f)
                        r.person.Agent.SetDestination(CrowdNavigation.Point(slots[index]));
                    break;
                case Stage.Service:
                    if (!Arrived(r)) break;
                    if (r.wake < 0)
                    {
                        r.wake = progress.Elapsed + (r.toilet ? 3 : 2);
                        if (r.toilet) progress.Toilets++; else progress.Pantry++;
                    }
                    if (progress.Elapsed < r.wake) break;
                    line.Remove(r);
                    Go(r, r.toilet ? bindings.toiletEntry : portal, r.toilet ? Stage.ExitEntry : Stage.ExitPortal);
                    break;
                case Stage.ExitEntry:
                    if (Arrived(r)) Go(r, portal, Stage.ExitPortal);
                    break;
                case Stage.ExitPortal:
                    if (Arrived(r)) Go(r, bindings.aisleGateNorth, Stage.ReturnNorth);
                    break;
                case Stage.ReturnNorth:
                    if (Arrived(r)) Go(r, bindings.aisleGateSouth, Stage.ReturnSouth);
                    break;
                case Stage.ReturnSouth:
                    if (Arrived(r)) Go(r, bindings.residentStarts[r.index], Stage.Home);
                    break;
                case Stage.Home:
                    if (!Arrived(r)) break;
                    progress.Trips++;
                    r.stage = Stage.Rest;
                    r.wake = progress.Elapsed + 3;
                    break;
            }
        }
        static void JoinLine(Resident r, List<Resident> line, Transform[] slots)
        {
            line.Add(r);
            r.stage = Stage.Queue;
            r.person.Agent.SetDestination(CrowdNavigation.Point(slots[line.Count - 1]));
        }
        public void ReportViewer(Vector3 position)
        {
            if (IsSpawned && !IsServer) ViewerRpc(position);
        }
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void ViewerRpc(Vector3 position, RpcParams rpc = default)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.z) || bindings == null) return;
            if (!NetworkManager.ConnectedClients.ContainsKey(rpc.Receive.SenderClientId)) return;
            if (ViewerConnected && viewerClient != rpc.Receive.SenderClientId) return;
            if (Time.unscaledTime - viewerUpdated < .1f) return;
            var floor = bindings.hallFloor.bounds;
            if (position.x < floor.min.x || position.x > floor.max.x || position.z < floor.min.z || position.z > floor.max.z) return;
            viewer = new Vector3(position.x, floor.max.y, position.z);
            viewerUpdated = Time.unscaledTime;
            viewerClient = rpc.Receive.SenderClientId;
        }
    }
}
