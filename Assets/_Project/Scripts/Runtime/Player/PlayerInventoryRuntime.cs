using ApexShift.Core.Inventory;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerInventoryRuntime : MonoBehaviour
    {
        [SerializeField]
        private int slotCount = InventoryState.DefaultSlotCount;

        private InventoryState inventory;

        public InventoryState Inventory => inventory;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (inventory != null) return;
            inventory = new InventoryState(Core.Items.ItemDatabase.CreateDefault(), slotCount);
        }
    }
}
