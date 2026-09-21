using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class MeleeTargetSelectorTests
    {
        private MeleeTargetSelector selector;

        [SetUp]
        public void SetUp() => selector = new MeleeTargetSelector(16);

        [Test]
        public void SelectsNearestValidTargetInFront()
        {
            GameObject far = CreateCreature("far", new Vector3(0f, 0f, 1.8f));
            GameObject near = CreateCreature("near", new Vector3(0f, 0f, 1.1f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsTrue(TrySelect(Vector3.forward, 2f, 145f, out MeleeTargetSelector.Result result));
                Assert.AreSame(near.GetComponent<CreatureHealthRuntime>(), result.Health);
            }
            finally { Destroy(far, near); }
        }

        [Test]
        public void RejectsTargetOutsideRange()
        {
            GameObject target = CreateCreature("outside", new Vector3(0f, 0f, 3f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsFalse(TrySelect(Vector3.forward, 1f, 145f, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void RejectsTargetOutsideAttackArcEvenWhenClose()
        {
            GameObject target = CreateCreature("behind", new Vector3(0f, 0f, -0.65f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsFalse(TrySelect(Vector3.forward, 1.5f, 90f, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void RejectsDeadTarget()
        {
            GameObject target = CreateCreature("dead", new Vector3(0f, 0f, 1f));
            try
            {
                target.GetComponent<CreatureHealthRuntime>().TakeDamage(99999f);
                Physics.SyncTransforms();
                Assert.IsFalse(TrySelect(Vector3.forward, 2f, 145f, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void RejectsMissingAuthoritativeHitbox()
        {
            GameObject target = new GameObject("NoHitbox");
            target.transform.position = new Vector3(0f, 0f, 1f);
            target.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
            target.AddComponent<SphereCollider>();
            try
            {
                Physics.SyncTransforms();
                Assert.IsFalse(TrySelect(Vector3.forward, 2f, 145f, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void RejectsTargetOnExcludedLayer()
        {
            GameObject target = CreateCreature("wrong-layer", new Vector3(0f, 0f, 1f));
            try
            {
                target.layer = 2;
                foreach (Transform child in target.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
                Physics.SyncTransforms();
                Assert.IsFalse(TrySelect(Vector3.forward, 2f, 145f, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void RejectsSelfTarget()
        {
            GameObject player = CreateCreature("player", new Vector3(0f, 0f, 1f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsFalse(selector.TrySelectTarget(Vector3.zero, Vector3.forward, 2f, 145f, Physics.DefaultRaycastLayers, player.transform, out _));
            }
            finally { Destroy(player); }
        }

        [Test]
        public void RequiresValidCreatureLayerMask()
        {
            GameObject target = CreateCreature("mask", new Vector3(0f, 0f, 1f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsFalse(selector.TrySelectTarget(Vector3.zero, Vector3.forward, 2f, 145f, 0, null, out _));
            }
            finally { Destroy(target); }
        }

        [Test]
        public void TieBreaksByAngleThenInstanceIdentity()
        {
            GameObject centered = CreateCreature("centered", new Vector3(0f, 0f, 1.1f));
            GameObject angled = CreateCreature("angled", new Vector3(0.25f, 0f, 1.1f));
            try
            {
                Physics.SyncTransforms();
                Assert.IsTrue(TrySelect(Vector3.forward, 2f, 145f, out MeleeTargetSelector.Result result));
                Assert.AreSame(centered.GetComponent<CreatureHealthRuntime>(), result.Health);
            }
            finally { Destroy(centered, angled); }
        }

        private bool TrySelect(Vector3 direction, float range, float arc, out MeleeTargetSelector.Result result)
        {
            return selector.TrySelectTarget(Vector3.up * 0.9f, direction, range, arc, Physics.DefaultRaycastLayers, null, out result);
        }

        private static GameObject CreateCreature(string id, Vector3 position)
        {
            GameObject target = new GameObject(id);
            target.transform.position = position;
            CreatureHealthRuntime health = target.AddComponent<CreatureHealthRuntime>();
            health.Configure("small_prey");
            CreatureHitboxRuntime hitbox = target.AddComponent<CreatureHitboxRuntime>();
            hitbox.Configure("small_prey");
            return target;
        }

        private static void Destroy(params GameObject[] objects)
        {
            foreach (GameObject value in objects)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
        }
    }
}
