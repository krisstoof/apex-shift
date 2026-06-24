using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Ecosystem;
using UnityEngine;

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
                    _instance = Object.FindAnyObjectByType<EcosystemRuntime>();
                }

                return _instance;
            }
        }

        public static EcosystemRuntime Active => Instance;

        private readonly List<FoodSourceView> _foodSources = new List<FoodSourceView>();

        public int FoodSourceCount => _foodSources.Count;
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

        public int GetFoodSourceCount(FoodKind kind)
        {
            return _foodSources.Count(s => s != null && s.Kind == kind);
        }

        public float GetAverageBiomassRatio(FoodKind kind)
        {
            var filtered = _foodSources.Where(s => s != null && s.Kind == kind).ToList();
            if (filtered.Count == 0) return 0f;
            return filtered.Average(s => s.BiomassRatio);
        }

        private void OnGUI()
        {
            GUI.Box(
                new Rect(12f, 120f, 270f, 104f),
                $"Ecosystem Debug\n" +
                $"food sources: {FoodSourceCount}\n" +
                $"plants: {PlantFoodSourceCount} avg:{GetAverageBiomassRatio(FoodKind.Plants):0.00}\n" +
                $"meat: {MeatFoodSourceCount} avg:{GetAverageBiomassRatio(FoodKind.Meat):0.00}");
        }
    }
}
