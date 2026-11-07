using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SafeShelter
{
    /// <summary>Explicit references allow reuse in B's scene without object-name searches.</summary>
    [DisallowMultipleComponent]
    public sealed class DesktopPlanner : MonoBehaviour
    {
        [SerializeField] Camera plannerCamera;
        [SerializeField] Transform floorPlane;
        [SerializeField] Collider placementSurface;
        [SerializeField] FurnitureLayout layout;
        [SerializeField] LayerMask selectionMask = ~0;
        [SerializeField] bool showControls = true;
        SharedFurniture selected;
        bool dragging;
        bool placingBed;
        Vector3 grabOffset;

        Rect Panel => new Rect(12, 12, 390, 225);

        void Awake()
        {
            // Never enable laptop interaction in a Quest build.
            if (Application.platform == RuntimePlatform.Android) { enabled = false; return; }
            if (plannerCamera == null || floorPlane == null || layout == null)
            {
                Debug.LogError("DesktopPlanner requires camera, floor-plane, and layout references.", this);
                enabled = false;
            }
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused) { dragging = false; return; }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                placingBed = false;
                dragging = false;
                selected = null;
                return;
            }
            var point = mouse.position.ReadValue();
            bool overUI = (showControls && Panel.Contains(new Vector2(point.x, Screen.height - point.y))) ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) dragging = false;
            if (mouse.rightButton.isPressed) { dragging = false; return; }

            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                dragging = false;
                if (placingBed)
                {
                    var placementRay = plannerCamera.ScreenPointToRay(point);
                    if (placementSurface != null && placementSurface.Raycast(placementRay, out var placementHit, 1000f))
                    {
                        var item = layout.AddBed(placementHit.point);
                        if (item != null) { selected = item; placingBed = false; }
                    }
                    return;
                }
                selected = null;
                var ray = plannerCamera.ScreenPointToRay(point);
                if (Physics.Raycast(ray, out var hit, 1000f, selectionMask, QueryTriggerInteraction.Ignore))
                {
                    var candidate = hit.collider.GetComponentInParent<SharedFurniture>();
                    // Only explicitly registered furniture may be edited/reset by this planner.
                    if (layout.Contains(candidate))
                        selected = candidate;
                }
                if (selected != null && TryFloorPoint(point, out var floorHit))
                {
                    grabOffset = selected.transform.position - floorHit;
                    dragging = true;
                }
            }
            if (dragging && selected != null && !overUI && TryFloorPoint(point, out var destination))
                selected.MoveTo(destination + grabOffset);

            if (!overUI && keyboard != null && keyboard.rKey.wasPressedThisFrame && selected != null && !placingBed)
                selected.Rotate90();
        }

        bool TryFloorPoint(Vector2 screenPoint, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, floorPlane.position);
            var ray = plannerCamera.ScreenPointToRay(screenPoint);
            if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
            point = default;
            return false;
        }

        public void ResetLayout()
        {
            dragging = false;
            placingBed = false;
            selected = null;
            layout.ResetLayout();
        }

        public void BeginAddBed()
        {
            if (layout == null || !layout.CanAddBed || placementSurface == null) return;
            dragging = false;
            selected = null;
            placingBed = true;
        }

        public void RemoveSelected()
        {
            if (selected == null) return;
            layout.Remove(selected);
            selected = null;
            dragging = false;
        }

        void OnDisable() { dragging = false; placingBed = false; selected = null; }
        void OnApplicationFocus(bool hasFocus) { if (!hasFocus) dragging = false; }

        void OnGUI()
        {
            if (!showControls) return;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label("SafeShelter — Desktop milestone 1 (local)");
            GUILayout.Label("Left drag: move    R: rotate 90°");
            GUILayout.Label("Right drag: orbit    Wheel: zoom");
            GUILayout.Label(placingBed ? "Click the floor to place a bed. Esc: cancel" :
                selected == null ? "Click a bed to select" : $"Selected: {selected.name}");
            GUI.enabled = layout != null && layout.CanAddBed && placementSurface != null;
            if (GUILayout.Button("Add Bed")) BeginAddBed();
            GUI.enabled = selected != null && !placingBed;
            if (GUILayout.Button("Remove Selected")) RemoveSelected();
            GUI.enabled = true;
            if (GUILayout.Button("Reset Layout")) ResetLayout();
            GUILayout.EndArea();
        }

        void OnDrawGizmos()
        {
            if (selected == null) return;
            Gizmos.color = Color.yellow;
            foreach (var collider in selected.GetComponentsInChildren<Collider>())
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }
}
