using System.Collections;
using System.Reflection;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.UI.Snapshots;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class Issue82GenerationRebindPlayModeTests
    {
        [UnityTest]
        public IEnumerator GenerateClearGenerate_RebindsSnapshotAndRegistryOwnership()
        {
            GameObject generatorObject = new GameObject("Issue82_RebindGenerator");
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetSeed(820082);

            try
            {
                generator.Generate();
                yield return null;
                yield return null;

                WorldRuntimeOwner owner = generator.GetComponent<WorldRuntimeOwner>();
                Transform firstRoot = owner.GenerationRoot;
                Assert.IsNotNull(firstRoot);
                GameSnapshotProvider firstSnapshot = firstRoot.GetComponentInChildren<GameSnapshotProvider>(true);
                PlayerInventoryRuntime firstInventory = firstRoot.GetComponentInChildren<PlayerInventoryRuntime>(true);
                PlayerSurvivalRuntime firstSurvival = firstRoot.GetComponentInChildren<PlayerSurvivalRuntime>(true);
                DayNightRuntime firstDayNight = firstRoot.GetComponentInChildren<DayNightRuntime>(true);
                EcosystemRuntime firstEcosystem = firstRoot.GetComponentInChildren<EcosystemRuntime>(true);
                BuildingRegistry firstBuildings = firstRoot.GetComponentInChildren<BuildingRegistry>(true);
                AssertSnapshotBindings(firstSnapshot, generator, firstInventory, firstSurvival, firstDayNight, firstEcosystem, firstBuildings);

                Vector3 firstPlayerPosition = firstInventory.transform.position;
                generator.ClearGeneratedWorld();
                yield return null;
                generator.SetSeed(820083);
                generator.Generate();
                yield return null;
                yield return null;

                Transform secondRoot = owner.GenerationRoot;
                Assert.IsNotNull(secondRoot);
                Assert.IsTrue(firstRoot == null, "The first generation root survived regeneration.");
                Assert.AreNotSame(firstRoot, secondRoot);

                GameSnapshotProvider secondSnapshot = secondRoot.GetComponentInChildren<GameSnapshotProvider>(true);
                PlayerInventoryRuntime secondInventory = secondRoot.GetComponentInChildren<PlayerInventoryRuntime>(true);
                PlayerSurvivalRuntime secondSurvival = secondRoot.GetComponentInChildren<PlayerSurvivalRuntime>(true);
                DayNightRuntime secondDayNight = secondRoot.GetComponentInChildren<DayNightRuntime>(true);
                EcosystemRuntime secondEcosystem = secondRoot.GetComponentInChildren<EcosystemRuntime>(true);
                BuildingRegistry secondBuildings = secondRoot.GetComponentInChildren<BuildingRegistry>(true);
                AssertSnapshotBindings(secondSnapshot, generator, secondInventory, secondSurvival, secondDayNight, secondEcosystem, secondBuildings);

                Assert.AreNotSame(firstInventory, secondInventory);
                Assert.AreNotSame(firstEcosystem, secondEcosystem);
                Assert.AreNotEqual(firstPlayerPosition, secondInventory.transform.position,
                    "The second snapshot generation unexpectedly reused the first player's position.");
                foreach (ResourceNodeView resource in ResourceRegistry.Resources)
                {
                    Assert.IsNotNull(resource);
                    Assert.IsTrue(resource.transform.IsChildOf(secondRoot));
                }
                foreach (CreatureAgentView creature in secondEcosystem.Creatures)
                {
                    Assert.IsNotNull(creature);
                    Assert.IsTrue(creature.transform.IsChildOf(secondRoot));
                }

                WorldQueryRuntime query = secondRoot.GetComponentInChildren<WorldQueryRuntime>(true);
                Assert.IsNotNull(query);
                if (query.TryFindNearestLivingCreature(firstPlayerPosition, 100f, out CreatureAgentView queriedCreature))
                {
                    Assert.IsTrue(queriedCreature.transform.IsChildOf(secondRoot));
                }

                GameSnapshot secondState = secondSnapshot.CaptureNow();
                Assert.IsTrue(secondState.worldDebug.hasPlayer);
                Assert.AreEqual(secondInventory.transform.position, secondState.worldDebug.playerPosition);
            }
            finally
            {
                Object.DestroyImmediate(generatorObject);
            }
        }

        private static void AssertSnapshotBindings(
            GameSnapshotProvider snapshot,
            WorldGeneratorRuntime generator,
            PlayerInventoryRuntime inventory,
            PlayerSurvivalRuntime survival,
            DayNightRuntime dayNight,
            EcosystemRuntime ecosystem,
            BuildingRegistry buildings)
        {
            Assert.IsNotNull(snapshot);
            Assert.AreSame(generator, GetPrivate(snapshot, "worldGenerator"));
            Assert.AreSame(inventory, GetPrivate(snapshot, "playerInventory"));
            Assert.AreSame(survival, GetPrivate(snapshot, "playerSurvival"));
            Assert.AreSame(inventory.transform, GetPrivate(snapshot, "playerTransform"));
            Assert.AreSame(dayNight, GetPrivate(snapshot, "dayNightRuntime"));
            Assert.AreSame(ecosystem, GetPrivate(snapshot, "ecosystemRuntime"));
            Assert.AreSame(buildings, GetPrivate(snapshot, "buildingRegistry"));
        }

        private static object GetPrivate(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private test binding field: {fieldName}");
            return field.GetValue(target);
        }
    }
}
