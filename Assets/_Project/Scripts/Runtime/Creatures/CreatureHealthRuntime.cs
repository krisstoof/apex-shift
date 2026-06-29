using UnityEngine;
using ApexShift.Runtime.Config;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    public sealed class CreatureHealthRuntime : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float currentHealth = 30f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private SpeciesDefinition speciesDefinition;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        public void Configure(string creatureId)
        {
            Configure(creatureId, null);
        }

        public void Configure(string creatureId, SpeciesDefinition overrideDefinition)
        {
            SpeciesDefinition resolved = GameBalanceConfigProvider.ResolveSpeciesDefinition(gameBalanceConfig, overrideDefinition != null ? overrideDefinition : speciesDefinition, creatureId, this);
            maxHealth = resolved != null ? resolved.MaxHealth : 30f;

            currentHealth = maxHealth;
        }

        public void SetSpeciesDefinitionForTests(SpeciesDefinition definition) => speciesDefinition = definition;

        public void SetGameBalanceConfigForTests(GameBalanceConfig config) => gameBalanceConfig = config;

        public void RestoreHealth(float restoredMaxHealth, float restoredCurrentHealth, bool dead)
        {
            maxHealth = Mathf.Max(0.01f, restoredMaxHealth);
            currentHealth = dead ? 0f : Mathf.Clamp(restoredCurrentHealth, 0f, maxHealth);
            enabled = !dead;

            if (dead)
            {
                CreatureBehaviorRuntime behavior = GetComponent<CreatureBehaviorRuntime>();
                if (behavior != null)
                {
                    behavior.OnCreatureDied();
                }
                else
                {
                    GetComponent<CreatureBehaviorBrain>()?.OnCreatureDied();
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
            if (currentHealth <= 0f)
            {
                var behavior = GetComponent<CreatureBehaviorRuntime>();
                if (behavior != null)
                {
                    behavior.OnCreatureDied();
                }

                CreatureMeatDropFactory.TrySpawnMeatDrop(transform.position, GetComponent<CreatureAgentView>());

                if (destroyOnDeath)
                {
                    Destroy(gameObject, 0.1f);
                }
                else
                {
                    enabled = false;
                }
            }
        }

        public void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        }
    }
}
