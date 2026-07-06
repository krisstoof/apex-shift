#if UNITY_EDITOR
using System.IO;
using System.Text;
using ApexShift.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Animation
{
    public static class KevinIglesiasAssetAuditMenu
    {
        private const string ReportPath = "Docs/animation/kevin-iglesias-asset-audit.md";

        [MenuItem("Apex Shift/Animation/Audit Kevin Iglesias Assets")]
        public static void AuditKevinIglesiasAssets()
        {
            KevinIglesiasPlayerAnimationBinder.ClipResolution resolution = KevinIglesiasPlayerAnimationBinder.ResolveKevinClips();

            string directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Kevin Iglesias animation asset audit");
            sb.AppendLine();
            sb.AppendLine("Generated from local Unity `AssetDatabase`.");
            sb.AppendLine();
            sb.AppendLine("## Selected mapping");
            sb.AppendLine();
            AppendClip(sb, "Idle", resolution.Clips.Idle);
            AppendClip(sb, "Walking", resolution.Clips.Walk);
            AppendClip(sb, "Running", resolution.Clips.Run);
            AppendClip(sb, "Swimming", resolution.Clips.Swim);
            AppendClip(sb, "SpearAttack", resolution.Clips.SpearAttack);
            AppendClip(sb, "BowAttack", resolution.Clips.BowAttack);
            AppendClip(sb, "AxeUse", resolution.Clips.AxeUse);
            AppendClip(sb, "PickaxeUse", resolution.Clips.PickaxeUse);
            AppendClip(sb, "TorchUse", resolution.Clips.TorchUse);
            AppendClip(sb, "Gather", resolution.Clips.Gather);
            AppendClip(sb, "Attack fallback", resolution.Clips.Attack);
            AppendClip(sb, "Hurt", resolution.Clips.Hurt);
            AppendClip(sb, "Death", resolution.Clips.Death);

            sb.AppendLine();
            sb.AppendLine("## All candidate clips");
            sb.AppendLine();
            sb.AppendLine("| Score | Clip | Path |");
            sb.AppendLine("|---:|---|---|");

            if (resolution.Candidates != null)
            {
                for (int i = 0; i < resolution.Candidates.Count; i++)
                {
                    KevinIglesiasPlayerAnimationBinder.Candidate candidate = resolution.Candidates[i];
                    sb.Append("| ");
                    sb.Append(candidate.BaseScore);
                    sb.Append(" | ");
                    sb.Append(Escape(candidate.Clip != null ? candidate.Clip.name : "missing"));
                    sb.Append(" | ");
                    sb.Append(Escape(candidate.Path));
                    sb.AppendLine(" |");
                }
            }

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            KevinIglesiasPlayerAnimationBinder.TryBuildKevinController();

            Debug.Log($"[KevinIglesiasAssetAudit] Wrote report: {ReportPath}. Candidate clips: {resolution.CandidateCount}");
        }

        private static void AppendClip(StringBuilder sb, string role, AnimationClip clip)
        {
            sb.Append("- **");
            sb.Append(role);
            sb.Append("**: ");
            sb.AppendLine(clip != null ? $"`{clip.name}`" : "`missing`");
        }

        private static string Escape(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\n", " ");
        }
    }
}
#endif
