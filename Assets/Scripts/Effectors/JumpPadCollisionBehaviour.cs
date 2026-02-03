using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPadCollisionBehaviour : MonoBehaviour
{
    [SerializeField]
    JumpPadBehaviour jumpPadBehaviour;

    Collider jumpPadCollider;

    void Awake()
    {
        jumpPadCollider = GetComponent<Collider>();
        if (jumpPadBehaviour == null)
        {
            jumpPadBehaviour = GetComponent<JumpPadBehaviour>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        TryBoost(collision.rigidbody, contactPoint);
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 referencePoint = other.attachedRigidbody != null
            ? other.attachedRigidbody.worldCenterOfMass
            : other.transform.position;
        Vector3 contactPoint = jumpPadCollider != null
            ? jumpPadCollider.ClosestPoint(referencePoint)
            : transform.position;
        TryBoost(other.attachedRigidbody, contactPoint);
    }

    void TryBoost(Rigidbody rb, Vector3 contactPoint)
    {
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        jumpPadBehaviour?.TryBoost(rb, contactPoint);
    }
}
