using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class SurvivalSaveData
    {
        public float health = 100f;
        public float hunger = 100f;
        public float stamina = 100f;
        public float rest = 100f;
        public bool campfireRegenActive;
        public float campfireRegenDistance = -1f;
        public bool godMode;
        public float posX;
        public float posY;
        public float posZ;
        public bool hasPosition;

        public float Health => health;
        public float Hunger => hunger;
        public float Stamina => stamina;
        public float Rest => rest;
        public bool CampfireRegenActive => campfireRegenActive;
        public float CampfireRegenDistance => campfireRegenDistance;
        public bool GodMode => godMode;

        public static SurvivalSaveData Default => new SurvivalSaveData(100f, 100f, 100f, 100f);

        public SurvivalSaveData()
        {
        }

        public SurvivalSaveData(
            float health,
            float hunger,
            float stamina,
            float rest,
            bool campfireRegenActive = false,
            float campfireRegenDistance = -1f,
            bool godMode = false)
        {
            this.health = health;
            this.hunger = hunger;
            this.stamina = stamina;
            this.rest = rest;
            this.campfireRegenActive = campfireRegenActive;
            this.campfireRegenDistance = campfireRegenActive ? campfireRegenDistance : -1f;
            this.godMode = godMode;
            this.hasPosition = false;
        }

        public void SetPosition(float x, float y, float z)
        {
            this.posX = x;
            this.posY = y;
            this.posZ = z;
            this.hasPosition = true;
        }
    }
}
