using ApexShift.Core.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class CreatureNeedsRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string creatureId;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerGrowthRate = 1f;
        [SerializeField] private float hungryThreshold = 30f;
        [SerializeField] private float starvingThreshold = 60f;
        [SerializeField] private float desperateThreshold = 85f;

        private CreatureNeedsState _state;
        private CreatureDietProfile _diet;

        public CreatureNeedsState State => _state;
        public CreatureDietProfile Diet => _diet;

        private void Awake()
        {
            if (_state == null)
                _state = new CreatureNeedsState(maxHunger, hungerGrowthRate, hungryThreshold, starvingThreshold, desperateThreshold);
        }

        public void Configure(string id)
        {
            creatureId = id;
            _diet = CreatureDietProfile.GetDefault(id);
            _state = new CreatureNeedsState(maxHunger, hungerGrowthRate, hungryThreshold, starvingThreshold, desperateThreshold);
        }

        private void Update()
        {
            _state.Tick(Time.deltaTime);
        }

        public void Eat(float nutrition)
        {
            _state.Eat(nutrition);
        }
    }
}
