using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ApexShift.EditorTools.Player
{
    public static class PlayerAnimationControllerBuilder
    {
        private const string ControllerPath = "Assets/_Project/Animations/Player/PlayerPrototype.controller";
        private const string PlayerFolder = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/AnimationsClips/Player";
        private const string IdleStateName = "Idle";
        private const string WalkingStateName = "Walking";
        private const string RunningStateName = "Running";
        private const string AttackStateName = "Attack";
        private const string InteractStateName = "Interact";
        private const string SpeedParameter = "Speed";
        private const string IsMovingParameter = "IsMoving";
        private const string IsSprintingParameter = "IsSprinting";
        private const string AttackTrigger = "Attack";
        private const string InteractTrigger = "Interact";

        [MenuItem("Tools/Apex Shift/Player/Create Prototype Animation Controller")]
        public static void CreatePrototypeAnimationController()
        {
            EnsureFolders();

            AnimationClip idle = FindClip(new[] { "idle", "idle_01" });
            AnimationClip walking = FindClip(new[] { "walk", "walking" });
            AnimationClip running = FindClip(new[] { "run", "running", "sprint" });
            AnimationClip attack = FindClip(new[] { "attack", "slash", "hit" });
            AnimationClip interact = FindClip(new[] { "interact", "gather", "pickup" });

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            EnsureParameters(controller);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            ClearStates(root);

            AnimationClip fallbackLoop = idle ?? walking ?? running;
            if (fallbackLoop == null)
            {
                Debug.LogWarning("Could not find any loop clip for the player controller. Creating state machine without motion clips.");
            }

            AnimatorState idleState = AddState(root, IdleStateName, idle ?? fallbackLoop);
            AnimatorState walkingState = AddState(root, WalkingStateName, walking ?? fallbackLoop);
            AnimatorState runningState = AddState(root, RunningStateName, running ?? walking ?? fallbackLoop);
            AnimatorState attackState = AddOptionalState(root, AttackStateName, attack);
            AnimatorState interactState = AddOptionalState(root, InteractStateName, interact);

            if (idleState != null)
            {
                root.defaultState = idleState;
            }

            WireMovementTransitions(idleState, walkingState, runningState);
            WireActionTransitions(root, attackState, interactState);

            LogClipSelection(idle, walking, running, attack, interact);
            LogControllerSummary(idleState, walkingState, runningState, attackState, interactState);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prototype animation controller created at " + ControllerPath);
        }

        private static AnimationClip FindClip(IEnumerable<string> keywords)
        {
            foreach (string keyword in keywords)
            {
                string[] guids = AssetDatabase.FindAssets(keyword + " t:AnimationClip", new[] { PlayerFolder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        private static void LogClipSelection(AnimationClip idle, AnimationClip walking, AnimationClip running, AnimationClip attack, AnimationClip interact)
        {
            LogClip("Idle", idle);
            LogClip("Walking", walking);
            LogClip("Running", running);
            LogClip("Attack", attack);
            LogClip("Interact", interact);
        }

        private static void LogClip(string label, AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("Prototype animation controller could not find " + label + " clip.");
                return;
            }

            Debug.Log("Prototype animation controller clip: " + label + " -> " + clip.name);
        }

        private static void LogControllerSummary(
            AnimatorState idleState,
            AnimatorState walkingState,
            AnimatorState runningState,
            AnimatorState attackState,
            AnimatorState interactState)
        {
            Debug.Log(
                "Prototype animation controller states: " +
                "Idle=" + StateLabel(idleState) + ", " +
                "Walking=" + StateLabel(walkingState) + ", " +
                "Running=" + StateLabel(runningState) + ", " +
                "Attack=" + StateLabel(attackState) + ", " +
                "Interact=" + StateLabel(interactState));
        }

        private static string StateLabel(AnimatorState state)
        {
            return state != null ? state.name : "missing";
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Animations");
            EnsureFolder("Assets/_Project/Animations/Player");
            EnsureFolder("Assets/_Project/Scripts");
            EnsureFolder("Assets/_Project/Scripts/Editor");
            EnsureFolder("Assets/_Project/Scripts/Editor/Player");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static AnimatorState AddState(AnimatorStateMachine root, string name, Motion motion)
        {
            AnimatorState state = root.AddState(name);
            state.motion = motion;
            return state;
        }

        private static AnimatorState AddOptionalState(AnimatorStateMachine root, string name, AnimationClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            return AddState(root, name, clip);
        }

        private static void ClearStates(AnimatorStateMachine root)
        {
            List<ChildAnimatorState> states = new List<ChildAnimatorState>(root.states);
            foreach (ChildAnimatorState state in states)
            {
                root.RemoveState(state.state);
            }
        }

        private static void EnsureParameters(AnimatorController controller)
        {
            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = SpeedParameter, type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = IsMovingParameter, type = AnimatorControllerParameterType.Bool },
                new AnimatorControllerParameter { name = IsSprintingParameter, type = AnimatorControllerParameterType.Bool },
                new AnimatorControllerParameter { name = AttackTrigger, type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = InteractTrigger, type = AnimatorControllerParameterType.Trigger }
            };
        }

        private static void WireMovementTransitions(AnimatorState idleState, AnimatorState walkingState, AnimatorState runningState)
        {
            if (idleState == null || walkingState == null || runningState == null)
            {
                return;
            }

            AddTransition(idleState, walkingState, IsMovingParameter, true);
            AddTransition(walkingState, idleState, IsMovingParameter, false);

            AddTransition(walkingState, runningState, IsSprintingParameter, true);
            AddTransition(runningState, walkingState, IsSprintingParameter, false);
        }

        private static void WireActionTransitions(AnimatorStateMachine root, AnimatorState attackState, AnimatorState interactState)
        {
            if (attackState != null)
            {
                AnimatorStateTransition transition = root.AddAnyStateTransition(attackState);
                ConfigureTriggerTransition(transition, AttackTrigger);
            }

            if (interactState != null)
            {
                AnimatorStateTransition transition = root.AddAnyStateTransition(interactState);
                ConfigureTriggerTransition(transition, InteractTrigger);
            }
        }

        private static void ConfigureTriggerTransition(AnimatorStateTransition transition, string triggerName)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
            transition.conditions = new[]
            {
                new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = triggerName }
            };
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
            transition.conditions = new[]
            {
                new AnimatorCondition
                {
                    mode = value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                    parameter = parameter
                }
            };
        }
    }
}
