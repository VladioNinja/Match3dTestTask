using System.Collections.Generic;
using UnityEngine;

namespace Match3d.Game
{
    public sealed class SpawnManager : MonoBehaviour
    {
        [SerializeField] private CollectableItem[] _itemPrefabs;
        [SerializeField] private SpawnArea _spawnArea;
        [SerializeField] private Transform _itemsRoot;
        [SerializeField] private GameSession _gameSession;
        [SerializeField] private int _itemCount = 12;

        private readonly List<CollectableItem> _spawnedItems = new List<CollectableItem>();

        public IReadOnlyList<CollectableItem> SpawnedItems => _spawnedItems;

        public void Spawn()
        {
            ClearSpawnedItems();

            if (_itemPrefabs == null || _itemPrefabs.Length == 0 || _spawnArea == null)
            {
                Debug.LogWarning("SpawnManager is missing item prefabs or spawn area.", this);
                return;
            }

            for (var i = 0; i < _itemCount; i++)
            {
                var prefab = _itemPrefabs[Random.Range(0, _itemPrefabs.Length)];
                var position = _spawnArea.GetRandomPoint();
                var rotation = Random.rotation;
                var item = Instantiate(prefab, position, rotation, _itemsRoot);

                _spawnedItems.Add(item);

                if (_gameSession != null)
                {
                    _gameSession.RegisterItem(item);
                }
            }
        }

        public void ClearSpawnedItems()
        {
            for (var i = _spawnedItems.Count - 1; i >= 0; i--)
            {
                if (_spawnedItems[i] != null)
                {
                    Destroy(_spawnedItems[i].gameObject);
                }
            }

            _spawnedItems.Clear();
        }
    }
}
