using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class CreatureSaveData
    {
        public string creatureId;
        public string speciesId;
        public int generation;
        public float x;
        public float y;
        public float z;
        public float health;
        public float maxHealth;
        public bool dead;
        public float hunger;
        public float energy;
        public string behaviorState;
        public string currentBiomeId;
        public string homeBiomeId;
        public string populationBiomeId;
        public string decisionReason;
        public string lastFoodSource;
        public float attackCooldown;
        public string currentNiche;
        public float huntDrive;

        public string CreatureId => creatureId;
        public string SpeciesId => speciesId;
        public int Generation => generation;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool Dead => dead;
        public float Hunger => hunger;
        public float Energy => energy;
        public string BehaviorState => behaviorState;
        public string CurrentBiomeId => currentBiomeId;
        public string HomeBiomeId => homeBiomeId;
        public string PopulationBiomeId => populationBiomeId;
        public string DecisionReason => decisionReason;
        public string LastFoodSource => lastFoodSource;
        public float AttackCooldown => attackCooldown;
        public string CurrentNiche => currentNiche;
        public float HuntDrive => huntDrive;

        public CreatureSaveData()
        {
        }

        public CreatureSaveData(string creatureId, string speciesId, int generation, float x, float y, float z, float health, float maxHealth, bool dead, float hunger, float energy, string behaviorState, string currentBiomeId, string homeBiomeId, string populationBiomeId, string decisionReason, string lastFoodSource, float attackCooldown, string currentNiche, float huntDrive)
        {
            string normalizedCreatureId = NormalizeCreatureId(creatureId);
            this.creatureId = normalizedCreatureId;
            this.speciesId = string.IsNullOrWhiteSpace(speciesId) ? normalizedCreatureId : speciesId.Trim();
            this.generation = Math.Max(1, generation);
            this.x = x;
            this.y = y;
            this.z = z;
            this.maxHealth = Math.Max(0.01f, maxHealth);
            this.health = Math.Max(0f, Math.Min(this.maxHealth, health));
            this.dead = dead || this.health <= 0f;
            if (this.dead)
            {
                this.health = 0f;
            }

            this.hunger = Math.Max(0f, hunger);
            this.energy = Math.Max(0f, Math.Min(1f, energy));
            this.behaviorState = string.IsNullOrWhiteSpace(behaviorState) ? "Wander" : behaviorState.Trim();
            this.currentBiomeId = NormalizeBiomeId(currentBiomeId);
            this.homeBiomeId = NormalizeBiomeId(homeBiomeId);
            this.populationBiomeId = NormalizeBiomeId(populationBiomeId);
            this.decisionReason = string.IsNullOrWhiteSpace(decisionReason) ? "save_load" : decisionReason.Trim();
            this.lastFoodSource = string.IsNullOrWhiteSpace(lastFoodSource) ? "none" : lastFoodSource.Trim();
            this.attackCooldown = Math.Max(0f, attackCooldown);
            this.currentNiche = string.IsNullOrWhiteSpace(currentNiche) ? "HERBIVORE" : currentNiche.Trim();
            this.huntDrive = Math.Max(0f, Math.Min(1f, huntDrive));
        }

        public static string NormalizeCreatureId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "small_prey";
            }

            string value = id.Trim().ToLowerInvariant();
            return value switch
            {
                "smallprey" => "small_prey",
                "small-prey" => "small_prey",
                "grazer" => "grazer",
                "varnak" => "varnak",
                _ => value
            };
        }

        public static string NormalizeBiomeId(string biomeId)
        {
            return string.IsNullOrWhiteSpace(biomeId) ? "default" : biomeId.Trim().ToLowerInvariant();
        }
    }
}
