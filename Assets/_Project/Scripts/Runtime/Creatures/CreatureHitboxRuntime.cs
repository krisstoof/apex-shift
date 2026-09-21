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

        public Collider CombatCollider => hitbox;

        public bool IsValidForMask(LayerMask mask, out string reason)
        {
            if (hitbox == null)
            {
                reason = "CreatureHitboxRuntime has no combat collider.";
                return false;
            }

            if (hitbox.gameObject == gameObject || !hitbox.transform.IsChildOf(transform) || hitbox.gameObject.name != "ApexShiftCombatHitbox")
            {
                reason = "Combat collider is not the dedicated ApexShiftCombatHitbox child.";
                return false;
            }

            if (!hitbox.enabled)
            {
                reason = "Combat collider is disabled.";
                return false;
            }

            if (!hitbox.isTrigger || hitbox.direction != 1)
            {
                reason = "Combat collider is not configured as a Y-axis trigger.";
                return false;
            }

            if ((mask.value & (1 << hitbox.gameObject.layer)) == 0)
            {
                reason = "Combat collider layer is not included in the melee mask.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

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
            if (hitbox == null || hitbox.gameObject == gameObject || !hitbox.transform.IsChildOf(transform) || hitbox.gameObject.name != "ApexShiftCombatHitbox")
            {
                Transform child = transform.Find("ApexShiftCombatHitbox");
                if (child == null)
                {
                    GameObject childObject = new GameObject("ApexShiftCombatHitbox");
                    childObject.layer = gameObject.layer;
                    child = childObject.transform;
                    child.SetParent(transform, false);
                }

                hitbox = child.GetComponent<CapsuleCollider>();
                if (hitbox == null)
                {
                    hitbox = child.gameObject.AddComponent<CapsuleCollider>();
                }
            }

            hitbox.gameObject.layer = gameObject.layer;
            hitbox.enabled = true;
            hitbox.isTrigger = true;
            hitbox.direction = 1;
            hitbox.radius = Mathf.Max(0.10f, radius);
            hitbox.height = Mathf.Max(hitbox.radius * 2f, height);
            hitbox.center = center;
        }
    }
}
