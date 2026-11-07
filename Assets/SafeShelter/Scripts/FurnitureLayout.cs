using System.Collections.Generic;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Owns local furniture creation/removal/reset, independently of mouse input.
    /// Networking will put server authority and network spawning at this boundary.</summary>
    [DisallowMultipleComponent]
    public sealed class FurnitureLayout : MonoBehaviour
    {
        [SerializeField] SharedFurniture bedPrefab;
        [SerializeField] SharedFurniture[] initialFurniture = new SharedFurniture[0];
        readonly List<SharedFurniture> addedFurniture = new List<SharedFurniture>();

        public bool CanAddBed => bedPrefab != null && bedPrefab.gameObject.activeSelf;

        public bool Contains(SharedFurniture item)
        {
            return item != null && item.isActiveAndEnabled &&
                (System.Array.IndexOf(initialFurniture, item) >= 0 || addedFurniture.Contains(item));
        }

        public SharedFurniture AddBed(Vector3 position)
        {
            if (!isActiveAndEnabled || !CanAddBed || !float.IsFinite(position.x) ||
                !float.IsFinite(position.y) || !float.IsFinite(position.z)) return null;

            // Instantiate at its resting pose so SharedFurniture.Awake captures the right height.
            var item = Instantiate(bedPrefab, position, Quaternion.identity);
            addedFurniture.Add(item);
            return item;
        }

        public void Remove(SharedFurniture item)
        {
            if (!isActiveAndEnabled || !Contains(item)) return;
            // Disable immediately because Unity destroys runtime objects at end of frame.
            item.gameObject.SetActive(false);
            if (addedFurniture.Remove(item)) Destroy(item.gameObject);
            // Scene furniture is retained inactive so Reset Layout can restore the baseline.
        }

        public void ResetLayout()
        {
            if (!isActiveAndEnabled) return;
            foreach (var item in addedFurniture)
            {
                if (item == null) continue;
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            addedFurniture.Clear();
            foreach (var item in initialFurniture)
            {
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.ResetPose();
            }
        }
    }
}
