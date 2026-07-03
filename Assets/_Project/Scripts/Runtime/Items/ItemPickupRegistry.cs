using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.Items
{
    public static class ItemPickupRegistry
    {
        private static readonly List<ItemPickupView> pickups = new List<ItemPickupView>();

        public static IReadOnlyList<ItemPickupView> Pickups
        {
            get
            {
                Cleanup();
                return pickups;
            }
        }

        public static int PickupCount
        {
            get
            {
                Cleanup();
                int count = 0;
                for (int i = 0; i < pickups.Count; i++)
                {
                    ItemPickupView pickup = pickups[i];
                    if (pickup != null && pickup.gameObject != null && pickup.gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public static void Register(ItemPickupView pickup)
        {
            if (pickup == null || pickups.Contains(pickup))
            {
                return;
            }

            pickups.Add(pickup);
        }

        public static void Unregister(ItemPickupView pickup)
        {
            if (pickup == null)
            {
                return;
            }

            pickups.Remove(pickup);
        }

        public static void Cleanup()
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                ItemPickupView pickup = pickups[i];
                if (pickup == null || pickup.gameObject == null || !pickup.gameObject.activeInHierarchy)
                {
                    pickups.RemoveAt(i);
                }
            }
        }

        public static void ClearForTests()
        {
            pickups.Clear();
        }
    }
}
