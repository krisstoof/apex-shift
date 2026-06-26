using UnityEngine;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MenuAmbientMotion : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 basePosition;
        private float amplitudeX;
        private float amplitudeY;
        private float speedX;
        private float speedY;

        public void Configure(float amplitudeX, float amplitudeY, float speedX, float speedY)
        {
            this.amplitudeX = amplitudeX;
            this.amplitudeY = amplitudeY;
            this.speedX = speedX;
            this.speedY = speedY;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                basePosition = rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            float t = Time.unscaledTime;
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Mathf.Sin(t * speedX) * amplitudeX,
                Mathf.Cos(t * speedY) * amplitudeY);
        }
    }
}
