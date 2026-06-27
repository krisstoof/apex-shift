using System;
using ApexShift.Core.Survival;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class SurvivalSnapshot
    {
        public float health;
        public float hunger;
        public float stamina;
        public float rest;
        public string conditionText;
        public bool canSprint;
        public bool isSprinting;

        public static SurvivalSnapshot Empty => new SurvivalSnapshot(0f, 0f, 0f, 0f, "missing", false, false);

        public SurvivalSnapshot(float health, float hunger, float stamina, float rest, string conditionText, bool canSprint, bool isSprinting)
        {
            this.health = health;
            this.hunger = hunger;
            this.stamina = stamina;
            this.rest = rest;
            this.conditionText = string.IsNullOrWhiteSpace(conditionText) ? "unknown" : conditionText;
            this.canSprint = canSprint;
            this.isSprinting = isSprinting;
        }

        public static SurvivalSnapshot FromStats(SurvivalStats stats, string conditionText, bool canSprint, bool isSprinting)
        {
            if (stats == null) return Empty;
            return new SurvivalSnapshot(stats.Health, stats.Hunger, stats.Stamina, stats.Rest, conditionText, canSprint, isSprinting);
        }
    }
}
