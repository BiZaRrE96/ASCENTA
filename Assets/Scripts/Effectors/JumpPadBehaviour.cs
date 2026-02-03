using System.Collections.Generic;
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
        if (rb == null || rb.isKinematic || boostForce <= 0f)
        {
            return;
        }

        if (!CanBoost(rb, out int instanceId))
        {
            return;
        }

        ApplyBoost(rb);
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

    void ApplyBoost(Rigidbody rb)
    {
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;

        Vector3 boostDirection = GetBoostDirection();
        float mass = Mathf.Max(rb.mass, 0.01f);
        float impulseMagnitude = boostForce * (scaleForceByMass ? mass : 1f);
        Vector3 forwardImpulse = boostDirection * impulseMagnitude;
        rb.AddForce(forwardImpulse, ForceMode.Impulse);

        if (landingTarget == null)
        {
            return;
        }

        Vector3 gravity = Physics.gravity;
        if (gravity.sqrMagnitude < Mathf.Epsilon)
        {
            gravity = Vector3.down * 9.81f;
        }

        Vector3 start = rb.worldCenterOfMass;
        Vector3 destination = landingTarget.position;
        Vector3 displacement = destination - start;

        Vector3 gravityDir = gravity.normalized;
        Vector3 upDir = -gravityDir;

        Vector3 planarForward = Vector3.ProjectOnPlane(boostDirection, gravityDir);
        if (planarForward.sqrMagnitude <= Mathf.Epsilon)
        {
            planarForward = Vector3.ProjectOnPlane(boostDirection, Vector3.up);
        }

        Vector3 forwardDir = planarForward.sqrMagnitude > Mathf.Epsilon
            ? planarForward.normalized
            : boostDirection.normalized;

        Vector3 planarDisplacement = Vector3.ProjectOnPlane(displacement, gravityDir);
        float forwardDistance = Mathf.Max(Vector3.Dot(planarDisplacement, forwardDir), 0f);

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, forwardDir);
        float deltaForwardSpeed = impulseMagnitude / mass;
        float expectedForwardSpeed = Mathf.Max(currentForwardSpeed + deltaForwardSpeed, 0.01f);

        float assumedTravel = forwardDistance > 0f ? forwardDistance / expectedForwardSpeed : 0f;
        float travelTime = Mathf.Clamp(assumedTravel, minTravelTime, maxTravelTime);

        float verticalDisplacement = Vector3.Dot(displacement, upDir);
        float verticalAcceleration = Vector3.Dot(gravity, upDir);
        float desiredVerticalSpeed = (verticalDisplacement - 0.5f * verticalAcceleration * travelTime * travelTime) /
            Mathf.Max(travelTime, float.Epsilon);

        float currentVerticalSpeed = Vector3.Dot(rb.linearVelocity, upDir);
        float verticalDelta = desiredVerticalSpeed - currentVerticalSpeed;
        Vector3 verticalImpulse = upDir * (verticalDelta * mass);
        rb.AddForce(verticalImpulse, ForceMode.Impulse);
        NotifyPlayerMovementState(rb, travelTime);
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

        movementController?.SetMovementState(MovementController.MovementState.Airborne, Mathf.Max(travelTime, minTravelTime));
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
