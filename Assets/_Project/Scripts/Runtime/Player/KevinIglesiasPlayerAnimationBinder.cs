using System;
using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class KevinIglesiasPlayerAnimationBinder : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController fallbackController;
        [SerializeField] private bool generateControllerInEditor = true;
        [SerializeField] private bool addAnimatorIfMissing = true;
        [SerializeField] private bool logBinding = true;

        private const string GeneratedControllerPath = "Assets/_Project/Generated/Animation/GeneratedKevinIglesiasPlayerItemUse.controller";

        public Animator BoundAnimator => animator;

        public void Configure(PlayerInputReader reader, Animator targetAnimator, RuntimeAnimatorController fallback = null)
        {
            inputReader = reader;
            if (targetAnimator != null) animator = targetAnimator;
            if (fallback != null) fallbackController = fallback;
            Bind();
        }

        private void Awake() => Bind();

        public void Bind()
        {
            ResolveAnimator();
            if (animator == null) return;

#if UNITY_EDITOR
            if (generateControllerInEditor)
            {
                RuntimeAnimatorController generated = TryBuildKevinController();
                if (generated != null)
                {
                    animator.runtimeAnimatorController = generated;
                    if (logBinding) Debug.Log($"[KevinIglesiasAnimationBinder] Bound generated controller: {generated.name}", this);
                    return;
                }
            }
#endif

            if (fallbackController != null && animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = fallbackController;
            }

            if (logBinding)
            {
                Debug.Log($"[KevinIglesiasAnimationBinder] Animator={animator.name}, controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "missing")}", this);
            }
        }

        private void ResolveAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null && addAnimatorIfMissing) animator = gameObject.AddComponent<Animator>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        }

#if UNITY_EDITOR
        private static RuntimeAnimatorController TryBuildKevinController()
        {
            ClipSet clips = ResolveKevinClips();
            if (clips.Idle == null || clips.Walk == null || clips.Run == null)
            {
                Debug.LogWarning("[KevinIglesiasAnimationBinder] Could not generate controller: missing Idle/Walk/Run clips.");
                return null;
            }

            EnsureGeneratedDirectory();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GeneratedControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(GeneratedControllerPath);
            RebuildController(controller, clips);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return controller;
        }

        private static ClipSet ResolveKevinClips()
        {
            List<Candidate> candidates = FindAnimationCandidates();
            return new ClipSet
            {
                Idle = FindBest(candidates, "Idle", "idle", "stand", "breathing"),
                Walk = FindBest(candidates, "Walk", "walk", "walking"),
                Run = FindBest(candidates, "Run", "run", "running", "sprint", "jog"),
                Swim = FindBest(candidates, "Swim", "swim", "swimming"),
                SpearAttack = FindBest(candidates, "SpearAttack", "spear", "stab", "thrust", "pierce", "2hand", "twohand"),
                BowAttack = FindBest(candidates, "BowAttack", "bow", "archery", "shoot", "arrow"),
                AxeUse = FindBest(candidates, "AxeUse", "axe", "chop", "slash", "1hand", "onehand"),
                PickaxeUse = FindBest(candidates, "PickaxeUse", "pickaxe", "pick", "mine", "mining"),
                TorchUse = FindBest(candidates, "TorchUse", "torch", "use", "raise"),
                Gather = FindBest(candidates, "Gather", "gather", "pickup", "pick_up", "interact", "use"),
                Attack = FindBest(candidates, "Attack", "attack", "melee", "slash", "hit"),
                Hurt = FindBest(candidates, "Hurt", "hurt", "hit", "damage"),
                Death = FindBest(candidates, "Death", "death", "die", "dead")
            };
        }

        private static List<Candidate> FindAnimationCandidates()
        {
            List<Candidate> result = new List<Candidate>();
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path)) continue;
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < assets.Length; a++)
                {
                    if (assets[a] is not AnimationClip clip || clip == null) continue;
                    if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) continue;
                    int score = ScoreKevinClip(path, clip.name);
                    if (score <= 0) continue;
                    result.Add(new Candidate(clip, path, score));
                }
            }
            return result;
        }

        private static AnimationClip FindBest(List<Candidate> candidates, string role, params string[] aliases)
        {
            AnimationClip best = null;
            int bestScore = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                string haystack = Normalize(candidate.Path + " " + candidate.Clip.name);
                int score = candidate.BaseScore + ScoreAliases(haystack, aliases);
                if (haystack.Contains(Normalize(role))) score += 80;
                if (score > bestScore) { bestScore = score; best = candidate.Clip; }
            }
            return bestScore > 0 ? best : null;
        }

        private static int ScoreKevinClip(string path, string clipName)
        {
            string normalized = Normalize(path + " " + clipName);
            int score = 0;
            if (normalized.Contains("kevin")) score += 60;
            if (normalized.Contains("iglesias")) score += 60;
            if (normalized.Contains("player")) score += 35;
            if (normalized.Contains("character")) score += 25;
            if (normalized.Contains("humanoid")) score += 20;
            if (normalized.Contains("animation")) score += 15;
            if (normalized.Contains("anim")) score += 10;
            if (normalized.Contains("creature")) score -= 50;
            if (normalized.Contains("varnak")) score -= 50;
            if (normalized.Contains("grazer")) score -= 50;
            if (normalized.Contains("prey")) score -= 50;
            if (score <= 0 && ContainsAny(normalized, "idle", "walk", "run", "attack", "spear", "axe", "pickaxe", "bow", "gather", "mine", "chop")) score = 5;
            return score;
        }

        private static int ScoreAliases(string haystack, params string[] aliases)
        {
            int score = 0;
            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = Normalize(aliases[i]);
                if (string.IsNullOrWhiteSpace(alias)) continue;
                if (haystack == alias) score += 150;
                else if (haystack.EndsWith(alias, StringComparison.OrdinalIgnoreCase)) score += 100;
                else if (haystack.Contains(alias)) score += 70;
            }
            return score;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (haystack.Contains(Normalize(needles[i]))) return true;
            }
            return false;
        }

        private static void RebuildController(AnimatorController controller, ClipSet clips)
        {
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "IsSprinting", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "IsSwimming", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Interact", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Gather", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "SpearAttack", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "BowAttack", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "AxeUse", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "PickaxeUse", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "TorchUse", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Hurt", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            ClearStateMachine(machine);

            AnimatorState idle = AddState(machine, "Idle", clips.Idle, new Vector3(240f, 40f, 0f), true);
            AnimatorState walk = AddState(machine, "Walking", clips.Walk, new Vector3(240f, 130f, 0f), true);
            AnimatorState run = AddState(machine, "Running", clips.Run, new Vector3(240f, 220f, 0f), true);
            machine.defaultState = idle;

            AddBoolTransition(idle, walk, "IsMoving", true, "IsSprinting", false, "IsSwimming", false);
            AddBoolTransition(idle, run, "IsMoving", true, "IsSprinting", true, "IsSwimming", false);
            AddBoolTransition(walk, idle, "IsMoving", false);
            AddBoolTransition(walk, run, "IsSprinting", true);
            AddBoolTransition(run, walk, "IsSprinting", false);
            AddBoolTransition(run, idle, "IsMoving", false);

            if (clips.Swim != null)
            {
                AnimatorState swim = AddState(machine, "Swimming", clips.Swim, new Vector3(240f, 310f, 0f), true);
                AddBoolTransition(idle, swim, "IsSwimming", true);
                AddBoolTransition(walk, swim, "IsSwimming", true);
                AddBoolTransition(run, swim, "IsSwimming", true);
                AddBoolTransition(swim, idle, "IsSwimming", false);
            }

            AddAction(machine, idle, "Attack", clips.Attack, new Vector3(560f, 40f, 0f));
            AddAction(machine, idle, "Interact", clips.Gather, new Vector3(560f, 130f, 0f));
            AddAction(machine, idle, "Gather", clips.Gather, new Vector3(560f, 220f, 0f));
            AddAction(machine, idle, "SpearAttack", clips.SpearAttack ?? clips.Attack, new Vector3(860f, 40f, 0f));
            AddAction(machine, idle, "BowAttack", clips.BowAttack ?? clips.Attack, new Vector3(860f, 130f, 0f));
            AddAction(machine, idle, "AxeUse", clips.AxeUse ?? clips.Attack, new Vector3(860f, 220f, 0f));
            AddAction(machine, idle, "PickaxeUse", clips.PickaxeUse ?? clips.AxeUse ?? clips.Attack, new Vector3(860f, 310f, 0f));
            AddAction(machine, idle, "TorchUse", clips.TorchUse ?? clips.Gather, new Vector3(1160f, 40f, 0f));
            AddAction(machine, idle, "Hurt", clips.Hurt, new Vector3(1160f, 130f, 0f));
            AddAction(machine, idle, "Death", clips.Death, new Vector3(1160f, 220f, 0f));
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position, bool loop)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = motion;
            state.speed = 1f;
            if (motion is AnimationClip clip)
            {
                SerializedObject serializedClip = new SerializedObject(clip);
                SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
                if (loopTime != null)
                {
                    loopTime.boolValue = loop;
                    serializedClip.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            return state;
        }

        private static void AddAction(AnimatorStateMachine machine, AnimatorState fallback, string trigger, AnimationClip clip, Vector3 position)
        {
            if (clip == null) return;
            AnimatorState state = AddState(machine, trigger, clip, position, false);
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.06f;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            AnimatorStateTransition exit = state.AddTransition(fallback);
            exit.hasExitTime = true;
            exit.exitTime = 0.88f;
            exit.duration = 0.08f;
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string a, bool av)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(av ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, a);
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string a, bool av, string b, bool bv)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(av ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, a);
            transition.AddCondition(bv ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, b);
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string a, bool av, string b, bool bv, string c, bool cv)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(av ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, a);
            transition.AddCondition(bv ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, b);
            transition.AddCondition(cv ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, c);
        }

        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type) => controller.AddParameter(name, type);

        private static void ClearStateMachine(AnimatorStateMachine machine)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = states.Length - 1; i >= 0; i--) machine.RemoveState(states[i].state);
            ChildAnimatorStateMachine[] stateMachines = machine.stateMachines;
            for (int i = stateMachines.Length - 1; i >= 0; i--) machine.RemoveStateMachine(stateMachines[i].stateMachine);
            AnimatorStateTransition[] anyStateTransitions = machine.anyStateTransitions;
            for (int i = anyStateTransitions.Length - 1; i >= 0; i--) machine.RemoveAnyStateTransition(anyStateTransitions[i]);
        }

        private static void EnsureGeneratedDirectory()
        {
            string directory = Path.GetDirectoryName(GeneratedControllerPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        private readonly struct Candidate
        {
            public readonly AnimationClip Clip;
            public readonly string Path;
            public readonly int BaseScore;
            public Candidate(AnimationClip clip, string path, int baseScore) { Clip = clip; Path = path; BaseScore = baseScore; }
        }

        private struct ClipSet
        {
            public AnimationClip Idle;
            public AnimationClip Walk;
            public AnimationClip Run;
            public AnimationClip Swim;
            public AnimationClip Attack;
            public AnimationClip SpearAttack;
            public AnimationClip BowAttack;
            public AnimationClip AxeUse;
            public AnimationClip PickaxeUse;
            public AnimationClip TorchUse;
            public AnimationClip Gather;
            public AnimationClip Hurt;
            public AnimationClip Death;
        }
#endif

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        }
    }
}
