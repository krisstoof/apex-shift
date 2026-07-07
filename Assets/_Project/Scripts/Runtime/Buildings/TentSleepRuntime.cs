using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class TentSleepRuntime : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float sleepInteractionDuration = 1.2f;
        [SerializeField] private float maxUseDistance = 3.0f;
        [SerializeField] private string sleepPrompt = "Sleep in tent";

        [Header("Recovery")]
        [SerializeField, Range(0f, 100f)] private float minimumRestAfterSleep = 80f;
        [SerializeField, Range(0f, 100f)] private float minimumStaminaAfterSleep = 70f;
        [SerializeField, Range(0f, 100f)] private float hungerCost = 14f;
        [SerializeField, Range(0f, 100f)] private float minimumHungerToSleep = 18f;
        [SerializeField, Range(0f, 25f)] private float healthBonus = 2f;

        [Header("Time Skip")]
        [SerializeField] private bool sleepUntilMorningAtNight = true;
        [SerializeField, Range(0.5f, 8f)] private float daytimeNapHours = 3f;
        [SerializeField, Range(0f, 24f)] private float wakeUpHour = 6.25f;

        [Header("Safety")]
        [SerializeField] private bool blockSleepNearVarnak = true;
        [SerializeField] private float unsafeVarnakRadius = 34f;

        public string Prompt => sleepPrompt;
        public float InteractionDuration => Mathf.Max(0.1f, sleepInteractionDuration);

        public bool CanInteract(GameObject actor)
        {
            if (actor == null || !isActiveAndEnabled)
            {
                return false;
            }

            if (!IsActorCloseEnough(actor))
            {
                return false;
            }

            PlayerSurvivalRuntime survival = actor.GetComponent<PlayerSurvivalRuntime>();
            return survival == null || !survival.IsDead;
        }

        public TentSleepResult TrySleep(GameObject actor)
        {
            if (actor == null)
            {
                return Report(TentSleepResult.Failed("Cannot sleep: missing player."), actor);
            }

            if (!IsActorCloseEnough(actor))
            {
                return Report(TentSleepResult.Failed("You are too far from the tent."), actor);
            }

            PlayerSurvivalRuntime survival = actor.GetComponent<PlayerSurvivalRuntime>();
            if (survival == null || survival.Stats == null)
            {
                return Report(TentSleepResult.Failed("Cannot sleep: survival state is missing."), actor);
            }

            if (survival.IsDead)
            {
                return Report(TentSleepResult.Failed("Cannot sleep after death."), actor);
            }

            if (survival.Stats.Hunger <= Mathf.Max(0f, minimumHungerToSleep))
            {
                return Report(TentSleepResult.Failed("You are too hungry to sleep."), actor);
            }

            if (IsThreatenedByVarnak(actor.transform.position))
            {
                return Report(TentSleepResult.Failed("Cannot sleep while threatened."), actor);
            }

            DayNightRuntime dayNight = DayNightRuntime.Active;
            float hoursAdvanced = ResolveSleepHours(dayNight);
            TentSleepResult result = survival.ApplySleepRecovery(minimumRestAfterSleep, minimumStaminaAfterSleep, hungerCost, healthBonus, hoursAdvanced);

            if (dayNight != null && hoursAdvanced > 0f)
            {
                dayNight.AdvanceHours(hoursAdvanced);
            }

            string message = result.Success
                ? $"Slept for {hoursAdvanced:0.0}h. Rest +{result.RestDelta:0}, stamina +{result.StaminaDelta:0}, hunger {result.HungerDelta:0}."
                : result.Message;

            return Report(new TentSleepResult(result.Success, message, hoursAdvanced, result.RestDelta, result.StaminaDelta, result.HungerDelta, result.HealthDelta), actor);
        }

        private float ResolveSleepHours(DayNightRuntime dayNight)
        {
            if (dayNight == null)
            {
                return Mathf.Max(0.5f, daytimeNapHours);
            }

            float currentHour = dayNight.Hour;
            if (sleepUntilMorningAtNight && dayNight.IsNight)
            {
                float targetHour = Mathf.Clamp(wakeUpHour, 0f, 24f);
                float delta = targetHour - currentHour;
                if (delta <= 0f)
                {
                    delta += 24f;
                }

                return Mathf.Clamp(delta, 1f, 12f);
            }

            return Mathf.Max(0.5f, daytimeNapHours);
        }

        private bool IsThreatenedByVarnak(Vector3 position)
        {
            if (!blockSleepNearVarnak)
            {
                return false;
            }

            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
            if (ecosystem == null)
            {
                return false;
            }

            CreatureAgentView varnak = ecosystem.TryFindNearestCreatureById(position, "varnak", Mathf.Max(0f, unsafeVarnakRadius));
            return varnak != null;
        }

        private bool IsActorCloseEnough(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            float maxDistance = Mathf.Max(0.25f, maxUseDistance);
            return (actor.transform.position - transform.position).sqrMagnitude <= maxDistance * maxDistance;
        }

        private TentSleepResult Report(TentSleepResult result, GameObject actor)
        {
            Debug.Log($"[TentSleep] {result.Message}", this);

            PlayerActionFeedback feedback = actor != null ? actor.GetComponent<PlayerActionFeedback>() : null;
            if (feedback != null)
            {
                feedback.ShowMessage(result.Message, result.Success ? new Color(0.35f, 0.85f, 1f) : new Color(1f, 0.55f, 0.35f));
            }

            return result;
        }
    }
}
