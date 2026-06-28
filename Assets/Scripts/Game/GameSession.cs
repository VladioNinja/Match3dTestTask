using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3d.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private TextMeshProUGUI counterLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverLabel;
        [SerializeField] private Button restartButton;

        private readonly HashSet<CollectableItem> activeItems = new HashSet<CollectableItem>();
        private float elapsedSeconds;
        private bool isRunning;

        public int RemainingItems => activeItems.Count;
        public float ElapsedSeconds => elapsedSeconds;

        private void Start()
        {
            elapsedSeconds = 0f;
            isRunning = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartSession);
            }

            if (spawnManager != null)
            {
                spawnManager.Spawn();
            }

            RefreshUi();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartSession);
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
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
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            int completedSeconds = Mathf.CeilToInt(elapsedSeconds);

            if (gameOverLabel != null)
            {
                gameOverLabel.text = $"Game Over\nTime: {completedSeconds}s";
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        public void RestartSession()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void RefreshUi()
        {
            if (counterLabel != null)
            {
                counterLabel.text = $"Stars: {RemainingItems}";
            }

            if (timerLabel != null)
            {
                int seconds = Mathf.FloorToInt(elapsedSeconds);
                timerLabel.text = $"{seconds}s";
            }
        }
    }
}
