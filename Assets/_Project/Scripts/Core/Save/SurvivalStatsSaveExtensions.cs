using ApexShift.Core.Survival;

namespace ApexShift.Core.Save
{
    public static class SurvivalStatsSaveExtensions
    {
        public static SurvivalSaveData ToSaveData(this SurvivalStats stats)
        {
            if (stats == null)
            {
                return SurvivalSaveData.Default;
            }

            return new SurvivalSaveData(
                stats.Health,
                stats.Hunger,
                stats.Stamina,
                stats.Rest,
                stats.CampfireRegenActive,
                stats.CampfireRegenDistance,
                stats.GodMode);
        }

        public static void LoadFromSaveData(this SurvivalStats stats, SurvivalSaveData data)
        {
            if (stats == null)
            {
                return;
            }

            if (data == null)
            {
                stats.ResetToMax();
                return;
            }

            stats.Restore(data.Health, data.Hunger, data.Stamina, data.Rest);
            stats.SetCampfireRegen(data.CampfireRegenActive, data.CampfireRegenDistance);
            stats.SetGodMode(data.GodMode);
        }
    }
}
