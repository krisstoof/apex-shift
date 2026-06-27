using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class EcosystemRuntime : MonoBehaviour
    {
        [SerializeField] private bool showDebugOverlay = true;
        private static EcosystemRuntime _instance;

        public static EcosystemRuntime Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<EcosystemRuntime>();
                }

                return _instance;
            }
        }

        public static EcosystemRuntime Active => Instance;

        private readonly List<FoodSourceView> _foodSources = new List<FoodSourceView>();
        private readonly List<CreatureAgentView> _creatures = new List<CreatureAgentView>();

        public int FoodSourceCount => _foodSources.Count;
        public int CreatureCount => _creatures.Count;
        public int PlantFoodSourceCount => GetFoodSourceCount(FoodKind.Plants);
        public int MeatFoodSourceCount => GetFoodSourceCount(FoodKind.Meat);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void RegisterFoodSource(FoodSourceView source)
        {
            if (source == null || _foodSources.Contains(source))
            {
                return;
            }

            _foodSources.Add(source);
        }

        public void UnregisterFoodSource(FoodSourceView source)
        {
            if (source == null)
            {
                return;
            }

            _foodSources.Remove(source);
        }

        public void RegisterCreature(CreatureAgentView creature)
        {
            if (creature == null || _creatures.Contains(creature))
            {
                return;
            }

            _creatures.Add(creature);
        }

        public void UnregisterCreature(CreatureAgentView creature)
        {
            if (creature == null)
            {
                return;
            }

            _creatures.Remove(creature);
        }

        public bool TryFindNearestPlantFood(Vector3 position, float maxDistance, out FoodSourceView source)
        {
            return TryFindNearestFood(position, FoodKind.Plants, maxDistance, out source);
        }

        public bool TryFindNearestMeatFood(Vector3 position, float maxDistance, out FoodSourceView source)
        {
            if (TryFindNearestFood(position, FoodKind.Meat, maxDistance, out source))
            {
                return true;
            }

            return TryFindNearestFood(position, FoodKind.Scavenger, maxDistance, out source);
        }

        public FoodSourceView TryFindNearestFood(Vector3 position, FoodKind preferredKind, float maxDistance = 50f)
        {
            TryFindNearestFood(position, preferredKind, maxDistance, out FoodSourceView source);
            return source;
        }

        public bool TryFindNearestFood(Vector3 position, FoodKind preferredKind, float maxDistance, out FoodSourceView source)
        {
            source = null;
            float minSqrDist = Mathf.Max(0f, maxDistance) * Mathf.Max(0f, maxDistance);

            for (int i = _foodSources.Count - 1; i >= 0; i--)
            {
                FoodSourceView candidate = _foodSources[i];
                if (candidate == null)
                {
                    _foodSources.RemoveAt(i);
                    continue;
                }

                if (candidate.Kind != preferredKind || candidate.IsEmpty)
                {
                    continue;
                }

                float sqrDist = (candidate.transform.position - position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    source = candidate;
                }
            }

            return source != null;
        }

        public CreatureAgentView TryFindNearestPrey(Vector3 position, string hunterCreatureId, float maxDistance = 40f)
        {
            CreatureAgentView prey = null;
            float minSqrDist = Mathf.Max(0f, maxDistance) * Mathf.Max(0f, maxDistance);

            for (int i = _creatures.Count - 1; i >= 0; i--)
            {
                CreatureAgentView candidate = _creatures[i];
                if (candidate == null)
                {
                    _creatures.RemoveAt(i);
                    continue;
                }

                if (!candidate.isActiveAndEnabled || candidate.gameObject == null)
                {
                    continue;
                }

                string candidateId = (candidate.CreatureId ?? string.Empty).Trim().ToLowerInvariant();
                if (string.Equals(candidateId, hunterCreatureId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // In this slice Varnak should hunt prey animals, not other predators.
                if (candidateId != "small_prey" && candidateId != "grazer")
                {
                    continue;
                }

                CreatureHealthRuntime health = candidate.GetComponent<CreatureHealthRuntime>();
                if (health != null && health.IsDead)
                {
                    continue;
                }

                float sqrDist = (candidate.transform.position - position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    prey = candidate;
                }
            }

            return prey;
        }

        public CreatureAgentView TryFindNearestCreatureById(Vector3 position, string creatureId, float maxDistance = 40f)
        {
            CreatureAgentView found = null;
            float minSqrDist = Mathf.Max(0f, maxDistance) * Mathf.Max(0f, maxDistance);
            string expectedId = (creatureId ?? string.Empty).Trim().ToLowerInvariant();

            for (int i = _creatures.Count - 1; i >= 0; i--)
            {
                CreatureAgentView candidate = _creatures[i];
                if (candidate == null)
                {
                    _creatures.RemoveAt(i);
                    continue;
                }

                if (!candidate.isActiveAndEnabled)
                {
                    continue;
                }

                string candidateId = (candidate.CreatureId ?? string.Empty).Trim().ToLowerInvariant();
                if (candidateId != expectedId)
                {
                    continue;
                }

                CreatureHealthRuntime health = candidate.GetComponent<CreatureHealthRuntime>();
                if (health != null && health.IsDead)
                {
                    continue;
                }

                float sqrDist = (candidate.transform.position - position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    found = candidate;
                }
            }

            return found;
        }

        public int GetFoodSourceCount(FoodKind kind)
        {
            return _foodSources.Count(s => s != null && s.Kind == kind);
        }

        public int GetCreatureCount(string creatureId)
        {
            return _creatures.Count(c => c != null && string.Equals(c.CreatureId, creatureId, System.StringComparison.OrdinalIgnoreCase));
        }

        public float GetAverageBiomassRatio(FoodKind kind)
        {
            var filtered = _foodSources.Where(s => s != null && s.Kind == kind).ToList();
            if (filtered.Count == 0) return 0f;
            return filtered.Average(s => s.BiomassRatio);
        }

        private void OnGUI()
        {
            if (!showDebugOverlay)
            {
                return;
            }

            // Move lower to avoid overlapping with player stats and action log
            GUI.Box(
                new Rect(12f, Screen.height * 0.55f, 270f, 104f),
                $"Ecosystem Debug\n" +
                $"food sources: {FoodSourceCount}\n" +
                $"creatures: {CreatureCount}\n" +
                $"plants: {PlantFoodSourceCount} avg:{GetAverageBiomassRatio(FoodKind.Plants):0.00}\n" +
                $"meat: {MeatFoodSourceCount} avg:{GetAverageBiomassRatio(FoodKind.Meat):0.00}");
        }
}
}
