using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    /// <summary>
    /// Sleep behaviour for the tent placeable. Interacting restores rest/stamina
    /// (plus a small health top-up) and advances the day/night cycle to the next
    /// morning through DayNightRuntime.Tick so day-change, morning and ecosystem
    /// events dispatch exactly as if the time had passed naturally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TentRestRuntime : MonoBehaviour
    {
        [SerializeField] private float restRestoreAmount = 100f;
        [SerializeField] private float staminaRestoreAmount = 100f;
        [SerializeField] private float healthRestoreAmount = 15f;
        [SerializeField] private bool skipToMorning = true;
        [SerializeField] private float sleepCooldownGameHours = 12f;

        public bool Interact(GameObject actor)
        {
            PlayerSurvivalRuntime survival = ResolveSurvival(actor);
            if (survival == null || survival.IsDead)
            {
                return false;
            }

            float hoursSinceLastSleep = CurrentTotalGameHours() - survival.LastSleepGameHours;
            if (hoursSinceLastSleep < Mathf.Max(0f, sleepCooldownGameHours))
            {
                Debug.Log($"[Tent] Sleep on cooldown: {sleepCooldownGameHours - hoursSinceLastSleep:0.0}h of in-game time remaining.", this);
                return false;
            }

            survival.RestoreRest(restRestoreAmount);
            survival.RestoreStamina(staminaRestoreAmount);
            if (healthRestoreAmount > 0f)
            {
                survival.Heal(healthRestoreAmount);
            }

            if (skipToMorning)
            {
                SkipToMorning();
            }

            survival.MarkSlept(CurrentTotalGameHours());
            Debug.Log($"[Tent] Player slept. rest={survival.Stats.Rest:0}, stamina={survival.Stats.Stamina:0}, health={survival.Stats.Health:0}.", this);
            return true;
        }

        private static float CurrentTotalGameHours()
        {
            DayNightRuntime dayNight = DayNightRuntime.Active;
            return dayNight != null ? dayNight.Day * 24f + dayNight.Hour : 0f;
        }

        private static void SkipToMorning()
        {
            DayNightRuntime dayNight = DayNightRuntime.Active;
            if (dayNight == null)
            {
                return;
            }

            float hoursToMorning = (dayNight.MorningStartHour - dayNight.Hour + 24f) % 24f;
            if (hoursToMorning < 0.05f)
            {
                hoursToMorning = 24f;
            }

            dayNight.Tick(hoursToMorning / 24f * dayNight.DayLengthSeconds);
        }

        private static PlayerSurvivalRuntime ResolveSurvival(GameObject actor)
        {
            if (actor == null)
            {
                return null;
            }

            PlayerSurvivalRuntime survival = actor.GetComponent<PlayerSurvivalRuntime>();
            if (survival == null)
            {
                survival = actor.GetComponentInParent<PlayerSurvivalRuntime>();
            }

            return survival;
        }
    }
}
