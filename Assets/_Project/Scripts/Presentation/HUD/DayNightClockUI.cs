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
            if (timeLabel == null) return;

            DayNightSnapshot snapshot = DayNightSnapshot.FromRuntime(dayNightRuntime);

            timeLabel.alignment      = TextAnchor.MiddleCenter;
            timeLabel.supportRichText = false;
            timeLabel.enabled        = true;
            timeLabel.fontSize       = 14;
            timeLabel.raycastTarget  = false;
            // Night = cool blue-white, Day = warm cream
            timeLabel.color = snapshot.isNight
                ? new Color(0.78f, 0.88f, 1.00f, 1f)
                : new Color(0.98f, 0.94f, 0.72f, 1f);
            timeLabel.text = snapshot.ClockText;

            // Hide icon if one was accidentally assigned without a sprite
            if (icon != null) icon.enabled = icon.sprite != null;

            // Keep panel width just enough for the text; no dynamic resizing that
            // could accidentally expose a gap below the clock.
            if (panelRoot != null)
            {
                float w = Mathf.Clamp(timeLabel.preferredWidth + 16f, 72f, 106f);
                panelRoot.sizeDelta = new Vector2(w, panelRoot.sizeDelta.y);
            }
        }
    }
}
