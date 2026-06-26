using System;
using UnityEngine;

namespace Match3d.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class CollectableItem : MonoBehaviour
    {
        [SerializeField] private ItemType type = ItemType.Unknown;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private AudioSource audioSource;

        public event Action<CollectableItem> Collected;

        public ItemType Type => type;
        public bool IsDragging { get; private set; }
        public bool IsCollected { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Collider Collider { get; private set; }
        public Transform VisualRoot => visualRoot;
        public AudioSource AudioSource => audioSource;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Collider = GetComponent<Collider>();

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public void SetType(ItemType itemType)
        {
            type = itemType;
        }

        public void BeginDrag()
        {
            if (IsCollected)
            {
                return;
            }

            IsDragging = true;
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;
        }

        public void EndDrag()
        {
            if (IsCollected)
            {
                return;
            }

            IsDragging = false;
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
        }

        public void MarkCollected()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            IsDragging = false;

            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;
            Collider.enabled = false;

            Collected?.Invoke(this);
        }
    }
}
