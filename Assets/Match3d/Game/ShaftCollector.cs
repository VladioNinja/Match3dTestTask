using System.Collections.Generic;
using UnityEngine;

namespace Match3d.Game
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShaftCollector : MonoBehaviour
    {
        [SerializeField] private Transform _collectedItemsRoot;
        [SerializeField] private CollectFeedbackController _collectFeedback;
        [SerializeField] private bool _hideCollectedItems = true;

        private readonly HashSet<CollectableItem> _itemsInside = new HashSet<CollectableItem>();

        private void OnEnable()
        {
            CollectableItem.Dropped += HandleItemDropped;
        }

        private void OnDisable()
        {
            CollectableItem.Dropped -= HandleItemDropped;
            _itemsInside.Clear();
        }

        private void Reset()
        {
            var triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TrackItem(other);
        }

        private void OnTriggerExit(Collider other)
        {
            var item = other.GetComponentInParent<CollectableItem>();

            if (item != null)
            {
                _itemsInside.Remove(item);
            }
        }

        private CollectableItem TrackItem(Collider other)
        {
            var item = other.GetComponentInParent<CollectableItem>();

            if (item == null || item.IsCollected)
            {
                return null;
            }

            _itemsInside.Add(item);
            return item;
        }

        private void HandleItemDropped(CollectableItem item)
        {
            // Collection is checked on drop, not on trigger enter, so players can pass over the shaft while dragging.
            if (item == null || item.IsCollected || !_itemsInside.Contains(item))
            {
                return;
            }

            Collect(item);
        }

        private void Collect(CollectableItem item)
        {
            _itemsInside.Remove(item);
            var collectPosition = item.transform.position;
            item.MarkCollected();

            if (_collectFeedback != null)
            {
                _collectFeedback.Play(collectPosition);
            }

            if (_collectedItemsRoot != null)
            {
                item.transform.SetParent(_collectedItemsRoot, true);
            }

            if (_hideCollectedItems)
            {
                item.gameObject.SetActive(false);
            }
        }
    }
}
