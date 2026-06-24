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
        public float BiomassRatio => _state != null && _state.MaxBiomass > 0f ? _state.Biomass / _state.MaxBiomass : 0f;
        public bool IsEmpty => _state == null || _state.IsEmpty;
        public bool IsAvailable => !IsEmpty;
        public float Biomass => _state != null ? _state.Biomass : 0f;

        private void Awake()
        {
            EnsureState();
        }

        private void OnEnable()
        {
            EnsureState();
            EcosystemRuntime.Instance?.RegisterFoodSource(this);
        }

        private void OnDisable()
        {
            EcosystemRuntime.Instance?.UnregisterFoodSource(this);
        }

        public void Configure(FoodKind kind, float maxBiomass, float nutritionPerBiomass)
        {
            this.kind = kind;
            this.maxBiomass = Mathf.Max(0.01f, maxBiomass);
            this.nutritionPerBiomass = Mathf.Max(0f, nutritionPerBiomass);
            _state = new FoodSourceState(this.maxBiomass, this.nutritionPerBiomass);

            if (isActiveAndEnabled)
            {
                EcosystemRuntime.Instance?.RegisterFoodSource(this);
            }
        }

        public float Consume(float requestedBiomass)
        {
            EnsureState();
            return _state.Consume(Mathf.Max(0f, requestedBiomass));
        }

        private void EnsureState()
        {
            if (_state == null)
            {
                _state = new FoodSourceState(Mathf.Max(0.01f, maxBiomass), Mathf.Max(0f, nutritionPerBiomass));
            }
        }
    }
}
