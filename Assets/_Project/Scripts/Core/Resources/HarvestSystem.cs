using ApexShift.Core.Inventory;

namespace ApexShift.Core.Resources
{
    public sealed class HarvestSystem
    {
        public bool CanHarvest(ResourceState state, InventoryState inventory, out string blockReason)
        {
            blockReason = GetBlockReason(state, inventory);
            return string.IsNullOrEmpty(blockReason);
        }

        public string GetPrompt(ResourceState state)
        {
            if (state == null) return string.Empty;
            if (!state.PlayerHarvestable) return string.Empty;
            if (!state.CanBeHarvested) return "Regrowing";
            if (state.Amount <= 0 || state.IsDepleted) return "Empty";
            return $"E: gather {state.ItemId} x{state.Amount}";
        }

        public HarvestResult Harvest(ResourceState state, InventoryState inventory)
        {
            if (!CanHarvest(state, inventory, out string blockReason))
            {
                return HarvestResult.Failure(state, blockReason);
            }

            int requestedAmount = state.Amount;
            int leftover = inventory.AddItem(state.ItemId, requestedAmount);
            int addedAmount = requestedAmount - leftover;
            if (addedAmount <= 0)
            {
                return HarvestResult.Failure(state, "Inventory full");
            }

            if (leftover > 0) state.SetAmount(leftover);
            else state.MarkDepleted();

            return new HarvestResult(true, $"Collected {state.ItemId} x{addedAmount}", state.ResourceId, state.ItemId, requestedAmount, addedAmount, leftover, state.RemoveWhenHarvested && state.IsDepleted, state.IsDepleted);
        }

        private static string GetBlockReason(ResourceState state, InventoryState inventory)
        {
            if (state == null) return "No resource";
            if (!state.PlayerHarvestable) return "Cannot be gathered";
            if (!state.CanBeHarvested) return "Regrowing";
            if (state.Amount <= 0 || state.IsDepleted) return "Empty";
            if (inventory == null) return "No inventory";
            if (!inventory.CanAddItem(state.ItemId, state.Amount)) return "Inventory full";
            return string.Empty;
        }
    }
}
