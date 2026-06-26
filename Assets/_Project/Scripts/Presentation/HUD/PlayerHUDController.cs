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

        [Header("Inventory")]
        [SerializeField] private List<InventorySlotUI> inventorySlots;

        public void Configure(
            PlayerSurvivalRuntime survival,
            PlayerInventoryRuntime inventory,
            StatBarUI health,
            StatBarUI hunger,
            StatBarUI stamina,
            StatBarUI rest,
            List<ResourceCounterUI> counters)
        {
            UnsubscribeFromInventory();
            
            survivalRuntime = survival;
            inventoryRuntime = inventory;
            healthBar = health;
            hungerBar = hunger;
            staminaBar = stamina;
            restBar = rest;
            resourceCounters = counters;

            Debug.Log("[HUD] Controller Configured.", this);

            UpdateStats();
            TryAttachInventory();
        }

        public void ConfigureInventorySlots(List<InventorySlotUI> slots)
        {
            inventorySlots = slots;
            RefreshInventorySlots();
        }

        private void Awake()
        {
            FindReferencesIfNull();
        }

        private void FindReferencesIfNull()
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
            FindReferencesIfNull();
            TryAttachInventory();
        }

        private void OnDisable()
        {
            UnsubscribeFromInventory();
        }

        private void Start()
        {
            FindReferencesIfNull();
            TryAttachInventory();
        }

        private bool subscribed;

        private void TryAttachInventory()
        {
            if (inventoryRuntime == null)
            {
                return;
            }

            inventoryRuntime.EnsureInitialized();
            if (inventoryRuntime.Inventory == null || subscribed)
            {
                return;
            }

            inventoryRuntime.Inventory.InventoryChanged += RefreshResources;
            inventoryRuntime.Inventory.InventoryChanged += RefreshInventorySlots;
            subscribed = true;
            Debug.Log($"[HUD] Subscribed to inventory on {inventoryRuntime.gameObject.name}", this);
        }

        private void UnsubscribeFromInventory()
        {
            if (!subscribed) return;
            if (inventoryRuntime != null && inventoryRuntime.Inventory != null)
            {
                inventoryRuntime.Inventory.InventoryChanged -= RefreshResources;
                inventoryRuntime.Inventory.InventoryChanged -= RefreshInventorySlots;
            }
            subscribed = false;
        }

        private void Update()
        {
            if (!subscribed)
            {
                FindReferencesIfNull();
                TryAttachInventory();
            }

            UpdateStats();
        }

        private float statLogTimer;
        private void UpdateStats()
        {
            if (survivalRuntime == null || survivalRuntime.Stats == null) return;

            var stats = survivalRuntime.Stats;
            if (healthBar != null) healthBar.SetValue(stats.Health, stats.Rules.MaxHealth);
            if (hungerBar != null) hungerBar.SetValue(stats.Hunger, stats.Rules.MaxHunger);
            if (staminaBar != null) staminaBar.SetValue(stats.Stamina, stats.Rules.MaxStamina);
            if (restBar != null) restBar.SetValue(stats.Rest, stats.Rules.MaxRest);

            statLogTimer += Time.deltaTime;
            if (statLogTimer >= 5f)
            {
                statLogTimer = 0f;
                Debug.Log($"[HUD] Stats Update: H:{stats.Health:0}, Hu:{stats.Hunger:0}, S:{stats.Stamina:0}, R:{stats.Rest:0}", this);
            }
        }

        private void RefreshResources()
        {
            if (inventoryRuntime == null || inventoryRuntime.Inventory == null) 
            {
                return;
            }

            Debug.Log($"[HUD] Refreshing resources. Counters: {resourceCounters.Count}", this);
            foreach (var counter in resourceCounters)
            {
                if (counter == null) continue;
                int amount = inventoryRuntime.Inventory.GetAmount(counter.ItemId);
                counter.UpdateCount(amount);
            }
        }

        private void RefreshInventorySlots()
        {
            if (inventoryRuntime == null || inventoryRuntime.Inventory == null)
            {
                return;
            }

            if (inventorySlots == null || inventorySlots.Count == 0)
            {
                return;
            }

            int slotCount = inventoryRuntime.Inventory.SlotCount;
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                InventorySlotUI slotUi = inventorySlots[i];
                if (slotUi == null)
                {
                    continue;
                }

                if (i >= slotCount)
                {
                    slotUi.UpdateSlot(i, string.Empty, 0);
                    continue;
                }

                var snapshot = inventoryRuntime.Inventory.PeekSlotStack(i);
                slotUi.UpdateSlot(i, snapshot.ItemId, snapshot.Amount);
            }
        }
}
}
