using UnityEngine;

public class GroundSurface : MonoBehaviour
{
    [Range(0f, 2f)] public float traction = 1f;
    [Range(0f, 2f)] public float damping = 1f;
}
