using UnityEngine;

namespace ApexShift.Presentation.Icons
{
    public static class ApexShiftIconPack
    {
        private const string BasePath = "ApexShift2D/Art/Icons";

        public static Texture2D GetResourceIcon(string resourceId)
        {
            return LoadIcon("Resources", ResolveResourceFileName(resourceId));
        }

        public static Texture2D GetItemIcon(string itemId)
        {
            return LoadIcon("Items", ResolveItemFileName(itemId));
        }

        public static Texture2D GetToolIcon(string toolId)
        {
            return LoadIcon("Tools", ResolveToolFileName(toolId));
        }

        public static Texture2D GetIcon(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            id = id.Trim().ToLowerInvariant();
            Texture2D resourceIcon = GetResourceIcon(id);
            if (resourceIcon != null)
            {
                return resourceIcon;
            }

            Texture2D itemIcon = GetItemIcon(id);
            if (itemIcon != null)
            {
                return itemIcon;
            }

            return GetToolIcon(id);
        }

        private static Texture2D LoadIcon(string categoryFolder, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return Resources.Load<Texture2D>($"{BasePath}/{categoryFolder}/{fileName}");
        }

        private static string ResolveResourceFileName(string id)
        {
            switch (id)
            {
                case "wood":
                case "resource_wood_log":
                    return "resource_wood_log";
                case "stone":
                case "resource_stone":
                    return "resource_stone";
                case "flint":
                case "resource_flint":
                    return "resource_flint";
                case "berries":
                case "resource_berries":
                    return "resource_berries";
                case "meat":
                case "resource_raw_meat":
                case "meat_drop":
                    return "resource_raw_meat";
                case "cooked_meat":
                    return "resource_cooked_meat";
                case "bone":
                case "resource_bone":
                    return "resource_bone";
                case "hide":
                case "resource_hide":
                    return "resource_hide";
                case "fiber":
                case "resource_fiber":
                    return "resource_fiber";
                case "leaf":
                case "resource_leaf":
                    return "resource_leaf";
                case "resin":
                case "resource_resin":
                    return "resource_resin";
                case "water":
                case "resource_water_drop":
                    return "resource_water_drop";
                case "mushroom":
                case "resource_mushroom":
                    return "resource_mushroom";
                case "herb":
                case "resource_herb":
                    return "resource_herb";
                default:
                    return null;
            }
        }

        private static string ResolveItemFileName(string id)
        {
            switch (id)
            {
                case "backpack":
                case "item_backpack":
                    return "item_backpack";
                case "storage_box":
                case "item_storage_box":
                    return "item_storage_box";
                case "bandage":
                case "item_bandage":
                    return "item_bandage";
                case "map":
                case "item_map":
                    return "item_map";
                case "campfire":
                case "item_campfire":
                    return "item_campfire";
                case "torch":
                case "item_torch":
                    return "item_torch";
                case "coin":
                case "item_coin":
                    return "item_coin";
                case "key":
                case "item_key":
                    return "item_key";
                case "day_token":
                case "item_day_token":
                    return "item_day_token";
                case "unknown":
                case "item_unknown":
                    return "item_unknown";
                default:
                    return null;
            }
        }

        private static string ResolveToolFileName(string id)
        {
            switch (id)
            {
                case "axe":
                case "tool_axe":
                    return "tool_axe";
                case "pickaxe":
                case "tool_pickaxe":
                    return "tool_pickaxe";
                case "spear":
                case "tool_spear":
                    return "tool_spear";
                case "knife":
                case "tool_knife":
                    return "tool_knife";
                case "hammer":
                case "tool_hammer":
                    return "tool_hammer";
                case "bow":
                case "tool_bow":
                    return "tool_bow";
                case "arrow":
                case "tool_arrow":
                    return "tool_arrow";
                default:
                    return null;
            }
        }
    }
}
