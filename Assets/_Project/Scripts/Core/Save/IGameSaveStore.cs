namespace ApexShift.Core.Save
{
    public interface IGameSaveStore
    {
        bool Exists(string slotName);
        void Save(string slotName, GameSaveData data);
        GameSaveData Load(string slotName);
        void Delete(string slotName);
    }
}
