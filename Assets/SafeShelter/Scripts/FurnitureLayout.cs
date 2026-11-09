using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SafeShelter
{
    /// <summary>Owns furniture lifecycle. A session reference enables host-only network spawning.</summary>
    [DisallowMultipleComponent]
    public sealed class FurnitureLayout : MonoBehaviour
    {
        [SerializeField] SharedFurniture bedPrefab;
        [SerializeField] SharedFurniture[] initialFurniture = new SharedFurniture[0];
        [SerializeField] SessionController session;
        readonly List<SharedFurniture> addedFurniture = new List<SharedFurniture>();
        public event System.Action Changed;

        void Awake()
        {
            if (session == null) return; // Explicit offline scene configuration.
            bool valid = bedPrefab != null && bedPrefab.GetComponent<NetworkFurniture>() != null;
            foreach (var item in initialFurniture)
                valid &= item != null && item.GetComponent<NetworkFurniture>() != null;
            if (!valid)
            {
                Debug.LogError("A network layout requires network furniture for its prefab and starting objects.", this);
                enabled = false;
            }
        }

        public bool CanEdit => isActiveAndEnabled && (session == null || session.CanEditFurniture);
        public bool CanAddBed => CanEdit && bedPrefab != null && bedPrefab.gameObject.activeSelf;

        /// <summary>Starting and added furniture, including items currently removed from the layout.</summary>
        public IEnumerable<SharedFurniture> Furniture
        {
            get
            {
                foreach (var item in initialFurniture) if (item != null) yield return item;
                foreach (var item in addedFurniture) if (item != null) yield return item;
            }
        }

        public bool Contains(SharedFurniture item)
        {
            return CanEdit && item != null && item.CanEdit &&
                (System.Array.IndexOf(initialFurniture, item) >= 0 || addedFurniture.Contains(item));
        }

        public SharedFurniture AddBed(Vector3 position)
        {
            if (!isActiveAndEnabled || !CanAddBed || !float.IsFinite(position.x) ||
                !float.IsFinite(position.y) || !float.IsFinite(position.z)) return null;

            // Instantiate at its resting pose so SharedFurniture.Awake captures the right height.
            position.y = bedPrefab.transform.position.y;
            var item = Instantiate(bedPrefab, position, Quaternion.identity);
            if (session != null) item.GetComponent<NetworkObject>().Spawn();
            addedFurniture.Add(item);
            Changed?.Invoke();
            return item;
        }

        public void Remove(SharedFurniture item)
        {
            if (!isActiveAndEnabled || !Contains(item)) return;
            Changed?.Invoke();
            if (session != null)
            {
                if (addedFurniture.Remove(item)) item.GetComponent<NetworkObject>().Despawn(true);
                else item.GetComponent<NetworkFurniture>().SetPresent(false);
                return;
            }
            item.gameObject.SetActive(false);
            if (addedFurniture.Remove(item)) Destroy(item.gameObject);
            // Scene furniture is retained inactive so Reset Layout can restore the baseline.
        }

        public void ResetLayout()
        {
            if (!CanEdit) return;
            Changed?.Invoke();
            foreach (var item in addedFurniture)
            {
                if (item == null) continue;
                if (session != null) { item.GetComponent<NetworkObject>().Despawn(true); continue; }
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            addedFurniture.Clear();
            foreach (var item in initialFurniture)
            {
                if (item == null) continue;
                if (session != null) item.GetComponent<NetworkFurniture>().SetPresent(true);
                else item.gameObject.SetActive(true);
                item.ResetPose();
            }
        }
    }
}
