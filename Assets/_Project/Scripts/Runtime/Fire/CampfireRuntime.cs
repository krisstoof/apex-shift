using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Fire
{
    [DisallowMultipleComponent]
    public sealed class CampfireRuntime : MonoBehaviour
    {
        [SerializeField] private string fuelItemId = "wood";
        [SerializeField] private float secondsPerFuel = 60f;
        [SerializeField] private float maxFuelSeconds = 300f;
        [SerializeField] private float startingFuelSeconds = 90f;
        [SerializeField] private float protectionRadius = 13f;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private bool autoLightWithStartingFuel = true;

        private FireSourceRuntime fireSource;
        private Light campfireLight;
        private float fuelSeconds;
        private float _flickerTime;

        public bool IsLit => fireSource != null && fireSource.IsActiveFire;

        private void Awake()
        {
            EnsureFireSource();
            EnsureVisuals();
            fuelSeconds = Mathf.Clamp(startingFuelSeconds, 0f, Mathf.Max(1f, maxFuelSeconds));
            bool shouldLight = autoLightWithStartingFuel && fuelSeconds > 0f;
            fireSource.SetActiveFire(shouldLight, publishEvent: false);
            SetVisuals(shouldLight);
        }

        private void Update()
        {
            if (!IsLit || campfireLight == null) return;
            // Subtle flicker: modulate intensity with layered sin waves
            _flickerTime += Time.deltaTime;
            float flicker = 1f
                + Mathf.Sin(_flickerTime * 7.3f) * 0.14f
                + Mathf.Sin(_flickerTime * 13.7f + 1.2f) * 0.07f;
            campfireLight.intensity = 8f * flicker;
        }

        public bool Interact(GameObject actor)
        {
            PlayerInventoryRuntime inventory = ResolveInventory(actor);
            if (inventory == null || inventory.Inventory == null)
            {
                return false;
            }

            if (inventory.Inventory.GetAmount(fuelItemId) <= 0)
            {
                return false;
            }

            if (!inventory.Inventory.RemoveItem(fuelItemId, 1))
            {
                return false;
            }

            fuelSeconds = Mathf.Clamp(fuelSeconds + secondsPerFuel, 0f, Mathf.Max(1f, maxFuelSeconds));
            fireSource.Configure("campfire", protectionRadius, intensity);
            fireSource.SetActiveFire(true);
            SetVisuals(true);
            return true;
        }

        private void EnsureFireSource()
        {
            if (fireSource == null)
            {
                fireSource = GetComponent<FireSourceRuntime>();
                if (fireSource == null)
                {
                    fireSource = gameObject.AddComponent<FireSourceRuntime>();
                }
            }

            fireSource.Configure("campfire", protectionRadius, intensity);
        }

        private void EnsureVisuals()
        {
            if (campfireLight != null)
            {
                return;
            }

            GameObject lightGo = new GameObject("CampfireLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.55f;
            campfireLight = lightGo.AddComponent<Light>();
            campfireLight.type = LightType.Point;
            campfireLight.color = new Color(1f, 0.46f, 0.14f);
            campfireLight.range = Mathf.Max(10f, protectionRadius * 1.1f);
            campfireLight.intensity = 8f;
        }

        private void SetVisuals(bool lit)
        {
            if (campfireLight != null)
            {
                campfireLight.enabled = lit;
            }
        }

        private static PlayerInventoryRuntime ResolveInventory(GameObject actor)
        {
            if (actor == null)
            {
                return null;
            }

            PlayerInventoryRuntime inventory = actor.GetComponent<PlayerInventoryRuntime>();
            if (inventory == null)
            {
                inventory = actor.GetComponentInParent<PlayerInventoryRuntime>();
            }

            return inventory;
        }
    }
}
