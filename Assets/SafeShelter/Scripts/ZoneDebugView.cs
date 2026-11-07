using UnityEngine;

namespace SafeShelter
{
    /// <summary>Minimal red/green zone display for desktop test scenes. Reads replicated state only.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneDebugView : MonoBehaviour
    {
        [SerializeField] ConstraintManager constraints;
        [SerializeField] Material material;
        [SerializeField] float heightAboveFloor = 0.01f;
        [SerializeField] Color blockedColor = new Color(0.9f, 0.15f, 0.1f);
        [SerializeField] Color clearColor = new Color(0.15f, 0.8f, 0.25f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColor = Shader.PropertyToID("_Color");
        Transform slab;
        Renderer slabRenderer;
        MaterialPropertyBlock block;

        void Awake()
        {
            if (constraints == null || material == null)
            {
                Debug.LogError("ZoneDebugView requires a ConstraintManager and a material.", this);
                enabled = false;
                return;
            }
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "KeepClearZone";
            // The planner raycasts colliders; the zone must never block selection or placement.
            DestroyImmediate(cube.GetComponent<Collider>());
            slab = cube.transform;
            slab.SetParent(transform, false);
            slabRenderer = cube.GetComponent<Renderer>();
            slabRenderer.sharedMaterial = material;
            slabRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            block = new MaterialPropertyBlock();
            cube.SetActive(false);
        }

        void OnEnable()
        {
            if (constraints == null) return;
            constraints.ZoneChanged += Show;
            Show(constraints.CurrentZone);
        }

        void OnDisable() { if (constraints != null) constraints.ZoneChanged -= Show; }

        void Show(ZoneState zone)
        {
            if (slab == null) return;
            slab.gameObject.SetActive(zone.IsActive);
            if (!zone.IsActive) return;
            var size = zone.SizeXZ;
            slab.position = zone.Center + Vector3.up * heightAboveFloor;
            slab.rotation = Quaternion.identity;
            slab.localScale = new Vector3(size.x, 0.01f, size.y);
            var color = zone.HasConflict ? blockedColor : clearColor;
            block.SetColor(BaseColor, color);
            block.SetColor(LegacyColor, color);
            slabRenderer.SetPropertyBlock(block);
        }
    }
}
