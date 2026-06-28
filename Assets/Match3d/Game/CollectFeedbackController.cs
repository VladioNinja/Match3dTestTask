using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Match3d.Game
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CollectFeedbackController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private RectTransform _starCounterTarget;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Color _starColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private float _flightDuration = 0.55f;
        [SerializeField] private float _startSize = 64f;
        [SerializeField] private float _endSize = 26f;
        [SerializeField] private bool _playAudio = true;
        [SerializeField] private AudioClip _collectClip;
        [SerializeField] private Sprite _starSprite;
        [SerializeField] private int _poolPrewarmCount = 4;

        private readonly Queue<GameObject> _starPool = new Queue<GameObject>();
        private RectTransform _canvasRect;
        private Sprite _fallbackSprite;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_canvas != null)
            {
                _canvasRect = _canvas.transform as RectTransform;
            }

            PrewarmPool();
        }

        public void Play(Vector3 worldPosition)
        {
            if (_canvasRect == null || _starCounterTarget == null)
            {
                return;
            }

            if (_playAudio && _audioSource != null && _collectClip != null)
            {
                _audioSource.PlayOneShot(_collectClip, 0.45f);
            }

            StartCoroutine(PlayStarFlight(worldPosition));
        }

        private IEnumerator PlayStarFlight(Vector3 worldPosition)
        {
            var starObject = GetStarObject();
            var rectTransform = starObject.GetComponent<RectTransform>();
            var canvasGroup = starObject.GetComponent<CanvasGroup>();
            var image = starObject.GetComponent<Image>();

            image.sprite = GetStarSprite();
            image.color = _starColor;
            image.raycastTarget = false;
            canvasGroup.alpha = 1f;

            var start = WorldToCanvasPoint(worldPosition);
            var end = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), _starCounterTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, end, GetUiCamera(), out end);

            rectTransform.anchoredPosition = start;
            rectTransform.sizeDelta = Vector2.one * _startSize;

            var elapsed = 0f;

            while (elapsed < _flightDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _flightDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var arc = Mathf.Sin(t * Mathf.PI) * 90f;

                rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, eased) + Vector2.up * arc;
                rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(_startSize, _endSize, eased);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 220f, eased));
                canvasGroup.alpha = t < 0.82f ? 1f : Mathf.InverseLerp(1f, 0.82f, t);

                yield return null;
            }

            ReleaseStarObject(starObject);
        }

        private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(_worldCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, GetUiCamera(), out var localPoint);
            return localPoint;
        }

        private Camera GetUiCamera()
        {
            return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        }

        private Sprite GetStarSprite()
        {
            if (_starSprite != null)
            {
                return _starSprite;
            }

            if (_fallbackSprite == null)
            {
                _fallbackSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            }

            return _fallbackSprite;
        }

        private void PrewarmPool()
        {
            if (_canvasRect == null)
            {
                return;
            }

            for (var i = 0; i < Mathf.Max(0, _poolPrewarmCount); i++)
            {
                ReleaseStarObject(CreateStarObject());
            }
        }

        private GameObject GetStarObject()
        {
            var starObject = _starPool.Count > 0 ? _starPool.Dequeue() : CreateStarObject();
            starObject.SetActive(true);
            return starObject;
        }

        private GameObject CreateStarObject()
        {
            var starObject = new GameObject("FlyingStar", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            starObject.transform.SetParent(_canvasRect, false);
            return starObject;
        }

        private void ReleaseStarObject(GameObject starObject)
        {
            starObject.SetActive(false);
            starObject.transform.SetParent(_canvasRect, false);
            _starPool.Enqueue(starObject);
        }
    }
}
