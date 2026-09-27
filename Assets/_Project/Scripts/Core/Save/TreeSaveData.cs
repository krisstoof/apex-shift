using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class TreeSaveData
    {
        public string treeId;
        public string speciesId;
        public string resourceKind;
        public string lifecycleState;
        public float currentHealth;
        public float maxHealth;
        public float regrowthProgress;
        public bool dropsSpawned;

        public string TreeId => treeId ?? string.Empty;
        public string SpeciesId => speciesId ?? string.Empty;
        public string ResourceKind => resourceKind ?? string.Empty;
        public string LifecycleState => lifecycleState ?? "Standing";
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float RegrowthProgress => regrowthProgress;
        public bool DropsSpawned => dropsSpawned;

        public TreeSaveData() { }

        public TreeSaveData(string treeId, string speciesId, string resourceKind, string lifecycleState,
            float currentHealth, float maxHealth, float regrowthProgress, bool dropsSpawned)
        {
            this.treeId = treeId ?? string.Empty;
            this.speciesId = speciesId ?? string.Empty;
            this.resourceKind = resourceKind ?? string.Empty;
            this.lifecycleState = string.IsNullOrWhiteSpace(lifecycleState) ? "Standing" : lifecycleState;
            this.maxHealth = Math.Max(1f, maxHealth);
            this.currentHealth = Math.Max(0f, Math.Min(this.maxHealth, currentHealth));
            this.regrowthProgress = Math.Max(0f, Math.Min(1f, regrowthProgress));
            this.dropsSpawned = dropsSpawned;
        }
    }
}
