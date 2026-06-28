using Match3d.Game;
using UnityEngine;

namespace Match3d.Input
{
    public sealed class DragInputController : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private LayerMask _itemLayerMask;
        [SerializeField] private SpawnArea _dragBounds;
        [SerializeField] private float _dragHeightOffset = 0.2f;

        private CollectableItem _selectedItem;
        private Plane _dragPlane;
        private Vector3 _grabOffset;
        private Vector3 _targetPosition;
        private bool _hasTargetPosition;
        private bool _releaseRequested;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (_targetCamera == null)
            {
                return;
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                TryBeginDrag(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                ContinueDrag(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                RequestEndDrag();
            }
        }

        private void TryBeginDrag(Vector3 pointerPosition)
        {
            var ray = _targetCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, _itemLayerMask))
            {
                return;
            }

            _selectedItem = hit.collider.GetComponentInParent<CollectableItem>();

            if (_selectedItem == null || _selectedItem.IsCollected)
            {
                _selectedItem = null;
                return;
            }

            _selectedItem.BeginDrag();
            // Keep the dragged item on a stable horizontal plane so physics objects do not jump toward the camera ray.
            _dragPlane = new Plane(Vector3.up, _selectedItem.transform.position + Vector3.up * _dragHeightOffset);
            _grabOffset = _selectedItem.transform.position - hit.point;
            _targetPosition = _selectedItem.transform.position;
            _hasTargetPosition = true;
            _releaseRequested = false;
        }

        private void ContinueDrag(Vector3 pointerPosition)
        {
            if (_selectedItem == null)
            {
                return;
            }

            var ray = _targetCamera.ScreenPointToRay(pointerPosition);

            if (!_dragPlane.Raycast(ray, out var enter))
            {
                return;
            }

            var targetPosition = ray.GetPoint(enter) + _grabOffset;
            _targetPosition = ClampToDragBounds(targetPosition);
            _hasTargetPosition = true;
        }

        private void FixedUpdate()
        {
            if (_selectedItem == null)
            {
                return;
            }

            if (_hasTargetPosition)
            {
                // MovePosition keeps dragging in the physics step, which plays nicer with Rigidbody collisions.
                _selectedItem.Rigidbody.MovePosition(_targetPosition);
            }

            if (_releaseRequested)
            {
                CompleteDrag();
            }
        }

        private void RequestEndDrag()
        {
            if (_selectedItem == null)
            {
                return;
            }

            _releaseRequested = true;
        }

        private void CompleteDrag()
        {
            _selectedItem.EndDrag();
            _selectedItem = null;
            _hasTargetPosition = false;
            _releaseRequested = false;
        }

        private Vector3 ClampToDragBounds(Vector3 targetPosition)
        {
            if (_dragBounds == null || _selectedItem == null)
            {
                return targetPosition;
            }

            var itemBounds = _selectedItem.CollisionBounds;
            var center = _dragBounds.transform.position;
            var halfSize = _dragBounds.Size * 0.5f;
            var extents = itemBounds.extents;

            var minX = center.x - halfSize.x + extents.x;
            var maxX = center.x + halfSize.x - extents.x;
            var minZ = center.z - halfSize.z + extents.z;
            var maxZ = center.z + halfSize.z - extents.z;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            return targetPosition;
        }
    }
}
