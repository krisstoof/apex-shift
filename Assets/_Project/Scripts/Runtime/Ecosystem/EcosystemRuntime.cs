using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ApexShift.Runtime.Ecosystem
{
    public class EcosystemRuntime : MonoBehaviour
    {
        private static EcosystemRuntime _instance;
        public static EcosystemRuntime Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<EcosystemRuntime>();
                }
                return _instance;
            }
        }

        private readonly List<FoodSourceView> _foodSources = new List<FoodSourceView>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void RegisterFoodSource(FoodSourceView source)
        {
            if (!_foodSources.Contains(source))
                _foodSources.Add(source);
        }

        public void UnregisterFoodSource(FoodSourceView source)
        {
            _foodSources.Remove(source);
        }

        public FoodSourceView TryFindNearestFood(Vector3 position, Core.Ecosystem.FoodKind preferredKind, float maxDistance = 50f)
        {
            FoodSourceView nearest = null;
            float minSqrDist = maxDistance * maxDistance;

            foreach (var source in _foodSources)
            {
                if (source.Kind != preferredKind || source.IsEmpty) continue;

                float sqrDist = (source.transform.position - position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = source;
                }
            }

            return nearest;
        }

        public int GetFoodSourceCount(Core.Ecosystem.FoodKind kind) => _foodSources.Count(s => s.Kind == kind);
        
        public float GetAverageBiomassRatio(Core.Ecosystem.FoodKind kind)
        {
            var filtered = _foodSources.Where(s => s.Kind == kind).ToList();
            if (filtered.Count == 0) return 0f;
            return filtered.Average(s => s.BiomassRatio);
        }
    }
}
