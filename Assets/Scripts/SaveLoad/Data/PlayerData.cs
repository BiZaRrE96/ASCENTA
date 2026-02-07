

using UnityEngine;

[System.Serializable]

public class PlayerData
{
    public Vector3 position;
    public Quaternion rotation;

    public PlayerData()
    {
        this.position = Vector3.zero;
        this.rotation = new Quaternion(0, 0, 0, 1);
    }
}
