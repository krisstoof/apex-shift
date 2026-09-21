using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>References owned by one world generation lifecycle.</summary>
    public sealed class WorldGenerationContext
    {
        public int Seed { get; }
        public WorldGenerationResult Result { get; set; }
        public Transform GenerationRoot { get; }
        public Transform TerrainRoot { get; set; }
        public Transform BiomeRoot { get; set; }
        public Transform ResourceRoot { get; set; }
        public Transform CreatureRoot { get; set; }
        public Transform BuildingRoot { get; set; }
        public Transform LandmarkRoot { get; set; }
        public GameObject Player { get; set; }
        public GameObject MainCamera { get; set; }
        public IslandTopographyRuntime IslandTopography { get; set; }
        public DayNightRuntime DayNight { get; set; }
        public WorldBounds WorldBounds { get; set; }
        public NavMeshSurface NavMeshSurface { get; set; }

        public WorldGenerationContext(int seed, Transform generationRoot)
        {
            Seed = seed;
            GenerationRoot = generationRoot;
        }
    }
}
