using System.Collections.Generic;
using ApexShift.Core.Inventory;
using ApexShift.Core.Save;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class SaveLoadParityTests
    {
        [Test]
        public void CaptureCurrentStateIncludesCreatureAndEcosystemState()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject creature = null;
            GameObject serviceObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                EcosystemDirectorRuntime director = ecosystemObject.AddComponent<EcosystemDirectorRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                director.LoadSaveData(new[]
                {
                    new BiomeEcosystemSaveData("default", "Default", 40f, 100f, 6f, 2f, 1f, 15f, 4f, 3f, 1f, 2, 2, 1, "HERBIVORE", "stressed")
                });

                creature = CreateCreature("small_prey", new Vector3(3f, 0f, 4f), 62f);
                ecosystem.RegisterCreature(creature.GetComponent<CreatureAgentView>());

                serviceObject = new GameObject("SaveService");
                GameSaveService save = serviceObject.AddComponent<GameSaveService>();

                GameSaveData state = save.CaptureCurrentState();

                Assert.AreEqual(1, state.World.CreatureStates.Count);
                Assert.AreEqual("small_prey", state.World.CreatureStates[0].CreatureId);
                Assert.AreEqual(62f, state.World.CreatureStates[0].Hunger, 0.001f);
                Assert.AreEqual(1, state.World.BiomeStates.Count);
                Assert.AreEqual(40f, state.World.BiomeStates[0].PlantBiomass, 0.001f);
            }
            finally
            {
                DestroyIfNotNull(serviceObject);
                DestroyIfNotNull(creature);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [Test]
        public void ApplyLoadedStateRestoresCreatureHungerHealthAndBehaviorMemory()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject creature = null;
            GameObject serviceObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                creature = CreateCreature("grazer", Vector3.zero, 20f);
                ecosystem.RegisterCreature(creature.GetComponent<CreatureAgentView>());

                serviceObject = new GameObject("SaveService");
                GameSaveService save = serviceObject.AddComponent<GameSaveService>();

                WorldSaveData world = new WorldSaveData(
                    0,
                    1,
                    0f,
                    new List<ResourceSaveData>(),
                    new List<BiomeEcosystemSaveData>(),
                    new[]
                    {
                        new CreatureSaveData(
                            "grazer",
                            "grazer",
                            1,
                            7f,
                            0f,
                            9f,
                            12f,
                            45f,
                            false,
                            77f,
                            0.55f,
                            "Scavenge",
                            "forest",
                            "forest",
                            "forest",
                            "loaded_scavenge",
                            "meat_drop",
                            0.8f,
                            "OMNIVORE",
                            0.42f)
                    },
                    3.5f,
                    "save");

                bool applied = save.ApplyLoadedState(new GameSaveData(InventorySaveData.Empty, SurvivalSaveData.Default, world));

                Assert.IsTrue(applied);
                Assert.AreEqual(new Vector3(7f, 0f, 9f), creature.transform.position);
                Assert.AreEqual(12f, creature.GetComponent<CreatureHealthRuntime>().CurrentHealth, 0.001f);
                Assert.AreEqual(77f, creature.GetComponent<CreatureNeedsRuntime>().State.Hunger, 0.001f);
                CreatureBehaviorBrain brain = creature.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.Scavenge, brain.State);
                Assert.AreEqual("loaded_scavenge", brain.DecisionReason);
                Assert.AreEqual("forest", brain.CurrentBiomeId);
                Assert.AreEqual("meat_drop", brain.LastFoodSource);
            }
            finally
            {
                DestroyIfNotNull(serviceObject);
                DestroyIfNotNull(creature);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [Test]
        public void ApplyLoadedStateRestoresDeadCreatureWithoutSpawningExtraMeat()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject creature = null;
            GameObject serviceObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                creature = CreateCreature("small_prey", Vector3.zero, 10f);
                ecosystem.RegisterCreature(creature.GetComponent<CreatureAgentView>());

                serviceObject = new GameObject("SaveService");
                GameSaveService save = serviceObject.AddComponent<GameSaveService>();

                WorldSaveData world = new WorldSaveData(
                    0,
                    1,
                    0f,
                    new List<ResourceSaveData>(),
                    new List<BiomeEcosystemSaveData>(),
                    new[]
                    {
                        new CreatureSaveData("small_prey", "small_prey", 1, 0f, 0f, 0f, 0f, 20f, true, 80f, 0.2f, "Dead", "default", "default", "default", "dead_from_save", "none", 0f, "HERBIVORE", 0f)
                    },
                    0f,
                    "save");

                bool applied = save.ApplyLoadedState(new GameSaveData(InventorySaveData.Empty, SurvivalSaveData.Default, world));

                Assert.IsTrue(applied);
                Assert.IsFalse(creature.activeInHierarchy);
            }
            finally
            {
                DestroyIfNotNull(serviceObject);
                DestroyIfNotNull(creature);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        private static GameObject CreateCreature(string creatureId, Vector3 position, float hunger)
        {
            GameObject creature = new GameObject($"Creature_{creatureId}");
            creature.transform.position = position;
            creature.AddComponent<CreatureNavigationAdapter>();
            creature.AddComponent<CreatureAgentView>().Configure(creatureId);
            CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
            needs.Configure(creatureId);
            needs.State.SetHunger(hunger);
            creature.AddComponent<CreatureHealthRuntime>().Configure(creatureId);
            creature.AddComponent<CreatureSimulationLodRuntime>();
            creature.AddComponent<CreatureBehaviorBrain>();
            creature.AddComponent<CreatureBehaviorRuntime>();
            return creature;
        }

        private static void DestroyIfNotNull(GameObject go)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
