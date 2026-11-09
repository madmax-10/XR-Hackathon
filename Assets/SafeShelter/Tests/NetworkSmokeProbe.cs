#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SafeShelter.Tests
{
    /// <summary>Opt-in two-process integration test. Inert unless --safeshelter-smoke is supplied.</summary>
    public sealed class NetworkSmokeProbe : MonoBehaviour
    {
        string folder;
        bool host;
        SessionController session;
        FurnitureLayout layout;
        int initialCount;
        Vector3 initialPosition;
        Quaternion initialRotation;
        ConstraintManager constraints;
        ZoneDebugView zoneView;
        bool desktopTest;
        MaterialPropertyBlock zoneProperties;
        static readonly Vector3 HostZoneA = new Vector3(1.5f, 3, .5f), HostZoneB = new Vector3(2.5f, -1, 1.5f);
        static readonly Vector3 ClientZoneA = new Vector3(-.8f, 0, 2.2f), ClientZoneB = new Vector3(-1.2f, 0, 1.8f);
        static readonly Vector3 AddedBedPosition = new Vector3(-2, .225f, -1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("--safeshelter-crowd")) return;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--safeshelter-smoke")
                {
                    var probe = new GameObject("NetworkingIntegrationProbe").AddComponent<NetworkSmokeProbe>();
                    DontDestroyOnLoad(probe.gameObject);
                    probe.folder = args[i + 1];
                    probe.host = args.Contains("host");
                }
        }

        IEnumerator Start()
        {
            Application.logMessageReceived += CaptureError;
            session = FindFirstObjectByType<SessionController>();
            layout = FindFirstObjectByType<FurnitureLayout>();
            zoneProperties = new MaterialPropertyBlock();
            if (!PlatformRigSelector.IsQuest &&
                (FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled) != 1 ||
                 FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled) != 1))
                throw new InvalidOperationException("Desktop requires one active camera and AudioListener.");
            var initialBeds = FindObjectsByType<NetworkFurniture>(FindObjectsSortMode.None);
            initialCount = initialBeds.Length;
            var initialBed = initialBeds.OrderBy(b => b.name, StringComparer.Ordinal).First();
            initialPosition = initialBed.transform.position;
            initialRotation = initialBed.transform.rotation;
            constraints = FindFirstObjectByType<ConstraintManager>();
            zoneView = FindFirstObjectByType<ZoneDebugView>();
            desktopTest = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesktopNetworkTest";
            if (desktopTest && (constraints == null || zoneView == null))
                throw new InvalidOperationException("Network test scene requires a ConstraintManager and ZoneDebugView.");
            if (constraints == null) Debug.Log("SafeShelter integration: zone checks skipped; no ConstraintManager in this scene.");
            yield return Await(() => session != null && (host ? session.CanEditFurniture :
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient), "connection");
            if (host) yield return HostTest();
            else yield return ClientTest();
            File.WriteAllText(Path.Combine(folder, host ? "host-pass" : "client-pass"), "PASS");
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(0);
        }

        NetworkFurniture[] Beds() => FindObjectsByType<NetworkFurniture>(FindObjectsSortMode.None)
            .Where(bed => bed.IsSpawned).ToArray();
        NetworkFurniture Baseline() => Beds().Where(bed => bed.NetworkObject.IsSceneObject == true).OrderBy(b => b.name, StringComparer.Ordinal).First();
        bool Has(string marker) => File.Exists(Path.Combine(folder, marker));
        void Mark(string marker) => File.WriteAllText(Path.Combine(folder, marker), "observed");
        static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .04f;
        // Corners are normalized to min/max X/Z and projected onto the floor (Y=0 in the test scene).
        bool ZoneIs(Vector3 a, Vector3 b, bool conflict)
        {
            if (constraints == null) return true; // Integration scene does not yet include zone marking.
            var zone = constraints.CurrentZone;
            return zone.IsActive && zone.HasConflict == conflict && constraints.GetConflict() == conflict &&
                Near(zone.CornerA, new Vector3(Mathf.Min(a.x, b.x), 0, Mathf.Min(a.z, b.z))) &&
                Near(zone.CornerB, new Vector3(Mathf.Max(a.x, b.x), 0, Mathf.Max(a.z, b.z))) &&
                ZoneDisplayIs(conflict);
        }

        bool ZoneIsInactive() => constraints == null ||
            (!constraints.CurrentZone.IsActive && !constraints.GetConflict() && ZoneDisplayIs(null));

        bool ZoneDisplayIs(bool? blocked)
        {
            if (zoneView == null) return !desktopTest;
            var slab = zoneView.transform.Find("KeepClearZone");
            if (slab == null || slab.GetComponent<Collider>() != null || slab.gameObject.activeSelf != blocked.HasValue)
                return false;
            if (!blocked.HasValue) return true;
            slab.GetComponent<Renderer>().GetPropertyBlock(zoneProperties);
            var color = zoneProperties.GetColor("_BaseColor");
            return blocked.Value ? color.r > color.g && color.r > color.b : color.g > color.r && color.g > color.b;
        }

        IEnumerator HostTest()
        {
            yield return Await(() => Beds().Length == initialCount, "initial scene bed spawned");
            var baseline = Baseline().GetComponent<SharedFurniture>();
            // Change state before the other process joins, exercising late-join synchronization.
            baseline.MoveTo(new Vector3(2, initialPosition.y, 1));
            baseline.Rotate90();
            var added = layout.AddBed(AddedBedPosition);
            if (added == null) throw new InvalidOperationException("Host failed to spawn an added bed.");
            if (constraints != null) constraints.RequestZone(HostZoneA, HostZoneB);
            yield return Await(() => ZoneIs(HostZoneA, HostZoneB, true), "host zone under moved bed is blocked");
            if (constraints != null) constraints.RequestZone(new Vector3(100, 0, 0), Vector3.zero);
            if (constraints != null) constraints.RequestZone(Vector3.zero, new Vector3(.05f, 0, 1));
            yield return null;
            if (!ZoneIs(HostZoneA, HostZoneB, true)) throw new InvalidOperationException("Invalid zone replaced state.");
            Mark("host-ready");
            yield return Await(() => Has("client-ready"), "client sees late-join state and rejects local edits");
            baseline.MoveTo(new Vector3(-1, initialPosition.y, 2));
            baseline.Rotate90();
            yield return Await(() => ZoneIs(HostZoneA, HostZoneB, false), "moving the bed clears the zone");
            Mark("host-moved");
            yield return Await(() => Has("client-moved"), "client receives live movement/rotation");
            yield return Await(() => ZoneIs(ClientZoneA, ClientZoneB, true), "host accepts client zone request");
            layout.Remove(added);
            yield return Await(() => Has("client-despawned"), "client receives runtime despawn");
            var clearPosition = new Vector3(-1, initialPosition.y, -2);
            baseline.MoveTo(clearPosition);
            yield return Await(() => ZoneIs(ClientZoneA, ClientZoneB, false), "host moves bed out of the client-marked zone");
            yield return Await(() => Has("client-saw-clear-move"), "client sees bed move and zone turn green");
            baseline.MoveTo(new Vector3(-1, initialPosition.y, 2));
            yield return Await(() => ZoneIs(ClientZoneA, ClientZoneB, true), "moving back blocks the zone again");
            layout.Remove(baseline);
            yield return Await(() => ZoneIs(ClientZoneA, ClientZoneB, false), "removed bed no longer blocks");
            yield return Await(() => Has("client-hidden"), "client receives scene-bed removal");
            yield return Await(ZoneIsInactive, "client clears the zone on the host");
            if (layout.AddBed(AddedBedPosition) == null) throw new InvalidOperationException("Could not add a bed for reset.");
            Mark("host-added-before-reset");
            yield return Await(() => Has("client-saw-bed-before-reset"), "client sees the bed that reset will despawn");
            layout.ResetLayout();
            yield return Await(() => Beds().Length == initialCount && Baseline().IsPresent &&
                Near(baseline.transform.position, initialPosition) && ZoneIsInactive(), "host reset restores baseline and despawns added bed");
            yield return Await(() => Has("client-reset"), "client receives baseline reset");
        }

        IEnumerator ClientTest()
        {
            yield return Await(() => Beds().Length == initialCount + 1 &&
                Near(Baseline().transform.position, new Vector3(2, initialPosition.y, 1)) &&
                Quaternion.Angle(Baseline().transform.rotation, Quaternion.Euler(0, 90, 0) * initialRotation) < 1 &&
                Beds().Any(bed => Near(bed.transform.position, AddedBedPosition)) &&
                ZoneIs(HostZoneA, HostZoneB, true), "late-join snapshot including blocked zone");
            var baseline = Baseline().GetComponent<SharedFurniture>();
            if (layout.CanEdit || baseline.CanEdit) throw new InvalidOperationException("Client has write authority.");
            var before = baseline.transform.position;
            var beforeRotation = baseline.transform.rotation;
            baseline.MoveTo(new Vector3(50, 0, 50));
            baseline.Rotate90();
            baseline.ResetPose();
            layout.Remove(baseline);
            layout.ResetLayout();
            if (layout.AddBed(Vector3.zero) != null || !Near(baseline.transform.position, before) ||
                Quaternion.Angle(baseline.transform.rotation, beforeRotation) > .1f || !Baseline().IsPresent)
                throw new InvalidOperationException("Client mutation was not rejected.");
            Mark("client-ready");
            yield return Await(() => Has("host-moved") && Near(baseline.transform.position, new Vector3(-1, initialPosition.y, 2)) &&
                Quaternion.Angle(baseline.transform.rotation, Quaternion.Euler(0, 180, 0) * initialRotation) < 1 &&
                ZoneIs(HostZoneA, HostZoneB, false), "live transform and green zone");
            QuestKeepClearMarker marker = null;
            if (desktopTest)
            {
                var simulatedController = new GameObject("SmokeTestRay");
                var ray = simulatedController.AddComponent<XRRayInteractor>();
                ray.enableUIInteraction = false;
                ray.raycastMask = 1 << 8;
                marker = simulatedController.AddComponent<QuestKeepClearMarker>();
                SetMarkerReference(marker, "constraints", constraints);
                SetMarkerReference(marker, "leftRay", ray);

                yield return PressOnFloor(ray, ray.selectInput, ClientZoneA);
                yield return Await(() =>
                {
                    var preview = GameObject.Find("KeepClearCornerPreview");
                    return preview != null && preview.GetComponent<Collider>() == null &&
                        Near(preview.transform.position, ClientZoneA + Vector3.up * .02f) &&
                        ZoneIs(HostZoneA, HostZoneB, false);
                }, "first XR selection shows a local-only preview");
                yield return PressOnFloor(ray, ray.activateInput, ClientZoneB);
                yield return Await(() => GameObject.Find("KeepClearCornerPreview") == null, "XR trigger submits the second corner and hides preview");
            }
            else if (constraints != null) constraints.RequestZone(ClientZoneA, ClientZoneB);
            yield return Await(() => ZoneIs(ClientZoneA, ClientZoneB, true), "client zone request replicated as blocked");
            Mark("client-moved");
            yield return Await(() => Beds().Length == initialCount, "runtime despawn");
            Mark("client-despawned");
            yield return Await(() => Near(baseline.transform.position, new Vector3(-1, initialPosition.y, -2)) &&
                ZoneIs(ClientZoneA, ClientZoneB, false), "client sees the marked zone turn green after the bed moves");
            Mark("client-saw-clear-move");
            yield return Await(() => !Baseline().IsPresent &&
                baseline.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
                baseline.GetComponentsInChildren<Collider>().All(c => !c.enabled) &&
                ZoneIs(ClientZoneA, ClientZoneB, false), "baseline hidden on client; zone green");
            Mark("client-hidden");
            if (marker != null)
            {
                marker.ClearZone();
                Destroy(marker.gameObject);
            }
            else if (constraints != null) constraints.RequestClearZone();
            yield return Await(() => Has("host-added-before-reset") && Beds().Length == initialCount + 1 &&
                ZoneIsInactive(), "client sees the clear and the bed that reset will despawn");
            Mark("client-saw-bed-before-reset");
            yield return Await(() => Beds().Length == initialCount && Baseline().IsPresent && Near(baseline.transform.position, initialPosition) &&
                Quaternion.Angle(baseline.transform.rotation, initialRotation) < 1 &&
                baseline.GetComponentsInChildren<Renderer>().All(r => r.enabled) &&
                baseline.GetComponentsInChildren<Collider>().All(c => c.enabled) &&
                ZoneIsInactive(), "reset layout and cleared zone");
            Mark("client-reset");
            yield return Await(() => NetworkManager.Singleton != null &&
                !NetworkManager.Singleton.IsConnectedClient, "host disconnect");
            if (layout.CanEdit || baseline.CanEdit) throw new InvalidOperationException("Disconnect granted write authority.");
            var oldSession = session;
            session.ReloadSession();
            yield return Await(() => FindFirstObjectByType<SessionController>() != null &&
                FindFirstObjectByType<SessionController>() != oldSession &&
                FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Length == 1, "clean session reload");
            if (FindFirstObjectByType<FurnitureLayout>().CanEdit)
                throw new InvalidOperationException("Reload granted write authority before hosting.");
            Mark("client-reloaded");
        }

        static void SetMarkerReference(QuestKeepClearMarker marker, string field, UnityEngine.Object value) =>
            typeof(QuestKeepClearMarker).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(marker, value);

        IEnumerator PressOnFloor(XRRayInteractor ray, XRInputButtonReader input, Vector3 corner)
        {
            ray.transform.SetPositionAndRotation(corner + Vector3.up, Quaternion.Euler(90, 0, 0));
            yield return Await(() => ray.TryGetCurrent3DRaycastHit(out var hit) && Near(hit.point, corner), "simulated XR ray hits floor corner");
            input.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            input.manualPerformed = true;
            input.manualFramePerformed = Time.frameCount;
            yield return null;
            input.manualPerformed = false;
        }

        IEnumerator Await(Func<bool> condition, string description)
        {
            float deadline = Time.realtimeSinceStartup + 60;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(description);
                yield return null;
            }
            Debug.Log("SafeShelter integration: " + description);
        }

        void CaptureError(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            File.WriteAllText(Path.Combine(folder, host ? "host-fail" : "client-fail"), message + "\n" + stack);
            Application.Quit(2);
        }

        void OnDestroy() { Application.logMessageReceived -= CaptureError; }
    }
}
#endif
