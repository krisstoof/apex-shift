using ApexShift.Presentation.HUD;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            GameObject optionsMenu = FindOptionsMenuIncludingInactive();
            if (optionsMenu == null)
            {
                return;
            }

            boundOptionsMenu = optionsMenu.GetComponent<OptionsMenuController>() ?? optionsMenu.AddComponent<OptionsMenuController>();
            boundOptionsMenu.BuildIfNeeded(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        }

        private static GameObject FindOptionsMenuIncludingInactive()
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate == null || candidate.name != "OptionsMenu")
                {
                    continue;
                }

                Scene scene = candidate.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
