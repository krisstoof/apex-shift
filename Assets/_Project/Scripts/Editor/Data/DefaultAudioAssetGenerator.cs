using System.IO;
using ApexShift.Runtime.Audio;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Data
{
    public static class DefaultAudioAssetGenerator
    {
        private const string AudioFolder = "Assets/_Project/Data/Audio";
        private const string CombatProfilePath = "Assets/_Project/Data/Audio/CombatAudioProfile.asset";
        private const string TrapProfilePath = "Assets/_Project/Data/Audio/TrapAudioProfile.asset";
        private const string CreatureProfilePath = "Assets/_Project/Data/Audio/CreatureAudioProfile.asset";

        [MenuItem("Tools/Apex Shift/Audio/Create Default Audio Profiles")]
        public static void CreateDefaultAudioProfiles()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder(AudioFolder);

            CreateOrUpdateAsset<CombatAudioProfile>(CombatProfilePath, _ => { });
            CreateOrUpdateAsset<TrapAudioProfile>(TrapProfilePath, _ => { });
            CreateOrUpdateAsset<CreatureAudioProfile>(CreatureProfilePath, _ => { });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static CombatAudioProfile LoadCombatProfile() => AssetDatabase.LoadAssetAtPath<CombatAudioProfile>(CombatProfilePath);
        public static TrapAudioProfile LoadTrapProfile() => AssetDatabase.LoadAssetAtPath<TrapAudioProfile>(TrapProfilePath);
        public static CreatureAudioProfile LoadCreatureProfile() => AssetDatabase.LoadAssetAtPath<CreatureAudioProfile>(CreatureProfilePath);

        private static T CreateOrUpdateAsset<T>(string assetPath, System.Action<T> configure)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            configure(asset);
            EditorUtility.SetDirty(asset);
            return asset;
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
    }
}
