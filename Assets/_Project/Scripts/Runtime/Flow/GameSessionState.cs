using System;

namespace ApexShift.Runtime.Flow
{
    public static class GameSessionState
    {
        public static event Action<bool> GameplayActiveChanged;

        public static bool IsGameplayActive { get; private set; }

        public static void EnterMainMenu()
        {
            SetGameplayActive(false);
        }

        public static void BeginGameplay()
        {
            SetGameplayActive(true);
        }

        public static void EndGameplay()
        {
            SetGameplayActive(false);
        }

        private static void SetGameplayActive(bool active)
        {
            if (IsGameplayActive == active)
            {
                return;
            }

            IsGameplayActive = active;
            GameplayActiveChanged?.Invoke(active);
        }
    }
}
