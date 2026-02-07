using UnityEngine;


[System.Serializable]
public class MovingPlatformData 
{
    // use moving platform UUID as unique identifier
    public string id;
    public Vector3 position;
    public float transitionProgress;
    public int currentPointIndex;
    public int nextPointIndex;
    public bool isIdling;
    public float idleElapsed;
    public int travelDirection;

    public MovingPlatformData()
    {
        id = string.Empty;
        position = Vector3.zero;
        transitionProgress = 0f;
        currentPointIndex = -1;
        nextPointIndex = -1;
        isIdling = true;
        idleElapsed = 0f;
        travelDirection = 1;
    }

    public MovingPlatformData(
        string id,
        Vector3 position,
        float transitionProgress,
        int currentPointIndex,
        int nextPointIndex,
        bool isIdling,
        float idleElapsed,
        int travelDirection)
    {
        this.id = id;
        this.position = position;
        this.transitionProgress = transitionProgress;
        this.currentPointIndex = currentPointIndex;
        this.nextPointIndex = nextPointIndex;
        this.isIdling = isIdling;
        this.idleElapsed = idleElapsed;
        this.travelDirection = travelDirection;
    }
}
