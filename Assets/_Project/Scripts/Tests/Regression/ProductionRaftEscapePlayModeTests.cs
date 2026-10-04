#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.Events;
using ApexShift.Infrastructure.Save;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class ProductionRaftEscapePlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratedWorld_CraftBoardEscapeFailAndLoadKeepsPlayerAliveAndStory()
        {
            var owner = new GameObject("ProductionRaftWorld");
            var generator = owner.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetSeed(12345);
            generator.SetBiomeCatalog(AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(
                "Assets/_Project/Data/Biomes/BiomeCatalog.asset"));
            generator.SetHabitatVegetationCatalog(AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(
                "Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset"));
            try
            {
                generator.Generate();
                var context = generator.CurrentGeneration;
                var player = context.Player;
                var inventory = player.GetComponent<PlayerInventoryRuntime>();
                var crafting = player.GetComponent<PlayerCraftingRuntime>();
                var placement = player.GetComponent<BuildingPlacementRuntime>();
                var registry = BuildingRegistry.Active;
                var definition = placement.AvailableDefinitions.Single(d => d.BuildingId == "raft");
                Assert.IsTrue(TryFindLaunchRoute(context, placement, definition, out var launch, out var direction),
                    "Production world must provide shallow water and a continuous route to open sea.");
                context.StoryProgression.Signal(StorySignalIds.CrashSurvived);
                context.StoryProgression.Signal(StorySignalIds.SurvivalEstablished);
                inventory.Inventory.AddItem("wood", 10);
                inventory.Inventory.AddItem("fiber", 6);
                Assert.IsTrue(crafting.CraftRecipe("raft").Succeeded);
                Assert.IsFalse(context.StoryProgression.HasMilestone(StorySignalIds.RaftBuilt));
                Assert.IsTrue(placement.TryPlaceAt("raft", launch, Quaternion.identity), placement.CurrentValidation.reason);
                Assert.AreEqual(0, inventory.Inventory.GetAmount("raft"));
                var raft = registry.Structures.Single(s => s.BuildingId == "raft").Raft;
                Assert.IsTrue(raft.TryLaunch(player));
                Vector3 safe = raft.SafeReturnPoint;
                bool leftPlayerBounds = false;
                float health = player.GetComponent<PlayerSurvivalRuntime>().ToSaveData().Health;
                for (int i = 0; i < 250 && raft.IsAttemptActive; i++)
                {
                    raft.TickWorldDirection(direction, 0.1f);
                    leftPlayerBounds |= !context.WorldBounds.Contains(raft.transform.position);
                }
                Assert.IsTrue(leftPlayerBounds, "Raft, unlike normal swimming, must leave player bounds.");
                Assert.AreEqual(RaftState.Failed, raft.State);
                Assert.AreEqual(safe, player.transform.position);
                Assert.IsTrue(context.IslandTopography.IsLandAt(safe.x, safe.z));
                Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
                Assert.IsTrue(player.GetComponent<IsometricPlayerController>().MovementEnabled);
                Assert.AreEqual(health, player.GetComponent<PlayerSurvivalRuntime>().ToSaveData().Health);
                Assert.IsTrue(context.StoryProgression.HasMilestone(StorySignalIds.RaftEscapeFailed));
                Assert.IsFalse(registry.Structures.Any(s => s.BuildingId == "raft"));

                var save = owner.GetComponent<GameSaveService>() ?? owner.AddComponent<GameSaveService>();
                var serializer = new UnityJsonGameSaveSerializer();
                var data = serializer.Deserialize(serializer.Serialize(save.CaptureCurrentState()));
                Assert.IsFalse(data.World.BuildingStates.Any(b => b.BuildingId == "raft"));
                int failedSignals = 0;
                using (GameEventBus.Subscribe(e => { if (e.kind == GameplayEventKind.StorySignal
                    && e.signalId == StorySignalIds.RaftEscapeFailed) failedSignals++; }))
                {
                    Assert.IsTrue(save.ApplyLoadedState(data));
                    Assert.AreEqual(0, failedSignals, "Restore must not replay the raft failure.");
                }
                yield return null;
                var restored = generator.CurrentGeneration;
                Assert.AreNotSame(player, restored.Player);
                Assert.IsTrue(restored.IslandTopography.IsLandAt(restored.Player.transform.position.x,
                    restored.Player.transform.position.z));
                Assert.IsFalse(restored.Player.GetComponent<PlayerSurvivalRuntime>().IsDead);
                Assert.IsTrue(restored.Player.GetComponent<IsometricPlayerController>().MovementEnabled);
                Assert.IsTrue(restored.StoryProgression.HasMilestone(StorySignalIds.RaftEscapeFailed));
                Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, restored.StoryProgression.CurrentStageId);
                Assert.IsFalse(BuildingRegistry.Active.Structures.Any(s => s.BuildingId == "raft"));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static bool TryFindLaunchRoute(WorldGenerationContext context, BuildingPlacementRuntime placement,
            PlaceableDefinition definition, out Vector3 launch, out Vector3 direction)
        {
            var topography = context.IslandTopography;
            var land = topography.GetAllLandCenters();
            Bounds grid = topography.WorldBounds;
            for (float x = grid.min.x + 4f; x < grid.max.x - 4f; x += 4f)
            for (float z = grid.min.z + 4f; z < grid.max.z - 4f; z += 4f)
            {
                Vector3 point = new Vector3(x, 0f, z);
                if (!topography.TryGetEnvironmentAt(point, out EnvironmentSample sample)
                    || !sample.IsWater || sample.DistanceToCoast < 4f || sample.DistanceToCoast > 12f
                    || !placement.ValidatePlacement(definition, point, Quaternion.identity).isValid) continue;
                Vector3 nearest = land.OrderBy(p => (new Vector2(p.x - x, p.z - z)).sqrMagnitude).First();
                Vector3 outward = Vector3.ProjectOnPlane(point - nearest, Vector3.up).normalized;
                bool valid = false;
                for (float distance = 1f; distance <= 70f; distance += 1f)
                {
                    Vector3 probe = point + outward * distance;
                    if (probe.x <= grid.min.x + 1f || probe.x >= grid.max.x - 1f
                        || probe.z <= grid.min.z + 1f || probe.z >= grid.max.z - 1f
                        || !topography.TryGetEnvironmentAt(probe, out sample) || !sample.IsWater) break;
                    if (sample.DistanceToCoast >= 38f) { valid = true; break; }
                }
                if (!valid) continue;
                launch = point;
                direction = outward;
                return true;
            }
            launch = direction = Vector3.zero;
            return false;
        }
    }
}
#endif
