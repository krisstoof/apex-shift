using System.Collections.Generic;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.DayNight;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Time
{
    public sealed class DayNightRuntimeTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameEventBus.ClearForTests();
        }

        [Test]
        public void Tick_PublishesNightStartedEventWhenCrossingNightThreshold()
        {
            GameObject go = new GameObject("DayNightRuntimeTest");
            List<GameplayEvent> received = new List<GameplayEvent>();
            try
            {
                DayNightRuntime runtime = go.AddComponent<DayNightRuntime>();
                runtime.SetAutoTick(false);
                runtime.SetTickEcosystemOnDayChange(false);
                runtime.SetDayLengthSeconds(100f);
                runtime.LoadFromWorldSaveData(1, 0.82f);

                using (GameEventBus.Subscribe(received.Add))
                {
                    runtime.Tick(3f);
                }

                Assert.IsTrue(runtime.IsNight);
                Assert.AreEqual(1, received.Count);
                Assert.AreEqual(GameplayEventKind.NightStarted, received[0].kind);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Tick_IncrementsDayAndPublishesDayChangedWhenWrapping()
        {
            GameObject go = new GameObject("DayNightRuntimeTest");
            List<GameplayEvent> received = new List<GameplayEvent>();
            try
            {
                DayNightRuntime runtime = go.AddComponent<DayNightRuntime>();
                runtime.SetAutoTick(false);
                runtime.SetTickEcosystemOnDayChange(false);
                runtime.SetDayLengthSeconds(100f);
                runtime.LoadFromWorldSaveData(4, 0.99f);

                using (GameEventBus.Subscribe(received.Add))
                {
                    runtime.Tick(2f);
                }

                Assert.AreEqual(5, runtime.Day);
                Assert.IsTrue(received.Exists(evt => evt.kind == GameplayEventKind.DayChanged));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AdvanceHours_AdvancesClockWithoutUsingRealSeconds()
        {
            GameObject go = new GameObject("DayNightRuntimeTest");
            try
            {
                DayNightRuntime runtime = go.AddComponent<DayNightRuntime>();
                runtime.SetAutoTick(false);
                runtime.SetTickEcosystemOnDayChange(false);
                runtime.LoadFromWorldSaveData(2, 0.25f);

                runtime.AdvanceHours(3f);

                Assert.AreEqual(2, runtime.Day);
                Assert.AreEqual(9f, runtime.Hour, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AdvanceHours_WrapsDayAndPublishesDayChanged()
        {
            GameObject go = new GameObject("DayNightRuntimeTest");
            List<GameplayEvent> received = new List<GameplayEvent>();
            try
            {
                DayNightRuntime runtime = go.AddComponent<DayNightRuntime>();
                runtime.SetAutoTick(false);
                runtime.SetTickEcosystemOnDayChange(false);
                runtime.LoadFromWorldSaveData(7, 23f / 24f);

                using (GameEventBus.Subscribe(received.Add))
                {
                    runtime.AdvanceHours(8f);
                }

                Assert.AreEqual(8, runtime.Day);
                Assert.AreEqual(7f, runtime.Hour, 0.01f);
                Assert.IsTrue(received.Exists(evt => evt.kind == GameplayEventKind.DayChanged));
                Assert.IsTrue(received.Exists(evt => evt.kind == GameplayEventKind.MorningStarted));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
