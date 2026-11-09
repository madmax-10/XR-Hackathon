#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace SafeShelter.Tests
{
    public sealed class CrowdSmokeProbe : MonoBehaviour
    {
        string folder;
        bool host;
        CrowdDirector director;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--safeshelter-smoke");
            if (!args.Contains("--safeshelter-crowd") || index < 0 || index+1 >= args.Length) return;
            var probe = new GameObject("CrowdSmokeProbe").AddComponent<CrowdSmokeProbe>();
            probe.folder = args[index+1];
            probe.host = args.Contains("host");
        }
        IEnumerator Start()
        {
            Application.logMessageReceived += Error;
            director = FindFirstObjectByType<CrowdDirector>();
            if (director == null) throw new InvalidOperationException("Crowd scene wiring is missing.");
            yield return Until(() => director.IsSpawned, "director connected");
            if (host) yield return Host();
            else yield return Client();
            Mark(host ? "host-pass" : "client-pass");
            yield return new WaitForSecondsRealtime(2);
            Application.Quit();
        }
        IEnumerator Host()
        {
            director.ApplyBlockedLayout();
            director.StartRun(CrowdProfile.MealRush);
            yield return Until(() => director.Running && director.Result.Queue > 0, "blocked crossing creates queue");
            Mark("host-ready");
            yield return Until(() => director.Result.Complete, "blocked run completes");
            var blocked = director.Result;
            if (blocked.Trips != 0 || blocked.Peak == 0) throw new InvalidOperationException("Blocked run bypassed the aisle.");
            yield return Until(() => Has("client-blocked"), "client observes blocked result");
            director.ApplyClearLayout();
            director.StartRun(CrowdProfile.MealRush);
            Mark("clear-started");
            yield return Until(() => director.Result.Complete, "clear run completes");
            var clear = director.Result;
            Debug.Log($"CROWD RESULTS blocked={blocked.Trips}, wait={blocked.MeanWait}; clear={clear.Trips}, pantry={clear.Pantry}, toilets={clear.Toilets}, wait={clear.MeanWait}");
            if (clear.Trips < 1 || clear.Pantry < 1 || clear.Toilets < 1 || clear.MeanWait >= blocked.MeanWait)
                throw new InvalidOperationException("Clear layout failed traffic comparison.");
            yield return Until(() => Has("client-clear"), "client observes clear traffic");
            director.StopRun();
            yield return Until(() => FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None).Length == 0, "stop despawns all residents");
            director.StartRun(CrowdProfile.Normal);
            yield return Until(() => director.Result.Residents == 6 && director.Running, "normal restart");
            Mark("normal-started");
            yield return Until(() => director.ViewerConnected, "viewer position reaches host");
            yield return Until(() => FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None).Any(p => p.Agent.isStopped),
                "residents yield to viewer proxy");
            yield return Until(() => Has("viewer-sent") && !director.ViewerConnected, "stale viewer proxy expires");
            director.Bindings.layout.ResetLayout();
            yield return Until(() => !director.Running && FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None).Length == 0, "layout reset stops crowd");
            Mark("cleanup");
            yield return Until(() => Has("client-cleanup"), "client sees cleanup");
        }
        IEnumerator Client()
        {
            yield return Until(() => FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None).Length == 12 &&
                director.Result.Queue > 0, "late join receives twelve queued residents");
            director.StopRun();
            director.StartRun(CrowdProfile.Normal);
            director.ApplyClearLayout();
            director.ApplyBlockedLayout();
            if (!director.Running) throw new InvalidOperationException("Client stopped host run.");
            yield return Until(() => FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None)
                .All(p => !p.Agent.enabled && p.GetComponentInChildren<Animator>() != null), "client visuals resolve without navigation agents");
            yield return Until(() => director.Result.Complete && director.Result.Peak > 0, "client blocked snapshot");
            Mark("client-blocked");
            yield return Until(() => Has("clear-started") && director.Running && director.Result.Elapsed < 15, "client clear restart");
            // Observe every resident: an arbitrary first object may still be queued or
            // waiting for its scheduled departure. Let replacement spawn messages settle.
            yield return new WaitForSecondsRealtime(1);
            var starts = FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None)
                .ToDictionary(p => p.NetworkObjectId, p => p.transform.position);
            yield return Until(() => FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None)
                .Any(p => starts.TryGetValue(p.NetworkObjectId, out var start) &&
                    Vector3.Distance(start, p.transform.position) > .5f), "client sees live movement");
            yield return Until(() => director.Result.Complete && director.Result.Trips > 0 &&
                director.Result.Pantry > 0 && director.Result.Toilets > 0, "client clear result");
            Mark("client-clear");
            yield return Until(() => Has("normal-started") && director.Running && director.Result.Residents == 6,
                "client normal restart");
            float until = Time.realtimeSinceStartup + 2;
            while (Time.realtimeSinceStartup < until)
            {
                var person = FindFirstObjectByType<NetworkOccupant>();
                if (person != null) director.ReportViewer(person.transform.position);
                yield return new WaitForSecondsRealtime(.125f);
            }
            Mark("viewer-sent");
            yield return Until(() => Has("cleanup") && !director.Running &&
                FindObjectsByType<NetworkOccupant>(FindObjectsSortMode.None).Length == 0, "client reset cleanup");
            Mark("client-cleanup");
            yield return Until(() => Has("host-pass"), "host passed");
        }
        IEnumerator Until(Func<bool> condition, string label)
        {
            float deadline = Time.realtimeSinceStartup + 120;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(label + " / " + director.Status);
                yield return null;
            }
            Debug.Log("CROWD PASS " + label);
        }
        bool Has(string name) => File.Exists(Path.Combine(folder,name));
        void Mark(string name) => File.WriteAllText(Path.Combine(folder,name),"PASS");
        void Error(string message,string trace,LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            File.WriteAllText(Path.Combine(folder,host?"host-fail":"client-fail"),message+"\n"+trace);
            Application.Quit(2);
        }
        void OnDestroy() => Application.logMessageReceived -= Error;
    }
}
#endif
