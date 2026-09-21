using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Runs the world lifecycle in a deterministic, observable order.</summary>
    public sealed class WorldGenerationCoordinator
    {
        public IReadOnlyList<string> LastStageOrder => stageOrder;
        private readonly List<string> stageOrder = new List<string>();

        public void Generate(WorldGenerationContext context, params WorldGenerationStage[] stages)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (stages == null) throw new ArgumentNullException(nameof(stages));

            stageOrder.Clear();
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(context.Seed);
            try
            {
                foreach (WorldGenerationStage stage in stages)
                {
                    if (stage == null) continue;
                    stageOrder.Add(stage.Name);
                    stage.Execute(context);
                }
            }
            finally
            {
                UnityEngine.Random.state = previousState;
            }
        }
    }

    public sealed class WorldGenerationStage
    {
        public string Name { get; }
        private readonly Action<WorldGenerationContext> execute;

        public WorldGenerationStage(string name, Action<WorldGenerationContext> execute)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public void Execute(WorldGenerationContext context) => execute(context);
    }
}
