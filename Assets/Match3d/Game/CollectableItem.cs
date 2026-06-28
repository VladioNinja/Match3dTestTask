using System;
using UnityEngine;

namespace Match3d.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CollectableItem : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private CollectableItemSettings _settings;

        private float _nextCollisionSoundTime;

        public static event Action<CollectableItem> Dropped;
        public event Action<CollectableItem> Collected;
        
        public bool IsDragging { get; private set; }
        public bool IsCollected { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Collider[] Colliders { get; private set; }
        public Transform VisualRoot => _visualRoot;
        public AudioSource AudioSource => _audioSource;
        public CollectableItemSettings Settings => _settings;
        public PhysicMaterial PhysicsMaterial => _settings != null ? _settings.PhysicsMaterial : null;
        public Bounds CollisionBounds
        {
            get
            {
                if (Colliders == null || Colliders.Length == 0)
                {
                    return new Bounds(transform.position, Vector3.zero);
                }

                var hasBounds = false;
                var bounds = default(Bounds);

                for (var i = 0; i < Colliders.Length; i++)
                {
                    if (!Colliders[i].enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = Colliders[i].bounds;
                        hasBounds = true;
                        continue;
                    }

                    bounds.Encapsulate(Colliders[i].bounds);
                }

                return hasBounds ? bounds : new Bounds(transform.position, Vector3.zero);
            }
        }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            ApplyRigidbodyTuning();

            if (_visualRoot == null && transform.childCount > 0)
            {
                _visualRoot = transform.GetChild(0);
            }

            Colliders = GetComponentsInChildren<Collider>();
            DisableRootColliderIfNeeded();
            ApplyColliderTuning();

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f;
            }
        }



        public void BeginDrag()
        {
            if (IsCollected)
            {
                return;
            }

            IsDragging = true;
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;

            if (_settings != null && _settings.DisableCollisionWhileDragging)
            {
                SetCollidersTrigger(true);
            }
        }

        public void EndDrag()
        {
            if (IsCollected)
            {
                return;
            }

            IsDragging = false;
            SetCollidersTrigger(false);
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;

            if (_settings != null)
            {
                Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity, _settings.MaxDropVelocity);
                Rigidbody.angularVelocity = Vector3.ClampMagnitude(Rigidbody.angularVelocity, _settings.MaxDropAngularVelocity);
            }

            Dropped?.Invoke(this);
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

            for (var i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].enabled = false;
            }

            Collected?.Invoke(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            LimitPushedItem(collision);
            PlayCollisionSound(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            LimitPushedItem(collision);
        }

        private void ApplyRigidbodyTuning()
        {
            if (_settings == null)
            {
                return;
            }

            Rigidbody.mass = Mathf.Max(0.01f, _settings.Mass);
            Rigidbody.drag = Mathf.Max(0f, _settings.LinearDrag);
            Rigidbody.angularDrag = Mathf.Max(0f, _settings.AngularDrag);
            Rigidbody.maxAngularVelocity = Mathf.Max(0.1f, _settings.MaxDropAngularVelocity);
            Rigidbody.maxDepenetrationVelocity = Mathf.Max(0.1f, _settings.MaxDepenetrationVelocity);
        }

        private void LimitPushedItem(Collision collision)
        {
            if (!IsDragging || collision.rigidbody == null)
            {
                return;
            }

            var otherItem = collision.rigidbody.GetComponent<CollectableItem>();

            if (otherItem == null || otherItem == this || otherItem.IsDragging || otherItem.IsCollected)
            {
                return;
            }

            var otherRigidbody = otherItem.Rigidbody;

            if (otherRigidbody == null)
            {
                return;
            }

            if (_settings == null)
            {
                return;
            }

            otherRigidbody.velocity = Vector3.ClampMagnitude(otherRigidbody.velocity, _settings.MaxPushedVelocity);
            otherRigidbody.angularVelocity = Vector3.ClampMagnitude(otherRigidbody.angularVelocity, _settings.MaxPushedAngularVelocity);
        }

        private void PlayCollisionSound(Collision collision)
        {
            if (_settings == null
                || _audioSource == null
                || _settings.CollisionClip == null
                || Time.time < _nextCollisionSoundTime)
            {
                return;
            }

            var otherItem = collision.rigidbody != null
                ? collision.rigidbody.GetComponent<CollectableItem>()
                : null;

            // Only one item in a collision pair plays audio, otherwise every bump is heard twice.
            if (otherItem == null || otherItem == this || GetInstanceID() > otherItem.GetInstanceID())
            {
                return;
            }

            var impulse = collision.impulse.magnitude;

            if (impulse < _settings.MinCollisionImpulseForSound)
            {
                return;
            }

            var normalizedImpulse = Mathf.InverseLerp(
                _settings.MinCollisionImpulseForSound,
                _settings.MinCollisionImpulseForSound * 5f,
                impulse);

            _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            _audioSource.PlayOneShot(
                _settings.CollisionClip,
                Mathf.Lerp(_settings.CollisionSoundVolume * 0.45f, _settings.CollisionSoundVolume, normalizedImpulse));

            _nextCollisionSoundTime = Time.time + _settings.CollisionSoundCooldown;
        }

        private void ApplyColliderTuning()
        {
            for (var i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].isTrigger = false;

                if (_settings != null && _settings.PhysicsMaterial != null)
                {
                    Colliders[i].sharedMaterial = _settings.PhysicsMaterial;
                }
            }
        }

        private void SetCollidersTrigger(bool isTrigger)
        {
            if (Colliders == null)
            {
                return;
            }

            for (var i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].isTrigger = isTrigger;
            }
        }

        private void DisableRootColliderIfNeeded()
        {
            var rootCollider = GetComponent<Collider>();

            if (rootCollider != null && Colliders.Length > 1)
            {
                rootCollider.enabled = false;
            }
        }
    }
}
