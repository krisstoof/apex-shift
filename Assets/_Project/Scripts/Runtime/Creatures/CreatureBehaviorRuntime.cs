using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CreatureBehaviorBrain))]
    public sealed class CreatureBehaviorRuntime : MonoBehaviour
    {
        private CreatureBehaviorBrain _brain;

        private void Awake() => Cache();
        private void OnEnable() => Cache();
        private void Cache() { if (_brain == null) _brain = GetComponent<CreatureBehaviorBrain>(); }

        public CreatureBehaviorState State => _brain != null ? _brain.State : CreatureBehaviorState.Idle;
        public Transform CurrentTargetTransform => _brain != null ? _brain.CurrentTargetTransform : null;
        public string CurrentTargetLabel => _brain != null ? _brain.CurrentTargetLabel : "none";
        public string DecisionReason => _brain != null ? _brain.DecisionReason : "spawn";
        public string LastFoodSource => _brain != null ? _brain.LastFoodSource : "none";
        public int DecisionCount => _brain != null ? _brain.DecisionCount : 0;
        public float AttackCooldown => _brain != null ? _brain.AttackCooldown : 0f;

        public void OnCreatureDied() { Cache(); if (_brain != null) _brain.OnCreatureDied(); }
    }
}
