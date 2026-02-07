using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : ReversibleMono
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

    protected override void Awake()
    {
        base.Awake();
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
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

        AdvanceState(FixedDeltaTime);
        Vector3 targetPosition = GetTargetPosition(rb.position);

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
        DeltaVelocity = Mathf.Abs(FixedDeltaTime) > Mathf.Epsilon ? FrameDelta / FixedDeltaTime : Vector3.zero;
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

    int GetPreviousPointIndex(int fromIndex, int forwardIndex)
    {
        if (!pingPong)
        {
            return GetNextValidPointIndex(fromIndex, -1, true);
        }

        int nextForward = FindAdjacentIndex(fromIndex, 1);
        int nextBackward = FindAdjacentIndex(fromIndex, -1);

        if (forwardIndex == nextForward && nextBackward != -1)
        {
            return nextBackward;
        }

        if (forwardIndex == nextBackward && nextForward != -1)
        {
            return nextForward;
        }

        if (nextBackward != -1)
        {
            return nextBackward;
        }

        if (nextForward != -1)
        {
            return nextForward;
        }

        return fromIndex;
    }

    int FindAdjacentIndex(int fromIndex, int direction)
    {
        if (points.Count == 0 || direction == 0)
        {
            return -1;
        }

        int length = points.Count;
        for (int i = 1; i <= length; i++)
        {
            int candidate = fromIndex + direction * i;
            if (candidate < 0 || candidate >= length)
            {
                break;
            }

            if (points[candidate] != null)
            {
                return candidate;
            }
        }

        return -1;
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

    void AdvanceState(float deltaTime)
    {
        if (Mathf.Abs(deltaTime) <= Mathf.Epsilon)
        {
            return;
        }

        float remaining = deltaTime;
        int safety = 0;
        while (Mathf.Abs(remaining) > Mathf.Epsilon && safety++ < 16)
        {
            if (isIdling)
            {
                if (idleOnPointTime <= Mathf.Epsilon)
                {
                    isIdling = false;
                    moveElapsed = Mathf.Clamp(moveElapsed, 0f, Mathf.Max(0.01f, travelTime));
                    continue;
                }

                if (remaining > 0f)
                {
                    float toEnd = idleOnPointTime - idleElapsed;
                    if (remaining >= toEnd)
                    {
                        remaining -= toEnd;
                        idleElapsed = 0f;
                        isIdling = false;
                        moveElapsed = 0f;
                    }
                    else
                    {
                        idleElapsed += remaining;
                        remaining = 0f;
                    }
                }
                else
                {
                    float toStart = idleElapsed;
                    float backwards = -remaining;
                    if (backwards >= toStart)
                    {
                        remaining += toStart;
                        idleElapsed = 0f;
                        isIdling = false;
                        moveElapsed = Mathf.Max(0.01f, travelTime);
                    }
                    else
                    {
                        idleElapsed += remaining;
                        remaining = 0f;
                    }
                }
            }
            else
            {
                float duration = Mathf.Max(0.01f, travelTime);
                if (remaining > 0f)
                {
                    float toEnd = duration - moveElapsed;
                    if (remaining >= toEnd)
                    {
                        remaining -= toEnd;
                        moveElapsed = duration;
                        CompleteMoveForward();
                    }
                    else
                    {
                        moveElapsed += remaining;
                        remaining = 0f;
                    }
                }
                else
                {
                    float toStart = moveElapsed;
                    float backwards = -remaining;
                    if (backwards >= toStart)
                    {
                        remaining += toStart;
                        moveElapsed = 0f;
                        CompleteMoveBackward();
                    }
                    else
                    {
                        moveElapsed += remaining;
                        remaining = 0f;
                    }
                }
            }
        }
    }

    void CompleteMoveForward()
    {
        currentPointIndex = nextPointIndex;
        nextPointIndex = GetNextPointIndex(currentPointIndex);
        isIdling = true;
        moveElapsed = 0f;
        idleElapsed = 0f;
    }

    void CompleteMoveBackward()
    {
        int previousIndex = GetPreviousPointIndex(currentPointIndex, nextPointIndex);
        nextPointIndex = currentPointIndex;
        currentPointIndex = previousIndex;
        isIdling = true;
        moveElapsed = 0f;
        idleElapsed = Mathf.Max(0f, idleOnPointTime);
    }

    Vector3 GetTargetPosition(Vector3 fallback)
    {
        if (isIdling)
        {
            return GetPointPositionOrFallback(currentPointIndex, fallback);
        }

        float normalized = Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, travelTime));
        float curveT = movementCurve != null ? movementCurve.Evaluate(normalized) : normalized;

        Vector3 start = GetPointPositionOrFallback(currentPointIndex, fallback);
        Vector3 end = GetPointPositionOrFallback(nextPointIndex, fallback);
        return Vector3.LerpUnclamped(start, end, curveT);
    }

    public MovingPlatformData CaptureSaveState(string platformId)
    {
        Vector3 position = rb != null ? rb.position : transform.position;
        return new MovingPlatformData(
            platformId,
            position,
            GetTransitionProgress(),
            currentPointIndex,
            nextPointIndex,
            isIdling,
            idleElapsed,
            travelDirection);
    }

    public void RestoreSaveState(MovingPlatformData data)
    {
        if (data == null)
        {
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        travelDirection = data.travelDirection == 0 ? 1 : data.travelDirection;
        currentPointIndex = data.currentPointIndex;
        nextPointIndex = data.nextPointIndex;
        isIdling = data.isIdling;
        idleElapsed = Mathf.Clamp(data.idleElapsed, 0f, idleOnPointTime);
        moveElapsed = Mathf.Clamp01(data.transitionProgress) * Mathf.Max(0.01f, travelTime);

        Vector3 targetPosition = data.position;
        previousPosition = targetPosition;
        FrameDelta = Vector3.zero;
        DeltaVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    float GetTransitionProgress()
    {
        if (travelTime <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, travelTime));
    }
}
