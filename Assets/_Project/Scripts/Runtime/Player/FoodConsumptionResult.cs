namespace ApexShift.Runtime.Player
{
    public readonly struct FoodConsumptionResult
    {
        public FoodConsumptionResult(
            bool success,
            string itemId,
            string displayName,
            string message,
            float hungerDelta,
            float healthDelta,
            float staminaDelta,
            bool unsafeRawFood)
        {
            Success = success;
            ItemId = itemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Message = message ?? string.Empty;
            HungerDelta = hungerDelta;
            HealthDelta = healthDelta;
            StaminaDelta = staminaDelta;
            UnsafeRawFood = unsafeRawFood;
        }

        public bool Success { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public string Message { get; }
        public float HungerDelta { get; }
        public float HealthDelta { get; }
        public float StaminaDelta { get; }
        public bool UnsafeRawFood { get; }

        public static FoodConsumptionResult Failed(string message)
        {
            return new FoodConsumptionResult(false, string.Empty, string.Empty, message, 0f, 0f, 0f, false);
        }
    }
}
