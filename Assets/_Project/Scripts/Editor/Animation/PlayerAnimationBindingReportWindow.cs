#if UNITY_EDITOR
using System.Text;
using ApexShift.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Animation
{
    public sealed class PlayerAnimationBindingReportWindow : EditorWindow
    {
        private Vector2 scroll;
        private string report = "Click Refresh to scan imported player animation clips.";

        [MenuItem("Apex Shift/Animation/Player Binding Report")]
        public static void Open()
        {
            GetWindow<PlayerAnimationBindingReportWindow>("Player Animations");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(120f)))
                {
                    report = BuildReport();
                }

                if (GUILayout.Button("Generate/Refresh Controller", GUILayout.Width(210f)))
                {
                    RuntimeAnimatorController controller = KevinIglesiasPlayerAnimationBinder.TryBuildKevinController();
                    report = controller != null
                        ? BuildReport() + "\n\nGenerated controller: " + AssetDatabase.GetAssetPath(controller)
                        : BuildReport() + "\n\nController generation failed. Missing Idle/Walk/Run clips.";
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static string BuildReport()
        {
            KevinIglesiasPlayerAnimationBinder.ClipResolution resolution = KevinIglesiasPlayerAnimationBinder.ResolveKevinClips();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Apex Shift Player Animation Binding Report");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("Resolved clips:");
            AppendClip(sb, "Idle", resolution.Clips.Idle);
            AppendClip(sb, "Walk", resolution.Clips.Walk);
            AppendClip(sb, "Run/Sprint", resolution.Clips.Run);
            AppendClip(sb, "Swim", resolution.Clips.Swim);
            AppendClip(sb, "Attack", resolution.Clips.Attack);
            AppendClip(sb, "SpearAttack", resolution.Clips.SpearAttack);
            AppendClip(sb, "BowAttack", resolution.Clips.BowAttack);
            AppendClip(sb, "AxeUse/Chop", resolution.Clips.AxeUse);
            AppendClip(sb, "PickaxeUse/Mine", resolution.Clips.PickaxeUse);
            AppendClip(sb, "TorchUse", resolution.Clips.TorchUse);
            AppendClip(sb, "Gather/Interact", resolution.Clips.Gather);
            AppendClip(sb, "Hurt", resolution.Clips.Hurt);
            AppendClip(sb, "Death", resolution.Clips.Death);
            sb.AppendLine();
            sb.AppendLine("Candidate clips found: " + resolution.Candidates.Count);
            sb.AppendLine();
            sb.AppendLine("Acceptance hints:");
            sb.AppendLine("- Idle, Walk and Run are required for generated controller creation.");
            sb.AppendLine("- Missing action clips safely fall back to Attack/Gather triggers.");
            sb.AppendLine("- Missing AnimatorController should not break player movement.");
            return sb.ToString();
        }

        private static void AppendClip(StringBuilder sb, string role, AnimationClip clip)
        {
            if (clip == null)
            {
                sb.AppendLine("- " + role + ": missing");
                return;
            }

            sb.AppendLine("- " + role + ": " + clip.name + " (" + AssetDatabase.GetAssetPath(clip) + ")");
        }
    }
}
#endif
