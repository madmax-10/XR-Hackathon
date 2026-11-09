using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
namespace SafeShelter
{
    public sealed class CrowdDesktopControls : MonoBehaviour
    {
        [SerializeField] CrowdDirector director;
        CrowdProfile selected;
        Vector2 scroll;
        string addresses;
        static CrowdDesktopControls active;
        public static bool Visible => active != null && active.isActiveAndEnabled && active.director != null;
        public static Rect Panel => new Rect(Mathf.Max(410, Screen.width - 332), 12, 320, Mathf.Max(200, Screen.height - 24));
        public static bool PointerOverPanel => Visible && Mouse.current != null && Panel.Contains(
            new Vector2(Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y));
        void OnEnable() { active = this; }
        void OnDisable() { if (active == this) active = null; }
        void Awake()
        {
            try
            {
            addresses = string.Join("\n", NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => n.Name + " · " + a.Address)));
            }
            catch (System.Exception) { addresses = "LAN addresses unavailable · check Windows network settings"; }
        }
        void OnGUI()
        {
            if (director == null || PlatformRigSelector.IsQuest) return;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("SafeShelter · People and traffic");
            GUILayout.Label(addresses);
            var session = FindFirstObjectByType<SessionController>();
            if (session != null) GUILayout.Label("LAN port " + session.Port + " · " + session.Status);
            GUILayout.Label(director.ViewerConnected ? "Quest tracking connected" : "Waiting for Quest tracking");
            GUILayout.Label(director.Status);
            GUILayout.Label("Layout · " + director.LayoutLabel);
            if (!director.CanControl) GUILayout.Label("Crowd controls require a connected Windows host.");
            GUILayout.Label("Active profile · " + director.ActiveProfile);
            selected = (CrowdProfile)GUILayout.SelectionGrid((int)selected, new[] { "Normal activity", "Meal rush" }, 1);
            GUI.enabled = director.CanControl;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Blocked layout")) director.ApplyBlockedLayout();
            if (GUILayout.Button("Clear layout")) director.ApplyClearLayout();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Start / Restart run")) director.StartRun(selected);
            if (GUILayout.Button("Stop")) director.StopRun();
            GUI.enabled = true;
            Draw("Live run", director.Result);
            Draw("Blocked result", director.Completed(selected, false));
            Draw("Clear result", director.Completed(selected, true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        static void Draw(string title, CrowdResult value)
        {
            GUILayout.Space(8);
            GUILayout.Label(title);
            if (value.Residents == 0) { GUILayout.Label("Pending"); return; }
            GUILayout.Label(value.Complete ? "Completed simulation" : "In progress · simulated traffic");
            GUILayout.Label($"{value.Elapsed:0.0} / 90 s · {value.Residents} residents");
            GUILayout.Label($"Round trips {value.Trips} · pantry {value.Pantry} · toilets {value.Toilets}");
            GUILayout.Label($"Aisle queue {value.Queue} · peak {value.Peak}");
            GUILayout.Label($"Mean aisle wait {value.MeanWait:0.0} s");
        }
    }
}
