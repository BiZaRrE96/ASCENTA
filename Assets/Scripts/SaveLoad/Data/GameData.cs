
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public int version;
    public PlayerData playerData;

    public List<MovingPlatformData> movingPlatformDatas;

    public GameData()
    {
        version = 1;
        playerData = new PlayerData();
        movingPlatformDatas = new List<MovingPlatformData>();
    }
}
