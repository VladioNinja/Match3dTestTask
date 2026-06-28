using UnityEngine;

namespace Match3d.Game
{
    [CreateAssetMenu(menuName = "Match3D/Collectable Item Settings")]
    public sealed class CollectableItemSettings : ScriptableObject
    {
        [Header("Physics")]
        [SerializeField] private PhysicMaterial _physicsMaterial;
        [SerializeField] private float _mass = 1.15f;
        [SerializeField] private float _linearDrag = 4f;
        [SerializeField] private float _angularDrag = 8f;
        [SerializeField] private float _maxDropVelocity = 1.4f;
        [SerializeField] private float _maxDropAngularVelocity = 4f;
        [SerializeField] private float _maxDepenetrationVelocity = 1.6f;
        [SerializeField] private float _maxPushedVelocity = 1.75f;
        [SerializeField] private float _maxPushedAngularVelocity = 4f;
        [SerializeField] private bool _disableCollisionWhileDragging;

        [Header("Collision Sound")]
        [SerializeField] private AudioClip _collisionClip;
        [SerializeField] private float _minCollisionImpulseForSound = 0.35f;
        [SerializeField] private float _collisionSoundCooldown = 0.08f;
        [SerializeField] private float _collisionSoundVolume = 0.28f;

        public PhysicMaterial PhysicsMaterial => _physicsMaterial;
        public float Mass => _mass;
        public float LinearDrag => _linearDrag;
        public float AngularDrag => _angularDrag;
        public float MaxDropVelocity => _maxDropVelocity;
        public float MaxDropAngularVelocity => _maxDropAngularVelocity;
        public float MaxDepenetrationVelocity => _maxDepenetrationVelocity;
        public float MaxPushedVelocity => _maxPushedVelocity;
        public float MaxPushedAngularVelocity => _maxPushedAngularVelocity;
        public bool DisableCollisionWhileDragging => _disableCollisionWhileDragging;
        public AudioClip CollisionClip => _collisionClip;
        public float MinCollisionImpulseForSound => _minCollisionImpulseForSound;
        public float CollisionSoundCooldown => _collisionSoundCooldown;
        public float CollisionSoundVolume => _collisionSoundVolume;
    }
}
