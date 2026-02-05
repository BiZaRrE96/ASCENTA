using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] Rigidbody rb;
    [SerializeField] List<Transform> points = new List<Transform>();
    [SerializeField, Min(0.01f)] float travelTime = 1f;
    [SerializeField, Min(0f)] float idleOnPointTime = 0f;
    [SerializeField] AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] bool pingPong;

    public Vector3 FrameDelta { get; private set; }
    public Vector3 DeltaVelocity { get; private set; }
    public bool MovesGroundedPlayerDirectly => false;
    public event Action<Vector3> OnDeltaMoved;

    int currentPointIndex;
    int nextPointIndex;
    float moveElapsed;
    float idleElapsed;
    bool isIdling = true;
    int travelDirection = 1;
    Vector3 previousPosition;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true;
        }
    }

    void OnEnable()
    {
        FrameDelta = Vector3.zero;
        DeltaVelocity = Vector3.zero;
        travelDirection = 1;
        currentPointIndex = GetFirstValidPointIndex();
        nextPointIndex = GetNextPointIndex(currentPointIndex);
        moveElapsed = 0f;
        idleElapsed = 0f;
        isIdling = true;

        Vector3 startPosition = GetPointPositionOrFallback(currentPointIndex, rb != null ? rb.position : transform.position);
        previousPosition = startPosition;
        if (rb != null)
        {
            rb.MovePosition(startPosition);
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!HasEnoughValidPoints())
        {
            RecordDelta(previousPosition);
            return;
        }

        Vector3 targetPosition = rb.position;
        if (isIdling)
        {
            idleElapsed += Time.fixedDeltaTime;
            if (idleElapsed >= idleOnPointTime)
            {
                isIdling = false;
                idleElapsed = 0f;
                moveElapsed = 0f;
            }

            targetPosition = GetPointPositionOrFallback(currentPointIndex, rb.position);
        }
        else
        {
            moveElapsed += Time.fixedDeltaTime;
            float normalized = Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, travelTime));
            float curveT = movementCurve != null ? movementCurve.Evaluate(normalized) : normalized;

            Vector3 start = GetPointPositionOrFallback(currentPointIndex, rb.position);
            Vector3 end = GetPointPositionOrFallback(nextPointIndex, rb.position);
            targetPosition = Vector3.LerpUnclamped(start, end, curveT);

            if (normalized >= 1f)
            {
                currentPointIndex = nextPointIndex;
                nextPointIndex = GetNextPointIndex(currentPointIndex);
                isIdling = true;
                moveElapsed = 0f;
                idleElapsed = 0f;
                targetPosition = GetPointPositionOrFallback(currentPointIndex, targetPosition);
            }
        }

        rb.MovePosition(targetPosition);
        RecordDelta(targetPosition);
        if (FrameDelta.sqrMagnitude > Mathf.Epsilon)
        {
            OnDeltaMoved?.Invoke(FrameDelta);
        }
    }

    void RecordDelta(Vector3 currentPosition)
    {
        FrameDelta = currentPosition - previousPosition;
        DeltaVelocity = Time.fixedDeltaTime > Mathf.Epsilon ? FrameDelta / Time.fixedDeltaTime : Vector3.zero;
        previousPosition = currentPosition;
    }

    bool HasEnoughValidPoints()
    {
        int validCount = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
            {
                validCount++;
                if (validCount > 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    int GetNextPointIndex(int fromIndex)
    {
        if (!pingPong)
        {
            return GetNextValidPointIndex(fromIndex, 1, true);
        }

        int next = GetNextValidPointIndex(fromIndex, travelDirection, false);
        if (next != fromIndex)
        {
            return next;
        }

        travelDirection *= -1;
        return GetNextValidPointIndex(fromIndex, travelDirection, false);
    }

    int GetNextValidPointIndex(int fromIndex, int direction, bool allowWrap)
    {
        if (points.Count == 0 || direction == 0)
        {
            return -1;
        }

        int length = points.Count;
        for (int i = 1; i <= length; i++)
        {
            int candidate = fromIndex + direction * i;
            if (allowWrap)
            {
                candidate = ((candidate % length) + length) % length;
            }
            else if (candidate < 0 || candidate >= length)
            {
                break;
            }

            if (points[candidate] != null)
            {
                return candidate;
            }
        }

        return fromIndex;
    }

    int GetFirstValidPointIndex()
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    Vector3 GetPointPositionOrFallback(int index, Vector3 fallback)
    {
        if (index >= 0 && index < points.Count && points[index] != null)
        {
            return points[index].position;
        }

        return fallback;
    }
}
