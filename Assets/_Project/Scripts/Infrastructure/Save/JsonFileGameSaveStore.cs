using System;
using System.IO;
using System.Linq;
using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Infrastructure.Save
{
    public sealed class JsonFileGameSaveStore : IGameSaveStore
    {
        private const string Extension = ".json";

        private readonly string directoryPath;
        private readonly IGameSaveSerializer serializer;

        public JsonFileGameSaveStore()
            : this(Path.Combine(Application.persistentDataPath, "Saves"), new UnityJsonGameSaveSerializer())
        {
        }

        public JsonFileGameSaveStore(string directoryPath, IGameSaveSerializer serializer = null)
        {
            this.directoryPath = string.IsNullOrWhiteSpace(directoryPath)
                ? Path.Combine(Application.persistentDataPath, "Saves")
                : directoryPath;
            this.serializer = serializer ?? new UnityJsonGameSaveSerializer();
        }

        public bool Exists(string slotName)
        {
            return File.Exists(GetPath(slotName));
        }

        public void Save(string slotName, GameSaveData data)
        {
            Directory.CreateDirectory(directoryPath);
            string payload = serializer.Serialize(data ?? new GameSaveData());
            File.WriteAllText(GetPath(slotName), payload);
        }

        public GameSaveData Load(string slotName)
        {
            string path = GetPath(slotName);
            if (!File.Exists(path))
            {
                return new GameSaveData();
            }

            string payload = File.ReadAllText(path);
            return serializer.Deserialize(payload);
        }

        public void Delete(string slotName)
        {
            string path = GetPath(slotName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string GetPath(string slotName)
        {
            string safeSlotName = SanitizeSlotName(slotName);
            return Path.Combine(directoryPath, safeSlotName + Extension);
        }

        private static string SanitizeSlotName(string slotName)
        {
            string value = string.IsNullOrWhiteSpace(slotName) ? "default" : slotName.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
        }
    }
}
