using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// Central audio asset library. Loads and caches AudioClips from the v3 realistic audio pack.
    /// Maps audio IDs to actual WAV files.
    /// </summary>
    public static class AudioLibrary
    {
        private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        // Combat - Player
        private static readonly string[] spear_swings = { "spear_swing_real_01", "spear_swing_real_02", "spear_swing_real_03", "spear_swing_real_04" };
        private static readonly string[] spear_hits = { "spear_hit_flesh_real_01", "spear_hit_flesh_real_02", "spear_hit_flesh_real_03", "spear_hit_flesh_real_04" };
        private static readonly string[] bow_releases = { "bow_release_real_01", "bow_release_real_02", "bow_release_real_03", "bow_release_real_04" };
        private static readonly string[] arrow_flies = { "arrow_fly_real_01", "arrow_fly_real_02", "arrow_fly_real_03", "arrow_fly_real_04" };
        private static readonly string[] arrow_hits_flesh = { "arrow_hit_flesh_real_01" };
        private static readonly string[] arrow_hits_wood = { "arrow_hit_wood_real_01" };
        private static readonly string[] arrow_hits_stone = { "arrow_hit_stone_real_01" };

        // Resources
        private static readonly string[] wood_chops = { "wood_chop_real_01", "wood_chop_real_02", "wood_chop_real_03", "wood_chop_real_04" };
        private static readonly string[] stone_hits = { "stone_hit_real_01", "stone_hit_real_02", "stone_hit_real_03", "stone_hit_real_04" };

        // Combat - Traps
        private static readonly string[] trap_snaps = { "trap_snap_real_01", "trap_snap_real_02", "trap_snap_real_03", "trap_snap_real_04" };

        // Creatures - Varnak
        private static readonly string[] varnak_growls = { "varnak_growl_real_01", "varnak_growl_real_02", "varnak_growl_real_03" };
        private static readonly string[] varnak_attacks = { "varnak_attack_real_01", "varnak_attack_real_02", "varnak_attack_real_03" };
        private static readonly string[] varnak_hurts = { "varnak_hurt_real_01", "varnak_hurt_real_02", "varnak_hurt_real_03" };
        private static readonly string[] varnak_deaths = { "varnak_death_real_01" };

        // Creatures - Grazer
        private static readonly string[] grazer_alerts = { "grazer_alert_real_01", "grazer_alert_real_02", "grazer_alert_real_03" };
        private static readonly string[] grazer_deaths = { "grazer_death_real_01" };

        // Creatures - Small Prey
        private static readonly string[] small_prey_alarms = { "small_prey_alarm_real_01", "small_prey_alarm_real_02", "small_prey_alarm_real_03" };

        // Footsteps
        private static readonly string[] footsteps_grass = { "footstep_grass_real_01", "footstep_grass_real_02", "footstep_grass_real_03" };
        private static readonly string[] footsteps_dirt = { "footstep_dirt_real_01", "footstep_dirt_real_02" };
        private static readonly string[] footsteps_stone = { "footstep_stone_real_01", "footstep_stone_real_02" };

        public static AudioClip GetRandomSpearSwing() => GetRandom(spear_swings);
        public static AudioClip GetRandomSpearHit() => GetRandom(spear_hits);
        public static AudioClip GetRandomBowRelease() => GetRandom(bow_releases);
        public static AudioClip GetRandomArrowFly() => GetRandom(arrow_flies);
        public static AudioClip GetRandomArrowHitFlesh() => GetRandom(arrow_hits_flesh);
        public static AudioClip GetRandomArrowHitWood() => GetRandom(arrow_hits_wood);
        public static AudioClip GetRandomArrowHitStone() => GetRandom(arrow_hits_stone);

        public static AudioClip GetRandomWoodChop() => GetRandom(wood_chops);
        public static AudioClip GetRandomStoneHit() => GetRandom(stone_hits);

        public static AudioClip GetRandomTrapSnap() => GetRandom(trap_snaps);

        public static AudioClip GetRandomVarnakGrowl() => GetRandom(varnak_growls);
        public static AudioClip GetRandomVarnakAttack() => GetRandom(varnak_attacks);
        public static AudioClip GetRandomVarnakHurt() => GetRandom(varnak_hurts);
        public static AudioClip GetRandomVarnakDeath() => GetRandom(varnak_deaths);

        public static AudioClip GetRandomGrazerAlert() => GetRandom(grazer_alerts);
        public static AudioClip GetRandomGrazerDeath() => GetRandom(grazer_deaths);

        public static AudioClip GetRandomSmallPreyAlarm() => GetRandom(small_prey_alarms);

        public static AudioClip GetRandomFootstepGrass() => GetRandom(footsteps_grass);
        public static AudioClip GetRandomFootstepDirt() => GetRandom(footsteps_dirt);
        public static AudioClip GetRandomFootstepStone() => GetRandom(footsteps_stone);

        public static AudioClip GetAudioClip(string audioId)
        {
            if (string.IsNullOrEmpty(audioId)) return null;

            if (cache.TryGetValue(audioId, out AudioClip clip) && clip != null)
                return clip;

            string path = ResolveAudioPath(audioId);
            if (string.IsNullOrEmpty(path)) return null;

            clip = UnityEngine.Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                cache[audioId] = clip;
            }
            return clip;
        }

        private static AudioClip GetRandom(string[] audioIds)
        {
            if (audioIds == null || audioIds.Length == 0) return null;
            string id = audioIds[Random.Range(0, audioIds.Length)];
            return GetAudioClip(id);
        }

        private static string ResolveAudioPath(string audioId)
        {
            // Map audio ID to file path within Resources folder
            // e.g., "spear_swing_real_01" -> "Audio/SFX/combat/player/spear_swing_real_01"
            
            if (audioId.Contains("spear_swing"))
                return $"Audio/SFX/combat/player/{audioId}";
            if (audioId.Contains("spear_hit"))
                return $"Audio/SFX/combat/player/{audioId}";
            if (audioId.Contains("bow_release"))
                return $"Audio/SFX/combat/player/{audioId}";
            if (audioId.Contains("arrow_fly"))
                return $"Audio/SFX/combat/player/{audioId}";
            if (audioId.Contains("arrow_hit"))
                return $"Audio/SFX/combat/player/{audioId}";
            if (audioId.Contains("wood_chop"))
                return $"Audio/SFX/resources/{audioId}";
            if (audioId.Contains("stone_hit"))
                return $"Audio/SFX/resources/{audioId}";
            if (audioId.Contains("trap_snap"))
                return $"Audio/SFX/combat/traps/{audioId}";
            if (audioId.Contains("varnak"))
            {
                if (audioId.Contains("growl"))
                    return $"Audio/SFX/creatures/varnak/{audioId}";
                if (audioId.Contains("attack"))
                    return $"Audio/SFX/creatures/varnak/{audioId}";
                if (audioId.Contains("hurt"))
                    return $"Audio/SFX/creatures/varnak/{audioId}";
                if (audioId.Contains("death"))
                    return $"Audio/SFX/creatures/varnak/{audioId}";
            }
            if (audioId.Contains("grazer"))
            {
                if (audioId.Contains("alert"))
                    return $"Audio/SFX/creatures/grazer/{audioId}";
                if (audioId.Contains("death"))
                    return $"Audio/SFX/creatures/grazer/{audioId}";
            }
            if (audioId.Contains("small_prey"))
                return $"Audio/SFX/creatures/small_prey/{audioId}";
            if (audioId.Contains("footstep"))
                return $"Audio/SFX/footsteps/{audioId}";

            return null;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }
    }
}
