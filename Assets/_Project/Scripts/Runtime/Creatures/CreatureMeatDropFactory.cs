using UnityEngine;
using ApexShift.Runtime.Items;

namespace ApexShift.Runtime.Creatures
{
    public static class CreatureMeatDropFactory
    {
        public static void TrySpawnMeatDrop(Vector3 position, CreatureAgentView sourceCreature)
        {
            if (sourceCreature == null)
            {
                return;
            }

            string id = string.IsNullOrWhiteSpace(sourceCreature.CreatureId)
                ? string.Empty
                : sourceCreature.CreatureId.Trim().ToLowerInvariant();

            int amount = ResolveMeatAmount(id);
            Vector3 dropPosition = position + new Vector3(0f, 0.18f, 0f);

            GameObject drop = ItemPickupSpawner.Spawn("meat", amount, dropPosition, Quaternion.identity);
            if (drop != null)
            {
                drop.name = $"Item_meat_from_{id}";
            }
        }

        private static int ResolveMeatAmount(string creatureId)
        {
            switch (creatureId)
            {
                case "small_prey": return 1;
                case "grazer": return 2;
                case "varnak": return 3;
                default: return 1;
            }
        }

        public static void TrySpawnBoneDrop(Vector3 position, CreatureAgentView sourceCreature)
        {
            if (sourceCreature == null)
            {
                return;
            }

            string id = string.IsNullOrWhiteSpace(sourceCreature.CreatureId) ? string.Empty : sourceCreature.CreatureId.Trim().ToLowerInvariant();
            if (id != "varnak")
            {
                return;
            }

            ItemPickupSpawner.Spawn("bone", Random.Range(1, 3), position + new Vector3(0.35f, 0.15f, 0.15f), Quaternion.identity);
        }
    }
}
