using System;

namespace ApexShift.Core.DayNight
{
    [Serializable]
    public sealed class DayNightState
    {
        public const float DefaultMorningStartHour = 6f;
        public const float DefaultNightStartHour = 20f;

        public int day = 1;
        public float timeOfDay01 = 0.25f;
        public float nightStartHour = DefaultNightStartHour;
        public float morningStartHour = DefaultMorningStartHour;

        public int Day => Math.Max(1, day);
        public float TimeOfDay01 => Normalize01(timeOfDay01);
        public float Hour => TimeOfDay01 * 24f;
        public float NightStartHour => ClampHour(nightStartHour);
        public float MorningStartHour => ClampHour(morningStartHour);
        public bool IsNight => IsNightAtHour(Hour, NightStartHour, MorningStartHour);
        public float NightAmount => IsNight ? 1f : 0f;
        public string PhaseLabel => GetPhaseLabel(Hour, IsNight);

        public DayNightState()
        {
        }

        public DayNightState(int day, float timeOfDay01, float nightStartHour = DefaultNightStartHour, float morningStartHour = DefaultMorningStartHour)
        {
            Set(day, timeOfDay01, nightStartHour, morningStartHour);
        }

        public void Set(int day, float timeOfDay01, float nightStartHour = DefaultNightStartHour, float morningStartHour = DefaultMorningStartHour)
        {
            this.day = Math.Max(1, day);
            this.timeOfDay01 = Normalize01(timeOfDay01);
            this.nightStartHour = ClampHour(nightStartHour);
            this.morningStartHour = ClampHour(morningStartHour);
        }

        public DayNightTickResult Advance(float normalizedDayDelta)
        {
            float safeDelta = Math.Max(0f, normalizedDayDelta);
            if (safeDelta <= 0f)
            {
                return new DayNightTickResult(0, false, false, TimeOfDay01, TimeOfDay01);
            }

            float previousTime = TimeOfDay01;
            float totalTime = previousTime + safeDelta;
            int daysAdvanced = (int)Math.Floor(totalTime);
            float nextTime = Normalize01(totalTime);

            bool nightStarted = CrossedNormalizedThreshold(previousTime, nextTime, NightStartHour / 24f, daysAdvanced);
            bool morningStarted = CrossedNormalizedThreshold(previousTime, nextTime, MorningStartHour / 24f, daysAdvanced);

            day = Math.Max(1, day + daysAdvanced);
            timeOfDay01 = nextTime;

            return new DayNightTickResult(daysAdvanced, nightStarted, morningStarted, previousTime, nextTime);
        }

        public DayNightState Clone()
        {
            return new DayNightState(Day, TimeOfDay01, NightStartHour, MorningStartHour);
        }

        public static bool IsNightAtHour(float hour, float nightStartHour = DefaultNightStartHour, float morningStartHour = DefaultMorningStartHour)
        {
            float h = ClampHour(hour);
            float night = ClampHour(nightStartHour);
            float morning = ClampHour(morningStartHour);

            if (Math.Abs(night - morning) < 0.001f)
            {
                return false;
            }

            if (night > morning)
            {
                return h >= night || h < morning;
            }

            return h >= night && h < morning;
        }

        public static string GetPhaseLabel(float hour, bool isNight)
        {
            if (isNight)
            {
                return "Night";
            }

            float h = ClampHour(hour);
            if (h < 12f)
            {
                return "Morning";
            }

            if (h < 18f)
            {
                return "Afternoon";
            }

            return "Evening";
        }

        private static bool CrossedNormalizedThreshold(float previous, float current, float threshold, int wraps)
        {
            float from = Normalize01(previous);
            float to = Normalize01(current);
            float target = Normalize01(threshold);

            if (wraps <= 0)
            {
                return from < target && to >= target;
            }

            if (from < target)
            {
                return true;
            }

            if (to >= target)
            {
                return true;
            }

            return wraps > 1;
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

        private static float ClampHour(float hour)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour))
            {
                return 0f;
            }

            return Math.Max(0f, Math.Min(24f, hour));
        }
    }

    public struct DayNightTickResult
    {
        public readonly int daysAdvanced;
        public readonly bool nightStarted;
        public readonly bool morningStarted;
        public readonly float previousTimeOfDay01;
        public readonly float currentTimeOfDay01;

        public bool HasAnyEvent => daysAdvanced > 0 || nightStarted || morningStarted;

        public DayNightTickResult(int daysAdvanced, bool nightStarted, bool morningStarted, float previousTimeOfDay01, float currentTimeOfDay01)
        {
            this.daysAdvanced = Math.Max(0, daysAdvanced);
            this.nightStarted = nightStarted;
            this.morningStarted = morningStarted;
            this.previousTimeOfDay01 = previousTimeOfDay01;
            this.currentTimeOfDay01 = currentTimeOfDay01;
        }
    }
}
