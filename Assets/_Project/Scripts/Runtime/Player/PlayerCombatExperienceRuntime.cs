using System.Collections;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Player
{
    /// <summary>
    /// Prototype combat presentation layer.
    /// It is intentionally independent from inventory/crafting and from Animator setup:
    /// attack must be visible/audible even when no authored combat clips are imported yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatExperienceRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private BuildingPlacementRuntime buildingPlacementRuntime;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform feedbackOrigin;

        [Header("Fallback Input")]
        [SerializeField] private bool pollMouseFallback = false;
        [SerializeField] private float fallbackInputCooldown = 0.18f;

        [Header("Visual Feedback")]
        [SerializeField] private float slashRange = 1.8f;
        [SerializeField] private float slashDuration = 0.16f;
        [SerializeField] private float visualLeanDistance = 0.22f;
        [SerializeField] private float visualLeanDuration = 0.18f;

        [Header("Creature Awareness")]
        [SerializeField] private float attackNoiseRadius = 22f;
        [SerializeField] private float playerProximityPingRadius = 24f;
        [SerializeField] private float playerProximityPingInterval = 0.35f;

        private float fallbackCooldownRemaining;
        private float proximityPingTimer;
        private Coroutine leanRoutine;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
            if (fallbackCooldownRemaining > 0f)
            {
                fallbackCooldownRemaining = Mathf.Max(0f, fallbackCooldownRemaining - Time.deltaTime);
            }

            proximityPingTimer -= Time.deltaTime;
            if (proximityPingTimer <= 0f)
            {
                proximityPingTimer = Mathf.Max(0.05f, playerProximityPingInterval);
                PingNearbyCreatures("player_presence");
            }

            if (pollMouseFallback && fallbackCooldownRemaining <= 0f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!IsBuildingPrimaryActionBlocked())
                {
                    fallbackCooldownRemaining = fallbackInputCooldown;
                    PlayAttackFeedback("mouse_fallback");
                }
            }
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader)
            {
                return;
            }

            inputReader = reader;
        }

        public void SetBuildingPlacementRuntime(BuildingPlacementRuntime runtime)
        {
            buildingPlacementRuntime = runtime;
        }

        public void SetVisualRoot(Transform root)
        {
            visualRoot = root != null ? root : transform;
        }

        private void PlayAttackFeedback(string reason)
        {
            ResolveReferences();
            Vector3 origin = GetOrigin();
            Vector3 direction = ResolveAimDirection(origin);

            CombatFxSpawner.SpawnSlashArc(origin, direction, slashRange, true, slashDuration);
            StartLean(direction);
            ProceduralCombatAudio.PlayMeleeSwing(origin, 0.55f);
            CreaturePlayerAwarenessBehavior.NotifyNearby(origin, transform, attackNoiseRadius, 1f, reason);
        }

        private void PingNearbyCreatures(string reason)
        {
            CreaturePlayerAwarenessBehavior.NotifyNearby(transform.position, transform, playerProximityPingRadius, 0.35f, reason);
        }

        private bool IsBuildingPrimaryActionBlocked()
        {
            if (buildingPlacementRuntime == null)
            {
                buildingPlacementRuntime = GetComponent<BuildingPlacementRuntime>();
            }

            return buildingPlacementRuntime != null && buildingPlacementRuntime.BlocksPlayerPrimaryAction;
        }

        private Vector3 GetOrigin()
        {
            Transform origin = feedbackOrigin != null ? feedbackOrigin : transform;
            return origin.position + Vector3.up * 0.95f;
        }

        private Vector3 ResolveAimDirection(Vector3 origin)
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main : Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (camera != null && Mouse.current != null)
            {
                Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                Plane plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 target = ray.GetPoint(enter);
                    Vector3 direction = target - origin;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        return direction.normalized;
                    }
                }
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }

        private void StartLean(Vector3 direction)
        {
            if (visualRoot == null)
            {
                visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
            }

            if (visualRoot == null)
            {
                return;
            }

            if (leanRoutine != null)
            {
                StopCoroutine(leanRoutine);
            }

            leanRoutine = StartCoroutine(LeanRoutine(direction));
        }

        private IEnumerator LeanRoutine(Vector3 direction)
        {
            Transform target = visualRoot;
            Vector3 start = target.localPosition;
            Vector3 localDirection = transform.InverseTransformDirection(direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward);
            localDirection.y = 0f;
            Vector3 peak = start + (localDirection.sqrMagnitude > 0.001f ? localDirection.normalized : Vector3.forward) * visualLeanDistance;
            float half = Mathf.Max(0.03f, visualLeanDuration * 0.5f);

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localPosition = Vector3.Lerp(start, peak, t / half);
                yield return null;
            }

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localPosition = Vector3.Lerp(peak, start, t / half);
                yield return null;
            }

            target.localPosition = start;
            leanRoutine = null;
        }

        private void ResolveReferences()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (buildingPlacementRuntime == null) buildingPlacementRuntime = GetComponent<BuildingPlacementRuntime>();
            if (feedbackOrigin == null) feedbackOrigin = transform;
            if (visualRoot == null) visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }
    }
}
