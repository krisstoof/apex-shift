using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class FpsCounterUI : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float refreshInterval = 0.25f;

        private float timer;
        private int frames;
        private float accumulatedTime;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponent<Text>();
            }
        }

        private void Update()
        {
            frames++;
            accumulatedTime += Time.unscaledDeltaTime;
            timer += Time.unscaledDeltaTime;

            if (timer < refreshInterval)
            {
                return;
            }

            float fps = frames / Mathf.Max(0.0001f, accumulatedTime);
            if (label != null)
            {
                label.text = $"FPS {fps:0}";
            }

            timer = 0f;
            frames = 0;
            accumulatedTime = 0f;
        }
    }
}
