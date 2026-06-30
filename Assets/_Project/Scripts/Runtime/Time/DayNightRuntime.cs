using System;
using ApexShift.Core.DayNight;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Events;
using UnityEngine;

namespace ApexShift.Runtime.DayNight
{
    [DisallowMultipleComponent]
    public sealed class DayNightRuntime : MonoBehaviour
    {
        [Header("Cycle")]
        [SerializeField] private float dayLengthSeconds = 120f;
        [SerializeField] private int startingDay = 1;
        [SerializeField, Range(0f, 1f)] private float startingTimeOfDay01 = 0.25f;
        [SerializeField] private float nightStartHour = DayNightState.DefaultNightStartHour;
        [SerializeField] private float morningStartHour = DayNightState.DefaultMorningStartHour;

        [Header("Runtime")]
        [SerializeField] private bool autoTick = true;
        [SerializeField] private bool tickEcosystemOnDayChange = true;

        private static DayNightRuntime instance;
        private DayNightState state;

        public static DayNightRuntime Active
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<DayNightRuntime>();
                }

                return instance;
            }
        }

        public static bool IsNightNow => Active != null && Active.IsNight;

        public event Action<int> DayChanged;
        public event Action NightStarted;
        public event Action MorningStarted;

        public int Day { get { EnsureState(); return state.Day; } }
        public float TimeOfDay01 { get { EnsureState(); return state.TimeOfDay01; } }
        public float Hour { get { EnsureState(); return state.Hour; } }
        public bool IsNight { get { EnsureState(); return state.IsNight; } }
        public float NightAmount { get { EnsureState(); return state.NightAmount; } }
        public string PhaseLabel { get { EnsureState(); return state.PhaseLabel; } }
        public float DayLengthSeconds => Mathf.Max(1f, dayLengthSeconds);
        public DayNightState StateSnapshot { get { EnsureState(); return state.Clone(); } }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            EnsureState();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (!autoTick)
            {
                return;
            }

            Tick(UnityEngine.Time.deltaTime);
        }

        public void Tick(float deltaSeconds)
        {
            EnsureState();
            if (deltaSeconds <= 0f)
            {
                return;
            }

            DayNightTickResult result = state.Advance(deltaSeconds / DayLengthSeconds);
            if (!result.HasAnyEvent)
            {
                return;
            }

            DispatchEvents(result);
        }

        public void LoadFromWorldSaveData(int day, float timeOfDay01)
        {
            SetTime(day, timeOfDay01);
        }

        public void SetTime(int day, float timeOfDay01)
        {
            EnsureState();
            state.Set(day, timeOfDay01, nightStartHour, morningStartHour);
        }

        public void SetAutoTick(bool enabled) => autoTick = enabled;
        public void SetDayLengthSeconds(float seconds) => dayLengthSeconds = Mathf.Max(1f, seconds);
        public void SetTickEcosystemOnDayChange(bool enabled) => tickEcosystemOnDayChange = enabled;

        private void EnsureState()
        {
            if (state != null)
            {
                return;
            }

            state = new DayNightState(startingDay, startingTimeOfDay01, nightStartHour, morningStartHour);
        }

        private void DispatchEvents(DayNightTickResult result)
        {
            if (result.daysAdvanced > 0)
            {
                GameEventBus.PublishDayChanged(state.Day);
                DayChanged?.Invoke(state.Day);

                if (tickEcosystemOnDayChange)
                {
                    EcosystemDirectorRuntime.Active?.TickDay(result.daysAdvanced);
                }
            }

            if (result.nightStarted)
            {
                GameEventBus.PublishNightStarted(state.Day, state.Hour);
                NightStarted?.Invoke();
            }

            if (result.morningStarted)
            {
                GameEventBus.PublishMorningStarted(state.Day, state.Hour);
                MorningStarted?.Invoke();
            }
        }
    }
}
