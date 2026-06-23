using System.Collections.Generic;
using System.Reflection;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.World;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.World
{
    public sealed class WorldBoundsTests
    {
        [Test]
        public void Contains_ReturnsTrueForAllowedTileCenter()
        {
            GameObject root = new GameObject("WorldBoundsRoot");
            WorldBounds bounds = root.AddComponent<WorldBounds>();
            bounds.Configure(4f, new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f) });

            Assert.IsTrue(bounds.Contains(new Vector3(0.5f, 0f, 0.5f)));
            Assert.IsFalse(bounds.Contains(new Vector3(10f, 0f, 10f)));

            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void ClampToNearestAllowed_ReturnsNearestTileCenter()
        {
            GameObject root = new GameObject("WorldBoundsRoot");
            WorldBounds bounds = root.AddComponent<WorldBounds>();
            bounds.Configure(4f, new List<Vector3>
            {
                new Vector3(-4f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            });

            Vector3 clamped = bounds.ClampToNearestAllowed(new Vector3(3.4f, 0f, 2f));
            Assert.AreEqual(new Vector3(4f, 0f, 0f), clamped);

            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void CameraRelativeMovement_MapsInputToViewDirections()
        {
            GameObject player = new GameObject("Player");
            IsometricPlayerController controller = player.AddComponent<IsometricPlayerController>();

            Vector3 forward = InvokeCameraRelativeMovement(controller, new Vector2(0f, 1f));
            Vector3 right = InvokeCameraRelativeMovement(controller, new Vector2(1f, 0f));
            Vector3 left = InvokeCameraRelativeMovement(controller, new Vector2(-1f, 0f));

            Assert.That(forward.z, Is.EqualTo(1f).Within(0.001f).Or.EqualTo(-1f).Within(0.001f));
            Assert.That(right.x, Is.EqualTo(1f).Within(0.001f).Or.EqualTo(-1f).Within(0.001f));
            Assert.That(left.x, Is.EqualTo(-right.x).Within(0.001f));

            UnityEngine.Object.DestroyImmediate(player);
        }

        private static Vector3 InvokeCameraRelativeMovement(IsometricPlayerController controller, Vector2 input)
        {
            MethodInfo method = TestReflection.GetInstanceMethod(typeof(IsometricPlayerController), "CalculateCameraRelativeMovement");
            return (Vector3)method.Invoke(controller, new object[] { input });
        }
    }
}
