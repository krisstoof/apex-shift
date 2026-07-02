using UnityEngine;
using ApexShift.Runtime.World.Generation;

namespace ApexShift.Runtime.Player
{
    /// <summary>
    /// Placed on the player to detect entry and exit of water volumes.
    /// Uses WaterVolume marker component rather than Unity tags (tags require manual
    /// registration in the TagManager; components work without any editor setup).
    /// </summary>
    [RequireComponent(typeof(IsometricPlayerController))]
    [RequireComponent(typeof(PlayerAnimationDriver))]
    public sealed class PlayerWaterDetector : MonoBehaviour
    {
        private IsometricPlayerController controller;
        private PlayerAnimationDriver animDriver;

        /// How many water triggers the player is currently inside.
        private int waterVolumeCount;

        private void Awake()
        {
            controller = GetComponent<IsometricPlayerController>();
            animDriver = GetComponent<PlayerAnimationDriver>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<WaterVolume>() == null) return;

            waterVolumeCount++;
            if (waterVolumeCount == 1)
            {
                controller.EnterWater();
                animDriver.SetSwimming(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<WaterVolume>() == null) return;

            waterVolumeCount = Mathf.Max(0, waterVolumeCount - 1);
            if (waterVolumeCount == 0)
            {
                controller.ExitWater();
                animDriver.SetSwimming(false);
            }
        }

        private void OnDisable()
        {
            if (waterVolumeCount > 0)
            {
                waterVolumeCount = 0;
                controller?.ExitWater();
                animDriver?.SetSwimming(false);
            }
        }
    }
}
