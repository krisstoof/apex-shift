#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.Story.Clues;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ApexShift.Tests.Regression
{
    public sealed class ProductionStoryCluePlayModeTests
    {
        private static WorldGeneratorRuntime Generator(GameObject owner)
        {
            var generator = owner.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetBiomeCatalog(AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>("Assets/_Project/Data/Biomes/BiomeCatalog.asset"));
            generator.SetHabitatVegetationCatalog(AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>("Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset"));
            generator.SetSeed(12345);
            return generator;
        }

        [UnityTest]
        public IEnumerator ThreeSeedsGenerateDeterministicSafeCluesWithVegetationClearance()
        {
            var owner = new GameObject("ClueProductionGeneration");
            var generator = Generator(owner);
            try
            {
                foreach (int seed in new[] { 12345, 81281, 91284 })
                {
                    generator.SetSeed(seed); generator.Generate();
                    ValidateProduction(generator, seed);
                    var positions = StoryClueRegistry.Clues.ToDictionary(c => c.ClueId, c => c.transform.position);
                    var old = StoryClueRegistry.Clues.ToArray();
                    generator.ClearGeneratedWorld();
                    Assert.IsEmpty(StoryClueRegistry.Clues);
                    yield return null;
                    Assert.IsTrue(old.All(c => c == null));
                    generator.Generate();
                    ValidateProduction(generator, seed);
                    foreach (var clue in StoryClueRegistry.Clues) Assert.AreEqual(positions[clue.ClueId], clue.transform.position);
                }
            }
            finally { generator.ClearGeneratedWorld(); Object.DestroyImmediate(owner); }
        }

        private static void ValidateProduction(WorldGeneratorRuntime generator, int seed)
        {
            var context = generator.CurrentGeneration;
            CollectionAssert.AreEquivalent(StoryClueDefinition.Production().Select(d => d.ClueId), StoryClueRegistry.Clues.Select(c => c.ClueId));
            Assert.AreEqual(3, context.LandmarkRoot.Find("StoryClues").GetComponentsInChildren<StoryClueRuntime>().Length);
            Vector3 crash = LandmarkRegistry.FindById("plane_crash").transform.position;
            Vector3 camp = LandmarkRegistry.FindById("smuggler_camp").transform.position;
            var entrance = LandmarkRegistry.FindById("base_entrance");
            Assert.IsFalse(entrance.IsDiscovered);
            var vegetation = new List<VegetationDebugPoint>();
            context.VegetationRuntime.CopyDebugPoints(vegetation);
            foreach (var clue in StoryClueRegistry.Clues)
            {
                var definition = StoryClueDefinition.Production().Single(d => d.ClueId == clue.ClueId);
                Vector3 point = clue.transform.position;
                Assert.IsTrue(context.IslandTopography.TryGetEnvironmentAt(point, out EnvironmentSample sample));
                Assert.IsTrue(sample.IsLand); Assert.IsFalse(sample.IsWater);
                Assert.LessOrEqual(sample.SlopeDegrees, definition.MaxSlopeDegrees);
                Assert.GreaterOrEqual(StoryCluePlacementPlanner.Distance(crash, point), definition.MinCrashDistance);
                Assert.IsTrue(clue.transform.IsChildOf(context.GenerationRoot));
                if (definition.IsRoute)
                {
                    Assert.IsFalse(sample.IsShoreline);
                    Assert.Less(StoryCluePlacementPlanner.Distance(point, entrance.transform.position), StoryCluePlacementPlanner.Distance(camp, entrance.transform.position));
                    Assert.Greater(StoryCluePlacementPlanner.Distance(point, entrance.transform.position), 16f);
                }
                else
                {
                    Vector3 anchor = LandmarkRegistry.FindById(definition.AnchorLandmarkId).transform.position;
                    Assert.That(StoryCluePlacementPlanner.Distance(point, anchor),
                        Is.InRange(definition.MinAnchorDistance - 0.001f, definition.MaxAnchorDistance + 0.001f));
                }
                foreach (var tree in vegetation.Where(v => v.Category == VegetationCategory.Tree || v.Category == VegetationCategory.DeadTree))
                    Assert.GreaterOrEqual(StoryCluePlacementPlanner.Distance(tree.Position, point) + 0.001f, clue.VegetationClearanceRadius);
                Debug.Log($"[StoryCluePlacement] seed={seed} id={clue.ClueId} position={point:F3} crashDistance={StoryCluePlacementPlanner.Distance(crash, point):F3} campToBase={StoryCluePlacementPlanner.Distance(camp, entrance.transform.position):F3} clueToBase={StoryCluePlacementPlanner.Distance(point, entrance.transform.position):F3}");
            }
        }

        [UnityTest]
        public IEnumerator NormalInteractionClueChainKeepsBaseHiddenUntilPhysicalDiscovery()
        {
            var owner = new GameObject("ClueAcceptanceWorld");
            var ui = new GameObject("UI");
            var generator = Generator(owner);
            try
            {
                var provisioner = owner.AddComponent<RuntimeHUDProvisioner>();
                provisioner.CreateHUD(null);
                Assert.IsFalse(ui.transform.Find("PlayerHUD/StoryCluePanel").gameObject.activeSelf);
                yield return null;
                ui.GetComponent<GameStartupController>().StartNewGame();
                var context = generator.CurrentGeneration;
                var story = context.StoryProgression;
                story.Signal(StorySignalIds.CrashSurvived); story.Signal(StorySignalIds.SurvivalEstablished);
                story.Signal(StorySignalIds.RaftBuilt); story.Signal(StorySignalIds.RaftEscapeFailed);
                Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
                var cache = LandmarkRegistry.FindById("smuggler_cache");
                Assert.IsTrue(cache.GetComponent<LandmarkDiscoveryRuntime>().TryDiscoverAt(cache.transform.position));
                Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
                var clue = StoryClueRegistry.FindById("smuggler_cache_manifest");
                var player = context.Player;
                var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
                player.transform.position = clue.transform.position + new Vector3(0.7f, 0.6f, 0f);
                player.GetComponent<IsometricPlayerController>().SetMovementEnabled(false);
                Physics.SyncTransforms();
                var interaction = player.GetComponent<PlayerInteractionController>();
                interaction.TryInteract();
                Assert.AreSame(clue, interaction.CurrentInteractable, "Root collider must be found by normal Physics interaction, with clue priority over resources.");
                yield return new WaitForSeconds(0.4f);
                Assert.IsTrue(clue.IsDiscovered);
                Assert.AreEqual(StoryStageIds.LocateSmugglerBase, story.CurrentStageId);
                var panel = ui.transform.Find("PlayerHUD/StoryCluePanel");
                Assert.IsTrue(panel.gameObject.activeInHierarchy);
                Assert.AreEqual(clue.DisplayName, panel.Find("Title").GetComponent<Text>().text);
                Assert.AreEqual(clue.InspectionText, panel.Find("InspectionText").GetComponent<Text>().text);
                Assert.AreEqual(1, ui.GetComponentsInChildren<StoryClueHUDView>(true).Length);
                Assert.IsTrue(StoryClueRegistry.FindById("smuggler_camp_evidence").Interact(player));
                Assert.IsTrue(StoryClueRegistry.FindById("smuggler_route_fragment").Interact(player));
                var entrance = LandmarkRegistry.FindById("base_entrance");
                Assert.IsFalse(entrance.IsDiscovered);
                Assert.IsFalse(story.HasMilestone(StoryMilestoneIds.LandmarkDiscovered("base_entrance")));
                Assert.IsTrue(entrance.GetComponent<LandmarkDiscoveryRuntime>().TryDiscoverAt(entrance.transform.position));
                Assert.AreEqual(StoryStageIds.GainBaseAccess, story.CurrentStageId);
                Assert.IsFalse(story.HasMilestone(StorySignalIds.BaseAccessGained));
            }
            finally
            {
                Object.DestroyImmediate(ui); generator.ClearGeneratedWorld(); Object.DestroyImmediate(owner);
                GameSessionState.EnterMainMenu(); Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator SaveLoadUpdatesGeneratedCluesWithoutReplayAndOldSaveRemainsCompatible()
        {
            var owner = new GameObject("ClueSaveWorld");
            var generator = Generator(owner);
            try
            {
                generator.Generate();
                var story = generator.CurrentGeneration.StoryProgression;
                story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.InvestigateHumanTraces });
                var cache = StoryClueRegistry.FindById("smuggler_cache_manifest");
                cache.Discover();
                cache.transform.position += Vector3.up * 0.25f;
                var position = cache.transform.position;
                var service = owner.AddComponent<GameSaveService>();
                var saved = service.CaptureCurrentState();
                Assert.AreEqual(3, saved.World.ClueStates.Count);
                int signals = 0, inspections = 0, transitions = 0;
                generator.OnGenerationComplete += _ => generator.CurrentGeneration.StoryProgression.StageChanged += __ => transitions++;
                using (GameEventBus.Subscribe(e =>
                {
                    if (e.kind == GameplayEventKind.StorySignal || e.kind == GameplayEventKind.LandmarkDiscovered) signals++;
                    if (e.kind == GameplayEventKind.StoryClueInspected) inspections++;
                }))
                {
                    Assert.IsTrue(service.ApplyLoadedState(saved));
                    Assert.IsTrue(cache == null);
                    var restored = StoryClueRegistry.FindById("smuggler_cache_manifest");
                    Assert.IsTrue(restored.IsDiscovered); Assert.AreEqual(position, restored.transform.position);
                    Assert.IsFalse(StoryClueRegistry.FindById("smuggler_camp_evidence").IsDiscovered);
                    Assert.IsFalse(StoryClueRegistry.FindById("smuggler_route_fragment").IsDiscovered);
                    Assert.AreEqual(3, StoryClueRegistry.Clues.Count);
                    Assert.AreEqual(StoryStageIds.LocateSmugglerBase, generator.CurrentGeneration.StoryProgression.CurrentStageId);
                    Assert.IsTrue(generator.CurrentGeneration.StoryProgression.HasMilestone(
                        StoryMilestoneIds.ClueDiscovered("smuggler_cache_manifest")));
                    Assert.IsTrue(generator.CurrentGeneration.StoryProgression.HasMilestone(StoryClueMilestoneIds.HumanTracesFound));
                    Assert.IsFalse(restored.Discover());
                    Assert.AreEqual(0, signals); Assert.AreEqual(0, inspections); Assert.AreEqual(0, transitions);
                    // Model an old save which predates the additive clueStates member.
                    saved.World.clueStates = null;
                    Assert.IsTrue(service.ApplyLoadedState(saved));
                    Assert.AreEqual(3, StoryClueRegistry.Clues.Count);
                    Assert.IsTrue(StoryClueRegistry.Clues.All(c => !c.IsDiscovered));
                    Assert.AreEqual(0, signals); Assert.AreEqual(0, inspections); Assert.AreEqual(0, transitions);
                    var legacy = new UnityJsonGameSaveSerializer().Deserialize(@"{""world"":{
                        ""seed"":12345,""day"":3,""timeOfDay"":0.5,
                        ""biomeStates"":[{""biomeId"":""westwood"",""plantBiomass"":70,
                        ""maxPlantBiomass"":100,""varnakPopulation"":2}]}}");
                    Assert.IsTrue(service.ApplyLoadedState(legacy), "A pre-story save with legacy biome data must load into production.");
                    Assert.AreEqual(StoryStageIds.SurviveCrash, generator.CurrentGeneration.StoryProgression.CurrentStageId);
                    Assert.IsEmpty(generator.CurrentGeneration.StoryProgression.CompletedMilestones);
                    Assert.IsFalse(generator.CurrentGeneration.SmugglerBaseInterior.IsPlayerInside);
                    Assert.AreEqual(0, signals); Assert.AreEqual(0, inspections); Assert.AreEqual(0, transitions);
                    yield return null;
                }
            }
            finally { generator.ClearGeneratedWorld(); Object.DestroyImmediate(owner); }
        }
    }
}
#endif
