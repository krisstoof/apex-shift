using ApexShift.Core.DayNight;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Time
{
    public sealed class DayNightStateTests
    {
        [Test]
        public void Advance_WrapsTimeAndIncrementsDay()
        {
            DayNightState state = new DayNightState(day: 2, timeOfDay01: 0.95f);

            DayNightTickResult result = state.Advance(0.10f);

            Assert.AreEqual(3, state.Day);
            Assert.AreEqual(0.05f, state.TimeOfDay01, 0.001f);
            Assert.AreEqual(1, result.daysAdvanced);
        }

        [Test]
        public void Advance_DetectsNightStartThreshold()
        {
            DayNightState state = new DayNightState(day: 1, timeOfDay01: 0.82f);

            DayNightTickResult result = state.Advance(0.03f);

            Assert.IsTrue(result.nightStarted);
            Assert.IsTrue(state.IsNight);
            Assert.AreEqual("Night", state.PhaseLabel);
        }

        [Test]
        public void Advance_DetectsMorningStartThreshold()
        {
            DayNightState state = new DayNightState(day: 1, timeOfDay01: 0.20f);

            DayNightTickResult result = state.Advance(0.07f);

            Assert.IsTrue(result.morningStarted);
            Assert.IsFalse(state.IsNight);
            Assert.AreEqual("Morning", state.PhaseLabel);
        }

        [Test]
        public void Set_NormalizesInvalidDayAndTime()
        {
            DayNightState state = new DayNightState(day: -4, timeOfDay01: 1.25f);

            Assert.AreEqual(1, state.Day);
            Assert.AreEqual(0.25f, state.TimeOfDay01, 0.001f);
            Assert.AreEqual(6f, state.Hour, 0.001f);
        }
    }
}
