using UnityEngine;


public class PlayerDataController : IDataPersistence
{
    [SerializeField] Transform playerTransform;

    void Awake()
    {
        if (playerTransform == null)
        {
            playerTransform = transform;
        }
    }

    public override void BeforeLoadData()
    {
        // lock player input, turn off gravity and collision
    }

    public override void LoadData(GameData data)
    {
        if (data == null || data.playerData == null || playerTransform == null)
        {
            return;
        }

        playerTransform.SetPositionAndRotation(data.playerData.position, data.playerData.rotation);
    }

    public override void LoadDataComplete()
    {
        // reenable input, gravity, and collision
    }

    public override void SaveData(ref GameData data)
    {
        if (playerTransform == null)
        {
            return;
        }

        if (data == null)
        {
            data = new GameData();
        }

        if (data.playerData == null)
        {
            data.playerData = new PlayerData();
        }

        data.playerData.position = playerTransform.position;
        data.playerData.rotation = playerTransform.rotation;
    }
}
