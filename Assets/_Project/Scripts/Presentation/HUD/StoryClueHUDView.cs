using System;
using System.Collections;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Story.Clues;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    /// <summary>Inspection-only presentation. Attach to an active owner, not the hidden panel.</summary>
    public sealed class StoryClueHUDView : MonoBehaviour
    {
        private GameObject panel;
        private Text title, body;
        private bool gameplayAvailable;
        private float displaySeconds = 7f;
        private IDisposable subscription;
        private Coroutine hideTimer;
        public bool IsVisible => panel != null && panel.activeInHierarchy;

        public void Configure(GameObject inspectionPanel, Text heading, Text text, bool available, float seconds = 7f)
        {
            Hide();
            panel = inspectionPanel;
            title = heading;
            body = text;
            gameplayAvailable = available;
            displaySeconds = Mathf.Max(0.1f, seconds);
            Hide();
        }

        private void OnEnable() => subscription = GameEventBus.Subscribe(HandleEvent);
        private void OnDisable() { subscription?.Dispose(); subscription = null; Hide(); }
        private void OnDestroy() { subscription?.Dispose(); subscription = null; }

        private void HandleEvent(GameplayEvent value)
        {
            if (value.kind != GameplayEventKind.StoryClueInspected || !gameplayAvailable || panel == null) return;
            StoryClueRuntime clue = StoryClueRegistry.FindById(value.subjectId);
            if (clue == null) { Hide(); return; }
            if (title != null) title.text = clue.DisplayName;
            if (body != null) body.text = clue.InspectionText;
            panel.SetActive(true);
            if (hideTimer != null) StopCoroutine(hideTimer);
            hideTimer = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            hideTimer = null;
            if (panel != null) panel.SetActive(false);
        }

        public void Hide()
        {
            if (hideTimer != null) { StopCoroutine(hideTimer); hideTimer = null; }
            if (panel != null) panel.SetActive(false);
        }
    }
}
