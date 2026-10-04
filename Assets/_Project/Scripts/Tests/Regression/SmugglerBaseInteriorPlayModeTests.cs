#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.Story.Clues;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Interiors;
using ApexShift.Runtime.World.Landmarks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;

namespace ApexShift.Tests.Regression
{
    public sealed class SmugglerBaseInteriorPlayModeTests
    {
        [UnityTest]
        public IEnumerator ControllerOverrideKeepsMovementOutsideIslandAndClampsInteriorEdges()
        {
            var island = new GameObject("IslandBounds"); var actor = new GameObject("BoundsActor");
            float previousTimeScale = Time.timeScale;
            try
            {
                var worldBounds = island.AddComponent<ApexShift.Runtime.World.WorldBounds>();
                worldBounds.Configure(4f, new[] { Vector3.zero });
                var controller = actor.AddComponent<IsometricPlayerController>();
                actor.transform.position = new Vector3(500f, 1f, 500f);
                var bounds = new Bounds(actor.transform.position, new Vector3(10f, 4f, 10f));
                controller.SetMovementBoundsOverride(bounds); controller.SetTopographyWaterQueriesEnabled(false);
                yield return null;
                Time.timeScale = 1f;
                yield return null;
                var move = typeof(IsometricPlayerController).GetMethod("MoveWithWorldBounds",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                move.Invoke(controller, new object[] { Vector3.right });
                Assert.Greater(actor.transform.position.x, 500f);
                move.Invoke(controller, new object[] { Vector3.right * 100000f });
                Assert.AreEqual(bounds.max.x, actor.transform.position.x, 0.001f);
                Assert.AreSame(worldBounds, ApexShift.Runtime.World.WorldBounds.Active);
                Assert.IsFalse(worldBounds.Contains(actor.transform.position));
                controller.EnterWater(); controller.RefreshWaterState(); Assert.IsFalse(controller.IsInWater);
                var animation = actor.AddComponent<PlayerAnimationDriver>();
                var detector = actor.AddComponent<PlayerWaterDetector>();
                var water = new GameObject("DelayedWaterTrigger", typeof(BoxCollider), typeof(WaterVolume));
                try
                {
                    typeof(PlayerWaterDetector).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(detector, new object[] { water.GetComponent<Collider>() });
                    Assert.IsFalse((bool)typeof(PlayerAnimationDriver).GetField("isSwimming", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(animation));
                }
                finally { Object.DestroyImmediate(water); }
                controller.ClearMovementBoundsOverride(); controller.SetTopographyWaterQueriesEnabled(true);
                Assert.IsFalse(controller.HasMovementBoundsOverride);
            }
            finally { Time.timeScale = previousTimeScale; Object.DestroyImmediate(actor); Object.DestroyImmediate(island); }
        }

        [UnityTest]
        public IEnumerator ProductionTraversalSaveLoadPreservesStateAndDoesNotReplaySignals()
        {
            var owner = new GameObject("InteriorAcceptance");
            var generator = owner.AddComponent<WorldGeneratorRuntime>(); generator.SetGenerateOnStart(false); generator.SetSeed(81281);
            generator.SetBiomeCatalog(AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>("Assets/_Project/Data/Biomes/BiomeCatalog.asset"));
            generator.SetHabitatVegetationCatalog(AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>("Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset"));
            try
            {
                generator.Generate();
                var c = generator.CurrentGeneration; var interior = c.SmugglerBaseInterior;
                Assert.NotNull(interior); Assert.IsFalse(c.InteriorRoot.gameObject.activeSelf);
                var entrance = LandmarkRegistry.FindById("base_entrance");
                var interaction = entrance.GetComponent<BaseEntranceInteractionRuntime>();
                Assert.NotNull(interaction); Assert.NotNull(entrance.GetComponent<Collider>());
                Assert.NotNull(entrance.GetComponent<LandmarkDiscoveryRuntime>());
                var player = c.Player; var controller = player.GetComponent<IsometricPlayerController>();
                var inventory = player.GetComponent<PlayerInventoryRuntime>(); var survival = player.GetComponent<PlayerSurvivalRuntime>();
                inventory.Inventory.AddItem("wood", 7); inventory.Inventory.AddItem("fiber", 4);
                survival.LoadFromSaveData(new SurvivalSaveData(73f, 61f, 52f, 44f));
                Assert.IsFalse(interaction.CanInteract(player)); Assert.IsFalse(interior.TryEnter(null));
                Assert.IsFalse(interior.TryEnter(owner));
                entrance.SetDiscovered(true);
                Assert.IsFalse(interaction.CanInteract(player)); Assert.IsFalse(interior.TryEnter(player));
                entrance.SetDiscovered(false);
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                GameEventBus.PublishStorySignal(StorySignalIds.SurvivalEstablished);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftBuilt);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftEscapeFailed);
                Assert.IsTrue(StoryClueRegistry.FindById("smuggler_cache_manifest").Discover());
                Assert.AreEqual(StoryStageIds.LocateSmugglerBase, c.StoryProgression.CurrentStageId);
                entrance.Discover();
                Assert.AreEqual(StoryStageIds.GainBaseAccess, c.StoryProgression.CurrentStageId);
                Assert.IsFalse(c.StoryProgression.HasMilestone(StorySignalIds.BaseAccessGained));
                Assert.IsFalse(c.InteriorRoot.gameObject.activeSelf);
                player.transform.position = entrance.transform.position + Vector3.right * 2f;
                Physics.SyncTransforms();
                string invBefore = JsonUtility.ToJson(inventory.ToSaveData());
                string statsBefore = JsonUtility.ToJson(survival.ToSaveData());
                int accessSignals = 0;
                using (GameEventBus.Subscribe(e => { if (e.kind == GameplayEventKind.StorySignal && e.signalId == StorySignalIds.BaseAccessGained) accessSignals++; }))
                {
                    for (int cycle = 0; cycle < 3; cycle++)
                    {
                        Assert.IsTrue(interaction.Interact(player)); Assert.IsFalse(interior.TryEnter(player));
                        Assert.AreSame(c, generator.CurrentGeneration); Assert.AreSame(player, c.Player);
                        Assert.IsFalse(player.transform.IsChildOf(c.InteriorRoot));
                        Assert.IsTrue(controller.MovementEnabled); Assert.IsFalse(controller.IsInWater);
                        Assert.IsTrue(controller.HasMovementBoundsOverride); Assert.IsFalse(controller.TopographyWaterQueriesEnabled);
                        Assert.AreEqual(StoryStageIds.PrepareBoat, c.StoryProgression.CurrentStageId);
                        Assert.AreEqual(invBefore, JsonUtility.ToJson(inventory.ToSaveData()));
                        Assert.AreEqual(statsBefore, JsonUtility.ToJson(survival.ToSaveData()));
                        Assert.IsTrue(interior.IslandExitInteraction.GetComponent<BaseInteriorExitRuntime>().Interact(player));
                        Assert.IsFalse(interior.IsPlayerInside); Assert.IsFalse(c.InteriorRoot.gameObject.activeSelf);
                        Assert.IsFalse(controller.HasMovementBoundsOverride); Assert.IsTrue(controller.TopographyWaterQueriesEnabled);
                        Assert.Less(Vector3.Distance(player.transform.position, interior.IslandReturnPosition), 0.3f);
                    }
                    Assert.AreEqual(1, accessSignals);
                    interaction.Interact(player);
                    Assert.Less(Vector3.Distance(c.MainCamera.transform.position, player.transform.position), 30f, "Camera must snap immediately.");
                    var walker = player.GetComponent<CharacterController>();
                    for (int step = 0; step < 180; step++) walker.Move(new Vector3(0f, 0f, 0.2f));
                    Assert.Greater(c.InteriorRoot.InverseTransformPoint(player.transform.position).z, 33f, "All four rooms must connect through walkable doorways.");
                    controller.RefreshWaterState(); controller.EnterWater();
                    Assert.IsFalse(controller.IsInWater);
                    var cc = player.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
                    player.transform.position = c.InteriorRoot.TransformPoint(new Vector3(0f, 0.1f, 22f));
                    Physics.SyncTransforms(); if (cc != null) cc.enabled = true;
                    var service = owner.AddComponent<GameSaveService>();
                    var saved = service.CaptureCurrentState();
                    Assert.AreEqual(PlayerAreaIds.SmugglerBase, saved.World.PlayerLocation.AreaId);
                    Assert.IsTrue(saved.World.PlayerLocation.HasLocalPosition);
                    Assert.AreEqual(22f, saved.World.PlayerLocation.LocalZ, 0.01f);
                    Vector3 returnPoint = interior.IslandReturnPosition;
                    Assert.AreEqual(returnPoint.x, saved.Survival.posX, 0.01f);
                    int transitions = 0;
                    Assert.IsTrue(service.ApplyLoadedState(saved));
                    c = generator.CurrentGeneration; interior = c.SmugglerBaseInterior; player = c.Player;
                    c.StoryProgression.StageChanged += _ => transitions++;
                    Assert.IsTrue(interior.IsPlayerInside); Assert.IsTrue(c.InteriorRoot.gameObject.activeSelf);
                    Assert.AreEqual(22f, c.InteriorRoot.InverseTransformPoint(player.transform.position).z, 0.01f);
                    Assert.AreEqual(invBefore, JsonUtility.ToJson(player.GetComponent<PlayerInventoryRuntime>().ToSaveData()));
                    Assert.AreEqual(statsBefore, JsonUtility.ToJson(player.GetComponent<PlayerSurvivalRuntime>().ToSaveData()));
                    Assert.AreEqual(1, accessSignals); Assert.AreEqual(0, transitions);
                    foreach (float badCoordinate in new[] { float.NaN, float.PositiveInfinity, 10000f })
                    {
                        var bad = new PlayerLocationSaveData { areaId = PlayerAreaIds.SmugglerBase,
                            hasLocalPosition = true, localX = badCoordinate, localY = 0.1f, localZ = 22f };
                        LogAssert.Expect(LogType.Warning, "[Interior] Invalid saved interior position; using EntrySpawn.");
                        interior.RestorePlayerLocation(player, bad, returnPoint);
                        Assert.Less(Vector3.Distance(player.transform.position, interior.EntrySpawn.position), 0.01f);
                    }
                    LogAssert.Expect(LogType.Warning, "[Interior] Unknown or inaccessible saved area; returning to island.");
                    interior.RestorePlayerLocation(player, new PlayerLocationSaveData { areaId = "unknown_area" }, returnPoint);
                    Assert.IsFalse(interior.IsPlayerInside);
                    c.StoryProgression.RestoreSaveData(StorySaveData.Default);
                    LogAssert.Expect(LogType.Warning, "[Interior] Unknown or inaccessible saved area; returning to island.");
                    interior.RestorePlayerLocation(player, saved.World.PlayerLocation, returnPoint);
                    Assert.IsFalse(interior.IsPlayerInside);
                    c.StoryProgression.RestoreSaveData(saved.World.StoryState);
                    Assert.IsTrue(LandmarkRegistry.FindById("base_entrance").GetComponent<BaseEntranceInteractionRuntime>().Interact(player));
                    Assert.AreEqual(1, c.GenerationRoot.GetComponentsInChildren<SmugglerBaseInteriorRuntime>(true).Length);
                    Assert.NotNull(interior.BoatAnchor.Find("SmugglerBoatPlaceholder"));
                    Assert.AreEqual("DockChamber", interior.BoatAnchor.parent.name);
                    Assert.IsTrue(interior.TryExit(player));
                    Assert.Less(Vector3.Distance(returnPoint, player.transform.position), 0.3f);
                    var islandSave = service.CaptureCurrentState(); Assert.AreEqual(PlayerAreaIds.Island, islandSave.World.PlayerLocation.AreaId);
                    islandSave.World.playerLocation = null; // Old save compatibility uses survival position.
                    Assert.IsTrue(service.ApplyLoadedState(islandSave));
                    Assert.IsFalse(generator.CurrentGeneration.InteriorRoot.gameObject.activeSelf);
                    Assert.AreEqual(1, accessSignals);
                }
                yield return null;
            }
            finally { generator.ClearGeneratedWorld(); Object.DestroyImmediate(owner); }
        }
    }
}
#endif
