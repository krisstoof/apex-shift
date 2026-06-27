using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.UI
{
    public class PrefabRegistryTests
    {
        [Test]
        public void TryGetResourcePrefab_ReturnsMatchingPrefab()
        {
            PrefabRegistry registry = ScriptableObject.CreateInstance<PrefabRegistry>();
            GameObject coniferA = new GameObject("ConiferA");
            GameObject coniferB = new GameObject("ConiferB");

            SetField(registry, "resourcePrefabs", new[]
            {
                CreateResourceEntry(VegetationSpawnKind.ConiferTree, coniferA),
                CreateResourceEntry(VegetationSpawnKind.ConiferTree, coniferB)
            });

            bool found = registry.TryGetResourcePrefab(VegetationSpawnKind.ConiferTree, out GameObject prefab);

            Assert.IsTrue(found);
            Assert.That(prefab, Is.EqualTo(coniferA).Or.EqualTo(coniferB));
        }

        [Test]
        public void TryGetCreaturePrefab_IsCaseInsensitive()
        {
            PrefabRegistry registry = ScriptableObject.CreateInstance<PrefabRegistry>();
            GameObject grazer = new GameObject("Grazer");

            SetField(registry, "creaturePrefabs", new[]
            {
                CreateCreatureEntry("Grazer", grazer)
            });

            bool found = registry.TryGetCreaturePrefab("  grazer  ", out GameObject prefab);

            Assert.IsTrue(found);
            Assert.That(prefab, Is.EqualTo(grazer));
        }

        [Test]
        public void TryGetBuildingPrefab_ReturnsFalseWhenMissing()
        {
            PrefabRegistry registry = ScriptableObject.CreateInstance<PrefabRegistry>();

            SetField(registry, "buildingPrefabs", new BuildingPrefabEntry[0]);

            bool found = registry.TryGetBuildingPrefab("campfire", out GameObject prefab);

            Assert.IsFalse(found);
            Assert.IsNull(prefab);
        }

        private static ResourcePrefabEntry CreateResourceEntry(VegetationSpawnKind kind, GameObject prefab)
        {
            ResourcePrefabEntry entry = new ResourcePrefabEntry();
            SetField(entry, "kind", kind);
            SetField(entry, "prefab", prefab);
            return entry;
        }

        private static CreaturePrefabEntry CreateCreatureEntry(string creatureId, GameObject prefab)
        {
            CreaturePrefabEntry entry = new CreaturePrefabEntry();
            SetField(entry, "creatureId", creatureId);
            SetField(entry, "prefab", prefab);
            return entry;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Could not resolve field " + fieldName + ".");
            field.SetValue(target, value);
        }
    }
}
