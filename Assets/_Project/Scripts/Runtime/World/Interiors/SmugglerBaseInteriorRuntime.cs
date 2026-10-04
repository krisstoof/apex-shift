using ApexShift.Core.Save;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using UnityEngine;
using Unity.Cinemachine;

namespace ApexShift.Runtime.World.Interiors
{
    [DisallowMultipleComponent]
    public sealed class SmugglerBaseInteriorRuntime : MonoBehaviour
    {
        private WorldGenerationContext context;
        private WorldGeneratorRuntime generator;
        private LandmarkRuntime entrance;
        private GameObject insidePlayer;
        private BuildingPlacementRuntime placement;
        private bool placementWasEnabled;
        private bool waterQueriesWereEnabled;
        public bool IsPlayerInside => insidePlayer != null;
        public string CurrentAreaId => IsPlayerInside ? PlayerAreaIds.SmugglerBase : PlayerAreaIds.Island;
        public Transform InteriorRoot => context.InteriorRoot;
        public Transform EntrySpawn { get; private set; }
        public Transform IslandExitInteraction { get; private set; }
        public Transform StorageLootAnchor { get; private set; }
        public Transform OperationsClueAnchor { get; private set; }
        public Transform FuelAnchor { get; private set; }
        public Transform BatteryAnchor { get; private set; }
        public Transform BoatKeyAnchor { get; private set; }
        public Transform BoatAnchor { get; private set; }
        public ApexShift.Runtime.Escape.EscapeBoatRuntime EscapeBoat { get; private set; }
        public void ConfigureEscapeBoat(ApexShift.Runtime.Escape.EscapeBoatRuntime boat) => EscapeBoat = boat;
        public Bounds InteriorMovementBounds { get; private set; }
        public Vector3 IslandReturnPosition { get; private set; }

        public void Configure(WorldGenerationContext generation, WorldGeneratorRuntime worldGenerator,
            LandmarkRuntime baseEntrance, Transform[] anchors, Bounds bounds)
        {
            context = generation; generator = worldGenerator; entrance = baseEntrance;
            EntrySpawn = anchors[0]; IslandExitInteraction = anchors[1]; StorageLootAnchor = anchors[2];
            OperationsClueAnchor = anchors[3]; FuelAnchor = anchors[4]; BatteryAnchor = anchors[5];
            BoatKeyAnchor = anchors[6]; BoatAnchor = anchors[7]; InteriorMovementBounds = bounds;
            if (!ValidateLayout()) throw new System.InvalidOperationException("Incomplete smuggler base layout.");
        }

        public bool ValidateLayout() => EntrySpawn != null && IslandExitInteraction != null
            && BoatAnchor != null && FuelAnchor != null && BatteryAnchor != null && BoatKeyAnchor != null
            && InteriorMovementBounds.size.x > 0f && InteriorMovementBounds.size.z > 0f;

        private bool IsPlayer(GameObject actor) => actor != null && context != null
            && actor == context.Player && actor.GetComponent<IsometricPlayerController>() != null;
        public bool IsInsidePlayer(GameObject actor) => IsPlayer(actor) && insidePlayer == actor;
        public bool CanEnter(GameObject actor)
        {
            var story = context?.StoryProgression;
            if (!IsPlayer(actor) || IsPlayerInside || entrance == null || !entrance.IsDiscovered || story == null) return false;
            var registry = context.BuildingRoot != null ? context.BuildingRoot.GetComponent<BuildingRegistry>() : null;
            if (registry != null && registry.TryGetActiveRaftAttempt(out var raft) && raft.MountedPlayer == actor) return false;
            return story.CurrentStageId == StoryStageIds.GainBaseAccess || story.HasMilestone(StorySignalIds.BaseAccessGained);
        }

        public bool TryEnter(GameObject player)
        {
            if (!CanEnter(player)) return false;
            IslandReturnPosition = ResolveSafeReturn(player.transform.position);
            EnterMode(player);
            Teleport(player, EntrySpawn.position);
            if (!context.StoryProgression.HasMilestone(StorySignalIds.BaseAccessGained))
                GameEventBus.PublishStorySignal(StorySignalIds.BaseAccessGained, "base_entrance");
            return true;
        }

        public bool TryExit(GameObject player)
        {
            if (!IsInsidePlayer(player) || (EscapeBoat != null && EscapeBoat.EscapeInProgress)) return false;
            var controller = player.GetComponent<IsometricPlayerController>();
            controller.ClearMovementBoundsOverride();
            Teleport(player, ResolveSafeReturn(IslandReturnPosition));
            controller.SetTopographyWaterQueriesEnabled(waterQueriesWereEnabled);
            controller.RefreshWaterState();
            if (placement != null) placement.enabled = placementWasEnabled;
            placement = null; insidePlayer = null;
            InteriorRoot.gameObject.SetActive(false);
            return true;
        }

        private void EnterMode(GameObject player)
        {
            InteriorRoot.gameObject.SetActive(true);
            insidePlayer = player;
            var controller = player.GetComponent<IsometricPlayerController>();
            waterQueriesWereEnabled = controller.TopographyWaterQueriesEnabled;
            controller.SetMovementBoundsOverride(InteriorMovementBounds);
            controller.SetTopographyWaterQueriesEnabled(false);
            placement = player.GetComponent<BuildingPlacementRuntime>();
            if (placement != null)
            {
                placementWasEnabled = placement.enabled;
                placement.ClearSelection();
                placement.enabled = false;
            }
        }

        public PlayerLocationSaveData CapturePlayerLocation(GameObject player)
        {
            if (!IsInsidePlayer(player)) return PlayerLocationSaveData.Island;
            Vector3 p = InteriorRoot.InverseTransformPoint(player.transform.position);
            return new PlayerLocationSaveData {
                areaId = PlayerAreaIds.SmugglerBase, hasLocalPosition = true,
                localX = p.x, localY = p.y, localZ = p.z, hasIslandReturnPosition = true,
                islandReturnX = IslandReturnPosition.x, islandReturnY = IslandReturnPosition.y, islandReturnZ = IslandReturnPosition.z
            };
        }

        // Restore never calls TryEnter: story restoration owns milestones and transitions.
        public void RestorePlayerLocation(GameObject player, PlayerLocationSaveData data, Vector3 islandFallback)
        {
            if (!IsPlayer(player)) return;
            if (IsInsidePlayer(player)) TryExit(player);
            data = data ?? PlayerLocationSaveData.Island;
            Vector3 savedReturn = data.hasIslandReturnPosition
                ? new Vector3(data.islandReturnX, data.islandReturnY, data.islandReturnZ) : islandFallback;
            IslandReturnPosition = ResolveSafeReturn(savedReturn);
            if (data.AreaId == PlayerAreaIds.Island)
            {
                Teleport(player, Finite(islandFallback) ? islandFallback : IslandReturnPosition);
                player.GetComponent<IsometricPlayerController>().RefreshWaterState();
                return;
            }
            if (data.AreaId != PlayerAreaIds.SmugglerBase || context.StoryProgression == null
                || !context.StoryProgression.HasMilestone(StorySignalIds.BaseAccessGained))
            {
                Debug.LogWarning("[Interior] Unknown or inaccessible saved area; returning to island.");
                Teleport(player, IslandReturnPosition);
                player.GetComponent<IsometricPlayerController>().RefreshWaterState();
                return;
            }
            Vector3 local = new Vector3(data.localX, data.localY, data.localZ);
            Vector3 target = InteriorRoot.TransformPoint(local);
            EnterMode(player);
            Physics.SyncTransforms();
            if (!data.hasLocalPosition || !Finite(local) || !IsSafeInteriorPosition(player, target))
            {
                Debug.LogWarning("[Interior] Invalid saved interior position; using EntrySpawn.");
                target = EntrySpawn.position;
            }
            Teleport(player, target);
        }

        private bool IsSafeInteriorPosition(GameObject player, Vector3 position)
        {
            if (!InteriorMovementBounds.Contains(position)) return false;
            // Floors and props remain authoritative; reject walls, boat, and unsupported space.
            if (!Physics.Raycast(position + Vector3.up * 0.15f, Vector3.down, out var floor, 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || !floor.transform.IsChildOf(transform)) return false;
            var cc = player.GetComponent<CharacterController>();
            float radius = cc != null ? cc.radius : 0.35f;
            float height = cc != null ? cc.height : 1.8f;
            Vector3 center = position + (cc != null ? cc.center : Vector3.up * height * 0.5f);
            foreach (var hit in Physics.OverlapCapsule(center + Vector3.up * (height * 0.5f - radius),
                center - Vector3.up * (height * 0.5f - radius), radius * 0.9f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.transform.IsChildOf(transform)) return false;
            return true;
        }

        private bool SafeLand(Vector3 p)
        {
            var topo = context.IslandTopography;
            return Finite(p) && topo != null && topo.IsLandAt(p.x, p.z) && !topo.IsWaterAt(p.x, p.z)
                && (context.WorldBounds == null || context.WorldBounds.Contains(p));
        }
        private Vector3 ResolveSafeReturn(Vector3 candidate)
        {
            var topo = context.IslandTopography;
            if (SafeLand(candidate)) return Ground(candidate);
            Vector3 origin = entrance != null ? entrance.transform.position : candidate;
            for (int ring = 1; ring <= 16; ring++)
                for (int step = 0; step < 16; step++)
                {
                    float angle = step * Mathf.PI / 8f;
                    Vector3 p = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    if (SafeLand(p)) return Ground(p);
                }
            return generator != null ? generator.ResolvePlayerSpawnPoint() : topo != null ? topo.GetSafePlayerSpawnPoint() : Vector3.up;
        }
        private Vector3 Ground(Vector3 p)
        {
            // Keep the terrain surface height authoritative, including saves with corrupt Y.
            if (context.IslandTopography.TryGetEnvironmentAt(p, out ApexShift.Runtime.World.Environment.EnvironmentSample sample)) p.y = sample.Height + 0.1f;
            else if (context.IslandTopography.GetCellAt(p.x, p.z) is var cell && cell != null) p.y = cell.WorldCenter.y + 0.1f;
            return p;
        }
        private static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x)
            && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        private void Teleport(GameObject player, Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            Vector3 delta = position - player.transform.position;
            player.transform.position = position;
            player.GetComponent<IsometricPlayerController>().ResetVerticalVelocity();
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = wasEnabled;
            var follow = context.MainCamera != null ? context.MainCamera.GetComponent<IsometricCameraFollow>() : null;
            if (follow != null) { follow.SetTarget(player.transform); follow.SnapToTarget(); }
            var brain = context.MainCamera != null ? context.MainCamera.GetComponent<CinemachineBrain>() : null;
            if (brain != null)
            {
                foreach (var camera in context.GenerationRoot.GetComponentsInChildren<CinemachineCamera>(true))
                {
                    if (camera.Target.TrackingTarget != player.transform) continue;
                    camera.OnTargetObjectWarped(player.transform, delta);
                    camera.PreviousStateIsValid = false;
                }
                var previousUpdateMethod = brain.UpdateMethod;
                try
                {
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                    brain.ManualUpdate(Time.frameCount, -1f);
                }
                finally { brain.UpdateMethod = previousUpdateMethod; }
            }
        }
    }
}
