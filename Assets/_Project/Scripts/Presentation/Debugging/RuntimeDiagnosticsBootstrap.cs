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

        public void Configure(WorldGeneratorRuntime owner)
        {
            if (generator != null) generator.OnGenerationComplete -= HandleGenerationComplete;
            generator = owner;
            if (generator != null) generator.OnGenerationComplete += HandleGenerationComplete;
        }

        private void OnDestroy()
        {
            if (generator != null) generator.OnGenerationComplete -= HandleGenerationComplete;
            if (snapshotProvider != null) snapshotProvider.SetAutoRefresh(false);
            CleanupDiagnostics();
        }

        private void HandleGenerationComplete(GameObject player)
        {
            CleanupDiagnostics();
            if (!RuntimeDebugSettings.DeveloperDiagnosticsEnabled)
            {
                snapshotProvider?.SetAutoRefresh(false);
                return;
            }

            snapshotProvider?.SetAutoRefresh(true);
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
                PlayerActionDebugLog log = player.GetComponent<PlayerActionDebugLog>() ?? player.AddComponent<PlayerActionDebugLog>();
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

            EcosystemRuntime ecosystem = generationRoot.GetComponentInChildren<EcosystemRuntime>(true);
            if (ecosystem != null)
            {
                foreach (CreatureAgentView creature in ecosystem.Creatures)
                {
                    if (creature == null) continue;
                    CreatureDebugOverlay overlay = creature.GetComponent<CreatureDebugOverlay>() ?? creature.gameObject.AddComponent<CreatureDebugOverlay>();
                    CreatureBehaviorBrain brain = creature.GetComponent<CreatureBehaviorBrain>();
                    if (brain != null) overlay.SetBehaviorState(brain.State);
                }
            }

            GameObject ui = GameObject.Find("UI");
            if (ui != null) uiDebugger = ui.GetComponent<UIDebugger>() ?? ui.AddComponent<UIDebugger>();
        }

        private void CleanupDiagnostics()
        {
            if (snapshotProvider != null) snapshotProvider.SetAutoRefresh(false);
            if (uiDebugger != null) DestroyOwned(uiDebugger);
            if (debugPanel != null) DestroyOwned(debugPanel);
            if (worldMapDebug != null) DestroyOwned(worldMapDebug);
            if (generationPresenter != null) DestroyOwned(generationPresenter);
            if (playerDiagnostics != null)
            {
                PlayerActionDebugLog log = playerDiagnostics.GetComponent<PlayerActionDebugLog>();
                if (log != null) DestroyOwned(log);
            }
            uiDebugger = null;
            debugPanel = null;
            worldMapDebug = null;
            generationPresenter = null;
            playerDiagnostics = null;
        }

        private static void DestroyOwned(Component instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }
    }
}
