using System;
using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Infrastructure.Save
{
    public sealed class UnityJsonGameSaveSerializer : IGameSaveSerializer
    {
        public string Serialize(GameSaveData data)
        {
            GameSaveData safeData = data ?? new GameSaveData();
            safeData.EnsureDefaults();
            return JsonUtility.ToJson(safeData, true);
        }

        public GameSaveData Deserialize(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new GameSaveData();
            }

            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(payload);
                data ??= new GameSaveData();
                data.EnsureDefaults();
                return data;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Failed to deserialize game save JSON.", exception);
            }
        }
    }
}
