using System;
using System.Linq;
using System.Reflection;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Editor
{
    public sealed class PlayerAnimationBinderEditorTests
    {
        [Test]
        public void RebuildingExistingTransitionsKeepsParametersValidAndPersistsThem()
        {
            string controllerPath = "Assets/AnimationBinderRegression_" + Guid.NewGuid().ToString("N") + ".controller";
            string clipPath = controllerPath.Replace(".controller", ".anim");
            try
            {
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                AnimationClip clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
                var clips = new KevinIglesiasPlayerAnimationBinder.ClipSet
                {
                    Idle = clip, Walk = clip, Run = clip, Swim = clip,
                    Attack = clip, Gather = clip, Hurt = clip, Death = clip
                };
                MethodInfo rebuild = typeof(KevinIglesiasPlayerAnimationBinder).GetMethod("RebuildController",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(rebuild);
                rebuild.Invoke(null, new object[] { controller, clips });
                rebuild.Invoke(null, new object[] { controller, clips });
                Assert.AreEqual(14, controller.parameters.Length);
                var parameters = controller.parameters.ToDictionary(parameter => parameter.name, parameter => parameter.type);
                Assert.AreEqual(AnimatorControllerParameterType.Float, parameters["Speed"]);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["IsMoving"]);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["IsSprinting"]);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["IsSwimming"]);
                Assert.AreEqual(AnimatorControllerParameterType.Trigger, parameters["Attack"]);
                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                foreach (AnimatorStateTransition transition in machine.states.SelectMany(state => state.state.transitions)
                    .Concat(machine.anyStateTransitions))
                    foreach (AnimatorCondition condition in transition.conditions)
                        Assert.IsTrue(parameters.ContainsKey(condition.parameter), condition.parameter);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
                AssetDatabase.ImportAsset(controllerPath, ImportAssetOptions.ForceUpdate);
                Assert.AreEqual(14, AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath).parameters.Length);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                AssetDatabase.DeleteAsset(controllerPath);
                AssetDatabase.DeleteAsset(clipPath);
            }
        }
    }
}
