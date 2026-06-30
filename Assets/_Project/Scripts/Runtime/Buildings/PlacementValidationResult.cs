namespace ApexShift.Runtime.Buildings
{
    public readonly struct PlacementValidationResult
    {
        public readonly bool isValid;
        public readonly string reason;

        public PlacementValidationResult(bool isValid, string reason)
        {
            this.isValid = isValid;
            this.reason = string.IsNullOrWhiteSpace(reason) ? (isValid ? "valid" : "invalid") : reason;
        }

        public static PlacementValidationResult Valid => new PlacementValidationResult(true, "valid");
        public static PlacementValidationResult Invalid(string reason) => new PlacementValidationResult(false, reason);
    }
}
