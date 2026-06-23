using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;

namespace ApexShift.Infrastructure.Data.Mapping
{
    public static class ItemDatabaseAssetMapper
    {
        public static ItemDatabase ToCoreDatabase(IEnumerable<ItemDefinitionAsset> assets)
        {
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }

            return ItemDatabase.FromDefinitions(assets.Select(asset => asset.ToCoreDefinition()));
        }
    }
}
