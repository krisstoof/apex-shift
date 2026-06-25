using UnityEngine;

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

    [RequireComponent(typeof(CreatureNavigationAdapter))]
    public class CreatureAgentView : MonoBehaviour
    {
        [SerializeField] private string creatureId;
        private CreatureNavigationAdapter _navigationAdapter;

        public string CreatureId => creatureId;

        private void Awake()
        {
            EnsureAdapter();
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
            creatureId = id;
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
