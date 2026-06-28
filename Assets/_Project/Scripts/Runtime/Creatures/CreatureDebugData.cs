using System;
using System.Text;
using ApexShift.Runtime.Ecosystem;
using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    [Serializable]
    public sealed class CreatureDebugData
    {
        public string species;
        public string speciesId;
        public CreatureBehaviorState state;
        public string decisionReason;
        public string currentTarget;
        public string targetDetails;
        public string lastFoodSource;
        public float health;
        public float maxHealth;
        public float hunger;
        public float maxHunger;
        public string hungerStage;
        public float energy;
        public float maxEnergy;
        public float plantDiet;
        public float meatDiet;
        public float scavengerDiet;
        public string navStatus;
        public float navRemainingDistance;
        public float navVelocity;
        public float distanceToPlayer;
        public float ageSeconds;
        public int decisionCount;
        public float attackCooldown;
        public string currentBiomeId;
        public string homeBiomeId;
        public string populationBiomeId;
        public string currentNiche;
        public float huntDrive;
        public string simulationLevel;
        public int simulationLodChangeCount;
        public int activeSimulationTickCount;
        public int farSimulationTickCount;
        public int backgroundSimulationTickCount;
        public bool visibilityCulled;
        public bool backgroundSimulated;
        public int movementSpikeCount;
        public float maxMovementSpikeDistance;

        public static CreatureDebugData Capture(GameObject source)
        {
            CreatureDebugData data = new CreatureDebugData
            {
                species = source != null ? source.name : "missing",
                speciesId = "unknown",
                state = CreatureBehaviorState.Idle,
                decisionReason = "none",
                currentTarget = "none",
                targetDetails = "none",
                lastFoodSource = "none",
                health = -1f,
                maxHealth = -1f,
                hunger = -1f,
                maxHunger = -1f,
                hungerStage = "n/a",
                energy = -1f,
                maxEnergy = -1f,
                navStatus = "missing",
                distanceToPlayer = -1f,
                currentBiomeId = "default",
                homeBiomeId = "default",
                populationBiomeId = "default",
                currentNiche = "unknown",
                simulationLevel = "none"
            };

            if (source == null)
            {
                return data;
            }

            CreatureAgentView agent = source.GetComponent<CreatureAgentView>();
            CreatureNeedsRuntime needs = source.GetComponent<CreatureNeedsRuntime>();
            CreatureHealthRuntime health = source.GetComponent<CreatureHealthRuntime>();
            CreatureBehaviorBrain brain = source.GetComponent<CreatureBehaviorBrain>();
            CreatureSimulationLodRuntime lod = source.GetComponent<CreatureSimulationLodRuntime>();
            NavMeshAgent nav = source.GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                data.speciesId = string.IsNullOrWhiteSpace(agent.CreatureId) ? source.name : agent.CreatureId;
                data.species = data.speciesId;
            }

            if (health != null)
            {
                data.health = health.CurrentHealth;
                data.maxHealth = health.MaxHealth;
            }

            if (needs != null)
            {
                data.hunger = needs.State.Hunger;
                data.maxHunger = needs.State.MaxHunger;
                data.hungerStage = needs.State.Stage.ToString().ToLowerInvariant();
                data.energy = needs.State.Energy;
                data.maxEnergy = needs.State.MaxEnergy;
                data.plantDiet = needs.Diet.PlantPreference;
                data.meatDiet = needs.Diet.MeatPreference;
                data.scavengerDiet = needs.Diet.ScavengerPreference;
            }

            if (brain != null)
            {
                data.state = brain.State;
                data.decisionReason = string.IsNullOrWhiteSpace(brain.DecisionReason) ? "none" : brain.DecisionReason;
                data.currentTarget = string.IsNullOrWhiteSpace(brain.CurrentTargetLabel) ? "none" : brain.CurrentTargetLabel;
                data.lastFoodSource = string.IsNullOrWhiteSpace(brain.LastFoodSource) ? "none" : brain.LastFoodSource;
                data.decisionCount = brain.DecisionCount;
                data.attackCooldown = brain.AttackCooldown;
                data.currentBiomeId = NormalizeDebugValue(brain.CurrentBiomeId, "default");
                data.homeBiomeId = NormalizeDebugValue(brain.HomeBiomeId, data.currentBiomeId);
                data.populationBiomeId = NormalizeDebugValue(brain.PopulationBiomeId, data.currentBiomeId);
                data.currentNiche = NormalizeDebugValue(brain.CurrentNiche, "unknown");
                data.huntDrive = Mathf.Clamp01(brain.HuntDrive);
                data.targetDetails = CaptureTargetDetails(source.transform, brain.CurrentTargetTransform);
            }

            if (nav != null)
            {
                if (nav.isOnNavMesh)
                {
                    data.navStatus = nav.hasPath ? "path" : "on";
                    data.navRemainingDistance = nav.remainingDistance;
                    data.navVelocity = nav.velocity.magnitude;
                }
                else
                {
                    data.navStatus = "off";
                }
            }

            if (lod != null)
            {
                data.simulationLevel = lod.LevelName;
                data.distanceToPlayer = lod.DistanceToPlayer;
                data.simulationLodChangeCount = lod.LodChangeCount;
                data.activeSimulationTickCount = lod.ActiveSimulationTickCount;
                data.farSimulationTickCount = lod.FarSimulationTickCount;
                data.backgroundSimulationTickCount = lod.BackgroundSimulationTickCount;
                data.visibilityCulled = lod.IsVisibilityCulled;
                data.backgroundSimulated = lod.IsBackgroundSimulated;
            }
            else
            {
                data.distanceToPlayer = ResolveDistanceToPlayer(source.transform);
            }

            return data;
        }

        public string ToOverlayText()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine(speciesId ?? "unknown");
            builder.AppendLine($"beh: {state.ToString().ToLowerInvariant()} why:{Shorten(decisionReason, 18)}");
            builder.AppendLine($"hp: {FormatValue(health)}/{FormatValue(maxHealth)} hun:{hungerStage} {FormatValue(hunger)}/{FormatValue(maxHunger)}");
            builder.AppendLine($"en: {FormatValue(energy)}/{FormatValue(maxEnergy)} diet P:{plantDiet:0.00} M:{meatDiet:0.00} S:{scavengerDiet:0.00}");
            builder.AppendLine($"nav: {navStatus} rem:{navRemainingDistance:0.0} vel:{navVelocity:0.0} dP:{distanceToPlayer:0.0}");
            builder.AppendLine($"tgt: {Shorten(currentTarget, 24)} {Shorten(targetDetails, 24)}");
            builder.AppendLine($"bio: {Shorten(currentBiomeId, 10)} pop:{Shorten(populationBiomeId, 10)} niche:{Shorten(currentNiche, 12)}");
            builder.AppendLine($"last: {Shorten(lastFoodSource, 14)} dec:{decisionCount} atk:{attackCooldown:0.0} hunt:{huntDrive:0.00}");
            builder.Append($"sim: {simulationLevel} lod:{simulationLodChangeCount} ticks a:{activeSimulationTickCount} f:{farSimulationTickCount} b:{backgroundSimulationTickCount}");
            if (visibilityCulled || backgroundSimulated)
            {
                builder.Append($" bg:{backgroundSimulated} culled:{visibilityCulled}");
            }

            return builder.ToString();
        }

        private static string CaptureTargetDetails(Transform owner, Transform target)
        {
            if (owner == null || target == null)
            {
                return "none";
            }

            float distance = Vector3.Distance(owner.position, target.position);
            FoodSourceView food = target.GetComponent<FoodSourceView>();
            if (food != null)
            {
                return $"d:{distance:0.0} biomass:{food.Biomass:0.0} ratio:{food.BiomassRatio:0.00}";
            }

            CreatureHealthRuntime preyHealth = target.GetComponent<CreatureHealthRuntime>();
            if (preyHealth != null)
            {
                return $"d:{distance:0.0} hp:{preyHealth.CurrentHealth:0}/{preyHealth.MaxHealth:0}";
            }

            return $"d:{distance:0.0}";
        }

        private static float ResolveDistanceToPlayer(Transform owner)
        {
            if (owner == null)
            {
                return -1f;
            }

            GameObject player = null;
            try
            {
                player = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
            }

            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            return player != null ? Vector3.Distance(owner.position, player.transform.position) : -1f;
        }

        private static string NormalizeDebugValue(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string Shorten(string value, int maxLength)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();
            return safe.Length <= maxLength ? safe : safe.Substring(0, Mathf.Max(1, maxLength));
        }

        private static string FormatValue(float value)
        {
            return value < 0f ? "n/a" : value.ToString("0");
        }
    }
}
