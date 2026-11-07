using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
namespace SafeShelter
{
    public sealed class QuestSessionPanel : MonoBehaviour
    {
        [SerializeField] SessionController session;
        [SerializeField] Camera xrCamera;
        GameObject panel;
        Text status;
        Text address;
        bool editingPort;
        void Start()
        {
            if (!PlatformRigSelector.IsQuest) { enabled = false; return; }
            panel = new GameObject("LAN Session", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            panel.transform.SetParent(xrCamera.transform, false);
            panel.transform.localPosition = new Vector3(0, -.08f, 1.2f);
            panel.transform.localScale = Vector3.one * .0018f;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 550);
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;
            status = Label("Status", new Vector2(0, 215), new Vector2(540, 105));
            address = Label("Address", new Vector2(0, 135), new Vector2(540, 45));
            Button("Edit IP / Port", 0, 80, () => editingPort = !editingPort);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "Back" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                Button(key, (i % 3 - 1) * 175, 20 - i / 3 * 55, () =>
                {
                    string value = editingPort ? session.Port : session.Address;
                    value = key == "Back" ? (value.Length > 0 ? value.Substring(0, value.Length - 1) : "") : value + key;
                    if (editingPort) session.Port = value; else session.Address = value;
                });
            }
            Button("Connect", -140, -215, session.JoinClient);
            Button("Reload", 140, -215, session.ReloadSession);
        }
        void Update()
        {
            if (panel == null) return;
            panel.SetActive(!session.ConnectedClient);
            status.text = "SafeShelter LAN Client\n" + session.Status;
            address.text = (editingPort ? "Port: " + session.Port : "Host IPv4: " + session.Address);
        }
        Text Label(string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(panel.transform, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
        void Button(string title, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(panel.transform, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(165, 48);
            obj.GetComponent<Image>().color = new Color(.06f, .12f, .2f, .95f);
            obj.GetComponent<Button>().onClick.AddListener(action);
            var text = Label(title + " Label", Vector2.zero, rect.sizeDelta);
            text.transform.SetParent(obj.transform, false);
            text.text = title;
        }
        void OnDestroy() { if (panel != null) Destroy(panel); }
    }
}