using UnityEngine;
using UnityEngine.EventSystems;

namespace CourierRunner
{
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private bool hovered;
        private bool pressed;
        private float phase;

        private void Awake() => phase = Random.Range(0f, 7f);

        private void Update()
        {
            float breathing = 1f + Mathf.Sin(Time.unscaledTime * 1.8f + phase) * 0.008f;
            float interactionScale = pressed ? 0.955f : hovered ? 1.035f : 1f;
            float target = breathing * interactionScale;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target,
                                                12f * Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
    }
}
