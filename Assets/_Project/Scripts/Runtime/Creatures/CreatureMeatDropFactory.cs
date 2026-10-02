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

            string id = sourceCreature.SpeciesId;
            int amount = sourceCreature.Definition.MeatDropAmount;
            if (amount <= 0) return;
            Vector3 dropPosition = position + new Vector3(0f, 0.18f, 0f);

            GameObject drop = ItemPickupSpawner.Spawn("meat", amount, dropPosition, Quaternion.identity);
            if (drop != null)
            {
                drop.name = $"MeatDrop_{id}";
            }
        }

        public static void TrySpawnBoneDrop(Vector3 position, CreatureAgentView sourceCreature)
        {
            if (sourceCreature == null)
            {
                return;
            }

            var definition = sourceCreature.Definition;
            if (definition.BoneDropMax <= 0) return;

            ItemPickupSpawner.Spawn("bone", Random.Range(definition.BoneDropMin, definition.BoneDropMax + 1), position + new Vector3(0.35f, 0.15f, 0.15f), Quaternion.identity);
        }
    }
}
