using UnityEngine;
using UnityEngine.InputSystem;

namespace SafeShelter
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class DesktopOrbitCamera : MonoBehaviour
    {
        [SerializeField] Transform focus;
        [SerializeField] float distance = 14f;
        [SerializeField] float yaw;
        [SerializeField] float pitch = 60f;
        [SerializeField] float orbitSensitivity = 0.2f;
        [SerializeField] float zoomSensitivity = 0.01f;
        [SerializeField] Vector2 distanceLimits = new Vector2(4f, 24f);
        Camera view;
        CrowdSceneBindings crowd;
        float initialDistance;

        void Awake()
        {
            view = GetComponent<Camera>();
            crowd = FindFirstObjectByType<CrowdSceneBindings>();
            initialDistance = distance;
        }

        void LateUpdate()
        {
            if (focus == null) return;
            var mouse = Mouse.current;
            if (mouse != null && Application.isFocused && !CrowdDesktopControls.PointerOverPanel &&
                !new Rect(12, 12, 390, 520).Contains(new Vector2(mouse.position.ReadValue().x, Screen.height - mouse.position.ReadValue().y)))
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * orbitSensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, 25f, 85f);
                }
                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSensitivity,
                    distanceLimits.x, distanceLimits.y);
            }
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            float viewDistance = distance;
            if (CrowdDesktopControls.Visible && crowd != null && crowd.hallFloor != null && Screen.width > 750)
            {
                // Reserve the two control columns. Fit the hall in the remaining viewport;
                // keep the operator's zoom relative to that fit when resizing the window.
                float left = 414f;
                float width = Mathf.Max(1, CrowdDesktopControls.Panel.x - 12f - left);
                view.pixelRect = new Rect(left, 12, width, Mathf.Max(1, Screen.height - 24));
                float tanVertical = Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad);
                float tanHorizontal = tanVertical * view.aspect;
                var bounds = crowd.hallFloor.bounds;
                bounds.Expand(new Vector3(.8f, 5.6f, .8f));
                float fit = initialDistance;
                var inverse = Quaternion.Inverse(rotation);
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    var local = inverse * (corner - focus.position);
                    fit = Mathf.Max(fit, Mathf.Abs(local.x) / tanHorizontal - local.z,
                        Mathf.Abs(local.y) / tanVertical - local.z);
                }
                viewDistance = fit * distance / Mathf.Max(1, initialDistance);
            }
            else view.rect = new Rect(0, 0, 1, 1);
            transform.SetPositionAndRotation(focus.position - rotation * Vector3.forward * viewDistance, rotation);
        }
    }
}
