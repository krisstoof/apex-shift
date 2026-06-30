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

        public void Configure(Text timeLabel, Image icon, DayNightRuntime runtime)
        {
            this.timeLabel = timeLabel;
            this.icon = icon;
            dayNightRuntime = runtime;
            Refresh();
        }

        private void Awake()
        {
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
            timeLabel.color = new Color(0.98f, 0.96f, 0.82f, 1f);
            timeLabel.text = snapshot.ClockText;

            if (icon != null)
            {
                icon.color = snapshot.isNight ? new Color(0.34f, 0.48f, 0.88f, 1f) : new Color(0.98f, 0.82f, 0.28f, 1f);
                icon.preserveAspect = true;
            }
        }
    }
}
