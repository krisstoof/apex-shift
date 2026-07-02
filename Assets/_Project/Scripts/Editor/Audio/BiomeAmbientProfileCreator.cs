using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Audio
{
    /// <summary>
    /// Creates or updates <see cref="BiomeAmbientProfile"/> ScriptableObjects for every biome
    /// defined in <see cref="BiomeCatalogAsset"/> and links them to their
    /// <see cref="BiomeDefinitionAsset"/> via the AmbientProfile field.
    ///
    /// Audio clips are auto-discovered from the project using keyword matching:
    ///   "forest" / "wind" / "bird" / "nature" → day/forest biomes
    ///   "night" / "cricket" / "owl"            → night layer
    ///   "rain" / "thunder" / "storm"           → rain layer
    ///   "stone" / "cave" / "rock" / "wind"     → ridge/rocky biomes
    ///   "rain" or generic ambient              → fallback
    ///
    /// Run from: Tools / Apex Shift / Audio / Create Biome Ambient Profiles
    /// </summary>
    public static class BiomeAmbientProfileCreator
    {
        private const string DataPath      = "Assets/_Project/Data/Biomes";
        private const string ProfileFolder = "Assets/_Project/Data/Audio/BiomeAmbientProfiles";
        private const string CatalogPath   = DataPath + "/BiomeCatalog.asset";

        // ── Keyword tables ───────────────────────────────────────────────────────

        // Ordered by specificity – checked top-to-bottom; first match wins.
        private static readonly string[] DayForestKeywords  = { "forest_day", "forest_loop", "forest", "bird", "nature_day", "nature_amb" };
        private static readonly string[] DayMeadowKeywords  = { "meadow", "field", "grass_loop", "open_day" };
        private static readonly string[] DayRidgeKeywords   = { "mountain", "wind_loop", "wind_amb", "stone_amb", "highland" };
        private static readonly string[] DayWildsKeywords   = { "desert", "savanna", "dry_loop", "dryland", "wilds" };
        private static readonly string[] NightKeywords      = { "night", "cricket", "owl", "nocturnal", "forest_night" };
        private static readonly string[] DawnDuskKeywords   = { "dawn", "dusk", "morning", "evening", "twilight" };
        private static readonly string[] RainKeywords       = { "rain", "thunder", "storm", "drizzle" };
        private static readonly string[] GenericAmbKeywords = { "forest_day_loop", "forest", "ambient_loop", "nature" };

        // ── Menu entry ───────────────────────────────────────────────────────────

        [MenuItem("Tools/Apex Shift/Audio/Create Biome Ambient Profiles")]
        public static void CreateBiomeAmbientProfiles()
        {
            EnsureFolder(ProfileFolder);

            BiomeCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[BiomeAmbientProfileCreator] BiomeCatalogAsset not found at {CatalogPath}. Run Tools/Apex Shift/World/Create Default Biome Data Assets first.");
                return;
            }

            // Discover all AudioClip assets in the project
            AudioClipIndex clipIndex = BuildClipIndex();
            Debug.Log($"[BiomeAmbientProfileCreator] Discovered {clipIndex.All.Count} AudioClip(s) in project.");

            int created = 0;
            int updated = 0;

            foreach (BiomeDefinitionAsset biome in catalog.Biomes)
            {
                if (biome == null) continue;

                string path     = $"{ProfileFolder}/{biome.BiomeId}_ambient.asset";
                bool   isNew    = !File.Exists(Path.GetFullPath(path));
                BiomeAmbientProfile profile = AssetDatabase.LoadAssetAtPath<BiomeAmbientProfile>(path);

                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<BiomeAmbientProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                    isNew = true;
                }

                AssignClips(profile, biome.BiomeId, clipIndex);
                EditorUtility.SetDirty(profile);

                // Link profile → biome definition
                SerializedObject so = new SerializedObject(biome);
                SerializedProperty prop = so.FindProperty("ambientProfile");
                if (prop != null)
                {
                    prop.objectReferenceValue = profile;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(biome);
                }

                if (isNew) created++;
                else       updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BiomeAmbientProfileCreator] Done – created {created}, updated {updated} BiomeAmbientProfile(s).");

            // Show results in the Project window
            Selection.objects = catalog.Biomes
                .Where(b => b != null)
                .Select(b => AssetDatabase.LoadAssetAtPath<Object>($"{ProfileFolder}/{b.BiomeId}_ambient.asset"))
                .Where(o => o != null)
                .ToArray();
        }

        // ── Clip assignment ──────────────────────────────────────────────────────

        private static void AssignClips(BiomeAmbientProfile profile, string biomeId, AudioClipIndex idx)
        {
            SerializedObject so = new SerializedObject(profile);

            // biomeId field
            SerializedProperty idProp = so.FindProperty("biomeId");
            if (idProp != null) idProp.stringValue = biomeId;

            // volumeMultiplier
            SerializedProperty volProp = so.FindProperty("volumeMultiplier");
            if (volProp != null) volProp.floatValue = 1f;

            // Day clips
            AudioClip[] day = PickDayClips(biomeId, idx);
            SetClipArray(so, "dayClips", day);

            // Night clips
            AudioClip[] night = PickClipsByKeywords(idx.All, NightKeywords, 3);
            SetClipArray(so, "nightClips", night);

            // Dawn/dusk clips
            AudioClip[] dawnDusk = PickClipsByKeywords(idx.All, DawnDuskKeywords, 2);
            SetClipArray(so, "dawnDuskClips", dawnDusk);

            // Rain clips
            AudioClip[] rain = PickClipsByKeywords(idx.All, RainKeywords, 3);
            SetClipArray(so, "rainClips", rain);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip[] PickDayClips(string biomeId, AudioClipIndex idx)
        {
            string id = (biomeId ?? string.Empty).ToLowerInvariant();

            // Forest / wooded biomes
            if (id.Contains("westwood") || id.Contains("thicket") || id.Contains("forest"))
                return PickClipsByKeywords(idx.All, DayForestKeywords, 4, GenericAmbKeywords);

            // Open meadow / starting area
            if (id.Contains("meadow") || id.Contains("hearth"))
                return PickClipsByKeywords(idx.All, DayMeadowKeywords, 4, DayForestKeywords, GenericAmbKeywords);

            // Rocky ridge / mountains
            if (id.Contains("ridge") || id.Contains("stone") || id.Contains("rock"))
                return PickClipsByKeywords(idx.All, DayRidgeKeywords, 4, GenericAmbKeywords);

            // Harsh / dry wilds
            if (id.Contains("wild") || id.Contains("redfang") || id.Contains("dry"))
                return PickClipsByKeywords(idx.All, DayWildsKeywords, 4, DayRidgeKeywords, GenericAmbKeywords);

            // Water / shore
            if (id.Contains("water") || id.Contains("shore") || id.Contains("coast"))
                return PickClipsByKeywords(idx.All, new[] { "water_amb", "ocean", "shore", "wave" }, 4, GenericAmbKeywords);

            return PickClipsByKeywords(idx.All, GenericAmbKeywords, 4);
        }

        /// <summary>Searches keyword tables in order; returns up to <paramref name="max"/> clips from first match.</summary>
        private static AudioClip[] PickClipsByKeywords(IEnumerable<AudioClip> pool, string[] primary, int max, params string[][] fallbacks)
        {
            List<AudioClip> hits = FindByKeywords(pool, primary, max);
            if (hits.Count > 0) return hits.ToArray();

            foreach (string[] fb in fallbacks)
            {
                hits = FindByKeywords(pool, fb, max);
                if (hits.Count > 0) return hits.ToArray();
            }

            return System.Array.Empty<AudioClip>();
        }

        private static List<AudioClip> FindByKeywords(IEnumerable<AudioClip> pool, string[] keywords, int max)
        {
            List<AudioClip> result = new List<AudioClip>();
            foreach (string kw in keywords)
            {
                foreach (AudioClip clip in pool)
                {
                    if (clip == null || result.Contains(clip)) continue;
                    if (clip.name.ToLowerInvariant().Contains(kw.ToLowerInvariant()))
                    {
                        result.Add(clip);
                        if (result.Count >= max) return result;
                    }
                }

                if (result.Count > 0) break; // found something for this keyword level, stop here
            }

            return result;
        }

        private static void SetClipArray(SerializedObject so, string fieldName, AudioClip[] clips)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;

            prop.ClearArray();
            for (int i = 0; i < clips.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }

        // ── Clip discovery ───────────────────────────────────────────────────────

        private static AudioClipIndex BuildClipIndex()
        {
            AudioClipIndex idx = new AudioClipIndex();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null && !idx.All.Contains(clip))
                {
                    idx.All.Add(clip);
                }
            }

            return idx;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
        }

        private sealed class AudioClipIndex
        {
            public List<AudioClip> All { get; } = new List<AudioClip>();
        }
    }
}
