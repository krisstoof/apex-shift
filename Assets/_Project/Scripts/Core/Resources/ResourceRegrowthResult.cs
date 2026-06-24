namespace ApexShift.Core.Resources
{
    public sealed class ResourceRegrowthResult
    {
        public ResourceRegrowthResult(bool stageChanged, ResourceGrowthStage oldStage, ResourceGrowthStage newStage, bool becameMature)
        {
            StageChanged = stageChanged;
            OldStage = oldStage;
            NewStage = newStage;
            BecameMature = becameMature;
        }

        public bool StageChanged { get; }
        public ResourceGrowthStage OldStage { get; }
        public ResourceGrowthStage NewStage { get; }
        public bool BecameMature { get; }

        public static ResourceRegrowthResult NoChange(ResourceGrowthStage stage)
        {
            return new ResourceRegrowthResult(false, stage, stage, false);
        }
    }
}
