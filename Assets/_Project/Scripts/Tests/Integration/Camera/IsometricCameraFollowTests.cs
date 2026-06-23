using System.Reflection;
using ApexShift.Runtime.Camera;
using NUnit.Framework;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Tests.Integration.Camera
{
    public class IsometricCameraFollowTests
    {
        [Test]
        public void AwakeConfiguresOrthographicCamera()
        {
            GameObject cameraObject = new GameObject("Camera");
            CameraComponent camera = cameraObject.AddComponent<CameraComponent>();
            IsometricCameraFollow follow = cameraObject.AddComponent<IsometricCameraFollow>();

            follow.SendMessage("Awake");

            Assert.IsTrue(camera.orthographic);
        }

        [Test]
        public void SetTargetStoresTargetTransform()
        {
            GameObject cameraObject = new GameObject("Camera");
            IsometricCameraFollow follow = cameraObject.AddComponent<IsometricCameraFollow>();
            GameObject targetObject = new GameObject("Target");

            follow.SetTarget(targetObject.transform);

            FieldInfo field = TestReflection.GetInstanceField(typeof(IsometricCameraFollow), "target");
            Assert.AreSame(targetObject.transform, field.GetValue(follow));
        }
    }
}
