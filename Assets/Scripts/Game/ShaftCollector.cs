using System.Collections.Generic;
using UnityEngine;

namespace Match3d.Game
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShaftCollector : MonoBehaviour
    {
        [SerializeField] private Transform collectedItemsRoot;
        [SerializeField] private CollectFeedbackController collectFeedback;
        [SerializeField] private bool hideCollectedItems = true;

        private readonly HashSet<CollectableItem> itemsInside = new HashSet<CollectableItem>();

        private void OnEnable()
        {
            CollectableItem.Dropped += HandleItemDropped;
        }

        private void OnDisable()
        {
            CollectableItem.Dropped -= HandleItemDropped;
            itemsInside.Clear();
        }

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TrackItem(other);
        }

        private void OnTriggerExit(Collider other)
        {
            CollectableItem item = other.GetComponentInParent<CollectableItem>();

            if (item != null)
            {
                itemsInside.Remove(item);
            }
        }

        private void TrackItem(Collider other)
        {
            CollectableItem item = other.GetComponentInParent<CollectableItem>();

            if (item == null || item.IsCollected)
            {
                return;
            }

            itemsInside.Add(item);
        }

        private void HandleItemDropped(CollectableItem item)
        {
            if (item == null || item.IsCollected || !itemsInside.Contains(item))
            {
                return;
            }

            Collect(item);
        }

        private void Collect(CollectableItem item)
        {
            itemsInside.Remove(item);
            Vector3 collectPosition = item.transform.position;
            item.MarkCollected();

            if (collectFeedback != null)
            {
                collectFeedback.Play(collectPosition);
            }

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
