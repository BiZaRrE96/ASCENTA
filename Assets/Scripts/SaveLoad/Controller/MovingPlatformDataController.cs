using System;
using UnityEngine;

// should save and restore current position and state in the movement
public class MovingPlatformDataController : IDataPersistence
{
    [SerializeField] MovingPlatform platform;
    [SerializeField] string platformId;

    void Awake()
    {
        if (platform == null)
        {
            platform = GetComponent<MovingPlatform>();
        }

        if (string.IsNullOrWhiteSpace(platformId))
        {
            platformId = Guid.NewGuid().ToString();
        }
    }

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(platformId))
        {
            platformId = Guid.NewGuid().ToString();
        }
    }

    public override void LoadData(GameData data)
    {
        if (data == null || data.movingPlatformDatas == null || platform == null)
        {
            return;
        }

        MovingPlatformData savedData = data.movingPlatformDatas.Find(item => item.id == platformId);
        if (savedData == null)
        {
            return;
        }

        platform.RestoreSaveState(savedData);
    }

    public override void SaveData(ref GameData data)
    {
        if (platform == null)
        {
            return;
        }

        if (data == null)
        {
            data = new GameData();
        }

        if (data.movingPlatformDatas == null)
        {
            data.movingPlatformDatas = new System.Collections.Generic.List<MovingPlatformData>();
        }

        MovingPlatformData platformData = platform.CaptureSaveState(platformId);

        int existingIndex = data.movingPlatformDatas.FindIndex(item => item.id == platformId);
        if (existingIndex >= 0)
        {
            data.movingPlatformDatas[existingIndex] = platformData;
        }
        else
        {
            data.movingPlatformDatas.Add(platformData);
        }
    }
}
