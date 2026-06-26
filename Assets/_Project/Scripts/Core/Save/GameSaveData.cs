using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public SaveVersion version = SaveVersion.Current;
        public long savedAtUnixSeconds;
        public InventorySaveData inventory = InventorySaveData.Empty;
        public SurvivalSaveData survival = SurvivalSaveData.Default;
        public WorldSaveData world = WorldSaveData.Empty;

        public SaveVersion Version => version;
        public InventorySaveData Inventory => inventory;
        public SurvivalSaveData Survival => survival;
        public WorldSaveData World => world;

        public GameSaveData()
        {
            savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public GameSaveData(
            InventorySaveData inventory,
            SurvivalSaveData survival,
            WorldSaveData world,
            SaveVersion version = null,
            long? savedAtUnixSeconds = null)
        {
            this.version = version ?? SaveVersion.Current;
            this.savedAtUnixSeconds = savedAtUnixSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.inventory = inventory ?? InventorySaveData.Empty;
            this.survival = survival ?? SurvivalSaveData.Default;
            this.world = world ?? WorldSaveData.Empty;
        }

        public void EnsureDefaults()
        {
            version ??= SaveVersion.Current;
            inventory ??= InventorySaveData.Empty;
            survival ??= SurvivalSaveData.Default;
            world ??= WorldSaveData.Empty;
        }
    }
}
