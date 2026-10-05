#if UNITY_EDITOR
using System.IO;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class PlayerAnimationBinderPlayModeTests
    {
        [Test]
        public void RepeatedBindingUsesExistingControllerWithoutChangingProductionAsset()
        {
            const string path = "Assets/_Project/Generated/Animation/GeneratedKevinIglesiasPlayerItemUse.controller";
            byte[] before = File.ReadAllBytes(path);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.NotNull(controller);
            Assert.AreEqual(14, controller.parameters.Length);
            bool wasDirty = EditorUtility.IsDirty(controller);
            GameObject player = new GameObject("AnimationBindingRegression");
            try
            {
                Animator animator = player.AddComponent<Animator>();
                KevinIglesiasPlayerAnimationBinder binder = player.AddComponent<KevinIglesiasPlayerAnimationBinder>();
                binder.Configure(null, animator);
                binder.Bind();
                binder.Bind();
                Assert.AreSame(controller, animator.runtimeAnimatorController);
                Assert.AreEqual(14, controller.parameters.Length);
                Assert.AreEqual(wasDirty, EditorUtility.IsDirty(controller));
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            }
            finally { Object.DestroyImmediate(player); }
        }
    }
}
#endif
