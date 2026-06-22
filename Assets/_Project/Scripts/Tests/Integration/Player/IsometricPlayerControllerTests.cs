using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Integration.Player
{
    public class IsometricPlayerControllerTests
    {
        [Test]
        public void ComponentCanBeAddedWithoutThrowing()
        {
            GameObject player = new GameObject("Player");

            Assert.DoesNotThrow(() => player.AddComponent<IsometricPlayerController>());
        }
    }
}
