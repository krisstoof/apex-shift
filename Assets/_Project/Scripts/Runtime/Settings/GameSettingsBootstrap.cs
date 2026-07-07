using ApexShift.Presentation.HUD;
using UnityEngine;

namespace ApexShift.Runtime.Settings
{
    public sealed class GameSettingsBootstrap : MonoBehaviour
    {
        private OptionsMenuController boundOptionsMenu;
        private float scanTimer;

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

        private void Update()
        {
            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer > 0f)
            {
                return;
            }

            scanTimer = 0.75f;
            BindOptionsMenuIfAvailable();
        }

        private void BindOptionsMenuIfAvailable()
        {
            if (boundOptionsMenu != null)
            {
                return;
            }

            GameObject optionsMenu = GameObject.Find("OptionsMenu");
            if (optionsMenu == null)
            {
                return;
            }

            boundOptionsMenu = optionsMenu.GetComponent<OptionsMenuController>() ?? optionsMenu.AddComponent<OptionsMenuController>();
            boundOptionsMenu.BuildIfNeeded(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        }
    }
}
