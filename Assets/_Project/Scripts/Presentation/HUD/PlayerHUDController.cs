using ApexShift.Runtime.Player;
using UnityEngine;
using System.Collections.Generic;

namespace ApexShift.Presentation.HUD
{
    public sealed class PlayerHUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSurvivalRuntime survivalRuntime;
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;

        [Header("Stats")]
        [SerializeField] private StatBarUI healthBar;
        [SerializeField] private StatBarUI hungerBar;
        [SerializeField] private StatBarUI staminaBar;
        [SerializeField] private StatBarUI restBar;

        [Header("Resources")]
        [SerializeField] private List<ResourceCounterUI> resourceCounters;

        private void Awake()
        {
            if (survivalRuntime == null) survivalRuntime = Object.FindAnyObjectByType<PlayerSurvivalRuntime>();
            if (inventoryRuntime == null) inventoryRuntime = Object.FindAnyObjectByType<PlayerInventoryRuntime>();

            if (inventoryRuntime != null)
            {
                inventoryRuntime.EnsureInitialized();
            }
        }

        private void OnEnable()
        {
            SubscribeToInventory();
        }

        private void OnDisable()
        {
            UnsubscribeFromInventory();
        }

        private void Start()
        {
            // Re-check subscription in case inventory was set after OnEnable
            SubscribeToInventory();
            RefreshResources();
        }

        private bool subscribed;

        private void SubscribeToInventory()
        {
            if (subscribed) return;
            if (inventoryRuntime != null && inventoryRuntime.Inventory != null)
            {
                inventoryRuntime.Inventory.InventoryChanged += RefreshResources;
                subscribed = true;
                Debug.Log($"[HUD] Subscribed to inventory on {inventoryRuntime.gameObject.name}", this);
            }
        }

        private void UnsubscribeFromInventory()
        {
            if (!subscribed) return;
            if (inventoryRuntime != null && inventoryRuntime.Inventory != null)
            {
                inventoryRuntime.Inventory.InventoryChanged -= RefreshResources;
                subscribed = false;
                Debug.Log($"[HUD] Unsubscribed from inventory on {inventoryRuntime.gameObject.name}", this);
            }
        }

        private void Update()
        {
            UpdateStats();
        }

        private void UpdateStats()
        {
            if (survivalRuntime == null || survivalRuntime.Stats == null) return;

            var stats = survivalRuntime.Stats;
            if (healthBar != null) healthBar.SetValue(stats.Health, stats.Rules.MaxHealth);
            if (hungerBar != null) hungerBar.SetValue(stats.Hunger, stats.Rules.MaxHunger);
            if (staminaBar != null) staminaBar.SetValue(stats.Stamina, stats.Rules.MaxStamina);
            if (restBar != null) restBar.SetValue(stats.Rest, stats.Rules.MaxRest);
        }

        private void RefreshResources()
        {
            if (inventoryRuntime == null || inventoryRuntime.Inventory == null) return;

            Debug.Log("[HUD] Refreshing resources...", this);
            foreach (var counter in resourceCounters)
            {
                if (counter == null) continue;
                int amount = inventoryRuntime.Inventory.GetAmount(counter.ItemId);
                Debug.Log($"[HUD] {counter.ItemId}: {amount}", this);
                counter.UpdateCount(amount);
            }
        }
    }
}
