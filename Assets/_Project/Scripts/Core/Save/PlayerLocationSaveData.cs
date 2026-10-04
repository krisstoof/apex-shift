using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class PlayerLocationSaveData
    {
        public string areaId = "island";
        public bool hasLocalPosition;
        public float localX, localY, localZ;
        public bool hasIslandReturnPosition;
        public float islandReturnX, islandReturnY, islandReturnZ;

        public string AreaId => areaId;
        public bool HasLocalPosition => hasLocalPosition;
        public float LocalX => localX;
        public float LocalY => localY;
        public float LocalZ => localZ;
        public bool HasIslandReturnPosition => hasIslandReturnPosition;
        public float IslandReturnX => islandReturnX;
        public float IslandReturnY => islandReturnY;
        public float IslandReturnZ => islandReturnZ;
        public static PlayerLocationSaveData Island => new PlayerLocationSaveData();
    }
}
