using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SafeShelter
{
    /// <summary>Collects two local floor hits and submits them to the host.</summary>
    [DisallowMultipleComponent]
    public sealed class QuestKeepClearMarker : MonoBehaviour
    {
        [SerializeField] ConstraintManager constraints;
        [SerializeField] XRRayInteractor leftRay;
        [SerializeField] XRRayInteractor rightRay;
        [SerializeField] LayerMask floorMask = 1 << 8;
        [SerializeField] float maxDistance = 30f;
        [SerializeField] float previewHeight = 0.02f;

        Vector3? firstCorner;
        GameObject preview;
        Material previewMaterial;

        bool CanMark => constraints != null && constraints.isActiveAndEnabled && constraints.IsSpawned;

        void LateUpdate()
        {
            if (!CanMark)
            {
                if (firstCorner.HasValue) CancelPreview();
                return;
            }

            if (!TryGetCorner(leftRay, out var corner) && !TryGetCorner(rightRay, out corner)) return;
            if (firstCorner.HasValue)
            {
                constraints.RequestZone(firstCorner.Value, corner);
                CancelPreview();
                return;
            }

            firstCorner = corner;
            ShowPreview(corner);
        }

        bool TryGetCorner(XRRayInteractor ray, out Vector3 corner)
        {
            corner = default;
            if (ray == null || !ray.isActiveAndEnabled || maxDistance <= 0f ||
                (!ray.selectInput.ReadWasPerformedThisFrame() &&
                 !ray.activateInput.ReadWasPerformedThisFrame() &&
                 !ray.uiPressInput.ReadWasPerformedThisFrame()) ||
                ray.IsOverUIGameObject() || !ray.TryGetCurrent3DRaycastHit(out var hit) ||
                hit.collider == null || (floorMask.value & (1 << hit.collider.gameObject.layer)) == 0 ||
                (hit.point - ray.rayOriginTransform.position).sqrMagnitude > maxDistance * maxDistance)
                return false;

            corner = hit.point;
            return true;
        }

        public void ClearZone()
        {
            CancelPreview();
            if (CanMark) constraints.RequestClearZone();
        }

        void ShowPreview(Vector3 corner)
        {
            if (preview == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) return;

                previewMaterial = new Material(shader) { color = new Color(1f, 0.75f, 0.1f) };
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "KeepClearCornerPreview";
                var collider = preview.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                var renderer = preview.GetComponent<Renderer>();
                renderer.sharedMaterial = previewMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                preview.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
            }

            preview.transform.position = corner + Vector3.up * previewHeight;
            preview.SetActive(true);
        }

        void CancelPreview()
        {
            firstCorner = null;
            if (preview != null) preview.SetActive(false);
        }

        void OnDisable() => CancelPreview();

        void OnDestroy()
        {
            if (preview != null) Destroy(preview);
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
