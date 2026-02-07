using UnityEngine;

public enum LoadPriority
{
    HIGH,
    MEDIUM,
    LOW
}
public abstract class IDataPersistence : MonoBehaviour
{
    public LoadPriority loadPriority;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public abstract void LoadData(GameData data);

    public abstract void SaveData(ref GameData data);

    public virtual void BeforeLoadData() {} // all before load data is called by DataPersistenceManager if implemented

    public virtual void LoadDataComplete() {}
}
