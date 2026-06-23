using System.Reflection;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.World;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Integration.Player
{
    public class IsometricPlayerWorldBoundsTests
    {
        [Test]
        public void MoveWithWorldBounds_StaysInsideAllowedTiles()
        {
            GameObject boundsObject = new GameObject("WorldBounds");
            WorldBounds bounds = boundsObject.AddComponent<WorldBounds>();
            bounds.Configure(4f, new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            });

            GameObject playerObject = new GameObject("Player");
            IsometricPlayerController controller = playerObject.AddComponent<IsometricPlayerController>();
            playerObject.transform.position = new Vector3(0f, 0f, 0f);

            InvokeMoveWithWorldBounds(controller, new Vector3(20f, 0f, 0f));

            Assert.That(playerObject.transform.position.x, Is.EqualTo(4f).Within(0.001f).Or.EqualTo(0f).Within(0.001f));
            Assert.That(bounds.Contains(playerObject.transform.position), Is.True);

            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(boundsObject);
        }

        private static void InvokeMoveWithWorldBounds(IsometricPlayerController controller, Vector3 movement)
        {
            MethodInfo method = TestReflection.GetInstanceMethod(typeof(IsometricPlayerController), "MoveWithWorldBounds");
            method.Invoke(controller, new object[] { movement });
        }
    }
}
