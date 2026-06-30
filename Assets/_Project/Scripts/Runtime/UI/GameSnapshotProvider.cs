using System;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.World.Generation;
using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.UI.Snapshots
{
    [DisallowMultipleComponent]
    public sealed class GameSnapshotProvider : MonoBehaviour
    {
        [SerializeField] private WorldGeneratorRuntime worldGenerator;
        [SerializeField] private PlayerInventoryRuntime playerInventory;
        [SerializeField] private PlayerSurvivalRuntime playerSurvival;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private DayNightRuntime dayNightRuntime;
        [SerializeField] private float refreshIntervalSeconds = 0.5f;
        [SerializeField] private bool autoRefresh = true;
        private float refreshTimer;
        private float smoothedFps;
        private GameSnapshot lastSnapshot = GameSnapshot.Empty;
        public GameSnapshot LastSnapshot => lastSnapshot;
        public event Action<GameSnapshot> SnapshotUpdated;
        private void Awake() { ResolveReferences(); CaptureNow(); }
        private void Update()
        {
            UpdateFps();
            if (!autoRefresh) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f) { refreshTimer = Mathf.Max(0.1f, refreshIntervalSeconds); CaptureNow(); }
        }
        public GameSnapshot CaptureNow()
        {
            ResolveReferences();
            InventorySnapshot inventory = InventorySnapshot.FromInventory(playerInventory != null ? playerInventory.Inventory : null);
            SurvivalSnapshot survival = playerSurvival != null ? SurvivalSnapshot.FromStats(playerSurvival.Stats, playerSurvival.ConditionText, playerSurvival.CanSprint, playerSurvival.IsSprinting) : SurvivalSnapshot.Empty;
            WorldDebugSnapshot world = CaptureWorldDebugSnapshot();
            DayNightSnapshot dayNight = DayNightSnapshot.FromRuntime(dayNightRuntime);
            lastSnapshot = new GameSnapshot(inventory, survival, world, dayNight, Time.realtimeSinceStartup);
            SnapshotUpdated?.Invoke(lastSnapshot);
            return lastSnapshot;
        }
        private WorldDebugSnapshot CaptureWorldDebugSnapshot()
        {
            Transform player = ResolvePlayerTransform();
            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
            ResourceNodeView[] resources = UnityEngine.Object.FindObjectsByType<ResourceNodeView>(FindObjectsInactive.Exclude);
            CreatureAgentView[] creatures = UnityEngine.Object.FindObjectsByType<CreatureAgentView>(FindObjectsInactive.Exclude);
            CreatureNeedsRuntime[] needs = UnityEngine.Object.FindObjectsByType<CreatureNeedsRuntime>(FindObjectsInactive.Exclude);
            FoodSourceView[] foods = UnityEngine.Object.FindObjectsByType<FoodSourceView>(FindObjectsInactive.Exclude);
            ItemPickupView[] pickups = UnityEngine.Object.FindObjectsByType<ItemPickupView>(FindObjectsInactive.Exclude);
            NavMeshAgent[] navAgents = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude);
            int plantFood = ecosystem != null ? ecosystem.PlantFoodSourceCount : foods.Count(food => food != null && food.Kind == FoodKind.Plants);
            int meatFood = ecosystem != null ? ecosystem.MeatFoodSourceCount : foods.Count(food => food != null && food.Kind == FoodKind.Meat);
            int foodCount = ecosystem != null ? ecosystem.FoodSourceCount : foods.Length;
            int storageContainers = UnityEngine.Object.FindObjectsByType<StorageContainerRuntime>(FindObjectsInactive.Exclude).Length;
            int creatureCount = ecosystem != null ? ecosystem.CreatureCount : creatures.Length;
            int navOnMesh = navAgents.Count(agent => agent != null && agent.isOnNavMesh);
            int hungryCreatures = needs.Count(need => need != null && need.State.IsHungry);
            string[] recentEvents = GameEventBus.GetRecentEventLines(8);
            return new WorldDebugSnapshot(worldGenerator != null ? worldGenerator.Seed : 0, player != null ? player.position : Vector3.zero, player != null, resources.Length, creatureCount, foodCount, plantFood, meatFood, navOnMesh, Mathf.Max(0, navAgents.Length - navOnMesh), hungryCreatures, storageContainers, pickups.Length, smoothedFps, Time.realtimeSinceStartup, recentEvents);
        }
        private void ResolveReferences()
        {
            if (worldGenerator == null) worldGenerator = UnityEngine.Object.FindAnyObjectByType<WorldGeneratorRuntime>();
            if (playerInventory == null) playerInventory = UnityEngine.Object.FindAnyObjectByType<PlayerInventoryRuntime>();
            if (playerSurvival == null) playerSurvival = UnityEngine.Object.FindAnyObjectByType<PlayerSurvivalRuntime>();
            if (dayNightRuntime == null) dayNightRuntime = UnityEngine.Object.FindAnyObjectByType<DayNightRuntime>();
            ResolvePlayerTransform();
        }
        private Transform ResolvePlayerTransform()
        {
            if (playerTransform != null && playerTransform.gameObject.activeInHierarchy) return playerTransform;
            if (playerSurvival != null) { playerTransform = playerSurvival.transform; return playerTransform; }
            GameObject player = GameObject.Find("Player");
            if (player != null) { playerTransform = player.transform; return playerTransform; }
            IsometricPlayerController controller = UnityEngine.Object.FindAnyObjectByType<IsometricPlayerController>();
            if (controller != null) playerTransform = controller.transform;
            return playerTransform;
        }
        private void UpdateFps()
        {
            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f) return;
            float instantFps = 1f / delta;
            smoothedFps = smoothedFps <= 0f ? instantFps : Mathf.Lerp(smoothedFps, instantFps, 0.08f);
        }
    }
}
