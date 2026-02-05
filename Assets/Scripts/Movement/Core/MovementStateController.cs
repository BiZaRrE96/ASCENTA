using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MovementStateController : MonoBehaviour
{
    [SerializeField] Groundcheck groundcheck;

    MovementState currentState = MovementState.Default;
    float stateLockUntil;
    Coroutine temporaryStateCoroutine;

    public MovementState CurrentState => currentState;

    void Awake()
    {
        if (groundcheck == null)
        {
            groundcheck = GetComponentInChildren<Groundcheck>();
        }
    }

    void FixedUpdate()
    {
        if (groundcheck == null)
        {
            return;
        }

        if (!IsStateLocked() && groundcheck.IsGrounded && currentState == MovementState.Airborne)
        {
            currentState = MovementState.Default;
        }
    }

    public bool IsStateLocked()
    {
        return Time.time < stateLockUntil;
    }

    public void SetState(MovementState targetState, float stateLockIn)
    {
        if (IsStateLocked() && currentState != targetState)
        {
            return;
        }

        currentState = targetState;
        float clampedDuration = Mathf.Max(0f, stateLockIn);
        stateLockUntil = clampedDuration > 0f ? Time.time + clampedDuration : 0f;
    }

    public void TemporarilySetState(MovementState targetState, float duration)
    {
        if (duration <= 0f)
        {
            SetState(targetState, duration);
            return;
        }

        if (temporaryStateCoroutine != null)
        {
            StopCoroutine(temporaryStateCoroutine);
            temporaryStateCoroutine = null;
        }

        temporaryStateCoroutine = StartCoroutine(TemporaryStateRoutine(targetState, duration));
    }

    IEnumerator TemporaryStateRoutine(MovementState targetState, float duration)
    {
        MovementState previousState = currentState;
        float previousLockUntil = stateLockUntil;

        SetState(targetState, duration);

        yield return new WaitForSeconds(duration);

        if (currentState == targetState)
        {
            currentState = previousState;
            stateLockUntil = previousLockUntil;
        }

        temporaryStateCoroutine = null;
    }
}
