using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Interiors;
using UnityEngine;

namespace ApexShift.Runtime.Escape
{
    public static class EscapeBoatBuilder
    {
        public static EscapeBoatRuntime Build(WorldGenerationContext context, SmugglerBaseInteriorRuntime interior, Transform boatVisual)
        {
            var boat = boatVisual.gameObject.AddComponent<EscapeBoatRuntime>();
            boat.Configure(context, interior);
            var collider = boatVisual.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.up; collider.size = new Vector3(3.5f, 2f, 6f); collider.isTrigger = true;
            Source(boat, interior.FuelAnchor, BoatRequirementDefinition.Production[0], true);
            Source(boat, interior.BatteryAnchor, BoatRequirementDefinition.Production[1], false);
            return boat;
        }
        private static void Source(EscapeBoatRuntime boat, Transform anchor, BoatRequirementDefinition definition, bool fuel)
        {
            var root = new GameObject(definition.SourceId); root.transform.SetParent(anchor, false);
            var source = root.AddComponent<BoatRequirementSourceRuntime>(); source.Configure(boat, definition); boat.RegisterSource(source);
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * 0.55f;
            collider.size = new Vector3(1f, 1.4f, 1f); collider.isTrigger = true;
            var color = fuel ? new Color(0.52f, 0.3f, 0.12f) : new Color(0.16f, 0.19f, 0.22f);
            Part(root.transform, fuel ? "JerryCan" : "BatteryBox", new Vector3(0f, 0.4f, 0f), new Vector3(0.65f, 0.8f, 0.45f), color);
            if (fuel)
            {
                Part(root.transform, "Handle", new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 0.15f, 0.12f), color);
                Part(root.transform, "Cap", new Vector3(0.25f, 0.85f, 0f), new Vector3(0.15f, 0.12f, 0.15f), Color.gray);
            }
            else
                for (int side = -1; side <= 1; side += 2)
                    Part(root.transform, "Terminal", new Vector3(side * 0.2f, 0.86f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Color.gray);
        }
        private static void Part(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Collider>().enabled = false;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            part.GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}
