using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public class RuntimeComponentSmokeTests
    {
        [UnityTest]
        public IEnumerator CameraFollowAndPlayerControllerCanBeCreated()
        {
            GameObject cameraObject = new GameObject("Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<IsometricCameraFollow>();

            GameObject playerObject = new GameObject("Player");
            playerObject.AddComponent<IsometricPlayerController>();

            yield return null;

            Assert.IsNotNull(cameraObject.GetComponent<IsometricCameraFollow>());
            Assert.IsNotNull(playerObject.GetComponent<IsometricPlayerController>());
        }
    }
}
