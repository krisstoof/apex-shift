using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class WorldSpawnRotationTests
    {
        [Test]
        public void ComposeYawWithPrefabRotation_PreservesAuthoredBaseRotation()
        {
            GameObject prefab = new GameObject("RotationTestPrefab");
            Quaternion baseRotation = Quaternion.Euler(270.02f, 0f, 0f);
            prefab.transform.rotation = baseRotation;

            try
            {
                Quaternion actual = WorldSpawnRotation.ComposeYawWithPrefabRotation(prefab, 137f);
                Quaternion expected = Quaternion.Euler(0f, 137f, 0f) * baseRotation;

                Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(actual, Quaternion.Euler(0f, 137f, 0f)), Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void ComposeYawWithPrefabRotation_UsesIdentityForFallbackPrefabs()
        {
            Quaternion actual = WorldSpawnRotation.ComposeYawWithPrefabRotation((GameObject)null, 42f);
            Assert.That(Quaternion.Angle(actual, Quaternion.Euler(0f, 42f, 0f)), Is.LessThan(0.001f));
        }
    }
}
