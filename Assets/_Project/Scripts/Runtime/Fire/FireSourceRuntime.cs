using ApexShift.Runtime.Events;
using UnityEngine;

namespace ApexShift.Runtime.Fire
{
    [DisallowMultipleComponent]
    public sealed class FireSourceRuntime : MonoBehaviour
    {
        [SerializeField] private string sourceId = "fire";
        [SerializeField] private float protectionRadius = 10f;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private bool activeOnEnable;
        [SerializeField] private bool drawDebugGizmos = true;

        private bool activeFire;

        public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? "fire" : sourceId.Trim().ToLowerInvariant();
        public float ProtectionRadius => Mathf.Max(0.1f, protectionRadius);
        public float Intensity => Mathf.Clamp01(intensity);
        public bool IsActiveFire => isActiveAndEnabled && activeFire;

        private void Awake()
        {
            if (activeOnEnable)
            {
                activeFire = true;
            }
        }

        private void OnEnable()
        {
            FireSourceRegistry.Register(this);
        }

        private void OnDisable()
        {
            FireSourceRegistry.Unregister(this);
        }

        private void OnDestroy()
        {
            FireSourceRegistry.Unregister(this);
        }

        public void Configure(string id, float radius, float fireIntensity = 1f)
        {
            sourceId = string.IsNullOrWhiteSpace(id) ? sourceId : id.Trim().ToLowerInvariant();
            protectionRadius = Mathf.Max(0.1f, radius);
            intensity = Mathf.Clamp01(fireIntensity);
        }

        public void SetActiveFire(bool value, bool publishEvent = true)
        {
            if (activeFire == value)
            {
                return;
            }

            activeFire = value;
            if (!publishEvent)
            {
                return;
            }

            GameEventBus.PublishFireSourceEvent(
                activeFire ? GameplayEventKind.FireSourceActivated : GameplayEventKind.FireSourceExpired,
                transform.position,
                SourceId,
                ProtectionRadius,
                activeFire ? $"{SourceId}_activated" : $"{SourceId}_expired");
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.color = IsActiveFire ? new Color(1f, 0.45f, 0.08f, 0.55f) : new Color(0.55f, 0.55f, 0.55f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, ProtectionRadius);
        }
    }
}
