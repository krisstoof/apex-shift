using ApexShift.Core.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class FoodSourceView : MonoBehaviour
    {
        [SerializeField] private FoodKind kind = FoodKind.Plants;
        [SerializeField] private float maxBiomass = 10f;
        [SerializeField] private float nutritionPerBiomass = 5f;

        private FoodSourceState _state;

        public FoodKind Kind => kind;
        public float BiomassRatio => _state != null ? _state.Biomass / _state.MaxBiomass : 0f;
        public bool IsEmpty => _state == null || _state.IsEmpty;

        private void Awake()
        {
            if (_state == null)
                _state = new FoodSourceState(maxBiomass, nutritionPerBiomass);
        }

        public void Configure(FoodKind kind, float maxBiomass, float nutritionPerBiomass)
        {
            this.kind = kind;
            this.maxBiomass = maxBiomass;
            this.nutritionPerBiomass = nutritionPerBiomass;
            _state = new FoodSourceState(maxBiomass, nutritionPerBiomass);
        }

        private void Start()
{
            if (EcosystemRuntime.Instance != null)
            {
                EcosystemRuntime.Instance.RegisterFoodSource(this);
            }
        }

        private void OnDestroy()
        {
            if (EcosystemRuntime.Instance != null)
            {
                EcosystemRuntime.Instance.UnregisterFoodSource(this);
            }
        }

        public float Consume(float requestedBiomass)
        {
            return _state.Consume(requestedBiomass);
        }
    }
}
