namespace ApexShift.Core.Resources
{
    public readonly struct HarvestResult
    {
        public HarvestResult(bool success, string message, string resourceId, string itemId, int requestedAmount, int addedAmount, int leftoverAmount, bool shouldRemoveNode, bool depleted)
        {
            Success = success;
            Message = message ?? string.Empty;
            ResourceId = resourceId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
            RequestedAmount = requestedAmount;
            AddedAmount = addedAmount;
            LeftoverAmount = leftoverAmount;
            ShouldRemoveNode = shouldRemoveNode;
            Depleted = depleted;
        }

        public bool Success { get; }
        public string Message { get; }
        public string ResourceId { get; }
        public string ItemId { get; }
        public int RequestedAmount { get; }
        public int AddedAmount { get; }
        public int LeftoverAmount { get; }
        public bool ShouldRemoveNode { get; }
        public bool Depleted { get; }

        public static HarvestResult Failure(ResourceState state, string message)
        {
            return new HarvestResult(false, message, state != null ? state.ResourceId : string.Empty, state != null ? state.ItemId : string.Empty, state != null ? state.Amount : 0, 0, state != null ? state.Amount : 0, false, state != null && state.IsDepleted);
        }
    }
}
