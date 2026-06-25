using ApexShift.Core.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class FoodSourceView : MonoBehaviour
    {
        [SerializeField] private string sourceId;
        [SerializeField] private string displayName;
        [SerializeField] private FoodKind kind = FoodKind.Plants;
        [SerializeField] private float maxBiomass = 10f;
        [SerializeField] private float nutritionPerBiomass = 5f;
        [SerializeField] private bool disappearWhenEmpty = true;
        [SerializeField] private bool deactivateObjectWhenEmpty = true;

        private FoodSourceState _state;
        private bool _depleted;

        public string SourceId => sourceId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public FoodKind Kind => kind;
        public float BiomassRatio => _state != null && _state.MaxBiomass > 0f ? _state.Biomass / _state.MaxBiomass : 0f;
        public bool IsEmpty => _depleted || _state == null || _state.IsEmpty;
        public bool IsAvailable => !_depleted && _state != null && !_state.IsEmpty && isActiveAndEnabled;
        public float Biomass => _state != null ? _state.Biomass : 0f;

        private void Awake()
        {
            EnsureState();
        }

        private void OnEnable()
        {
            EnsureState();
            if (IsAvailable)
            {
                EcosystemRuntime.Instance?.RegisterFoodSource(this);
            }
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
            _depleted = false;

            if (isActiveAndEnabled && IsAvailable)
            {
                EcosystemRuntime.Instance?.RegisterFoodSource(this);
            }
        }

        public void Configure(string sourceId, string displayName, FoodKind kind, float maxBiomass, float nutritionPerBiomass)
        {
            this.sourceId = sourceId;
            this.displayName = displayName;
            Configure(kind, maxBiomass, nutritionPerBiomass);
        }

        public float Consume(float requestedBiomass)
        {
            EnsureState();

            if (_depleted)
            {
                return 0f;
            }

            float nutrition = _state.Consume(Mathf.Max(0f, requestedBiomass));
            if (_state.IsEmpty)
            {
                MarkDepleted();
            }

            return nutrition;
        }

        private void MarkDepleted()
        {
            if (_depleted)
            {
                return;
            }

            _depleted = true;
            EcosystemRuntime.Instance?.UnregisterFoodSource(this);

            if (!disappearWhenEmpty)
            {
                return;
            }

            if (deactivateObjectWhenEmpty)
            {
                gameObject.SetActive(false);
                return;
            }

            HideRenderersAndColliders();
        }

        private void HideRenderersAndColliders()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
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
