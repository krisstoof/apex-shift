using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.World.Landmarks
{
    [DisallowMultipleComponent, RequireComponent(typeof(LandmarkRuntime))]
    public sealed class LandmarkDiscoveryRuntime : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float discoveryRadius = 12f;
        [SerializeField] private bool autoDiscoverOnProximity = true;
        private LandmarkRuntime landmark;

        private void Awake() => landmark = GetComponent<LandmarkRuntime>();

        private void Update()
        {
            Transform player = PlayerPresenceRuntime.ActiveTransform;
            if (autoDiscoverOnProximity && player != null) TryDiscoverAt(player.position);
        }

        public bool TryDiscoverAt(Vector3 position)
        {
            if (landmark == null) landmark = GetComponent<LandmarkRuntime>();
            if (!autoDiscoverOnProximity || landmark == null || landmark.IsDiscovered) return false;
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= discoveryRadius * discoveryRadius && landmark.Discover();
        }
    }
}
