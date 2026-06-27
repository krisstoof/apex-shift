using System;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class GameSnapshot
    {
        public InventorySnapshot inventory;
        public SurvivalSnapshot survival;
        public WorldDebugSnapshot worldDebug;
        public float capturedAtRealtime;

        public static GameSnapshot Empty => new GameSnapshot(InventorySnapshot.Empty, SurvivalSnapshot.Empty, WorldDebugSnapshot.Empty, 0f);
        public GameSnapshot(InventorySnapshot inventory, SurvivalSnapshot survival, WorldDebugSnapshot worldDebug, float capturedAtRealtime)
        {
            this.inventory = inventory ?? InventorySnapshot.Empty;
            this.survival = survival ?? SurvivalSnapshot.Empty;
            this.worldDebug = worldDebug ?? WorldDebugSnapshot.Empty;
            this.capturedAtRealtime = Math.Max(0f, capturedAtRealtime);
        }
    }
}
