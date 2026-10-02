using System;
using System.Collections.Generic;
using ApexShift.Runtime.Config;
using ApexShift.Runtime.World.Environment;
using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    public readonly struct CreatureSpawnPlacement
    {
        public readonly string SpeciesId;
        public readonly CreatureRole Role;
        public readonly Vector3 Position;
        public readonly string HabitatId;
        public CreatureSpawnPlacement(SpeciesDefinition species, Vector3 position, string habitat)
        { SpeciesId = species.SpeciesId; Role = species.Role; Position = position; HabitatId = habitat; }
    }

    /// <summary>Pure placement planning. Never instantiates objects or consumes Unity's global RNG.</summary>
    public static class CreaturePopulationPlanner
    {
        public delegate bool EnvironmentSampler(Vector3 position, out EnvironmentSample sample);
        public static List<CreatureSpawnPlacement> Plan(int seed, SpeciesDefinition species, int day, Bounds bounds,
            EnvironmentSampler sampleEnvironment, Vector3 playerPosition, int currentPopulation, bool initial = false)
        {
            var placements = new List<CreatureSpawnPlacement>();
            var rules = species.PopulationRules;
            if (!rules.IsValid) return placements;
            var random = new System.Random(DeriveSeed(seed, day, species.SpeciesId));
            int target = rules.GetPopulationCapForDay(day);
            if (initial) target = Math.Min(target, random.Next(rules.InitialMinCount, rules.InitialMaxCount + 1));
            int available = Math.Max(0, target - currentPopulation);
            int attempts = initial ? Math.Max(128, available * 128) : rules.DailySpawnAttempts;
            for (int i = 0; i < attempts && placements.Count < available; i++)
            {
                var position = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                    0f, Mathf.Lerp(bounds.min.z, bounds.max.z, (float)random.NextDouble()));
                // Draw independently of acceptance so the sequence is stable for a fixed attempt.
                float chance = (float)random.NextDouble();
                if (!initial && chance >= rules.DailySpawnChance) continue;
                if (!sampleEnvironment(position, out var environment)
                    || !IsCandidateValid(species, bounds, playerPosition, position, environment)) continue;
                position.y = environment.Height;
                placements.Add(new CreatureSpawnPlacement(species, position, environment.HabitatId));
            }
            return placements;
        }
        public static bool IsCandidateValid(SpeciesDefinition species, Bounds bounds, Vector3 player,
            Vector3 position, EnvironmentSample environment)
        {
            if (position.x < bounds.min.x || position.x > bounds.max.x || position.z < bounds.min.z || position.z > bounds.max.z
                || !environment.IsLand || environment.IsWater || (environment.IsShoreline && !species.AllowShoreline)) return false;
            bool habitatAllowed = false, terrainAllowed = false;
            foreach (string id in species.AllowedHabitatIds) if (id == environment.HabitatId) habitatAllowed = true;
            foreach (var type in species.AllowedTerrainTypes) if (type == environment.TerrainType) terrainAllowed = true;
            Vector3 delta = position - player; delta.y = 0f;
            return habitatAllowed && terrainAllowed && delta.sqrMagnitude >= species.PopulationRules.MinPlayerSpawnDistance * species.PopulationRules.MinPlayerSpawnDistance
                && environment.NormalizedElevation >= species.MinElevation01 && environment.NormalizedElevation <= species.MaxElevation01
                && environment.Moisture01 >= species.MinMoisture01 && environment.Moisture01 <= species.MaxMoisture01
                && environment.SlopeDegrees >= species.MinSlopeDegrees && environment.SlopeDegrees <= species.MaxSlopeDegrees
                && environment.DistanceToCoast >= species.MinDistanceToCoast && environment.DistanceToCoast <= species.MaxDistanceToCoast;
        }
        private static int DeriveSeed(int seed, int day, string id)
        {
            unchecked { uint hash = 2166136261; foreach (char c in id) hash = (hash ^ c) * 16777619; return (int)(hash ^ (uint)seed ^ ((uint)day * 2654435761)); }
        }
    }
}
