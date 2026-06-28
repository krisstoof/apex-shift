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

        [SerializeField]
        private float debugLogInterval = 2f;

        private SurvivalRules rules;
        private SurvivalSystem survivalSystem;
        private SurvivalStats stats;
        private float debugLogTimer;
        public PlayerInputReader InputReader => inputReader;
        public SurvivalStats Stats => stats;
        public SurvivalSystem SurvivalSystem => survivalSystem;
        public bool WantsSprint { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool CanSprint => stats != null && survivalSystem != null && survivalSystem.CanSprint(stats);
        public float SpeedMultiplier => stats != null && survivalSystem != null ? survivalSystem.GetSpeedMultiplier(stats) : 1f;
        public string ConditionText => stats != null && survivalSystem != null ? survivalSystem.GetConditionText(stats) : "uninitialized";

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            InitializeCore();
        }

        private void Update()
        {
            EnsureInitialized();

            WantsSprint = inputReader != null && inputReader.SprintHeld && inputReader.Move.sqrMagnitude > 0.0001f;
            SurvivalTickResult tickResult = survivalSystem.Tick(stats, Time.deltaTime, WantsSprint);
            IsSprinting = tickResult.IsSprinting;

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
            return survivalSystem.ApplyFood(stats, nutrition);
        }

        public SurvivalTickResult EatMeat()
        {
            EnsureInitialized();
            return survivalSystem.ApplyFood(stats, rules.MeatNutrition);
        }

        public SurvivalTickResult Damage(float amount)
        {
            EnsureInitialized();
            return survivalSystem.ApplyDamage(stats, amount);
        }

        public SurvivalTickResult Heal(float amount)
        {
            EnsureInitialized();
            return survivalSystem.ApplyHeal(stats, amount);
        }

        public void Restore(float health, float hunger, float stamina, float rest)
        {
            EnsureInitialized();
            stats.Restore(health, hunger, stamina, rest);
        }

        public void SetCampfireRegen(bool active, float nearestDistance = -1f)
        {
            EnsureInitialized();
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
        }

        private void InitializeCore()
        {
            rules = SurvivalRules.CreateDefault();
            survivalSystem = new SurvivalSystem(rules);
            stats = new SurvivalStats(startingHealth, startingHunger, startingStamina, startingRest, rules);
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
