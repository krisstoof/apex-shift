using System;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.Story;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class RunCompletionHUDView : MonoBehaviour
    {
        private GameObject panel;
        private Text title, message;
        private Button returnButton;
        private GameStartupController startup;
        private StoryProgressionRuntime story, subscribedStory;
        private EscapeBoatRuntime boat;
        private IDisposable events;
        public bool IsVisible => panel != null && panel.activeInHierarchy;
        public string TitleText => title != null ? title.text : string.Empty;
        public string MessageText => message != null ? message.text : string.Empty;
        public void Configure(GameObject root, Text heading, Text body, Button button, GameStartupController flow)
        {
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToMenu);
            panel = root; title = heading; message = body; returnButton = button; startup = flow;
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);
            Refresh();
        }
        public void Bind(StoryProgressionRuntime progression, EscapeBoatRuntime runtime)
        { Unsubscribe(); story = progression; boat = runtime; if (isActiveAndEnabled) Subscribe(); Refresh(); }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() { Unsubscribe(); if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToMenu); }
        private void Subscribe()
        {
            if (story != null && subscribedStory == null) { subscribedStory = story; story.StateChanged += Refresh; }
            if (events == null) events = GameEventBus.Subscribe(e => { if (e.kind == GameplayEventKind.FinalEscapeStarted) Refresh(); });
        }
        private void Unsubscribe()
        {
            if (subscribedStory != null) subscribedStory.StateChanged -= Refresh;
            subscribedStory = null; events?.Dispose(); events = null;
        }
        public void Refresh()
        {
            bool completed = story != null && story.CurrentStageId == StoryStageIds.Completed;
            bool leaving = boat != null && boat.EscapeInProgress;
            if (panel != null) panel.SetActive(completed || leaving);
            if (title != null) title.text = completed ? "RUN COMPLETE" : "Leaving the island...";
            if (message != null) message.text = completed ? "You escaped the island." : "The smuggler boat is taking you away.";
            if (returnButton != null) returnButton.gameObject.SetActive(completed);
            if (completed) { GameSessionState.EndGameplay(); Time.timeScale = 0f; }
        }
        private void ReturnToMenu() { if (startup != null) startup.ShowMainMenu(); }
    }
}
