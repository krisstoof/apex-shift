using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ApexShift.EditorTools.Validation;

namespace ApexShift.EditorTools.Validation.Tests
{
    public sealed class AssetRepositoryLayoutTests
    {
        [Test]
        public void UnityTemplateLeftovers_AreRemoved()
        {
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/Readme.asset"), Is.Null);
            Assert.That(AssetDatabase.IsValidFolder("Assets/TutorialInfo"), Is.False);
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/Scenes/SampleScene.unity"), Is.Null);
            Assert.That(System.IO.File.Exists("Assets/Editor.meta"), Is.False);
        }

        [Test]
        public void CanonicalProjectAssetRoots_Exist()
        {
            Assert.That(AssetDatabase.IsValidFolder(AssetRepositoryLayoutValidator.CanonicalAudioPath), Is.True);
            Assert.That(AssetDatabase.IsValidFolder(AssetRepositoryLayoutValidator.CanonicalPlaceablesPath), Is.True);
            Assert.That(AssetDatabase.IsValidFolder(AssetRepositoryLayoutValidator.CanonicalIconsPath), Is.True);
        }

        [Test]
        public void LegacyProjectImportWrappers_AreRemoved()
        {
            Assert.That(AssetDatabase.IsValidFolder("Assets/ApexShiftIconPack"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/apex_shift_audio_assets_v3_realistic"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/apex_shift_placeables_3d_v2_unity_obj"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/apex_shift_audio_assets_v3_oga_ref"), Is.False);
        }

        [Test]
        public void UrpSettings_AreStillPresent()
        {
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/PC_RPAsset.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.GUIDToAssetPath(AssetRepositoryLayoutValidator.SampleSceneProfileGuid), Is.EqualTo("Assets/Settings/SampleSceneProfile.asset"));
        }

        [Test]
        public void CanonicalIcons_CanBeLoaded()
        {
            Assert.That(UnityEngine.Resources.Load<Texture2D>("ApexShift2D/Art/Icons/Items/item_unknown"), Is.Not.Null);
            Assert.That(UnityEngine.Resources.Load<Texture2D>("ApexShift2D/Art/Icons/Resources/resource_wood_log"), Is.Not.Null);
            Assert.That(UnityEngine.Resources.Load<Texture2D>("ApexShift2D/Art/Icons/Tools/tool_axe"), Is.Not.Null);
        }

        [Test]
        public void CanonicalPlaceableModels_Exist()
        {
            string[] models = AssetDatabase.FindAssets("t:Model", new[] { AssetRepositoryLayoutValidator.CanonicalPlaceablesPath });
            Assert.That(models, Is.Not.Empty);
            Assert.That(AssetDatabase.GUIDToAssetPath(models[0]), Does.StartWith(AssetRepositoryLayoutValidator.CanonicalPlaceablesPath + "/"));
        }
    }
}
