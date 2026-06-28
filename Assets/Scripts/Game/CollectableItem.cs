using System;
using UnityEngine;

namespace Match3d.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CollectableItem : MonoBehaviour
    {
        private const string CollidersRootName = "Colliders";
        private static AudioClip collisionClip;

        [SerializeField] private ItemType type = ItemType.Unknown;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PhysicMaterial physicsMaterial;
        [SerializeField] private float mass = 1.15f;
        [SerializeField] private float linearDrag = 4f;
        [SerializeField] private float angularDrag = 8f;
        [SerializeField] private float maxDropVelocity = 1.4f;
        [SerializeField] private float maxDropAngularVelocity = 4f;
        [SerializeField] private float maxDepenetrationVelocity = 1.6f;
        [SerializeField] private float maxPushedVelocity = 1.75f;
        [SerializeField] private float maxPushedAngularVelocity = 4f;
        [SerializeField] private float minCollisionImpulseForSound = 0.35f;
        [SerializeField] private float collisionSoundCooldown = 0.08f;
        [SerializeField] private float collisionSoundVolume = 0.28f;
        [SerializeField] private bool disableCollisionWhileDragging;

        public static event Action<CollectableItem> Dropped;
        public event Action<CollectableItem> Collected;

        public ItemType Type => type;
        public bool IsDragging { get; private set; }
        public bool IsCollected { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Collider[] Colliders { get; private set; }
        public Transform VisualRoot => visualRoot;
        public AudioSource AudioSource => audioSource;
        private float nextCollisionSoundTime;
        public PhysicMaterial PhysicsMaterial => physicsMaterial;
        public Bounds CollisionBounds
        {
            get
            {
                if (Colliders == null || Colliders.Length == 0)
                {
                    return new Bounds(transform.position, Vector3.zero);
                }

                bool hasBounds = false;
                Bounds bounds = default;

                for (int i = 0; i < Colliders.Length; i++)
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

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            EnsurePrimitiveColliders();
            Colliders = GetComponentsInChildren<Collider>();
            ApplyColliderTuning();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            if (collisionClip == null)
            {
                collisionClip = CreateCollisionClip();
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
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;

            if (disableCollisionWhileDragging)
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
            Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity, maxDropVelocity);
            Rigidbody.angularVelocity = Vector3.ClampMagnitude(Rigidbody.angularVelocity, maxDropAngularVelocity);

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

            for (int i = 0; i < Colliders.Length; i++)
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
            Rigidbody.mass = Mathf.Max(0.01f, mass);
            Rigidbody.drag = Mathf.Max(0f, linearDrag);
            Rigidbody.angularDrag = Mathf.Max(0f, angularDrag);
            Rigidbody.maxAngularVelocity = Mathf.Max(0.1f, maxDropAngularVelocity);
            Rigidbody.maxDepenetrationVelocity = Mathf.Max(0.1f, maxDepenetrationVelocity);
        }

        private void LimitPushedItem(Collision collision)
        {
            if (!IsDragging || collision.rigidbody == null)
            {
                return;
            }

            CollectableItem otherItem = collision.rigidbody.GetComponent<CollectableItem>();

            if (otherItem == null || otherItem == this || otherItem.IsDragging || otherItem.IsCollected)
            {
                return;
            }

            Rigidbody otherRigidbody = otherItem.Rigidbody;

            if (otherRigidbody == null)
            {
                return;
            }

            otherRigidbody.velocity = Vector3.ClampMagnitude(otherRigidbody.velocity, maxPushedVelocity);
            otherRigidbody.angularVelocity = Vector3.ClampMagnitude(otherRigidbody.angularVelocity, maxPushedAngularVelocity);
        }

        private void PlayCollisionSound(Collision collision)
        {
            if (audioSource == null || collisionClip == null || Time.time < nextCollisionSoundTime)
            {
                return;
            }

            CollectableItem otherItem = collision.rigidbody != null
                ? collision.rigidbody.GetComponent<CollectableItem>()
                : null;

            if (otherItem == null || otherItem == this || GetInstanceID() > otherItem.GetInstanceID())
            {
                return;
            }

            float impulse = collision.impulse.magnitude;

            if (impulse < minCollisionImpulseForSound)
            {
                return;
            }

            float normalizedImpulse = Mathf.InverseLerp(minCollisionImpulseForSound, minCollisionImpulseForSound * 5f, impulse);
            audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            audioSource.PlayOneShot(collisionClip, Mathf.Lerp(collisionSoundVolume * 0.45f, collisionSoundVolume, normalizedImpulse));
            nextCollisionSoundTime = Time.time + collisionSoundCooldown;
        }

        private static AudioClip CreateCollisionClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.08f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float envelope = Mathf.Exp(-normalized * 12f);
                float body = Mathf.Sin(2f * Mathf.PI * 320f * t);
                float tick = Mathf.Sin(2f * Mathf.PI * 1450f * t) * Mathf.Exp(-normalized * 28f);
                samples[i] = (body * 0.45f + tick * 0.55f) * envelope * 0.55f;
            }

            AudioClip clip = AudioClip.Create("ItemCollisionClick", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void ApplyColliderTuning()
        {
            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].isTrigger = false;

                if (physicsMaterial != null)
                {
                    Colliders[i].sharedMaterial = physicsMaterial;
                }
            }
        }

        private void SetCollidersTrigger(bool isTrigger)
        {
            if (Colliders == null)
            {
                return;
            }

            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].isTrigger = isTrigger;
            }
        }

        private void EnsurePrimitiveColliders()
        {
            Transform collidersRoot = transform.Find(CollidersRootName);
            Collider rootCollider = GetComponent<Collider>();

            if (collidersRoot == null)
            {
                collidersRoot = CreateCollidersRoot();
                CreatePrimitiveColliders(collidersRoot);
            }

            if (rootCollider != null)
            {
                rootCollider.enabled = false;
            }
        }

        private Transform CreateCollidersRoot()
        {
            GameObject collidersObject = new GameObject(CollidersRootName);
            collidersObject.layer = gameObject.layer;
            collidersObject.transform.SetParent(transform);
            collidersObject.transform.localPosition = Vector3.zero;
            collidersObject.transform.localRotation = Quaternion.identity;
            collidersObject.transform.localScale = Vector3.one;
            return collidersObject.transform;
        }

        private void CreatePrimitiveColliders(Transform collidersRoot)
        {
            if (visualRoot == null || !TryGetVisualLocalBounds(out Bounds bounds))
            {
                AddBoxCollider(collidersRoot, "Collider_Core", Vector3.zero, Vector3.one * 0.1f);
                AddSphereCollider(collidersRoot, "Collider_Left", new Vector3(-0.03f, 0f, 0f), 0.035f);
                AddSphereCollider(collidersRoot, "Collider_Right", new Vector3(0.03f, 0f, 0f), 0.035f);
                return;
            }

            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.02f);
            size.y = Mathf.Max(size.y, 0.02f);
            size.z = Mathf.Max(size.z, 0.02f);

            int longestAxis = GetLongestAxis(size);
            float longestSize = GetAxis(size, longestAxis);
            float secondSize = GetSecondLongestSize(size, longestAxis);

            if (longestSize > secondSize * 1.65f)
            {
                AddSphereChain(collidersRoot, bounds, longestAxis, 3);
                return;
            }

            AddBoxCollider(collidersRoot, "Collider_Core", bounds.center, size * 0.72f);

            int supportAxis = size.x >= size.z ? 0 : 2;
            float supportOffset = GetAxis(size, supportAxis) * 0.25f;
            float supportRadius = Mathf.Max(GetAverageOtherAxes(size, supportAxis) * 0.28f, 0.025f);

            Vector3 leftCenter = bounds.center;
            Vector3 rightCenter = bounds.center;
            SetAxis(ref leftCenter, supportAxis, GetAxis(bounds.center, supportAxis) - supportOffset);
            SetAxis(ref rightCenter, supportAxis, GetAxis(bounds.center, supportAxis) + supportOffset);

            AddSphereCollider(collidersRoot, "Collider_Left", leftCenter, supportRadius);
            AddSphereCollider(collidersRoot, "Collider_Right", rightCenter, supportRadius);
        }

        private bool TryGetVisualLocalBounds(out Bounds localBounds)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                localBounds = default;
                return false;
            }

            localBounds = ToLocalBounds(renderers[0].bounds);

            for (int i = 1; i < renderers.Length; i++)
            {
                Bounds rendererBounds = ToLocalBounds(renderers[i].bounds);
                localBounds.Encapsulate(rendererBounds.min);
                localBounds.Encapsulate(rendererBounds.max);
            }

            return true;
        }

        private void AddSphereChain(Transform parent, Bounds bounds, int axis, int count)
        {
            Vector3 size = bounds.size;
            float length = GetAxis(size, axis);
            float radius = Mathf.Max(GetAverageOtherAxes(size, axis) * 0.38f, 0.02f);
            float step = length / Mathf.Max(count, 1) * 0.55f;

            for (int i = 0; i < count; i++)
            {
                float centeredIndex = i - (count - 1) * 0.5f;
                Vector3 center = bounds.center;
                SetAxis(ref center, axis, GetAxis(bounds.center, axis) + centeredIndex * step);
                AddSphereCollider(parent, $"Collider_Sphere_{i + 1}", center, radius);
            }
        }

        private void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            GameObject colliderObject = CreateColliderObject(parent, name);
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private void AddSphereCollider(Transform parent, string name, Vector3 center, float radius)
        {
            GameObject colliderObject = CreateColliderObject(parent, name);
            SphereCollider collider = colliderObject.AddComponent<SphereCollider>();
            collider.center = center;
            collider.radius = radius;
        }

        private GameObject CreateColliderObject(Transform parent, string name)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.layer = gameObject.layer;
            colliderObject.transform.SetParent(parent);
            colliderObject.transform.localPosition = Vector3.zero;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;
            return colliderObject;
        }

        private Bounds ToLocalBounds(Bounds worldBounds)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                        min = Vector3.Min(min, localPoint);
                        max = Vector3.Max(max, localPoint);
                    }
                }
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static int GetLongestAxis(Vector3 value)
        {
            if (value.x >= value.y && value.x >= value.z) return 0;
            if (value.y >= value.x && value.y >= value.z) return 1;
            return 2;
        }

        private static float GetSecondLongestSize(Vector3 value, int longestAxis)
        {
            if (longestAxis == 0) return Mathf.Max(value.y, value.z);
            if (longestAxis == 1) return Mathf.Max(value.x, value.z);
            return Mathf.Max(value.x, value.y);
        }

        private static float GetAverageOtherAxes(Vector3 value, int axis)
        {
            if (axis == 0) return (value.y + value.z) * 0.5f;
            if (axis == 1) return (value.x + value.z) * 0.5f;
            return (value.x + value.y) * 0.5f;
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            if (axis == 0) return value.x;
            if (axis == 1) return value.y;
            return value.z;
        }

        private static void SetAxis(ref Vector3 value, int axis, float axisValue)
        {
            if (axis == 0)
            {
                value.x = axisValue;
            }
            else if (axis == 1)
            {
                value.y = axisValue;
            }
            else
            {
                value.z = axisValue;
            }
        }
    }
}
