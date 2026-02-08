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
            Debug.LogWarning($"File not found at {fullPath}");
            return null;
        }

        try
        {
            string dataToLoad = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(dataToLoad))
            {
                Debug.LogWarning($"Data not found at {fullPath}");
                return null;
            }

            return JsonUtility.FromJson<GameData>(dataToLoad);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Exception {e}");
            return null;
        }
    }

    public bool HasSaveData()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Doesnt have save at {fullPath}");
            return false;
        }

        try
        {
            FileInfo fileInfo = new FileInfo(fullPath);
            Debug.Log($"Data is {fileInfo.Length}");
            return fileInfo.Length > 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Exception when reading save : {e}");
            return false;
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
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(dataToStore);
            }
        }
        catch (Exception)
        {
            // Swallow errors for now; consider logging if needed.
        }
    }

    public bool Delete()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
