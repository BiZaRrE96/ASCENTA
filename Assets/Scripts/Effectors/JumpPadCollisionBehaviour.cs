using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPadCollisionBehaviour : MonoBehaviour
{
    [SerializeField]
    JumpPadBehaviour jumpPadBehaviour;

    void Awake()
    {
        if (jumpPadBehaviour == null)
        {
            jumpPadBehaviour = GetComponent<JumpPadBehaviour>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TryBoost(collision.rigidbody);
    }

    void OnTriggerEnter(Collider other)
    {
        TryBoost(other.attachedRigidbody);
    }

    void TryBoost(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        jumpPadBehaviour?.TryBoost(rb);
    }
}
