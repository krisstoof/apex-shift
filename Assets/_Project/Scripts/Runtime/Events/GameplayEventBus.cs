using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ApexShift.Runtime.Events
{
    public enum GameplayEventKind
    {
        ResourceHarvested,
        SmallPreyConsumedPlants,
        GrazerConsumedPlants,
        GrazerScavengedMeat,
        GrazerHuntedSmallPrey,
        VarnakHuntedSmallPrey,
        VarnakHuntedGrazer,
        VarnakAttackedPlayer,
        VarnakScaredByFire,
        BiomassChanged,
        PopulationChanged,
        EcosystemTickAdvanced
    }

    [Serializable]
    public sealed class GameplayEvent
    {
        public GameplayEventKind kind;
        public Vector3 position;
        public string biomeId;
        public string actorSpecies;
        public string targetKind;
        public string resourceId;
        public string itemId;
        public float amount;
        public float nutrition;
        public float biomassImpact;
        public string message;
        public float realtimeSinceStartup;

        public GameplayEvent(GameplayEventKind kind, Vector3 position, string biomeId = "default", string actorSpecies = "", string targetKind = "", string resourceId = "", string itemId = "", float amount = 0f, float nutrition = 0f, float biomassImpact = 0f, string message = null, float realtimeSinceStartup = -1f)
        {
            this.kind = kind;
            this.position = position;
            this.biomeId = Normalize(biomeId, "default");
            this.actorSpecies = Normalize(actorSpecies, string.Empty);
            this.targetKind = Normalize(targetKind, string.Empty);
            this.resourceId = Normalize(resourceId, string.Empty);
            this.itemId = Normalize(itemId, string.Empty);
            this.amount = Mathf.Max(0f, amount);
            this.nutrition = Mathf.Max(0f, nutrition);
            this.biomassImpact = Mathf.Max(0f, biomassImpact);
            this.message = string.IsNullOrWhiteSpace(message) ? kind.ToString() : message.Trim();
            this.realtimeSinceStartup = realtimeSinceStartup >= 0f ? realtimeSinceStartup : Time.realtimeSinceStartup;
        }

        public string ToDebugLine()
        {
            string actor = string.IsNullOrWhiteSpace(actorSpecies) ? "-" : actorSpecies;
            string target = string.IsNullOrWhiteSpace(targetKind) ? "-" : targetKind;
            string item = string.IsNullOrWhiteSpace(itemId) ? resourceId : itemId;
            string payload = string.IsNullOrWhiteSpace(item) ? target : item;
            return $"{realtimeSinceStartup:0.0}s {kind} biome:{biomeId} actor:{actor} target:{payload} amt:{amount:0.##} bio:{biomassImpact:0.##}";
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public static class GameEventBus
    {
        private const int DefaultLogCapacity = 64;
        private static readonly List<Action<GameplayEvent>> subscribers = new List<Action<GameplayEvent>>();
        private static readonly List<GameplayEvent> recentEvents = new List<GameplayEvent>(DefaultLogCapacity);
        private static int logCapacity = DefaultLogCapacity;

        public static int RecentEventCount => recentEvents.Count;
        public static IReadOnlyList<GameplayEvent> RecentEvents => recentEvents;

        public static IDisposable Subscribe(Action<GameplayEvent> handler)
        {
            if (handler == null)
            {
                return NoopSubscription.Instance;
            }

            if (!subscribers.Contains(handler))
            {
                subscribers.Add(handler);
            }

            return new Subscription(handler);
        }

        public static void Publish(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent == null)
            {
                return;
            }

            AddToRecentEvents(gameplayEvent);
            Action<GameplayEvent>[] snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i]?.Invoke(gameplayEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public static void PublishResourceHarvested(Vector3 position, string biomeId, string resourceId, string itemId, float amount, string message = null)
        {
            Publish(new GameplayEvent(GameplayEventKind.ResourceHarvested, position, biomeId, actorSpecies: "player", targetKind: "resource", resourceId: resourceId, itemId: itemId, amount: amount, message: message ?? "resource_harvested"));
        }

        public static void PublishCreatureEvent(GameplayEventKind kind, Vector3 position, string biomeId, string actorSpecies, string targetKind, float amount = 0f, float nutrition = 0f, float biomassImpact = 0f, string message = null)
        {
            Publish(new GameplayEvent(kind, position, biomeId, actorSpecies, targetKind, amount: amount, nutrition: nutrition, biomassImpact: biomassImpact, message: message));
        }

        public static void PublishBiomassChanged(Vector3 position, string biomeId, float biomassImpact, float remainingBiomass, string message = null)
        {
            Publish(new GameplayEvent(GameplayEventKind.BiomassChanged, position, biomeId, actorSpecies: "ecosystem", targetKind: "plant_biomass", amount: remainingBiomass, biomassImpact: biomassImpact, message: message ?? "biomass_changed"));
        }

        public static void PublishPopulationChanged(Vector3 position, string biomeId, float populationCount, string message = null)
        {
            Publish(new GameplayEvent(GameplayEventKind.PopulationChanged, position, biomeId, actorSpecies: "ecosystem", targetKind: "population", amount: populationCount, message: message ?? "population_changed"));
        }

        public static void PublishEcosystemTickAdvanced(string biomeId, float amount = 1f, string message = null)
        {
            Publish(new GameplayEvent(GameplayEventKind.EcosystemTickAdvanced, Vector3.zero, biomeId, actorSpecies: "ecosystem", targetKind: "tick", amount: amount, message: message ?? "ecosystem_tick_advanced"));
        }

        public static IReadOnlyList<GameplayEvent> GetRecentEvents(int maxCount)
        {
            int safeCount = Mathf.Max(0, maxCount);
            if (safeCount == 0 || recentEvents.Count == 0)
            {
                return Array.Empty<GameplayEvent>();
            }

            return recentEvents.Skip(Mathf.Max(0, recentEvents.Count - safeCount)).ToArray();
        }

        public static string[] GetRecentEventLines(int maxCount)
        {
            return GetRecentEvents(maxCount).Select(evt => evt != null ? evt.ToDebugLine() : string.Empty).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        }

        public static void ConfigureLogCapacity(int capacity)
        {
            logCapacity = Mathf.Max(1, capacity);
            TrimRecentEvents();
        }

        public static void ClearForTests()
        {
            subscribers.Clear();
            recentEvents.Clear();
            logCapacity = DefaultLogCapacity;
        }

        private static void AddToRecentEvents(GameplayEvent gameplayEvent)
        {
            recentEvents.Add(gameplayEvent);
            TrimRecentEvents();
        }

        private static void TrimRecentEvents()
        {
            while (recentEvents.Count > logCapacity)
            {
                recentEvents.RemoveAt(0);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action<GameplayEvent> handler;

            public Subscription(Action<GameplayEvent> handler)
            {
                this.handler = handler;
            }

            public void Dispose()
            {
                if (handler == null)
                {
                    return;
                }

                subscribers.Remove(handler);
                handler = null;
            }
        }

        private sealed class NoopSubscription : IDisposable
        {
            public static readonly NoopSubscription Instance = new NoopSubscription();
            public void Dispose() { }
        }
    }
}
