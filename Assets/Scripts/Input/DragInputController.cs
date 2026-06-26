using Match3d.Game;
using UnityEngine;

namespace Match3d.Input
{
    public sealed class DragInputController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask itemLayerMask;
        [SerializeField] private SpawnArea dragBounds;
        [SerializeField] private float dragHeightOffset = 0.2f;

        private CollectableItem selectedItem;
        private Plane dragPlane;
        private Vector3 grabOffset;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (targetCamera == null)
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
                EndDrag();
            }
        }

        private void TryBeginDrag(Vector3 pointerPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, itemLayerMask))
            {
                return;
            }

            selectedItem = hit.collider.GetComponentInParent<CollectableItem>();

            if (selectedItem == null || selectedItem.IsCollected)
            {
                selectedItem = null;
                return;
            }

            selectedItem.BeginDrag();
            dragPlane = new Plane(Vector3.up, selectedItem.transform.position + Vector3.up * dragHeightOffset);
            grabOffset = selectedItem.transform.position - hit.point;
        }

        private void ContinueDrag(Vector3 pointerPosition)
        {
            if (selectedItem == null)
            {
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

            if (!dragPlane.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 targetPosition = ray.GetPoint(enter) + grabOffset;
            targetPosition = ClampToDragBounds(targetPosition);
            selectedItem.Rigidbody.MovePosition(targetPosition);
        }

        private void EndDrag()
        {
            if (selectedItem == null)
            {
                return;
            }

            selectedItem.EndDrag();
            selectedItem = null;
        }

        private Vector3 ClampToDragBounds(Vector3 targetPosition)
        {
            if (dragBounds == null || selectedItem == null || selectedItem.Collider == null)
            {
                return targetPosition;
            }

            Bounds itemBounds = selectedItem.Collider.bounds;
            Vector3 center = dragBounds.transform.position;
            Vector3 halfSize = dragBounds.Size * 0.5f;
            Vector3 extents = itemBounds.extents;

            float minX = center.x - halfSize.x + extents.x;
            float maxX = center.x + halfSize.x - extents.x;
            float minZ = center.z - halfSize.z + extents.z;
            float maxZ = center.z + halfSize.z - extents.z;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            return targetPosition;
        }
    }
}
