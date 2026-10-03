using ApexShift.Runtime.Story;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    /// <summary>Event-driven presentation only; it never evaluates story rules.</summary>
    public sealed class ObjectiveHUDView : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        private StoryProgressionRuntime progression;
        private StoryProgressionRuntime subscribedProgression;

        public void Configure(Text text) { objectiveText = text; Refresh(); }

        public void Bind(StoryProgressionRuntime runtime)
        {
            Unsubscribe();
            progression = runtime;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (progression == null || subscribedProgression != null) return;
            subscribedProgression = progression;
            subscribedProgression.StateChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (subscribedProgression != null) subscribedProgression.StateChanged -= Refresh;
            subscribedProgression = null;
        }

        private void Refresh()
        {
            if (objectiveText != null)
            {
                objectiveText.text = progression != null ? progression.CurrentObjectiveText : string.Empty;
                objectiveText.enabled = progression != null;
            }
        }
    }
}
