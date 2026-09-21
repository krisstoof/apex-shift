using UnityEngine;
using Unity.AI.Navigation;

namespace ApexShift.Runtime.World.Generation
{
    public static class WorldNavMeshBuildStage
    {
        public static void Execute(WorldGenerationContext context)
        {
            if (context == null || context.TerrainRoot == null) return;
            NavMeshSurface surface = context.TerrainRoot.GetComponent<NavMeshSurface>();
            if (surface == null) surface = context.TerrainRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
            context.NavMeshSurface = surface;
        }
    }
}
