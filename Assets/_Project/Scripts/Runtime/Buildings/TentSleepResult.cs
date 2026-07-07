namespace ApexShift.Runtime.Buildings
{
    public readonly struct TentSleepResult
    {
        public TentSleepResult(
            bool success,
            string message,
            float hoursAdvanced,
            float restDelta,
            float staminaDelta,
            float hungerDelta,
            float healthDelta)
        {
            Success = success;
            Message = message ?? string.Empty;
            HoursAdvanced = hoursAdvanced;
            RestDelta = restDelta;
            StaminaDelta = staminaDelta;
            HungerDelta = hungerDelta;
            HealthDelta = healthDelta;
        }

        public bool Success { get; }
        public string Message { get; }
        public float HoursAdvanced { get; }
        public float RestDelta { get; }
        public float StaminaDelta { get; }
        public float HungerDelta { get; }
        public float HealthDelta { get; }

        public static TentSleepResult Failed(string message)
        {
            return new TentSleepResult(false, message, 0f, 0f, 0f, 0f, 0f);
        }
    }
}
