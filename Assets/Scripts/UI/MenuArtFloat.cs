using UnityEngine;

namespace CourierRunner
{
    public sealed class MenuArtFloat : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 startPosition;

        private void Awake()
        {
            rect = (RectTransform)transform;
            startPosition = rect.anchoredPosition;
            rect.localScale = Vector3.one * 1.025f;
        }

        private void Update()
        {
            // A slow, subtle loop keeps the illustrated title feeling alive without making
            // buttons or text harder to read.
            rect.anchoredPosition = startPosition + Vector2.up * (Mathf.Sin(Time.unscaledTime * 1.15f) * 7f);
        }
    }
}
