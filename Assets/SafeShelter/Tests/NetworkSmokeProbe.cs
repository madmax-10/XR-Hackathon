#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SafeShelter.Tests
{
    /// <summary>Opt-in two-process integration test. Inert unless --safeshelter-smoke is supplied.</summary>
    public sealed class NetworkSmokeProbe : MonoBehaviour
    {
        string folder;
        bool host;
        SessionController session;
        FurnitureLayout layout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
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
        NetworkFurniture Baseline() => Beds().Single(bed => bed.NetworkObject.IsSceneObject == true);
        bool Has(string marker) => File.Exists(Path.Combine(folder, marker));
        void Mark(string marker) => File.WriteAllText(Path.Combine(folder, marker), "observed");
        static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .04f;

        IEnumerator HostTest()
        {
            yield return Await(() => Beds().Length == 1, "initial scene bed spawned");
            var baseline = Baseline().GetComponent<SharedFurniture>();
            // Change state before the other process joins, exercising late-join synchronization.
            baseline.MoveTo(new Vector3(2, 0, 1));
            baseline.Rotate90();
            var added = layout.AddBed(new Vector3(-2, 0, -1));
            if (added == null) throw new InvalidOperationException("Host failed to spawn an added bed.");
            Mark("host-ready");
            yield return Await(() => Has("client-ready"), "client sees late-join state and rejects local edits");
            baseline.MoveTo(new Vector3(-1, 0, 2));
            baseline.Rotate90();
            Mark("host-moved");
            yield return Await(() => Has("client-moved"), "client receives live movement/rotation");
            layout.Remove(added);
            yield return Await(() => Has("client-despawned"), "client receives runtime despawn");
            layout.Remove(baseline);
            yield return Await(() => Has("client-hidden"), "client receives scene-bed removal");
            layout.ResetLayout();
            yield return Await(() => Has("client-reset"), "client receives baseline reset");
        }

        IEnumerator ClientTest()
        {
            yield return Await(() => Beds().Length == 2 &&
                Near(Baseline().transform.position, new Vector3(2, 0, 1)) &&
                Quaternion.Angle(Baseline().transform.rotation, Quaternion.Euler(0, 90, 0)) < 1 &&
                Beds().Any(bed => Near(bed.transform.position, new Vector3(-2, 0, -1))), "late-join snapshot");
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
            yield return Await(() => Has("host-moved") && Near(baseline.transform.position, new Vector3(-1, 0, 2)) &&
                Quaternion.Angle(baseline.transform.rotation, Quaternion.Euler(0, 180, 0)) < 1, "live transform");
            Mark("client-moved");
            yield return Await(() => Beds().Length == 1, "runtime despawn");
            Mark("client-despawned");
            yield return Await(() => !Baseline().IsPresent &&
                baseline.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
                baseline.GetComponentsInChildren<Collider>().All(c => !c.enabled), "baseline hidden on client");
            Mark("client-hidden");
            yield return Await(() => Baseline().IsPresent && Near(baseline.transform.position, Vector3.zero) &&
                Quaternion.Angle(baseline.transform.rotation, Quaternion.identity) < 1 &&
                baseline.GetComponentsInChildren<Renderer>().All(r => r.enabled) &&
                baseline.GetComponentsInChildren<Collider>().All(c => c.enabled), "reset to original layout");
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
