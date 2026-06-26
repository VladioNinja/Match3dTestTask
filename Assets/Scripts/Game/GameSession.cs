using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Match3d.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private TextMeshProUGUI counterLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private float durationSeconds = 120f;

        private readonly HashSet<CollectableItem> activeItems = new HashSet<CollectableItem>();
        private float timeLeft;
        private bool isRunning;

        public int RemainingItems => activeItems.Count;

        private void Start()
        {
            timeLeft = durationSeconds;
            isRunning = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (spawnManager != null)
            {
                spawnManager.Spawn();
            }

            RefreshUi();
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            timeLeft = Mathf.Max(0f, timeLeft - Time.deltaTime);

            if (timeLeft <= 0f)
            {
                FinishSession();
            }

            RefreshUi();
        }

        public void RegisterItem(CollectableItem item)
        {
            if (item == null || !activeItems.Add(item))
            {
                return;
            }

            item.Collected += HandleItemCollected;
            RefreshUi();
        }

        private void HandleItemCollected(CollectableItem item)
        {
            item.Collected -= HandleItemCollected;
            activeItems.Remove(item);

            if (RemainingItems == 0)
            {
                FinishSession();
            }

            RefreshUi();
        }

        private void FinishSession()
        {
            isRunning = false;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void RefreshUi()
        {
            if (counterLabel != null)
            {
                counterLabel.text = $"Items: {RemainingItems}";
            }

            if (timerLabel != null)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                timerLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }
    }
}
