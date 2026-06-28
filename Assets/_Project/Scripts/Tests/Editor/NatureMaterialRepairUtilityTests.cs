using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class NatureMaterialRepairUtilityTests
    {
        [Test]
        public void RepairMaterialsUnder_ReplacesBrokenMaterialWithGeneratedURPMaterial()
        {
            GameObject root = new GameObject("Root");
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = "TestRenderer";
            child.transform.SetParent(root.transform, false);

            Renderer renderer = child.GetComponent<Renderer>();
            Material broken = new Material(Shader.Find("Standard"));
            broken.name = "BrokenNature";
            broken.color = new Color(0.25f, 0.5f, 0.75f, 1f);
            renderer.sharedMaterial = broken;

            string expectedAssetPath = "Assets/_Project/Materials/Generated/Nature/BrokenNature.mat";

            try
            {
                InvokeRepair(root.transform);

                Material repaired = renderer.sharedMaterial;
                Assert.IsNotNull(repaired);
                Assert.AreNotSame(broken, repaired);
                Assert.IsTrue(AssetDatabase.Contains(repaired));
                Assert.AreEqual(expectedAssetPath, AssetDatabase.GetAssetPath(repaired));
                AssertColorApproximately(broken.color, repaired.color);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(expectedAssetPath) != null)
                {
                    AssetDatabase.DeleteAsset(expectedAssetPath);
                }
            }
        }

        private static void InvokeRepair(Transform root)
        {
            Type utilityType = EditorTestReflection.GetTypeByName("ApexShift.EditorTools.World.NatureMaterialRepairUtility, ApexShift.Editor");

            MethodInfo method = utilityType.GetMethod("RepairMaterialsUnder", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not resolve RepairMaterialsUnder.");

            method.Invoke(null, new object[] { root });
        }

        private static void AssertColorApproximately(Color expected, Color actual, float tolerance = 0.001f)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }
    }
}
