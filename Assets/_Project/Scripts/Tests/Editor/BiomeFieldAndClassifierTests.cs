using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ApexShift.Runtime.World.Biomes;

namespace ApexShift.Tests.Editor
{
    public sealed class BiomeFieldAndClassifierTests
    {
        [Test]
        public void BiomeField_SameSeedIsDeterministic_AndDifferentSeedDiffers()
        {
            var a = new BiomeFieldGenerator(101, new BiomeFieldSettings());
            var b = new BiomeFieldGenerator(101, new BiomeFieldSettings());
            var c = new BiomeFieldGenerator(202, new BiomeFieldSettings());

            Assert.That(a.SampleMoisture(12.3f, -8.1f), Is.EqualTo(b.SampleMoisture(12.3f, -8.1f)).Within(0.000001f));
            Assert.That(a.SampleTemperature(12.3f, -8.1f), Is.EqualTo(b.SampleTemperature(12.3f, -8.1f)).Within(0.000001f));
            Random.InitState(9876);
            float before = a.SampleMoisture(12.3f, -8.1f);
            Random.InitState(123);
            float after = a.SampleMoisture(12.3f, -8.1f);
            Assert.That(after, Is.EqualTo(before).Within(0.000001f));
            Assert.That(Mathf.Abs(a.SampleMoisture(12.3f, -8.1f) - c.SampleMoisture(12.3f, -8.1f))
                        + Mathf.Abs(a.SampleTemperature(12.3f, -8.1f) - c.SampleTemperature(12.3f, -8.1f)), Is.GreaterThan(0.0001f));
        }

        [Test]
        public void Classifier_RecognizesExpectedEnvironmentSamples()
        {
            var classifier = new BiomeClassifier(101, new BiomeFieldSettings());
            Assert.That(classifier.Classify(Vector3.zero, new BiomeEnvironmentSample(0.05f, 2f, 0.45f, 0.5f)), Is.EqualTo("hearth_meadow"));
            Assert.That(classifier.Classify(new Vector3(50f, 0f, 50f), new BiomeEnvironmentSample(0.95f, 38f, 0.25f, 0.45f)), Is.EqualTo("stoneback_ridge"));
            Assert.That(classifier.Classify(new Vector3(45f, 0f, -35f), new BiomeEnvironmentSample(0.12f, 4f, 0.92f, 0.45f)), Is.EqualTo("south_thicket"));
            Assert.That(classifier.Classify(new Vector3(-45f, 0f, 20f), new BiomeEnvironmentSample(0.42f, 8f, 0.72f, 0.52f)), Is.EqualTo("westwood"));
            Assert.That(classifier.Classify(new Vector3(45f, 0f, 20f), new BiomeEnvironmentSample(0.45f, 8f, 0.12f, 0.90f)), Is.EqualTo("redfang_wilds"));
        }

        [TestCase(101)]
        [TestCase(202)]
        [TestCase(303)]
        public void Classifier_DistributesBiomesAcrossSeeds(int seed)
        {
            var field = new BiomeFieldGenerator(seed, new BiomeFieldSettings());
            var classifier = new BiomeClassifier(seed, new BiomeFieldSettings());
            var counts = new Dictionary<string, int>();
            for (int z = -72; z <= 72; z += 6)
                for (int x = -96; x <= 96; x += 6)
                {
                    float elevation = Mathf.Clamp01(Mathf.PerlinNoise((x + seed) * .025f, (z - seed) * .025f));
                    var sample = new BiomeEnvironmentSample(elevation, 8f + Mathf.PerlinNoise(x * .02f, z * .02f) * 22f,
                        field.SampleMoisture(x, z), field.SampleTemperature(x, z, elevation));
                    string id = classifier.Classify(new Vector3(x, 0f, z), sample);
                    counts[id] = counts.TryGetValue(id, out int count) ? count + 1 : 1;
                }

            Assert.That(counts.Count, Is.GreaterThanOrEqualTo(3));
            int total = 25 * 33;
            int largest = 0;
            foreach (int value in counts.Values) largest = Mathf.Max(largest, value);
            Assert.That((float)largest / total, Is.LessThan(0.99f));
        }
    }
}
