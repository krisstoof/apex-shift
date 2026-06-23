using System;

namespace ApexShift.Runtime.Input
{
    public sealed class GameplayInputEvents
    {
        public event Action InteractPressed;
        public event Action AttackPressed;
        public event Action OpenInventoryPressed;
        public event Action OpenCraftingPressed;
        public event Action ToggleMapPressed;
        public event Action PausePressed;

        public void RaiseInteractPressed() => InteractPressed?.Invoke();
        public void RaiseAttackPressed() => AttackPressed?.Invoke();
        public void RaiseOpenInventoryPressed() => OpenInventoryPressed?.Invoke();
        public void RaiseOpenCraftingPressed() => OpenCraftingPressed?.Invoke();
        public void RaiseToggleMapPressed() => ToggleMapPressed?.Invoke();
        public void RaisePausePressed() => PausePressed?.Invoke();
    }
}
