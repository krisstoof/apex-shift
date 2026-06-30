using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.UI.Snapshots;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class DayNightClockUI : MonoBehaviour
    {
        [SerializeField] private Text timeLabel;
        [SerializeField] private Image icon;
        [SerializeField] private DayNightRuntime dayNightRuntime;
        [SerializeField] private RectTransform panelRoot;

        public void Configure(Text timeLabel, Image icon, DayNightRuntime runtime)
        {
            this.timeLabel = timeLabel;
            this.icon = icon;
            dayNightRuntime = runtime;
            Refresh();
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = GetComponent<RectTransform>();
            }

            if (dayNightRuntime == null)
            {
                dayNightRuntime = UnityEngine.Object.FindAnyObjectByType<DayNightRuntime>();
            }

            if (timeLabel == null)
            {
                timeLabel = GetComponent<Text>();
            }

            if (icon == null)
            {
                icon = GetComponent<Image>();
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (timeLabel == null)
            {
                return;
            }

            DayNightSnapshot snapshot = DayNightSnapshot.FromRuntime(dayNightRuntime);
            timeLabel.alignment = TextAnchor.MiddleLeft;
            timeLabel.supportRichText = false;
            timeLabel.enabled = true;
            timeLabel.fontSize = 14;
            timeLabel.raycastTarget = false;
            timeLabel.color = new Color(0.98f, 0.96f, 0.82f, 1f);
            timeLabel.text = snapshot.ClockText;

            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.color = snapshot.isNight ? new Color(0.34f, 0.48f, 0.88f, 1f) : new Color(0.98f, 0.82f, 0.28f, 1f);
                icon.preserveAspect = true;
            }

            if (panelRoot != null)
            {
                float iconWidth = icon != null ? 22f : 8f;
                float preferredWidth = Mathf.Clamp(iconWidth + timeLabel.preferredWidth + 12f, 76f, 96f);
                Vector2 size = panelRoot.sizeDelta;
                panelRoot.sizeDelta = new Vector2(preferredWidth, Mathf.Max(24f, size.y));
            }
        }
    }
}
