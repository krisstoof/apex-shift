using System;
using ApexShift.Core.Survival;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerSurvivalRuntime : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private float startingHealth = 100f;

        [SerializeField]
        private float startingHunger = 100f;

        [SerializeField]
        private float startingStamina = 100f;

        [SerializeField]
        private float startingRest = 100f;

        [SerializeField]
        private bool logToConsole;

        [Header("Stamina Movement Penalty")]
        [SerializeField] private float tiredStaminaThreshold = 25f;
        [SerializeField] private float exhaustedStaminaThreshold = 7.5f;
        [SerializeField] private float tiredSpeedMultiplier = 0.78f;
        [SerializeField] private float exhaustedSpeedMultiplier = 0.48f;
        [SerializeField] private float noStaminaSpeedMultiplier = 0.35f;

        [Header("Fatigue / Exhaustion")]
        [SerializeField] private float fatigueRestThreshold = 20f;
        [SerializeField] private float exhaustionRestThreshold = 5f;
        [SerializeField] private float lowHungerFatigueThreshold = 15f;
        [SerializeField] private float exhaustedHealthDamagePerSecond = 0.35f;
        [SerializeField] private float exhaustedStaminaDrainPerSecond = 2f;

        [Header("Death")]
        [SerializeField] private bool autoInstallDeathRuntime = true;

        [SerializeField]
        private float debugLogInterval = 2f;

        private SurvivalRules rules;
        private SurvivalSystem survivalSystem;
        private SurvivalStats stats;
        private float debugLogTimer;
        private bool deathEventRaised;

        public event Action<PlayerSurvivalRuntime, string> PlayerDied;

        public PlayerInputReader InputReader => inputReader;
        public SurvivalStats Stats => stats;
        public SurvivalSystem SurvivalSystem => survivalSystem;
        public bool WantsSprint { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsDead => stats != null && stats.Health <= 0f;
        public bool IsFatigued => stats != null && (stats.Rest <= Mathf.Max(0f, fatigueRestThreshold) || stats.Hunger <= Mathf.Max(0f, lowHungerFatigueThreshold));
        public bool IsExhausted => stats != null && (stats.Rest <= Mathf.Max(0f, exhaustionRestThreshold) || stats.Stamina <= 0.01f);
        public string DeathReason { get; private set; } = string.Empty;
        public bool CanSprint => !IsDead && stats != null && survivalSystem != null && survivalSystem.CanSprint(stats);
        public float SpeedMultiplier => stats != null && survivalSystem != null ? survivalSystem.GetSpeedMultiplier(stats) * GetStaminaSpeedMultiplier() : 1f;
        public string ConditionText
        {
            get
            {
                if (IsDead)
                {
                    return string.IsNullOrWhiteSpace(DeathReason) ? "dead" : "dead: " + DeathReason;
                }

                if (IsExhausted)
                {
                    return "exhausted";
                }

                if (IsFatigued)
                {
                    return "fatigued";
                }

                return stats != null && survivalSystem != null ? survivalSystem.GetConditionText(stats) : "uninitialized";
            }
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            InitializeCore();
            if (autoInstallDeathRuntime && GetComponent<PlayerDeathRuntime>() == null)
            {
                gameObject.AddComponent<PlayerDeathRuntime>();
            }
        }

        private void Update()
        {
            EnsureInitialized();

            if (IsDead)
            {
                WantsSprint = false;
                IsSprinting = false;
                RaiseDeathOnce(string.IsNullOrWhiteSpace(DeathReason) ? "unknown" : DeathReason);
                return;
            }

            WantsSprint = inputReader != null && inputReader.SprintHeld && inputReader.Move.sqrMagnitude > 0.0001f;
            SurvivalTickResult tickResult = survivalSystem.Tick(stats, Time.deltaTime, WantsSprint);
            IsSprinting = tickResult.IsSprinting;
            ApplyFatiguePenalties(Time.deltaTime);

            if (tickResult.Died)
            {
                RaiseDeathOnce(tickResult.DeathReason);
            }
            else if (stats.Health <= 0f)
            {
                RaiseDeathOnce(IsExhausted ? "exhaustion" : "damage");
            }

            if (logToConsole)
            {
                debugLogTimer += Time.deltaTime;
                if (debugLogTimer >= Mathf.Max(0.1f, debugLogInterval))
                {
                    debugLogTimer = 0f;
                    Debug.Log(FormatDebugLine(), this);
                }
            }
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public SurvivalTickResult ApplyFood(float nutrition)
        {
            EnsureInitialized();
            if (IsDead)
            {
                return SurvivalTickResult.NoChange(stats);
            }

            return survivalSystem.ApplyFood(stats, nutrition);
        }

        public SurvivalTickResult EatMeat()
        {
            EnsureInitialized();
            if (IsDead)
            {
                return SurvivalTickResult.NoChange(stats);
            }

            return survivalSystem.ApplyFood(stats, rules.MeatNutrition);
        }

        public SurvivalTickResult Damage(float amount)
        {
            EnsureInitialized();
            if (IsDead)
            {
                return SurvivalTickResult.NoChange(stats);
            }

            SurvivalTickResult result = survivalSystem.ApplyDamage(stats, amount);
            if (result.Died || stats.Health <= 0f)
            {
                RaiseDeathOnce(result.Died ? result.DeathReason : "damage");
            }

            return result;
        }

        public SurvivalTickResult Heal(float amount)
        {
            EnsureInitialized();
            if (IsDead)
            {
                return SurvivalTickResult.NoChange(stats);
            }

            return survivalSystem.ApplyHeal(stats, amount);
        }

        public float RestoreStamina(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            return stats.ChangeStamina(amount);
        }

        public void Restore(float health, float hunger, float stamina, float rest)
        {
            EnsureInitialized();
            stats.Restore(health, hunger, stamina, rest);
            DeathReason = string.Empty;
            deathEventRaised = stats.Health <= 0f;
        }

        public void SetCampfireRegen(bool active, float nearestDistance = -1f)
        {
            EnsureInitialized();
            if (IsDead)
            {
                return;
            }

            stats.SetCampfireRegen(active, nearestDistance);
            if (active)
            {
                GameEventBus.PublishCreatureEvent(
                    GameplayEventKind.VarnakScaredByFire,
                    transform.position,
                    "default",
                    "player",
                    "campfire",
                    amount: Mathf.Max(0f, nearestDistance),
                    message: "varnak_scared_by_fire");
            }
        }

        public void SetGodMode(bool enabled)
        {
            EnsureInitialized();
            stats.SetGodMode(enabled);
        }

        public SurvivalSaveData ToSaveData()
        {
            EnsureInitialized();
            return stats.ToSaveData();
        }

        public void LoadFromSaveData(SurvivalSaveData data)
        {
            EnsureInitialized();
            stats.LoadFromSaveData(data);
            DeathReason = stats.Health <= 0f ? "loaded_dead_state" : string.Empty;
            deathEventRaised = stats.Health <= 0f;
        }

        private void ApplyFatiguePenalties(float deltaTime)
        {
            if (deltaTime <= 0f || stats == null || stats.GodMode || IsDead)
            {
                return;
            }

            if (!IsExhausted)
            {
                return;
            }

            if (exhaustedStaminaDrainPerSecond > 0f)
            {
                stats.ChangeStamina(-exhaustedStaminaDrainPerSecond * deltaTime);
            }

            if (stats.Rest <= Mathf.Max(0f, exhaustionRestThreshold) && exhaustedHealthDamagePerSecond > 0f)
            {
                SurvivalTickResult result = survivalSystem.ApplyDamage(stats, exhaustedHealthDamagePerSecond * deltaTime);
                if (result.Died || stats.Health <= 0f)
                {
                    RaiseDeathOnce("exhaustion");
                }
            }
        }

        private float GetStaminaSpeedMultiplier()
        {
            if (stats == null)
            {
                return 1f;
            }

            if (stats.Stamina <= 0.01f)
            {
                return Mathf.Clamp01(noStaminaSpeedMultiplier);
            }

            if (stats.Stamina <= Mathf.Max(0f, exhaustedStaminaThreshold))
            {
                return Mathf.Clamp01(exhaustedSpeedMultiplier);
            }

            if (stats.Stamina <= Mathf.Max(exhaustedStaminaThreshold, tiredStaminaThreshold))
            {
                return Mathf.Clamp01(tiredSpeedMultiplier);
            }

            return 1f;
        }

        private void RaiseDeathOnce(string reason)
        {
            if (deathEventRaised)
            {
                return;
            }

            deathEventRaised = true;
            DeathReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
            WantsSprint = false;
            IsSprinting = false;
            PlayerDied?.Invoke(this, DeathReason);
            Debug.Log($"[PlayerSurvival] Player died: {DeathReason}", this);
        }

        private void InitializeCore()
        {
            rules = SurvivalRules.CreateDefault();
            survivalSystem = new SurvivalSystem(rules);
            stats = new SurvivalStats(startingHealth, startingHunger, startingStamina, startingRest, rules);
            DeathReason = stats.Health <= 0f ? "initial_dead_state" : string.Empty;
            deathEventRaised = stats.Health <= 0f;
        }

        private void EnsureInitialized()
        {
            if (stats == null || survivalSystem == null)
            {
                InitializeCore();
            }
        }

        private string FormatDebugLine()
        {
            return "Survival: health=" + stats.Health.ToString("0.0")
                   + " hunger=" + stats.Hunger.ToString("0.0")
                   + " stamina=" + stats.Stamina.ToString("0.0")
                   + " rest=" + stats.Rest.ToString("0.0")
                   + " condition=" + ConditionText;
        }
    }
}
