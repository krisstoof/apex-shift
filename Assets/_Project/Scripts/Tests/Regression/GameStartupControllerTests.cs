using ApexShift.Runtime.Flow;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public class GameStartupControllerTests
    {
        [Test]
        public void ContinueOrLoadGame_HidesAllMenus_WhenNoSaveExists()
        {
            GameObject root = new GameObject("StartupControllerRoot");
            GameStartupController controller = root.AddComponent<GameStartupController>();

            GameObject mainMenuRoot = new GameObject("MainMenu");
            GameObject pauseMenuRoot = new GameObject("PauseMenu");
            GameObject gameplayHudRoot = new GameObject("GameplayHUD");
            GameObject optionsMenuRoot = new GameObject("OptionsMenu");

            CanvasGroup mainMenuGroup = mainMenuRoot.AddComponent<CanvasGroup>();
            CanvasGroup pauseMenuGroup = pauseMenuRoot.AddComponent<CanvasGroup>();
            CanvasGroup optionsMenuGroup = optionsMenuRoot.AddComponent<CanvasGroup>();

            controller.Configure(
                null,
                mainMenuRoot,
                pauseMenuRoot,
                gameplayHudRoot,
                optionsMenuRoot,
                mainMenuGroup,
                pauseMenuGroup,
                optionsMenuGroup);

            mainMenuRoot.SetActive(true);
            pauseMenuRoot.SetActive(true);
            optionsMenuRoot.SetActive(true);
            gameplayHudRoot.SetActive(false);

            controller.ContinueOrLoadGame();

            Assert.That(mainMenuRoot.activeSelf, Is.False);
            Assert.That(pauseMenuRoot.activeSelf, Is.False);
            Assert.That(optionsMenuRoot.activeSelf, Is.False);
            Assert.That(gameplayHudRoot.activeSelf, Is.True);
            Assert.That(mainMenuGroup.alpha, Is.EqualTo(0f));
            Assert.That(pauseMenuGroup.alpha, Is.EqualTo(0f));
            Assert.That(optionsMenuGroup.alpha, Is.EqualTo(0f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mainMenuRoot);
            Object.DestroyImmediate(pauseMenuRoot);
            Object.DestroyImmediate(gameplayHudRoot);
            Object.DestroyImmediate(optionsMenuRoot);
        }
    }
}
