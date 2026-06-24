using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    public class CreatureAgentView : MonoBehaviour
    {
        [SerializeField] private string creatureId;
        private CreatureNavigationAdapter _navigationAdapter;

        public string CreatureId => creatureId;

        private void Awake()
        {
            _navigationAdapter = GetComponent<CreatureNavigationAdapter>();
            if (_navigationAdapter == null)
            {
                _navigationAdapter = gameObject.AddComponent<CreatureNavigationAdapter>();
            }
        }

        public void Configure(string id)
        {
            creatureId = id;
        }

        public void MoveTo(Vector3 position)
        {
            _navigationAdapter.TryMoveTo(position);
        }

        public void Stop()
        {
            _navigationAdapter.Stop();
        }

        public CreatureNavigationAdapter GetNavigationAdapter() => _navigationAdapter;
    }
}
