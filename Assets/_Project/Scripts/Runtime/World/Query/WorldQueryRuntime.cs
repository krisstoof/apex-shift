using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.World.Query
{
    /// <summary>
    /// Unity migration counterpart for the Godot world query service used by creature AI.
    ///
    /// This first parity layer is intentionally a facade over EcosystemRuntime's registries.
    /// It removes per-decision scene scans from creature brains while keeping a stable API
    /// that can later be backed by a spatial grid / biome index without changing AI code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldQueryRuntime : MonoBehaviour
    {
        [SerializeField] private EcosystemRuntime ecosystem;
        [SerializeField] private string defaultBiomeId = "default";

        private static WorldQueryRuntime _instance;

        public static WorldQueryRuntime Active
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<WorldQueryRuntime>();
                }

                return _instance;
            }
        }

        public EcosystemRuntime Ecosystem
        {
            get
            {
                ResolveEcosystem();
                return ecosystem;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            ResolveEcosystem();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static WorldQueryRuntime GetOrCreate(EcosystemRuntime ecosystemRuntime)
        {
            if (_instance != null)
            {
                if (_instance.ecosystem == null && ecosystemRuntime != null)
                {
                    _instance.ecosystem = ecosystemRuntime;
                }

                return _instance;
            }

            WorldQueryRuntime found = Active;
            if (found != null)
            {
                if (found.ecosystem == null && ecosystemRuntime != null)
                {
                    found.ecosystem = ecosystemRuntime;
                }

                return found;
            }

            if (ecosystemRuntime == null)
            {
                return null;
            }

            WorldQueryRuntime created = ecosystemRuntime.GetComponent<WorldQueryRuntime>();
            if (created == null)
            {
                created = ecosystemRuntime.gameObject.AddComponent<WorldQueryRuntime>();
            }

            created.ecosystem = ecosystemRuntime;
            _instance = created;
            return created;
        }

        public bool TryFindNearestPlantFood(Vector3 position, float maxDistance, out FoodSourceView source)
        {
            source = null;
            ResolveEcosystem();
            return ecosystem != null && ecosystem.TryFindNearestPlantFood(position, maxDistance, out source);
        }

        public bool TryFindNearestMeatFood(Vector3 position, float maxDistance, out FoodSourceView source)
        {
            source = null;
            ResolveEcosystem();
            return ecosystem != null && ecosystem.TryFindNearestMeatFood(position, maxDistance, out source);
        }

        public bool TryFindNearestFood(Vector3 position, FoodKind preferredKind, float maxDistance, out FoodSourceView source)
        {
            source = null;
            ResolveEcosystem();
            return ecosystem != null && ecosystem.TryFindNearestFood(position, preferredKind, maxDistance, out source);
        }

        public bool TryFindNearestCreatureById(Vector3 position, string creatureId, float maxDistance, out CreatureAgentView creature)
        {
            creature = null;
            ResolveEcosystem();
            if (ecosystem == null)
            {
                return false;
            }

            creature = ecosystem.TryFindNearestCreatureById(position, creatureId, maxDistance);
            return creature != null;
        }

        public bool TryFindNearestPrey(Vector3 position, string hunterCreatureId, float maxDistance, out CreatureAgentView prey)
        {
            prey = null;
            ResolveEcosystem();
            if (ecosystem == null)
            {
                return false;
            }

            prey = ecosystem.TryFindNearestPrey(position, hunterCreatureId, maxDistance);
            return prey != null;
        }

        public string GetBiomeIdForPosition(Vector3 position)
        {
            // Placeholder parity API for the Godot world query service.
            // A later biome-index issue can replace this without changing creature AI callers.
            return string.IsNullOrWhiteSpace(defaultBiomeId) ? "default" : defaultBiomeId;
        }

        private void ResolveEcosystem()
        {
            if (ecosystem == null)
            {
                ecosystem = EcosystemRuntime.Instance;
            }
        }
    }
}
