using System.Collections.Generic;
using UnityEngine;

namespace Match3d.Game
{
    public sealed class SpawnManager : MonoBehaviour
    {
        [SerializeField] private CollectableItem[] itemPrefabs;
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private Transform itemsRoot;
        [SerializeField] private GameSession gameSession;
        [SerializeField] private int itemCount = 12;

        private readonly List<CollectableItem> spawnedItems = new List<CollectableItem>();

        public IReadOnlyList<CollectableItem> SpawnedItems => spawnedItems;

        public void Spawn()
        {
            ClearSpawnedItems();

            if (itemPrefabs == null || itemPrefabs.Length == 0 || spawnArea == null)
            {
                Debug.LogWarning("SpawnManager is missing item prefabs or spawn area.", this);
                return;
            }

            for (int i = 0; i < itemCount; i++)
            {
                CollectableItem prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
                Vector3 position = spawnArea.GetRandomPoint();
                Quaternion rotation = Random.rotation;
                CollectableItem item = Instantiate(prefab, position, rotation, itemsRoot);

                spawnedItems.Add(item);

                if (gameSession != null)
                {
                    gameSession.RegisterItem(item);
                }
            }
        }

        public void ClearSpawnedItems()
        {
            for (int i = spawnedItems.Count - 1; i >= 0; i--)
            {
                if (spawnedItems[i] != null)
                {
                    Destroy(spawnedItems[i].gameObject);
                }
            }

            spawnedItems.Clear();
        }
    }
}
