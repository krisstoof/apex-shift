using UnityEngine;

namespace ApexShift.Runtime.Settings
{
    public sealed class GameSettingsBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            _ = GameSettingsService.Instance;
            if (FindAnyObjectByType<GameSettingsBootstrap>() != null)
            {
                return;
            }

            GameObject go = new GameObject("GameSettingsBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSettingsBootstrap>();
        }
    }
}
