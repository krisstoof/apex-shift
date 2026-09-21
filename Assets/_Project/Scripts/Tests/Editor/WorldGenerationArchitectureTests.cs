using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ApexShift.Runtime.World.Generation;

namespace ApexShift.Tests.Editor
{
    public sealed class WorldGenerationArchitectureTests
    {
        [Test]
        public void Coordinator_ExecutesStagesInDeclaredOrder()
        {
            var ownerObject = new GameObject("ArchitectureTestOwner");
            try
            {
                var owner = ownerObject.AddComponent<WorldRuntimeOwner>();
                WorldGenerationContext context = owner.BeginGeneration(12345);
                var seen = new List<string>();
                var coordinator = new WorldGenerationCoordinator();
                coordinator.Generate(context,
                    new WorldGenerationStage("prepare", _ => seen.Add("prepare")),
                    new WorldGenerationStage("terrain", _ => seen.Add("terrain")),
                    new WorldGenerationStage("navmesh", _ => seen.Add("navmesh")),
                    new WorldGenerationStage("finalize", _ => seen.Add("finalize")));

                CollectionAssert.AreEqual(new[] { "prepare", "terrain", "navmesh", "finalize" }, seen);
                CollectionAssert.AreEqual(seen, coordinator.LastStageOrder);
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void Coordinator_SameSeedProducesSameStageRandomValues()
        {
            var ownerObject = new GameObject("ArchitectureTestOwner");
            try
            {
                var owner = ownerObject.AddComponent<WorldRuntimeOwner>();
                var firstValues = new List<int>();
                var secondValues = new List<int>();
                var coordinator = new WorldGenerationCoordinator();
                WorldGenerationStage sample = new WorldGenerationStage("sample", _ => firstValues.Add(Random.Range(0, 100000)));
                coordinator.Generate(owner.BeginGeneration(4321), sample);
                owner.Clear();
                sample = new WorldGenerationStage("sample", _ => secondValues.Add(Random.Range(0, 100000)));
                coordinator.Generate(owner.BeginGeneration(4321), sample);

                CollectionAssert.AreEqual(firstValues, secondValues);
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void Owner_Clear_DoesNotDestroyUnrelatedSameNamedObjects()
        {
            var ownerObject = new GameObject("ArchitectureTestOwner");
            var unrelatedPlayer = new GameObject("Player");
            try
            {
                var owner = ownerObject.AddComponent<WorldRuntimeOwner>();
                WorldGenerationContext context = owner.BeginGeneration(7);
                var generatedPlayer = new GameObject("Player");
                generatedPlayer.transform.SetParent(context.GenerationRoot, false);

                owner.Clear();

                Assert.That(unrelatedPlayer, Is.Not.Null);
                Assert.IsTrue(generatedPlayer == null, "Generated objects must be destroyed with their owner.");
            }
            finally
            {
                Object.DestroyImmediate(unrelatedPlayer);
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void Owner_GenerateClearGenerate_HasOneCurrentGenerationRoot()
        {
            var ownerObject = new GameObject("ArchitectureTestOwner");
            try
            {
                var owner = ownerObject.AddComponent<WorldRuntimeOwner>();
                WorldGenerationContext first = owner.BeginGeneration(1);
                Transform firstRoot = first.GenerationRoot;
                owner.Clear();
                WorldGenerationContext second = owner.BeginGeneration(1);

                Assert.IsTrue(firstRoot == null, "The first generation root must be destroyed.");
                Assert.That(second.GenerationRoot, Is.Not.Null);
                Assert.That(ownerObject.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void Owner_Clear_DestroysSerializedRootWhenCurrentContextIsUnavailable()
        {
            var ownerObject = new GameObject("SerializedOwnershipTestOwner");
            try
            {
                var owner = ownerObject.AddComponent<WorldRuntimeOwner>();
                var serializedRoot = new GameObject(WorldRuntimeOwner.GenerationRootName).transform;
                serializedRoot.SetParent(ownerObject.transform, false);

                SerializedObject serializedOwner = new SerializedObject(owner);
                SerializedProperty rootProperty = serializedOwner.FindProperty("generationRoot");
                Assert.That(rootProperty, Is.Not.Null, "WorldRuntimeOwner must serialize its owned GenerationRoot.");
                rootProperty.objectReferenceValue = serializedRoot;
                serializedOwner.ApplyModifiedPropertiesWithoutUndo();

                owner.Clear();

                Assert.IsTrue(serializedRoot == null, "Clear must destroy the serialized root without CurrentContext.");
                Assert.That(owner.GenerationRoot, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
            }
        }
    }
}
