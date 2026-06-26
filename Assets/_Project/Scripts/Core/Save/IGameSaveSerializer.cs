namespace ApexShift.Core.Save
{
    public interface IGameSaveSerializer
    {
        string Serialize(GameSaveData data);
        GameSaveData Deserialize(string payload);
    }
}
