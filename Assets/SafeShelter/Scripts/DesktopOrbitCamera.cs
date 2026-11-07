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

        void LateUpdate()
        {
            if (focus == null) return;
            var mouse = Mouse.current;
            if (mouse != null && Application.isFocused)
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
            transform.SetPositionAndRotation(focus.position - rotation * Vector3.forward * distance, rotation);
        }
    }
}
