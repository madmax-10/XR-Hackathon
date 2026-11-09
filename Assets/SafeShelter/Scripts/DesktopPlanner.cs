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
        SharedFurniture highlighted;
        Renderer[] highlightedRenderers;
        MaterialPropertyBlock[] savedProperties;
        bool dragging;
        bool placingBed;
        bool markingZone;
        bool hasFirstCorner;
        Vector3 firstCorner;
        Vector3 grabOffset;

        [SerializeField] SessionController session;
        [SerializeField] ConstraintManager constraints;
        Rect Panel => new Rect(12, session == null ? 12 : 209, 390, constraints == null ? 225 : 310);

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
            if (!layout.CanEdit) { dragging = false; placingBed = false; CancelZone(); selected = null; return; }
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused) { dragging = false; return; }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                placingBed = false;
                CancelZone();
                dragging = false;
                selected = null;
                return;
            }
            var point = mouse.position.ReadValue();
            bool overUI = CrowdDesktopControls.PointerOverPanel || (showControls && Panel.Contains(new Vector2(point.x, Screen.height - point.y))) ||
                (session != null && session.Panel.Contains(new Vector2(point.x, Screen.height - point.y))) ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) dragging = false;
            if (mouse.rightButton.isPressed) { dragging = false; return; }

            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                dragging = false;
                if (markingZone)
                {
                    var zoneRay = plannerCamera.ScreenPointToRay(point);
                    if (placementSurface != null && placementSurface.Raycast(zoneRay, out var cornerHit, 1000f))
                    {
                        if (!hasFirstCorner) { firstCorner = cornerHit.point; hasFirstCorner = true; }
                        else { constraints.RequestZone(firstCorner, cornerHit.point); CancelZone(); }
                    }
                    return;
                }
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

            if (!overUI && keyboard != null && keyboard.rKey.wasPressedThisFrame && selected != null &&
                !placingBed && !markingZone)
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
            CancelZone();
            dragging = false;
            selected = null;
            placingBed = true;
        }

        /// <summary>Debug marking through the same request path as the Quest inspector.</summary>
        public void BeginMarkZone()
        {
            if (constraints == null || placementSurface == null || !layout.CanEdit) return;
            placingBed = false;
            dragging = false;
            selected = null;
            markingZone = true;
            hasFirstCorner = false;
        }

        void CancelZone() { markingZone = false; hasFirstCorner = false; }

        public void RemoveSelected()
        {
            if (selected == null) return;
            layout.Remove(selected);
            selected = null;
            dragging = false;
        }

        void OnDisable() { dragging = false; placingBed = false; CancelZone(); selected = null; RestoreHighlight(); }

        void LateUpdate()
        {
            if (highlighted == selected) return;
            RestoreHighlight();
            highlighted = selected;
            if (highlighted == null) return;
            highlightedRenderers = highlighted.GetComponentsInChildren<Renderer>();
            savedProperties = new MaterialPropertyBlock[highlightedRenderers.Length];
            for (int i = 0; i < highlightedRenderers.Length; i++)
            {
                var renderer = highlightedRenderers[i];
                savedProperties[i] = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(savedProperties[i]);
                var selectionProperties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(selectionProperties);
                selectionProperties.SetColor("_BaseColor", Color.Lerp(renderer.sharedMaterial.color, Color.yellow, .45f));
                renderer.SetPropertyBlock(selectionProperties);
            }
        }

        void RestoreHighlight()
        {
            if (highlightedRenderers != null)
                for (int i = 0; i < highlightedRenderers.Length; i++)
                    if (highlightedRenderers[i] != null) highlightedRenderers[i].SetPropertyBlock(savedProperties[i]);
            highlightedRenderers = null;
            savedProperties = null;
            highlighted = null;
        }
        void OnApplicationFocus(bool hasFocus) { if (!hasFocus) dragging = false; }

        void OnGUI()
        {
            if (!showControls) return;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label(session == null ? "SafeShelter — Local Planner" : "SafeShelter — Host Planner");
            if (!layout.CanEdit) GUILayout.Label("Editing is available on the connected host only.");
            GUILayout.Label("Left drag: move    R: rotate 90°");
            GUILayout.Label("Right drag: orbit    Wheel: zoom");
            GUILayout.Label(markingZone ? (hasFirstCorner ? "Click the opposite zone corner. Esc: cancel" :
                    "Click the first keep-clear corner. Esc: cancel") :
                placingBed ? "Click the floor to place a bed. Esc: cancel" :
                selected == null ? "Click a bed to select" : $"Selected: {selected.name}");
            GUI.enabled = layout != null && layout.CanAddBed && placementSurface != null;
            if (GUILayout.Button("Add Bed")) BeginAddBed();
            GUI.enabled = selected != null && !placingBed;
            if (GUILayout.Button("Remove Selected")) RemoveSelected();
            GUI.enabled = layout.CanEdit;
            if (GUILayout.Button("Reset Layout")) ResetLayout();
            if (constraints != null)
            {
                var zone = constraints.CurrentZone;
                GUI.enabled = true;
                GUILayout.Label(!zone.IsActive ? "Keep-clear zone: none" :
                    zone.HasConflict ? "Keep-clear zone: BLOCKED (red)" : "Keep-clear zone: clear (green)");
                GUI.enabled = layout.CanEdit && placementSurface != null && !markingZone;
                if (GUILayout.Button("Mark Keep-Clear Zone (debug)")) BeginMarkZone();
                GUI.enabled = layout.CanEdit && zone.IsActive;
                if (GUILayout.Button("Clear Zone")) constraints.RequestClearZone();
            }
            GUI.enabled = true;
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
