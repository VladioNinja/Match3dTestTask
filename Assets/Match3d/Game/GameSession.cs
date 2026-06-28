using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3d.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private SpawnManager _spawnManager;
        [SerializeField] private TextMeshProUGUI _counterLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _gameOverLabel;
        [SerializeField] private Button _restartButton;

        private readonly HashSet<CollectableItem> _activeItems = new HashSet<CollectableItem>();
        private float _elapsedSeconds;
        private bool _isRunning;
        private int _lastDisplayedRemaining = -1;
        private int _lastDisplayedSeconds = -1;

        public int RemainingItems => _activeItems.Count;
        public float ElapsedSeconds => _elapsedSeconds;

        private void Start()
        {
            _elapsedSeconds = 0f;
            _isRunning = true;

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartSession);
            }

            if (_spawnManager != null)
            {
                _spawnManager.Spawn();
            }

            RefreshUi(true);
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartSession);
            }
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _elapsedSeconds += Time.deltaTime;
            RefreshUi(false);
        }

        public void RegisterItem(CollectableItem item)
        {
            if (item == null || !_activeItems.Add(item))
            {
                return;
            }

            item.Collected += HandleItemCollected;
            RefreshUi(true);
        }

        private void HandleItemCollected(CollectableItem item)
        {
            item.Collected -= HandleItemCollected;
            _activeItems.Remove(item);

            if (RemainingItems == 0)
            {
                FinishSession();
            }

            RefreshUi(true);
        }

        private void FinishSession()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            var completedSeconds = Mathf.CeilToInt(_elapsedSeconds);

            if (_gameOverLabel != null)
            {
                _gameOverLabel.text = $"Game Over\nTime: {completedSeconds}s";
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }
        }

        public void RestartSession()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void RefreshUi(bool force)
        {
            if (_counterLabel != null && (force || _lastDisplayedRemaining != RemainingItems))
            {
                _lastDisplayedRemaining = RemainingItems;
                _counterLabel.text = $"Stars: {RemainingItems}";
            }

            var seconds = Mathf.FloorToInt(_elapsedSeconds);

            if (_timerLabel != null && (force || _lastDisplayedSeconds != seconds))
            {
                _lastDisplayedSeconds = seconds;
                _timerLabel.text = $"{seconds}s";
            }
        }
    }
}
