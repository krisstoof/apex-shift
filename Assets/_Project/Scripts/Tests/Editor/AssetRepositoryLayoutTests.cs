using NUnit.Framework;
using System.IO;
using System.Linq;
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
            Assert.That(HasModel("campfire_low_poly"), Is.True, "campfire_low_poly model is missing.");
            Assert.That(HasModel("storage_box_low_poly"), Is.True, "storage_box_low_poly model is missing.");
            Assert.That(HasModel("tent_low_poly") || HasModel("tent_stylized"), Is.True, "A tent model is missing.");
            Assert.That(HasModel("trap_low_poly"), Is.True, "trap_low_poly model is missing.");
            Assert.That(HasModel("wall_low_poly"), Is.True, "wall_low_poly model is missing.");
        }

        private static bool HasModel(string modelName)
        {
            return AssetDatabase.FindAssets(modelName + " t:Model", new[] { AssetRepositoryLayoutValidator.CanonicalPlaceablesPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(path => Path.GetFileNameWithoutExtension(path) == modelName);
        }
    }
}
