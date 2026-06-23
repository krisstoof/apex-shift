using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ApexShift.Tests.Editor
{
    public sealed class HandcraftedBiomeWorldBuilderTests
    {
        [TestCaseSource(nameof(IsInsideIslandCases))]
        public void IsInsideIsland_ContainsCenterAndExcludesFarCorner(float x, float z, bool expected)
        {
            Assert.AreEqual(expected, InvokeIsInsideIsland(x, z));
        }

        [TestCaseSource(nameof(DetermineBiomeCases))]
        public void DetermineBiome_PlacesBiomesInExpectedDirections(Vector3 position, string expectedBiome)
        {
            Assert.AreEqual(expectedBiome, InvokeDetermineBiome(position).ToString());
        }

        [TestCaseSource(nameof(ManualVegetationOverrideCases))]
        public void ManualVegetationOverrides_MapKnownPrefabNamesToExpectedRoles(string assetPath, string prefabName, string expectedRole)
        {
            AssertManualVegetationRole(assetPath, prefabName, expectedRole);
        }

        [TestCaseSource(nameof(ManualVegetationPriorityCases))]
        public void ManualVegetationOverrides_PrioritizeSpecificNamesBeforeBroadNames(string assetPath, string prefabName, string expectedRole)
        {
            AssertManualVegetationRole(assetPath, prefabName, expectedRole);
        }

        [TestCaseSource(nameof(ManualOverrideNameCases))]
        public void ManualVegetationOverrides_UseExactNamesForRoleSelection(string roleName, string expectedName)
        {
            Type builderType = EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor");
            Type roleType = builderType.GetNestedType("VegetationRole", BindingFlags.NonPublic);
            Assert.IsNotNull(roleType, "Could not resolve VegetationRole.");

            object role = Enum.Parse(roleType, roleName);
            MethodInfo method = EditorTestReflection.GetStaticMethod(builderType, "GetManualOverrideNamesForRole", BindingFlags.NonPublic | BindingFlags.Static);
            string[] names = (string[])method.Invoke(null, new[] { role });

            CollectionAssert.Contains(names, expectedName);
        }

        [TestCaseSource(nameof(SnowVariantCases))]
        public void ManualVegetationOverrides_RejectSnowVariants(string assetPath, string prefabName)
        {
            MethodInfo method = EditorTestReflection.GetStaticMethod(
                EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor"),
                "TryGetManualVegetationRole",
                BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = { assetPath, prefabName, null };

            bool found = (bool)method.Invoke(null, args);

            Assert.IsFalse(found);
            Assert.IsNull(args[2]);
        }

        [Test]
        public void ApplyBiomeMaterialProperties_UsesProfileColorAndKeepsSourceTexture()
        {
            object profile = InvokeGetProfile("Westwood");
            Type profileType = profile.GetType();

            Material target = new Material(Shader.Find("Standard"));
            Material source = new Material(Shader.Find("Standard"));
            Texture2D texture = Texture2D.whiteTexture;
            source.mainTexture = texture;
            source.color = new Color(0.9f, 0.2f, 0.2f, 1f);

            InvokeApplyBiomeMaterialProperties(target, profile, source);

            Color profileColor = (Color)profileType.GetProperty("GroundColor").GetValue(profile);
            Color targetColor = target.HasProperty("_BaseColor") ? target.GetColor("_BaseColor") : target.color;

            Assert.AreEqual(profileColor, targetColor);
            Assert.AreSame(texture, target.mainTexture);
        }

        [Test]
        public void CreateHandcraftedBiomeWorld_CreatesWorldBoundsAndBoundaryRoot()
        {
            InvokeCreateWorld();

            Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/BiomeWorldTest.unity");
            Assert.IsTrue(scene.IsValid());
            Assert.IsNotNull(GameObject.Find("WorldBounds"));
            Assert.IsNotNull(GameObject.Find("BoundaryRoot"));
        }

        [Test]
        public void SceneBuilders_ReferenceTheGeneratedInputActionsAsset()
        {
            string handcraftedSource = System.IO.File.ReadAllText("Assets/_Project/Scripts/Editor/World/HandcraftedBiomeWorldBuilder.cs");
            string baseSceneSource = System.IO.File.ReadAllText("Assets/_Project/Scripts/Editor/ApexShiftSceneBuilder.cs");

            Assert.IsTrue(handcraftedSource.Contains("ApexShiftInputActions.inputactions"));
            Assert.IsTrue(handcraftedSource.Contains("SetInputActions"));
            Assert.IsTrue(handcraftedSource.Contains("PlayerInputReader"));
            Assert.IsTrue(handcraftedSource.Contains("PlayerActionDebugLog"));
            Assert.IsTrue(handcraftedSource.Contains("PlayerActionFeedback"));
            Assert.IsTrue(baseSceneSource.Contains("ApexShiftInputActions.inputactions"));
            Assert.IsTrue(baseSceneSource.Contains("SetInputActions"));
            Assert.IsTrue(baseSceneSource.Contains("PlayerInputReader"));
            Assert.IsTrue(baseSceneSource.Contains("PlayerActionDebugLog"));
            Assert.IsTrue(baseSceneSource.Contains("PlayerActionFeedback"));
        }

        private static bool InvokeIsInsideIsland(float x, float z)
        {
            MethodInfo method = EditorTestReflection.GetStaticMethod(
                EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor"),
                "IsInsideIsland",
                BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { x, z });
        }

        private static object InvokeDetermineBiome(Vector3 position)
        {
            MethodInfo method = EditorTestReflection.GetStaticMethod(
                EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor"),
                "DetermineBiome",
                BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(null, new object[] { position });
        }

        private static object InvokeGetProfile(string biomeName)
        {
            Type builderType = EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor");
            Type biomeEnumType = builderType.GetNestedType("BiomeKind", BindingFlags.NonPublic);
            Assert.IsNotNull(biomeEnumType, "Could not resolve BiomeKind.");

            MethodInfo method = EditorTestReflection.GetStaticMethod(builderType, "GetProfile", BindingFlags.NonPublic | BindingFlags.Static);
            object biomeEnum = Enum.Parse(biomeEnumType, biomeName);
            return method.Invoke(null, new[] { biomeEnum });
        }

        private static void InvokeCreateWorld()
        {
            Type builderType = EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor");
            MethodInfo method = builderType.GetMethod("CreateHandcraftedBiomeWorld", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not resolve CreateHandcraftedBiomeWorld.");
            method.Invoke(null, null);
        }

        private static void InvokeApplyBiomeMaterialProperties(Material target, object profile, Material source)
        {
            MethodInfo method = EditorTestReflection.GetStaticMethod(
                EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor"),
                "ApplyBiomeMaterialProperties",
                BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { target, profile, source });
        }

        private static void AssertManualVegetationRole(string assetPath, string prefabName, string expectedRole)
        {
            MethodInfo method = EditorTestReflection.GetStaticMethod(
                EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.HandcraftedBiomeWorldBuilder, Assembly-CSharp-Editor"),
                "TryGetManualVegetationRole",
                BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = { assetPath, prefabName, null };

            bool found = (bool)method.Invoke(null, args);

            Assert.IsTrue(found, prefabName + " should be classified manually.");
            Assert.AreEqual(expectedRole, args[2].ToString());
        }

        private static object[] ManualVegetationOverrideCases()
        {
            return new object[]
            {
                new object[] { "Assets/Any/tree_04.4.prefab", "tree_04.4", "DryTree" },
                new object[] { "Assets/Any/tree_02.1.prefab", "tree_02.1", "ConiferTree" },
                new object[] { "Assets/Any/tree_04.prefab", "tree_04", "LeafyTree" },
                new object[] { "Assets/Any/stone_01.prefab", "stone_01", "Rock" },
                new object[] { "Assets/Any/bush_02.1.prefab", "bush_02.1", "GreenBush" },
                new object[] { "Assets/Any/bush_02.2.prefab", "bush_02.2", "DryBush" }
            };
        }

        private static object[] ManualVegetationPriorityCases()
        {
            return new object[]
            {
                new object[] { "Assets/Any/tree_04.4.prefab", "tree_04.4", "DryTree" },
                new object[] { "Assets/Any/bush_02.2.prefab", "bush_02.2", "DryBush" },
                new object[] { "Assets/Any/bush_02.1.prefab", "bush_02.1", "GreenBush" },
                new object[] { "Assets/Any/stone_01.prefab", "stone_01", "Rock" }
            };
        }

        private static object[] IsInsideIslandCases()
        {
            return new object[]
            {
                new object[] { 0f, 0f, true },
                new object[] { 16f, 0f, true },
                new object[] { 0f, 14f, true },
                new object[] { 80f, 80f, false }
            };
        }

        private static object[] DetermineBiomeCases()
        {
            return new object[]
            {
                new object[] { new Vector3(-24f, 0f, 0f), "Westwood" },
                new object[] { new Vector3(24f, 0f, 0f), "SouthThicket" },
                new object[] { new Vector3(0f, 0f, 28f), "StonebackRidge" },
                new object[] { new Vector3(0f, 0f, -28f), "RedfangWilds" },
                new object[] { new Vector3(0f, 0f, 0f), "HearthMeadow" }
            };
        }

        private static object[] ManualOverrideNameCases()
        {
            return new object[]
            {
                new object[] { "DryTree", "tree_04.4" },
                new object[] { "ConiferTree", "tree_02.1" },
                new object[] { "LeafyTree", "tree_04" },
                new object[] { "Rock", "stone_01" },
                new object[] { "GreenBush", "bush_02.1" },
                new object[] { "DryBush", "bush_02.2" }
            };
        }

        private static object[] SnowVariantCases()
        {
            return new object[]
            {
                new object[] { "Assets/Any/tree_04.4_snow.prefab", "tree_04.4_snow" },
                new object[] { "Assets/Any/tree_02.1_snow.prefab", "tree_02.1_snow" },
                new object[] { "Assets/Any/tree_04_snow.prefab", "tree_04_snow" },
                new object[] { "Assets/Any/bush_02.1_snow.prefab", "bush_02.1_snow" },
                new object[] { "Assets/Any/bush_02.2_snow.prefab", "bush_02.2_snow" }
            };
        }

    }
}
