using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CreatureHealthRuntime))]
    public sealed class CreatureHitboxRuntime : MonoBehaviour
    {
        [SerializeField] private CapsuleCollider hitbox;
        [SerializeField] private float radius = 0.55f;
        [SerializeField] private float height = 1.25f;
        [SerializeField] private Vector3 center = new Vector3(0f, 0.75f, 0f);

        private void Awake() => EnsureHitbox();

        public void Configure(string creatureId)
        {
            string id = string.IsNullOrWhiteSpace(creatureId) ? string.Empty : creatureId.Trim().ToLowerInvariant();
            switch (id)
            {
                case "small_prey":
                    radius = 0.35f;
                    height = 0.70f;
                    center = new Vector3(0f, 0.38f, 0f);
                    break;
                case "grazer":
                    radius = 0.65f;
                    height = 1.35f;
                    center = new Vector3(0f, 0.72f, 0f);
                    break;
                case "varnak":
                    radius = 0.70f;
                    height = 1.65f;
                    center = new Vector3(0f, 0.90f, 0f);
                    break;
                default:
                    radius = 0.55f;
                    height = 1.25f;
                    center = new Vector3(0f, 0.75f, 0f);
                    break;
            }

            EnsureHitbox();
        }

        private void EnsureHitbox()
        {
            if (hitbox == null)
            {
                hitbox = GetComponent<CapsuleCollider>();
                if (hitbox == null)
                {
                    hitbox = gameObject.AddComponent<CapsuleCollider>();
                }
            }

            hitbox.isTrigger = true;
            hitbox.direction = 1;
            hitbox.radius = Mathf.Max(0.10f, radius);
            hitbox.height = Mathf.Max(hitbox.radius * 2f, height);
            hitbox.center = center;
        }
    }
}
