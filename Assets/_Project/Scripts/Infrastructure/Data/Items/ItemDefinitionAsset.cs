using ApexShift.Core.Items;
using UnityEngine;

namespace ApexShift.Infrastructure.Data.Items
{
    [CreateAssetMenu(menuName = "Apex Shift/Data/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        [Min(1)]
        private int maxStackSize = 20;

        public ItemDefinition ToCoreDefinition()
        {
            return new ItemDefinition(new ItemId(itemId), displayName, maxStackSize);
        }

        public void ConfigureForTests(string itemId, string displayName, int maxStackSize)
        {
            this.itemId = itemId;
            this.displayName = displayName;
            this.maxStackSize = maxStackSize;
        }
    }
}
