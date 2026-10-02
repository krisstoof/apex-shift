using UnityEngine;
using ApexShift.Runtime.Config;
using UnityEngine.AI;
using ApexShift.Runtime.Ecosystem;

namespace ApexShift.Runtime.Creatures
{
    public enum CreatureBehaviorState
    {
        Idle,
        Wander,
        Stalk,
        Chase,
        Attack,
        SeekFood,
        EatPlants,
        Scavenge,
        HuntSmallPrey,
        HuntPrey,
        EatMeat,
        Flee,
        Eat,
        Dead
    }

    public class CreatureAgentView : MonoBehaviour
    {
        [SerializeField] private string creatureId;
        private CreatureNavigationAdapter _navigationAdapter;
        public CreatureHealthRuntime CachedHealth { get; private set; }
        public CreatureNeedsRuntime CachedNeeds { get; private set; }
        public NavMeshAgent CachedNavMeshAgent { get; private set; }
        public CreaturePlayerAwarenessBehavior CachedAwareness { get; private set; }

        public string SpeciesId => CreatureSpeciesCompatibility.Canonicalize(creatureId);
        public string CreatureId => SpeciesId;
        private SpeciesDefinition _fallbackDefinition;
        public SpeciesDefinition Definition
        {
            get
            {
                if (CachedNeeds == null) CachedNeeds = GetComponent<CreatureNeedsRuntime>();
                if (CachedNeeds != null) return CachedNeeds.Definition;
                return _fallbackDefinition != null ? _fallbackDefinition : (_fallbackDefinition = SpeciesDefinition.CreateDefault(SpeciesId));
            }
        }
        public CreatureRole Role => Definition.Role;

        private void Awake()
        {
            EnsureAdapter();
            RefreshRuntimeReferences();
        }

        public void RefreshRuntimeReferences()
        {
            EnsureAdapter();
            CachedHealth = GetComponent<CreatureHealthRuntime>();
            CachedNeeds = GetComponent<CreatureNeedsRuntime>();
            CachedNavMeshAgent = GetComponent<NavMeshAgent>();
            CachedAwareness = GetComponent<CreaturePlayerAwarenessBehavior>();
        }

        private void EnsureAdapter()
        {
            if (_navigationAdapter == null)
            {
                _navigationAdapter = GetComponent<CreatureNavigationAdapter>();
            }
        }

        public void Configure(string id)
        {
            creatureId = CreatureSpeciesCompatibility.Canonicalize(id);
            _fallbackDefinition = null;
        }

        public void MoveTo(Vector3 position)
        {
            EnsureAdapter();
            if (_navigationAdapter != null)
                _navigationAdapter.TryMoveTo(position);
        }

        public void Stop()
        {
            EnsureAdapter();
            if (_navigationAdapter != null)
                _navigationAdapter.Stop();
        }

        public CreatureNavigationAdapter GetNavigationAdapter()
        {
            EnsureAdapter();
            return _navigationAdapter;
        }
    }
}
