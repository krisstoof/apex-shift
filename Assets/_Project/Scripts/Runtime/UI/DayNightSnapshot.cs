using System;
using ApexShift.Runtime.DayNight;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class DayNightSnapshot
    {
        public int day;
        public float timeOfDay01;
        public float hour;
        public bool isNight;
        public float nightAmount;
        public string phaseLabel;

        public static DayNightSnapshot Empty => new DayNightSnapshot(1, 0f, 0f, true, 1f, "Night");

        public string ClockText => FormatClock(hour);

        public DayNightSnapshot(int day, float timeOfDay01, float hour, bool isNight, float nightAmount, string phaseLabel)
        {
            this.day = Math.Max(1, day);
            this.timeOfDay01 = Normalize01(timeOfDay01);
            this.hour = Math.Max(0f, Math.Min(24f, hour));
            this.isNight = isNight;
            this.nightAmount = Math.Max(0f, Math.Min(1f, nightAmount));
            this.phaseLabel = string.IsNullOrWhiteSpace(phaseLabel) ? (isNight ? "Night" : "Day") : phaseLabel.Trim();
        }

        public static DayNightSnapshot FromRuntime(DayNightRuntime runtime)
        {
            if (runtime == null)
            {
                return Empty;
            }

            return new DayNightSnapshot(runtime.Day, runtime.TimeOfDay01, runtime.Hour, runtime.IsNight, runtime.NightAmount, runtime.PhaseLabel);
        }

        private static string FormatClock(float hour)
        {
            int totalMinutes = (int)Math.Round(Math.Max(0f, Math.Min(24f, hour)) * 60f);
            if (totalMinutes >= 24 * 60)
            {
                totalMinutes = 0;
            }

            int hh = totalMinutes / 60;
            int mm = totalMinutes % 60;
            return $"{hh:00}:{mm:00}";
        }

        private static float Normalize01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            float normalized = value % 1f;
            return normalized < 0f ? normalized + 1f : normalized;
        }
    }
}
