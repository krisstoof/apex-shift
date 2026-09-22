namespace ApexShift.Runtime.Player
{
    public static class ActionBarItemPolicy
    {
        public static bool IsActionBarItem(string itemId)
        {
            switch (Normalize(itemId)) { case "spear": case "bow": case "axe": case "pickaxe": case "torch": return true; default: return false; }
        }
        public static bool IsBlockedResourceItem(string itemId)
        {
            switch (Normalize(itemId)) { case "wood": case "stone": case "fiber": case "meat": case "hide": case "bone": case "berries": return true; default: return false; }
        }
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
