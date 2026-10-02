using System.Collections.Generic;
using UnityEngine;
using ApexShift.Runtime.Creatures;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [SerializeField] private SpeciesDefinition[] speciesDefinitions;
        [SerializeField] private CreatureSimulationLodConfig simulationLodConfig;
        [SerializeField] private EcosystemBalanceConfig ecosystemBalanceConfig;
        [SerializeField] private ResourceBalanceConfig resourceBalanceConfig;

        public IReadOnlyList<SpeciesDefinition> SpeciesDefinitions => speciesDefinitions ?? System.Array.Empty<SpeciesDefinition>();
        public CreatureSimulationLodConfig SimulationLodConfig => simulationLodConfig;
        public EcosystemBalanceConfig EcosystemBalanceConfig => ecosystemBalanceConfig;
        public ResourceBalanceConfig ResourceBalanceConfig => resourceBalanceConfig;

        public SpeciesDefinition GetSpecies(string speciesId)
        {
            string normalized = SpeciesDefinition.NormalizeSpeciesId(speciesId);
            if (speciesDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < speciesDefinitions.Length; i++)
            {
                SpeciesDefinition definition = speciesDefinitions[i];
                if (definition != null && definition.Matches(normalized))
                {
                    return definition;
                }
            }

            return null;
        }

        public void ConfigureForRuntime(SpeciesDefinition[] species, CreatureSimulationLodConfig lod, EcosystemBalanceConfig ecosystem, ResourceBalanceConfig resources)
        {
            speciesDefinitions = species ?? System.Array.Empty<SpeciesDefinition>();
            simulationLodConfig = lod;
            ecosystemBalanceConfig = ecosystem;
            resourceBalanceConfig = resources;
        }

        public List<string> ValidateConfig()
        {
            List<string> errors = new List<string>();
            var ids = new HashSet<string>();
            var roles = new HashSet<CreatureRole>();
            if (speciesDefinitions == null || speciesDefinitions.Length == 0) errors.Add("Missing species list");
            foreach (var species in SpeciesDefinitions)
            {
                if (species == null) { errors.Add("Null species definition"); continue; }
                if (!ids.Add(species.SpeciesId)) errors.Add("Duplicate species: " + species.SpeciesId);
                roles.Add(species.Role);
                foreach (string problem in species.ValidateProfile()) errors.Add(species.SpeciesId + ": " + problem);
            }
            foreach (var role in new[] { CreatureRole.SmallPrey, CreatureRole.HerbivoreOmnivore, CreatureRole.Predator })
                if (!roles.Contains(role)) errors.Add("Missing species role: " + role);
            if (simulationLodConfig == null) errors.Add("Missing CreatureSimulationLodConfig");
            if (ecosystemBalanceConfig == null) errors.Add("Missing EcosystemBalanceConfig");
            if (resourceBalanceConfig == null) errors.Add("Missing ResourceBalanceConfig");
            return errors;
        }

        public static GameBalanceConfig CreateFallback()
        {
            GameBalanceConfig config = CreateInstance<GameBalanceConfig>();
            config.ConfigureForRuntime(
                new[]
                {
                    SpeciesDefinition.CreateDefault("island_small_prey"),
                    SpeciesDefinition.CreateDefault("island_forager"),
                    SpeciesDefinition.CreateDefault("island_predator")
                },
                CreatureSimulationLodConfig.CreateDefault(),
                EcosystemBalanceConfig.CreateDefault(),
                ResourceBalanceConfig.CreateDefault());
            return config;
        }
    }

    public static class GameBalanceConfigProvider
    {
        public static SpeciesDefinition ResolveSpeciesDefinition(GameBalanceConfig config, SpeciesDefinition directDefinition, string speciesId, UnityEngine.Object context)
        {
            if (directDefinition != null && directDefinition.Matches(speciesId))
            {
                return directDefinition;
            }

            SpeciesDefinition fromConfig = config != null ? config.GetSpecies(speciesId) : null;
            if (fromConfig != null)
            {
                return fromConfig;
            }

            return SpeciesDefinition.CreateDefault(speciesId);
        }
    }
}
