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
                Assert.AreEqual(broken.color, repaired.color);
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
            Type utilityType = Type.GetType("ApexShift.EditorTools.World.NatureMaterialRepairUtility, Assembly-CSharp-Editor");
            Assert.IsNotNull(utilityType, "Could not resolve NatureMaterialRepairUtility.");

            MethodInfo method = utilityType.GetMethod("RepairMaterialsUnder", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not resolve RepairMaterialsUnder.");

            method.Invoke(null, new object[] { root });
        }
    }
}
