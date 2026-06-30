using System.Text;
using System.Linq;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.UI.Snapshots;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ApexShift.Runtime.UI.Debugging
{
    [DisallowMultipleComponent]
    public sealed class DebugPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameSnapshotProvider snapshotProvider;
        [SerializeField] private Text debugText;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private bool visible;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private float presentIntervalSeconds = 0.5f;
        private float presentTimer;
        private string cachedText = "Debug snapshot waiting...";
        private void Awake() { ResolveReferences(); ApplyVisibility(); PresentNow(); }
        private void Update()
        {
            if (!GameSessionState.IsGameplayActive) { if (visible) { visible = false; ApplyVisibility(); } return; }
            if (WasTogglePressed()) { visible = !visible; ApplyVisibility(); }
            if (!visible) return;
            presentTimer -= Time.unscaledDeltaTime;
            if (presentTimer <= 0f) { presentTimer = Mathf.Max(0.1f, presentIntervalSeconds); PresentNow(); }
        }
        public void PresentNow()
        {
            ResolveReferences();
            GameSnapshot snapshot = snapshotProvider != null ? snapshotProvider.LastSnapshot : GameSnapshot.Empty;
            cachedText = FormatSnapshot(snapshot);
            if (debugText != null) debugText.text = cachedText;
        }
        public static string FormatSnapshot(GameSnapshot snapshot)
        {
            snapshot ??= GameSnapshot.Empty;
            WorldDebugSnapshot world = snapshot.worldDebug ?? WorldDebugSnapshot.Empty;
            SurvivalSnapshot survival = snapshot.survival ?? SurvivalSnapshot.Empty;
            InventorySnapshot inventory = snapshot.inventory ?? InventorySnapshot.Empty;
            DayNightSnapshot dayNight = snapshot.dayNight ?? DayNightSnapshot.Empty;
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine("=== GAME SNAPSHOT ===");
            builder.AppendLine($"t: {world.realtimeSinceStartup:0.0}s  fps: {world.fps:0}");
            builder.AppendLine($"seed: {world.seed}");
            builder.AppendLine($"day: {dayNight.day}  time: {dayNight.ClockText}  phase: {dayNight.phaseLabel}");
            builder.AppendLine($"player: {(world.hasPlayer ? world.playerPosition.ToString("F1") : "missing")}");
            builder.AppendLine();
            builder.AppendLine("=== WORLD ===");
            builder.AppendLine($"resources: {world.resourceCount}");
            builder.AppendLine($"creatures: {world.creatureCount}  hungry: {world.hungryCreatureCount}");
            builder.AppendLine($"food all/plants/meat: {world.foodSourceCount}/{world.plantFoodSourceCount}/{world.meatFoodSourceCount}");
            builder.AppendLine($"nav on/off: {world.navAgentsOnMesh}/{world.navAgentsOffMesh}");
            builder.AppendLine();
            builder.AppendLine("=== SURVIVAL ===");
            builder.AppendLine($"hp/hun/sta/rest: {survival.health:0}/{survival.hunger:0}/{survival.stamina:0}/{survival.rest:0}");
            builder.AppendLine($"condition: {survival.conditionText}");
            builder.AppendLine($"sprint: can={survival.canSprint} active={survival.isSprinting}");
            builder.AppendLine();
            builder.AppendLine("=== INVENTORY ===");
            builder.AppendLine($"slots: {inventory.occupiedSlotCount}/{inventory.slotCount}  empty: {inventory.emptySlotCount}");
            foreach (InventoryItemSnapshot item in inventory.items.OrderBy(item => item.itemId, System.StringComparer.Ordinal))
            {
                builder.AppendLine($"- {item.itemId}: {item.amount}");
            }
            return builder.ToString();
        }
        private bool WasTogglePressed()
        {
            if (Keyboard.current != null)
            {
                if (toggleKey == KeyCode.F3 && Keyboard.current[Key.F3].wasPressedThisFrame) return true;
                if (toggleKey == KeyCode.F4 && Keyboard.current[Key.F4].wasPressedThisFrame) return true;
                if (toggleKey == KeyCode.F1 && Keyboard.current[Key.F1].wasPressedThisFrame) return true;
            }
            return UnityEngine.Input.GetKeyDown(toggleKey);
        }
        private void ResolveReferences()
        {
            if (snapshotProvider == null) snapshotProvider = Object.FindAnyObjectByType<GameSnapshotProvider>();
            if (debugText == null) debugText = GetComponentInChildren<Text>(true);
            if (panelRoot == null && debugText != null) panelRoot = debugText.transform.parent != null ? debugText.transform.parent.gameObject : debugText.gameObject;
            if (panelRoot == null) panelRoot = gameObject;
        }
        private void ApplyVisibility() { if (panelRoot != null) panelRoot.SetActive(visible); }
    }
}
