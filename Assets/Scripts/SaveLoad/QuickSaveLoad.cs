using UnityEngine;

public class QuickSaveLoad : MonoBehaviour
{
    public void OnQuicksave()
    {
        if (DataPersistenceManager.Instance == null)
        {
            return;
        }

        DataPersistenceManager.Instance.SaveGame();
    }

    public void OnQuickload()
    {
        if (DataPersistenceManager.Instance == null)
        {
            return;
        }

        DataPersistenceManager.Instance.LoadGame();
    }
}
