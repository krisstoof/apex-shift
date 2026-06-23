using System;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceState
    {
        public ResourceState(ResourceDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ResourceId = definition.Id.ToString();
            DisplayName = definition.DisplayName;
            ItemId = definition.ItemId;
            Amount = definition.HarvestAmount;
            MaxAmount = definition.HarvestAmount;
            PlayerHarvestable = definition.PlayerHarvestable;
            CanBeHarvested = definition.PlayerHarvestable;
            RemoveWhenHarvested = definition.RemoveWhenHarvested;
        }

        public ResourceDefinition Definition { get; }
        public string ResourceId { get; }
        public string DisplayName { get; }
        public string ItemId { get; }
        public int Amount { get; private set; }
        public int MaxAmount { get; }
        public bool PlayerHarvestable { get; private set; }
        public bool CanBeHarvested { get; private set; }
        public bool RemoveWhenHarvested { get; }
        public bool IsDepleted { get; private set; }

        public bool IsInteractable => PlayerHarvestable && CanBeHarvested && Amount > 0 && !IsDepleted;
        public void SetAmount(int amount)
        {
            Amount = Math.Max(0, Math.Min(amount, MaxAmount));
            if (Amount <= 0) MarkDepleted();
            else IsDepleted = false;
        }
        public void SetHarvestable(bool canBeHarvested) => CanBeHarvested = canBeHarvested && PlayerHarvestable && !IsDepleted;
        public void MarkDepleted() { Amount = 0; IsDepleted = true; CanBeHarvested = false; }
        public void RestoreToFull() { Amount = MaxAmount; IsDepleted = false; CanBeHarvested = PlayerHarvestable; }
    }
}
