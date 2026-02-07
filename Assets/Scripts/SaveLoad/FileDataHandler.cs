using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    string dataDirPath;
    string dataFileName;
    public FileDataHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
    }

    public GameData Load()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            string dataToLoad = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(dataToLoad))
            {
                return null;
            }

            return JsonUtility.FromJson<GameData>(dataToLoad);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Save(GameData data)
    {
        if (data == null)
        {
            return;
        }

        string fullPath = Path.Combine(dataDirPath, dataFileName);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string dataToStore = JsonUtility.ToJson(data, true);
            File.WriteAllText(fullPath, dataToStore);
        }
        catch (Exception)
        {
            // Swallow errors for now; consider logging if needed.
        }
    }
}
