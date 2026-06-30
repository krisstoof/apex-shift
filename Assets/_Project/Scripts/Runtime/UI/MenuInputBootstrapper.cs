using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ApexShift.Runtime.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ApexShift.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuInputBootstrapper : MonoBehaviour
    {
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private bool forceCanvasOnTop = true;
        [SerializeField] private int sortingOrder = 5000;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private AudioClip[] uiClickClips;
        [SerializeField] private AudioClip[] uiInvalidClips;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureGraphicRaycaster();
            EnsureMenuAudio();
            LogDiagnostics();
        }

        private void OnEnable()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureGraphicRaycaster();
            EnsureMenuAudio();
            LogDiagnostics();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssignMenuAudio();
        }

        private void AutoAssignMenuAudio()
        {
            string basePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/ui/";
            uiClickClips = LoadClips(new[] { basePath + "ui_click_real_01.wav" }, uiClickClips);
            uiInvalidClips = LoadClips(new[] { basePath + "ui_invalid_real_01.wav" }, uiInvalidClips);
        }

        private static AudioClip[] LoadClips(string[] paths, AudioClip[] fallback)
        {
            if (paths == null || paths.Length == 0)
            {
                return fallback;
            }

            AudioClip[] clips = new AudioClip[paths.Length];
            bool anyLoaded = false;
            for (int i = 0; i < paths.Length; i++)
            {
                clips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
                anyLoaded |= clips[i] != null;
            }

            return anyLoaded ? clips : fallback;
        }
#endif

        private void EnsureMenuAudio()
        {
            if (menuCanvas == null)
            {
                return;
            }

            Button[] buttons = menuCanvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(PlayUIClickFallback);
                button.onClick.AddListener(PlayUIClickFallback);
            }
        }

        private void PlayUIClickFallback()
        {
            WorldActionAudio.PlayUIClick(menuCanvas != null ? menuCanvas.transform.position : transform.position);
        }

        private void PlayClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(start + i) % clips.Length];
                if (clip == null)
                {
                    continue;
                }

                PlayUIAudio(clip, 0.35f);
                return;
            }
        }

        private static void PlayUIAudio(AudioClip clip, float volume)
        {
            GameObject emitter = new GameObject($"UIAudio_{clip.name}");
            emitter.transform.position = Vector3.zero;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.Play();
            Object.Destroy(emitter, clip.length + 0.25f);
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
            }
#else
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private void EnsureCanvas()
        {
            if (menuCanvas == null)
            {
                menuCanvas = GetComponentInParent<Canvas>();
            }

            if (menuCanvas == null)
            {
                menuCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (menuCanvas == null)
            {
                return;
            }

            if (forceCanvasOnTop)
            {
                menuCanvas.overrideSorting = true;
                menuCanvas.sortingOrder = sortingOrder;
            }
        }

        private void EnsureGraphicRaycaster()
        {
            if (menuCanvas == null)
            {
                return;
            }

            GraphicRaycaster raycaster = menuCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                menuCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void LogDiagnostics()
        {
            if (!logDiagnostics)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            string eventSystemInfo = eventSystem != null
                ? $"{eventSystem.name}, module={eventSystem.currentInputModule?.GetType().Name ?? "none"}"
                : "missing";

            string canvasInfo = menuCanvas != null
                ? $"{menuCanvas.name}, active={menuCanvas.gameObject.activeInHierarchy}, enabled={menuCanvas.enabled}, renderMode={menuCanvas.renderMode}, sorting={menuCanvas.sortingOrder}, raycaster={menuCanvas.GetComponent<GraphicRaycaster>() != null}"
                : "missing";

            Debug.Log($"[MenuInputBootstrapper] EventSystem: {eventSystemInfo}");
            Debug.Log($"[MenuInputBootstrapper] Canvas: {canvasInfo}");
        }
    }
}
