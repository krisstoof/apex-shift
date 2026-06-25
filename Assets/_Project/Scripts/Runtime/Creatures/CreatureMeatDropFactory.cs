using UnityEngine;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;

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

            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = $"MeatDrop_{sourceCreature.CreatureId}";
            drop.transform.position = position + Vector3.up * 0.1f;
            drop.transform.localScale = new Vector3(0.45f, 0.2f, 0.45f);

            CreatureMeatDropConfigurator.Configure(drop, sourceCreature.CreatureId);
        }
    }

    internal static class CreatureMeatDropConfigurator
    {
        public static void Configure(GameObject drop, string creatureId)
        {
            FoodSourceView food = drop.GetComponent<FoodSourceView>() ?? drop.AddComponent<FoodSourceView>();
            food.Configure($"meat_{creatureId}", "Meat", FoodKind.Meat, 20f, 10f);

            Renderer renderer = drop.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                Color meatColor = new Color(0.55f, 0.08f, 0.06f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", meatColor);
                else mat.color = meatColor;
                renderer.sharedMaterial = mat;
            }
        }
    }
}
