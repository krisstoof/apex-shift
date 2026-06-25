using ApexShift.Core.Ecosystem;

namespace ApexShift.Runtime.Creatures
{
    public sealed class CreatureDebugData
    {
        public string CreatureId;
        public string State;
        public string Target;
        public float Hunger;
        public float MaxHunger;
        public HungerStage HungerStage;
        public float Health;
        public float MaxHealth;
        public float LastMoveIntensity;
        public bool IsDead;
    }
}
