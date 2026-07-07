using ApexShift.Runtime.Settings;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Settings
{
    public sealed class GameSettingsDataTests
    {
        [Test]
        public void SanitizeClampsAudioAndGraphicsValues()
        {
            GameSettingsData settings = new GameSettingsData
            {
                masterVolume = 2f,
                musicVolume = -1f,
                ambientVolume = 4f,
                sfxVolume = -0.5f,
                uiVolume = 9f,
                resolutionWidth = 10,
                resolutionHeight = 10,
                targetFps = 999,
                renderScale = 9f,
                shadowQuality = 9
            };

            settings.Sanitize();

            Assert.AreEqual(1f, settings.masterVolume);
            Assert.AreEqual(0f, settings.musicVolume);
            Assert.AreEqual(1f, settings.ambientVolume);
            Assert.AreEqual(0f, settings.sfxVolume);
            Assert.AreEqual(1f, settings.uiVolume);
            Assert.GreaterOrEqual(settings.resolutionWidth, 640);
            Assert.GreaterOrEqual(settings.resolutionHeight, 360);
            Assert.AreEqual(240, settings.targetFps);
            Assert.AreEqual(1.5f, settings.renderScale);
            Assert.AreEqual(2, settings.shadowQuality);
        }

        [Test]
        public void CloneCreatesIndependentCopy()
        {
            GameSettingsData source = GameSettingsData.CreateDefaults();
            GameSettingsData clone = source.Clone();

            clone.masterVolume = 0.25f;

            Assert.AreNotSame(source, clone);
            Assert.AreNotEqual(source.masterVolume, clone.masterVolume);
        }
    }
}
