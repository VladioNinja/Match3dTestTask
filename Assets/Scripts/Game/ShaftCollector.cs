using UnityEngine;

namespace Match3d.Game
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShaftCollector : MonoBehaviour
    {
        [SerializeField] private Transform collectedItemsRoot;
        [SerializeField] private bool hideCollectedItems = true;

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider other)
        {
            CollectableItem item = other.GetComponentInParent<CollectableItem>();

            if (item == null || item.IsCollected || item.IsDragging)
            {
                return;
            }

            item.MarkCollected();

            if (collectedItemsRoot != null)
            {
                item.transform.SetParent(collectedItemsRoot, true);
            }

            if (hideCollectedItems)
            {
                item.gameObject.SetActive(false);
            }
        }
    }
}
