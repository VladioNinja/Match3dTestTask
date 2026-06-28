using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3d.Game
{
    public sealed class CollectFeedbackController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform starCounterTarget;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Color starColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private float flightDuration = 0.55f;
        [SerializeField] private float startSize = 64f;
        [SerializeField] private float endSize = 26f;
        [SerializeField] private bool playAudio = true;

        private RectTransform canvasRect;
        private Sprite starSprite;
        private AudioClip collectClip;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (canvas != null)
            {
                canvasRect = canvas.transform as RectTransform;
            }

            starSprite = CreateStarSprite();
            collectClip = CreateCollectClip();
        }

        public void Play(Vector3 worldPosition)
        {
            if (canvasRect == null || starCounterTarget == null)
            {
                return;
            }

            if (playAudio && audioSource != null && collectClip != null)
            {
                audioSource.PlayOneShot(collectClip, 0.45f);
            }

            StartCoroutine(PlayStarFlight(worldPosition));
        }

        private IEnumerator PlayStarFlight(Vector3 worldPosition)
        {
            GameObject starObject = new GameObject("FlyingStar", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            starObject.transform.SetParent(canvasRect, false);

            RectTransform rectTransform = starObject.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = starObject.GetComponent<CanvasGroup>();
            Image image = starObject.GetComponent<Image>();

            image.sprite = starSprite;
            image.color = starColor;
            image.raycastTarget = false;

            Vector2 start = WorldToCanvasPoint(worldPosition);
            Vector2 end = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), starCounterTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, end, GetUiCamera(), out end);

            rectTransform.anchoredPosition = start;
            rectTransform.sizeDelta = Vector2.one * startSize;

            float elapsed = 0f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float arc = Mathf.Sin(t * Mathf.PI) * 90f;

                rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, eased) + Vector2.up * arc;
                rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, eased);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 220f, eased));
                canvasGroup.alpha = t < 0.82f ? 1f : Mathf.InverseLerp(1f, 0.82f, t);

                yield return null;
            }

            Destroy(starObject);
        }

        private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, GetUiCamera(), out Vector2 localPoint);
            return localPoint;
        }

        private Camera GetUiCamera()
        {
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }

        private static Sprite CreateStarSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outerRadius = size * 0.47f;
            float innerRadius = size * 0.22f;

            Vector2[] points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? outerRadius : innerRadius;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    texture.SetPixel(x, y, IsPointInPolygon(point, points) ? Color.white : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool intersects = polygon[i].y > point.y != polygon[j].y > point.y
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;

                if (intersects)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static AudioClip CreateCollectClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.22f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float frequency = Mathf.Lerp(720f, 1320f, normalized);
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.32f;
            }

            AudioClip clip = AudioClip.Create("CollectChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
