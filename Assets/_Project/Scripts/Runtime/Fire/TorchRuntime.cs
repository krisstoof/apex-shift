using ApexShift.Runtime.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Fire
{
    [DisallowMultipleComponent]
    public sealed class TorchRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private string torchItemId = "torch";
        [SerializeField] private float torchDurationSeconds = 120f;
        [SerializeField] private float protectionRadius = 9f;
        [SerializeField] private float intensity = 0.85f;
        [SerializeField] private Key toggleKey = Key.T;
        [SerializeField] private bool consumeTorchOnActivation = true;

        private FireSourceRuntime fireSource;
        private Light torchLight;
        private float remainingSeconds;

        public bool IsLit => fireSource != null && fireSource.IsActiveFire;

        private void Awake()
        {
            ResolveReferences();
            EnsureFireSource();
            EnsureVisuals();
            SetVisuals(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleTorch();
            }
        }

        public bool TryLightTorch()
        {
            ResolveReferences();
            EnsureFireSource();
            EnsureVisuals();

            if (IsLit)
            {
                return true;
            }

            if (consumeTorchOnActivation)
            {
                if (inventoryRuntime == null || inventoryRuntime.Inventory == null || inventoryRuntime.Inventory.GetAmount(torchItemId) <= 0)
                {
                    return false;
                }

                if (!inventoryRuntime.Inventory.RemoveItem(torchItemId, 1))
                {
                    return false;
                }
            }

            remainingSeconds = Mathf.Max(1f, torchDurationSeconds);
            fireSource.Configure("torch", protectionRadius, intensity);
            fireSource.SetActiveFire(true);
            SetVisuals(true);
            return true;
        }

        public void Extinguish()
        {
            if (fireSource != null)
            {
                fireSource.SetActiveFire(false);
            }

            remainingSeconds = 0f;
            SetVisuals(false);
        }

        private void ToggleTorch()
        {
            if (IsLit)
            {
                Extinguish();
            }
            else
            {
                TryLightTorch();
            }
        }

        private void ResolveReferences()
        {
            if (inventoryRuntime == null)
            {
                inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            }
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

            fireSource.Configure("torch", protectionRadius, intensity);
        }

        private void EnsureVisuals()
        {
            if (torchLight != null)
            {
                return;
            }

            GameObject lightGo = new GameObject("TorchLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.45f, 1.35f, 0.25f);
            torchLight = lightGo.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.color = new Color(1f, 0.54f, 0.20f);
            torchLight.range = Mathf.Max(6f, protectionRadius * 0.9f);
            torchLight.intensity = 6f;
        }

        private void SetVisuals(bool lit)
        {
            if (torchLight != null)
            {
                torchLight.enabled = lit;
            }
        }
    }
}
