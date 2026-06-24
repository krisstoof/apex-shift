namespace ApexShift.Core.Ecosystem
{
    public readonly struct FoodPreference
    {
        public FoodPreference(FoodKind kind, float availability, float preference)
        {
            Kind = kind;
            Availability = availability < 0f ? 0f : availability;
            Preference = preference < 0f ? 0f : preference;
        }

        public FoodKind Kind { get; }
        public float Availability { get; }
        public float Preference { get; }
        public float Score => Availability * Preference;
        public bool IsViable => Availability > 0f && Preference > 0f;
    }
}
