using UnityEngine;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.World.Biomes;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class RuntimeAudioSetup
    {
        public void Compose(Transform parent, BiomeCatalogAsset catalog, bool enabled, float volume)
        {
            if (!enabled) return;
            AmbientMusicRuntime ambient = GetOrCreate<AmbientMusicRuntime>("AmbientMusicRuntime", parent);
            ambient.SetVolume(volume);
            AmbientSoundController controller = GetOrCreate<AmbientSoundController>("AmbientSoundController", parent);
            if (catalog != null)
            {
                foreach (BiomeDefinitionAsset biome in catalog.Biomes)
                {
                    if (biome != null && biome.AmbientProfile != null)
                        controller.RegisterProfile(biome.AmbientProfile);
                }
            }
            if (catalog == null || !HasAnyProfiles(catalog)) ambient.Play();
        }

        private static bool HasAnyProfiles(BiomeCatalogAsset catalog)
        {
            foreach (BiomeDefinitionAsset biome in catalog.Biomes)
                if (biome != null && biome.AmbientProfile != null && biome.AmbientProfile.HasAnyClips()) return true;
            return false;
        }

        private static T GetOrCreate<T>(string name, Transform parent) where T : Component
        {
            T existing = parent != null ? parent.GetComponentInChildren<T>(true) : null;
            if (existing != null) return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
