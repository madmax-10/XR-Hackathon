using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeShelter
{
    /// <summary>References the scene's one NetworkManager. Does not create another manager.</summary>
    [DisallowMultipleComponent]
    public sealed class SessionController : MonoBehaviour
    {
        [SerializeField] NetworkManager networkManager;
        [SerializeField] UnityTransport transport;
        [SerializeField] string address = "127.0.0.1";
        [SerializeField] string port = "7777";
        [SerializeField] bool autoStartHost;
        float connectDeadline;
        public string Address { get => address; set { if (!attempted) address = value; } }
        public string Port { get => port; set { if (!attempted) port = value; } }
        public bool Attempted => attempted;
        public bool ConnectedClient => networkManager != null && networkManager.IsConnectedClient;
        [Serializable] sealed class LanSettings { public string address; public string port = "7777"; }
        bool attempted;
        bool reloading;
        public bool CanEditFurniture => networkManager != null && networkManager.IsHost &&
            networkManager.IsListening && !networkManager.ShutdownInProgress;
        public string Status { get; private set; } = "Choose Start Host or Join Client.";
        public Rect Panel => new Rect(12, 12, 390, 185);

        void Awake()
        {
            Application.runInBackground = true;
            if (networkManager == null || transport == null ||
                networkManager.NetworkConfig.NetworkTransport != transport)
            {
                Debug.LogError("Assign the scene's NetworkManager and its UnityTransport to SessionController.", this);
                enabled = false;
                return;
            }
            networkManager.OnClientConnectedCallback += Connected;
            networkManager.OnClientDisconnectCallback += Disconnected;
            networkManager.OnTransportFailure += TransportFailed;
        }

        void Start()
        {
            address = PlayerPrefs.GetString("SafeShelter.HostIPv4", address);
            port = PlayerPrefs.GetString("SafeShelter.Port", port);
            if (PlatformRigSelector.IsQuest)
            {
                string config = Path.Combine(Application.persistentDataPath, "safeshelter-lan.json");
                try
                {
                    if (File.Exists(config))
                    {
                        var settings = JsonUtility.FromJson<LanSettings>(File.ReadAllText(config));
                        if (settings != null && !string.IsNullOrWhiteSpace(settings.address))
                        {
                            address = settings.address;
                            port = settings.port;
                        }
                    }
                }
                catch (Exception error) { Debug.LogWarning("Cannot read LAN settings: " + error.Message); }
            }
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "--safeshelter-address") address = args[i + 1];
                if (args[i] == "--safeshelter-port") port = args[i + 1];
            }
            bool explicitRole = Array.IndexOf(args, "--safeshelter-role") >= 0;
            if (autoStartHost && !PlatformRigSelector.IsQuest && !explicitRole) StartHost();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--safeshelter-role")
                {
                    if (args[i + 1] == "host") StartHost();
                    if (args[i + 1] == "client") JoinClient();
                }
        }

        bool Prepare(bool hosting)
        {
            if (!enabled || attempted || networkManager.IsListening || networkManager.ShutdownInProgress) return false;
            if (!ushort.TryParse(port, out var parsedPort) || parsedPort == 0 ||
                !IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                Status = "Enter an IPv4 address and a port from 1 to 65535.";
                return false;
            }
            if (PlatformRigSelector.IsQuest && hosting)
            {
                Status = "Quest joins as a client; start the host on Windows.";
                return false;
            }
            if (!hosting && PlatformRigSelector.IsQuest && IPAddress.IsLoopback(ip))
            {
                Status = "Enter the Windows LAN IPv4 address, not loopback.";
                return false;
            }
            // Command-line test launches must not overwrite the user's LAN settings.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--safeshelter-role") < 0)
            {
                PlayerPrefs.SetString("SafeShelter.HostIPv4", address);
                PlayerPrefs.SetString("SafeShelter.Port", port);
                PlayerPrefs.Save();
            }
            // Host listens on all LAN interfaces; same-machine clients use loopback.
            transport.SetConnectionData(address, parsedPort, hosting ? "0.0.0.0" : null);
            attempted = true;
            return true;
        }

        public void StartHost()
        {
            if (!Prepare(true)) return;
            Status = "Starting host...";
            if (!networkManager.StartHost()) Status = "Host failed to start. Check Console and port availability.";
            else Status = "Host ready. Furniture editing enabled.";
            Debug.Log(Status);
        }

        public void JoinClient()
        {
            if (!Prepare(false)) return;
            Status = "Connecting to " + address + ":" + port + "...";
            connectDeadline = Time.realtimeSinceStartup + 30f;
            if (!networkManager.StartClient()) Status = "Client failed to start. Check Console.";
            Debug.Log(Status);
        }

        void Update()
        {
            if (connectDeadline > 0 && !networkManager.IsConnectedClient && Time.realtimeSinceStartup > connectDeadline)
            {
                connectDeadline = 0;
                networkManager.Shutdown();
                Status = "Connection timed out. Check LAN/IP; Reload Session to retry.";
                Debug.LogWarning(Status);
            }
        }

        void Connected(ulong clientId)
        {
            connectDeadline = 0;
            if (networkManager.IsHost) Status = "Host ready. Connected peers: " + (networkManager.ConnectedClientsIds.Count - 1);
            else if (clientId == networkManager.LocalClientId) Status = "Connected as client. Furniture is read-only.";
        }

        void Disconnected(ulong clientId)
        {
            if (networkManager.IsHost && !networkManager.ShutdownInProgress)
                Status = "Host ready. A client disconnected.";
            else Status = "Disconnected. Check host/IP/port, then Reload Session to retry.";
        }

        void TransportFailed() { Status = "Transport failed. Check network/port, then Reload Session."; }

        public void ReloadSession()
        {
            if (reloading || networkManager.ShutdownInProgress) return;
            reloading = true;
            if (networkManager.IsListening) networkManager.Shutdown();
            // Shutdown completes across frames; do not rebuild the session during teardown.
            // This component lives on NetworkManager. Its destruction must not stop the reload.
            var runner = new GameObject("Session Reload").AddComponent<SessionReloadRunner>();
            DontDestroyOnLoad(runner.gameObject);
            runner.Begin(ReloadWhenStopped());
        }

        System.Collections.IEnumerator ReloadWhenStopped()
        {
            while (networkManager != null && (networkManager.IsListening || networkManager.ShutdownInProgress))
                yield return null;
            if (networkManager != null) Destroy(networkManager.gameObject);
            yield return null;
            string scenePath = SceneManager.GetActiveScene().path;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scenePath);
#endif
        }

        void OnDestroy()
        {
            if (networkManager == null) return;
            networkManager.OnClientConnectedCallback -= Connected;
            networkManager.OnClientDisconnectCallback -= Disconnected;
            networkManager.OnTransportFailure -= TransportFailed;
        }

        void OnGUI()
        {
            if (PlatformRigSelector.IsQuest) return;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label("SafeShelter - Network Session");
            GUILayout.Label(Status);
            GUI.enabled = !attempted;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Host IPv4", GUILayout.Width(75));
            address = GUILayout.TextField(address, 45);
            GUILayout.Label("Port", GUILayout.Width(30));
            port = GUILayout.TextField(port, 5, GUILayout.Width(55));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Host")) StartHost();
            if (GUILayout.Button("Join Client")) JoinClient();
            GUILayout.EndHorizontal();
            GUI.enabled = attempted && !reloading && !networkManager.ShutdownInProgress;
            if (GUILayout.Button("Reload Session")) ReloadSession();
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
    // Runtime-only coroutine owner; destroyed after the replacement scene has loaded.
    sealed class SessionReloadRunner : MonoBehaviour
    {
        public void Begin(System.Collections.IEnumerator reload) { StartCoroutine(Run(reload)); }
        System.Collections.IEnumerator Run(System.Collections.IEnumerator reload)
        {
            yield return reload;
            Destroy(gameObject);
        }
    }
}