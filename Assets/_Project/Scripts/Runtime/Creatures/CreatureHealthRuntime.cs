using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    public sealed class CreatureHealthRuntime : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float currentHealth = 30f;
        [SerializeField] private bool destroyOnDeath = true;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        public void Configure(string creatureId)
        {
            switch ((creatureId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "small_prey":
                    maxHealth = 20f;
                    break;
                case "grazer":
                    maxHealth = 45f;
                    break;
                case "varnak":
                    maxHealth = 90f;
                    break;
                default:
                    maxHealth = 30f;
                    break;
            }

            currentHealth = maxHealth;
        }

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
