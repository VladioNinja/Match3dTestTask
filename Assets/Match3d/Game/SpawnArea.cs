using UnityEngine;

namespace Match3d.Game
{
    public sealed class SpawnArea : MonoBehaviour
    {
        [SerializeField] private Vector3 _size = new Vector3(8f, 3f, 8f);
        [SerializeField] private bool _drawGizmo = true;

        public Vector3 Size => _size;

        public Vector3 GetRandomPoint()
        {
            var halfSize = _size * 0.5f;

            return transform.position + new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                Random.Range(-halfSize.z, halfSize.z));
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmo)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.75f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _size);
        }
    }
}
