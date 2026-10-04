using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using UnityEngine;

namespace ApexShift.Runtime.World.Interiors
{
    public static class SmugglerBaseInteriorBuilder
    {
        public static SmugglerBaseInteriorRuntime Build(WorldGenerationContext context, WorldGeneratorRuntime generator = null)
        {
            if (context == null || context.InteriorRoot == null)
                throw new System.ArgumentException("InteriorRoot must belong to the generation context.");
            if (context.SmugglerBaseInterior != null) return context.SmugglerBaseInterior;
            context.InteriorRoot.gameObject.SetActive(false);
            Vector3 edge = context.IslandTopography != null ? context.IslandTopography.WorldBounds.max : Vector3.zero;
            context.InteriorRoot.position = edge + new Vector3(220f, 30f, 220f);
            var root = Child(context.InteriorRoot, "SmugglerBaseInterior", Vector3.zero);
            var runtime = root.gameObject.AddComponent<SmugglerBaseInteriorRuntime>();
            var tunnel = Room(root, "EntryTunnel", 4f, 12f, 4f, true, false);
            var storage = Room(root, "StorageRoom", 9f, 8f, 14f, false, false);
            var operations = Room(root, "OperationsRoom", 10f, 9f, 22.5f, false, false);
            var dock = Room(root, "DockChamber", 16f, 14f, 34f, false, true);
            Transform[] anchors = {
                Child(tunnel, "EntrySpawn", new Vector3(0f, 0.1f, -4f)),
                Child(tunnel, "IslandExitInteraction", new Vector3(0f, 1f, -5f)),
                Child(storage, "StorageLootAnchor", new Vector3(-3f, 0.1f, 0f)),
                Child(operations, "OperationsClueAnchor", new Vector3(-3f, 0.1f, 1f)),
                Child(storage, "FuelAnchor", new Vector3(3f, 0.1f, 1f)),
                Child(operations, "BatteryAnchor", new Vector3(3f, 0.1f, 1f)),
                Child(operations, "BoatKeyAnchor", new Vector3(-3f, 0.1f, -2f)),
                Child(dock, "BoatAnchor", new Vector3(3.5f, 0.1f, 2f))
            };
            var entrance = LandmarkRegistry.FindById("base_entrance");
            runtime.Configure(context, generator, entrance, anchors,
                new Bounds(context.InteriorRoot.TransformPoint(new Vector3(0f, 1.5f, 19.5f)), new Vector3(15f, 4f, 42f)));
            var exitCollider = anchors[1].gameObject.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true; exitCollider.size = new Vector3(2f, 2f, 1f);
            anchors[1].gameObject.AddComponent<BaseInteriorExitRuntime>().Configure(runtime);
            Box(tunnel, "TimberExitDoor", new Vector3(0f, 1.1f, -5.8f), new Vector3(1.8f, 2.2f, 0.15f), Wood);
            Box(storage, "Crates", new Vector3(-3f, 0.6f, 0f), new Vector3(1.6f, 1.2f, 2f), Wood);
            Box(anchors[5], "BatteryShelf", Vector3.up * 0.5f, new Vector3(1.3f, 1f, 0.7f), Metal);
            Box(anchors[6], "KeyTable", Vector3.up * 0.4f, new Vector3(1.5f, 0.8f, 0.8f), Wood);
            Box(operations, "OperationsTable", new Vector3(-3f, 0.5f, 1f), new Vector3(1.6f, 1f, 2f), Wood);
            Box(dock, "TimberPier", new Vector3(-2.5f, 0.08f, 1f), new Vector3(2.5f, 0.16f, 10f), Wood);
            var boat = Child(anchors[7], "SmugglerBoatPlaceholder", Vector3.zero);
            Box(boat, "Hull", new Vector3(0f, 0.3f, 0f), new Vector3(2.5f, 0.6f, 5f), Metal);
            Box(boat, "PortGunwale", new Vector3(-1.2f, 0.8f, 0f), new Vector3(0.2f, 0.7f, 5f), Metal);
            Box(boat, "StarboardGunwale", new Vector3(1.2f, 0.8f, 0f), new Vector3(0.2f, 0.7f, 5f), Metal);
            Box(boat, "Bow", new Vector3(0f, 0.7f, 2.3f), new Vector3(2.4f, 0.6f, 0.5f), Metal);
            Box(boat, "Transom", new Vector3(0f, 0.7f, -2.4f), new Vector3(2.4f, 0.6f, 0.2f), Wood);
            Box(boat, "Console", new Vector3(0f, 1f, 0.4f), new Vector3(0.8f, 1f, 0.8f), Wood);
            Box(boat, "EngineBlock", new Vector3(0f, 0.8f, -2.7f), new Vector3(0.7f, 1.4f, 0.8f), Metal);
            runtime.ConfigureEscapeBoat(ApexShift.Runtime.Escape.EscapeBoatBuilder.Build(context, runtime, boat));
            context.SmugglerBaseInterior = runtime;
            if (entrance != null)
            {
                var interaction = entrance.GetComponent<BaseEntranceInteractionRuntime>();
                if (interaction == null) interaction = entrance.gameObject.AddComponent<BaseEntranceInteractionRuntime>();
                interaction.Configure(runtime);
            }
            return runtime;
        }

        private static readonly Color Stone = new Color(0.29f, 0.28f, 0.24f);
        private static readonly Color Wood = new Color(0.34f, 0.23f, 0.13f);
        private static readonly Color Metal = new Color(0.24f, 0.31f, 0.3f);
        private static Transform Room(Transform root, string name, float width, float length, float z, bool closedBack, bool closedFront)
        {
            var room = Child(root, name, new Vector3(0f, 0f, z));
            Box(room, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(width, 0.5f, length), Stone);
            Box(room, "WestWall", new Vector3(-width * 0.5f, 1f, 0f), new Vector3(0.35f, 2f, length), Stone);
            Box(room, "EastWall", new Vector3(width * 0.5f, 1f, 0f), new Vector3(0.35f, 2f, length), Stone);
            EndWall(room, width, -length * 0.5f, closedBack);
            EndWall(room, width, length * 0.5f, closedFront);
            var lamp = Child(room, "WarmLamp", new Vector3(-width * 0.5f + 0.7f, 2.3f, 0f));
            var light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1f, 0.72f, 0.38f);
            light.range = 12f; light.intensity = 2f; light.shadows = LightShadows.None;
            return room;
        }
        private static void EndWall(Transform room, float width, float z, bool closed)
        {
            if (closed) Box(room, "EndWall", new Vector3(0f, 1f, z), new Vector3(width, 2f, 0.35f), Stone);
            else
            {
                float segment = (width - 3f) * 0.5f;
                for (int side = -1; side <= 1; side += 2)
                    Box(room, "DoorwayWall", new Vector3(side * (1.5f + segment * 0.5f), 1f, z), new Vector3(segment, 2f, 0.35f), Stone);
            }
        }
        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position;
            return child;
        }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name; box.transform.SetParent(parent, false);
            box.transform.localPosition = position; box.transform.localScale = size;
            // Per-renderer color avoids creating owned native Material instances on each regeneration.
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            box.GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}
