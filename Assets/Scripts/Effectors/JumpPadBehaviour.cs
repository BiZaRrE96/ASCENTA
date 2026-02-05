using System.Collections.Generic;
using ASCENTA.Events;
using UnityEngine;

public class JumpPadBehaviour : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField, Min(0f)]
    float boostForce = 18f;
    [SerializeField, Min(0f)]
    float minBoostInterval = 0.1f;
    [SerializeField]
    bool scaleForceByMass = true;

    [Header("Targeting")]
    [SerializeField]
    Transform landingTarget;
    [SerializeField, Min(0f)]
    float minTravelTime = 0.15f;
    [SerializeField, Min(0f)]
    float maxTravelTime = 1.2f;

    [Header("Direction")]
    [SerializeField]
    Vector3 directionRotationOffset = Vector3.zero;

    readonly Dictionary<int, float> lastBoostTimestamps = new Dictionary<int, float>();

    public void TryBoost(Rigidbody rb)
    {
        Vector3 fallbackPoint = rb != null ? rb.worldCenterOfMass : transform.position;
        TryBoost(rb, fallbackPoint);
    }

    public void TryBoost(Rigidbody rb, Vector3 contactPoint)
    {
        if (rb == null || rb.isKinematic || boostForce <= 0f)
        {
            return;
        }

        if (!CanBoost(rb, out int instanceId))
        {
            return;
        }

        ApplyBoost(rb, contactPoint);
        lastBoostTimestamps[instanceId] = Time.time;
    }

    bool CanBoost(Rigidbody rb, out int instanceId)
    {
        instanceId = rb.GetInstanceID();
        if (lastBoostTimestamps.TryGetValue(instanceId, out float lastBoost))
        {
            if (Time.time - lastBoost < minBoostInterval)
            {
                return false;
            }
        }

        return true;
    }

    void ApplyBoost(Rigidbody rb, Vector3 contactPoint)
    {
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;

        Vector3 boostDirection = GetBoostDirection();
        float mass = Mathf.Max(rb.mass, 0.01f);
        float impulseMagnitude = boostForce * (scaleForceByMass ? mass : 1f);
        Vector3 appliedImpulse;

        if (landingTarget == null)
        {
            Vector3 forwardImpulse = boostDirection * impulseMagnitude;
            rb.AddForce(forwardImpulse, ForceMode.Impulse);
            appliedImpulse = forwardImpulse;
            PublishBoostEvent(contactPoint, boostDirection, appliedImpulse.magnitude);
            return;
        }

        // Targeting logic
        Vector3 gravity = Physics.gravity;
        if (gravity.sqrMagnitude < Mathf.Epsilon)
        {
            gravity = Vector3.down * 9.81f;
        }

        Vector3 start = rb.worldCenterOfMass;
        Vector3 destination = landingTarget.position;
        Vector3 displacement = destination - start;

        float baseSpeed = boostForce * (scaleForceByMass ? 1f : 1f / mass);

        // Decompose gravity and displacement relative to boost direction
        Vector3 gravityDir = gravity.normalized;
        float gravityMag = gravity.magnitude;

        // Create coordinate system: forward = boost direction, up = perpendicular to gravity
        Vector3 forwardDir = boostDirection.normalized;
        Vector3 upDir = -gravityDir;

        // Project displacement onto forward and up axes
        float forwardDistance = Vector3.Dot(displacement, forwardDir);
        float verticalDistance = Vector3.Dot(displacement, upDir);

        // Solve ballistic trajectory: given initial speed and direction, find time to reach target
        // Using the boost direction means we're solving:
        // forwardDistance = baseSpeed * cos(angle) * t
        // verticalDistance = baseSpeed * sin(angle) * t - 0.5 * g * t^2
        // where angle is the angle between boostDirection and horizontal plane

        // Component of boost in forward direction
        float vForward = Vector3.Dot(forwardDir, boostDirection.normalized) * baseSpeed;
        // Component of boost in vertical direction
        float vVertical = Vector3.Dot(upDir, boostDirection.normalized) * baseSpeed;
        // Component of gravity in vertical direction
        float gVertical = gravityMag;

        // Solve for time: verticalDistance = vVertical * t - 0.5 * gVertical * t^2
        // Rearranged: 0.5 * gVertical * t^2 - vVertical * t + verticalDistance = 0
        // Use quadratic formula, but also constrain by forward distance if needed

        float travelTime;
        
        if (Mathf.Abs(forwardDistance) > 0.01f && Mathf.Abs(vForward) > 0.01f)
        {
            // Estimate time based on forward distance
            travelTime = Mathf.Abs(forwardDistance / vForward);
        }
        else
        {
            // Solve quadratic for vertical motion
            float a = 0.5f * gVertical;
            float b = -vVertical;
            float c = verticalDistance;
            
            float discriminant = b * b - 4 * a * c;
            if (discriminant >= 0)
            {
                float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                travelTime = Mathf.Max(t1, t2); // Take the later time (full arc)
            }
            else
            {
                travelTime = maxTravelTime;
            }
        }

        // Clamp travel time
        travelTime = Mathf.Clamp(travelTime, minTravelTime, maxTravelTime);

        // Now calculate what vertical velocity we actually need
        // verticalDistance = vVertical_needed * t - 0.5 * gVertical * t^2
        // vVertical_needed = (verticalDistance + 0.5 * gVertical * t^2) / t
        float neededVerticalSpeed = (verticalDistance + 0.5f * gVertical * travelTime * travelTime) / 
            Mathf.Max(travelTime, 0.01f);

        // Apply base impulse in boost direction
        Vector3 baseImpulse = boostDirection * impulseMagnitude;
        rb.AddForce(baseImpulse, ForceMode.Impulse);

        // Calculate current vertical speed after base impulse
        float currentVerticalSpeed = Vector3.Dot(rb.linearVelocity, upDir);
        
        // Add correction impulse to reach needed vertical speed
        float verticalDelta = neededVerticalSpeed - currentVerticalSpeed;
        Vector3 verticalImpulse = upDir * (verticalDelta * mass);
        rb.AddForce(verticalImpulse, ForceMode.Impulse);

        appliedImpulse = baseImpulse + verticalImpulse;
        PublishBoostEvent(contactPoint, boostDirection, appliedImpulse.magnitude);
        NotifyPlayerMovementState(rb, travelTime);
    }

    void PublishBoostEvent(Vector3 contactPoint, Vector3 boostDirection, float force)
    {
        if (force <= Mathf.Epsilon)
        {
            return;
        }

        EventBus.Publish(new OnJumpPadBoostEvent(contactPoint, boostDirection, force));
    }

    void NotifyPlayerMovementState(Rigidbody rb, float travelTime)
    {
        if (landingTarget == null)
        {
            return;
        }

        MovementController movementController = rb.GetComponent<MovementController>();
        if (movementController == null)
        {
            movementController = rb.GetComponentInChildren<MovementController>();
        }

        movementController?.SetMovementState(MovementState.Airborne, Mathf.Max(travelTime, minTravelTime));
    }

    Vector3 GetBoostDirection()
    {
        Quaternion adjustment = Quaternion.Euler(directionRotationOffset);
        Vector3 localDir = adjustment * Vector3.up;
        if (localDir.sqrMagnitude <= Mathf.Epsilon)
        {
            localDir = Vector3.up;
        }

        return transform.TransformDirection(localDir).normalized;
    }

    void OnValidate()
    {
        minTravelTime = Mathf.Max(0f, minTravelTime);
        maxTravelTime = Mathf.Max(minTravelTime, maxTravelTime);
        minBoostInterval = Mathf.Max(0f, minBoostInterval);
        boostForce = Mathf.Max(0f, boostForce);
    }
}
