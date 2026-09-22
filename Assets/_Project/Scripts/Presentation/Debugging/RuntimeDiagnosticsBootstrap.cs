using System.Collections.Generic;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.UI.Snapshots;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.Ecosystem;
using UnityEngine;

namespace ApexShift.Presentation.Debugging
{
    /// <summary>Owns optional developer diagnostics for the active world generation.</summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeDiagnosticsBootstrap : MonoBehaviour
    {
        private WorldGeneratorRuntime generator;
        private GameSnapshotProvider snapshotProvider;
        private GameObject playerDiagnostics;
        private UIDebugger uiDebugger;
        private DebugPanelPresenter debugPanel;
        private WorldMapDebugWindow worldMapDebug;
        private WorldGenerationDebugPresenter generationPresenter;
        private EcosystemDebugOverlay ecosystemOverlay;
        private EcosystemRuntime ecosystem;
        private readonly List<CreatureDebugOverlay> ownedCreatureOverlays = new List<CreatureDebugOverlay>();
        private bool ownsPlayerDiagnostics;
        private bool ownsUiDebugger;

        public void Configure(WorldGeneratorRuntime owner)
        {
            if (generator != null) generator.OnGenerationComplete -= HandleGenerationComplete;
            RuntimeDebugSettings.DeveloperDiagnosticsChanged -= HandleDiagnosticsChanged;
            generator = owner;
            if (generator != null) generator.OnGenerationComplete += HandleGenerationComplete;
            RuntimeDebugSettings.DeveloperDiagnosticsChanged += HandleDiagnosticsChanged;
            ApplyDiagnosticsState(RuntimeDebugSettings.DeveloperDiagnosticsEnabled, generator?.CurrentGeneration?.Player);
        }

        private void OnDestroy()
        {
            if (generator != null) generator.OnGenerationComplete -= HandleGenerationComplete;
            RuntimeDebugSettings.DeveloperDiagnosticsChanged -= HandleDiagnosticsChanged;
            if (snapshotProvider != null) snapshotProvider.SetAutoRefresh(false);
            CleanupDiagnostics();
        }

        private void HandleGenerationComplete(GameObject player)
        {
            ApplyDiagnosticsState(RuntimeDebugSettings.DeveloperDiagnosticsEnabled, player);
        }

        private void HandleDiagnosticsChanged(bool enabled)
        {
            ApplyDiagnosticsState(enabled, generator?.CurrentGeneration?.Player);
        }

        private void ApplyDiagnosticsState(bool enabled, GameObject player)
        {
            CleanupDiagnostics();
            if (!enabled)
            {
                return;
            }

            Transform generationRoot = generator != null ? generator.CurrentGeneration?.GenerationRoot : transform.parent;
            if (generationRoot == null) return;
            snapshotProvider = generationRoot.GetComponentInChildren<GameSnapshotProvider>(true);
            snapshotProvider?.SetAutoRefresh(true);

            debugPanel = generationRoot.gameObject.AddComponent<DebugPanelPresenter>();
            debugPanel.SetSnapshotProvider(snapshotProvider);
            worldMapDebug = generationRoot.gameObject.AddComponent<WorldMapDebugWindow>();
            worldMapDebug.SetSnapshotProvider(snapshotProvider);
            generationPresenter = generationRoot.gameObject.AddComponent<WorldGenerationDebugPresenter>();
            WorldGenerationContext context = generator?.CurrentGeneration;
            generationPresenter.Configure(context?.Result, context?.IslandTopography);

            if (player != null)
            {
                PlayerActionDebugLog log = player.GetComponent<PlayerActionDebugLog>();
                if (log == null)
                {
                    log = player.AddComponent<PlayerActionDebugLog>();
                    ownsPlayerDiagnostics = true;
                }
                PlayerInputReader input = player.GetComponent<PlayerInputReader>();
                IsometricPlayerController controller = player.GetComponent<IsometricPlayerController>();
                PlayerMotionVisualFeedback motion = player.GetComponent<PlayerMotionVisualFeedback>();
                log.SetInputReader(input);
                log.SetWatchedTarget(player.transform);
                log.SetMovementController(controller);
                log.SetMotionFeedback(motion);
                log.SetCameraFollow(Camera.main != null ? Camera.main.GetComponent<IsometricCameraFollow>() : null);
                playerDiagnostics = player;
            }

            ecosystem = generationRoot.GetComponentInChildren<EcosystemRuntime>(true);
            if (ecosystem != null)
            {
                ecosystemOverlay = generationRoot.gameObject.AddComponent<EcosystemDebugOverlay>();
                ecosystemOverlay.Configure(ecosystem);
                ecosystem.CreatureRegistered += HandleCreatureRegistered;
                ecosystem.CreatureUnregistered += HandleCreatureUnregistered;
                foreach (CreatureAgentView creature in ecosystem.Creatures)
                {
                    AttachCreatureOverlay(creature);
                }
            }

            GameObject ui = GameObject.Find("UI");
            if (ui != null)
            {
                uiDebugger = ui.GetComponent<UIDebugger>();
                if (uiDebugger == null)
                {
                    uiDebugger = ui.AddComponent<UIDebugger>();
                    ownsUiDebugger = true;
                }
            }
        }

        private void HandleCreatureRegistered(CreatureAgentView creature)
        {
            if (RuntimeDebugSettings.DeveloperDiagnosticsEnabled) AttachCreatureOverlay(creature);
        }

        private void HandleCreatureUnregistered(CreatureAgentView creature)
        {
            for (int i = ownedCreatureOverlays.Count - 1; i >= 0; i--)
            {
                CreatureDebugOverlay overlay = ownedCreatureOverlays[i];
                if (overlay == null || overlay.GetComponent<CreatureAgentView>() == creature)
                {
                    if (overlay != null) DestroyOwned(overlay);
                    ownedCreatureOverlays.RemoveAt(i);
                }
            }
        }

        private void AttachCreatureOverlay(CreatureAgentView creature)
        {
            if (creature == null || creature.GetComponent<CreatureDebugOverlay>() != null) return;
            CreatureDebugOverlay overlay = creature.gameObject.AddComponent<CreatureDebugOverlay>();
            CreatureBehaviorBrain brain = creature.GetComponent<CreatureBehaviorBrain>();
            if (brain != null) overlay.SetBehaviorState(brain.State);
            ownedCreatureOverlays.Add(overlay);
        }

        private void CleanupDiagnostics()
        {
            if (snapshotProvider != null) snapshotProvider.SetAutoRefresh(false);
            if (uiDebugger != null && ownsUiDebugger) DestroyOwned(uiDebugger);
            if (debugPanel != null) DestroyOwned(debugPanel);
            if (worldMapDebug != null) DestroyOwned(worldMapDebug);
            if (generationPresenter != null) DestroyOwned(generationPresenter);
            if (ecosystemOverlay != null) DestroyOwned(ecosystemOverlay);
            if (playerDiagnostics != null)
            {
                PlayerActionDebugLog log = playerDiagnostics.GetComponent<PlayerActionDebugLog>();
                if (log != null && ownsPlayerDiagnostics) DestroyOwned(log);
            }
            if (ecosystem != null)
            {
                ecosystem.CreatureRegistered -= HandleCreatureRegistered;
                ecosystem.CreatureUnregistered -= HandleCreatureUnregistered;
            }
            for (int i = ownedCreatureOverlays.Count - 1; i >= 0; i--)
            {
                if (ownedCreatureOverlays[i] != null) DestroyOwned(ownedCreatureOverlays[i]);
            }
            ownedCreatureOverlays.Clear();
            ecosystem = null;
            uiDebugger = null;
            debugPanel = null;
            worldMapDebug = null;
            generationPresenter = null;
            ecosystemOverlay = null;
            playerDiagnostics = null;
            ownsPlayerDiagnostics = false;
            ownsUiDebugger = false;
        }

        private static void DestroyOwned(Component instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }
    }
}
