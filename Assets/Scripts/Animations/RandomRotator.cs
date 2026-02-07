using UnityEngine;

public class RandomRotator : MonoBehaviour
{
    [Header("Randomization")]
    [SerializeField] float changeInterval = 2f;
    [SerializeField] int axesToChange = 1;
    [SerializeField] float maxSpeed = 2f;
    [SerializeField] float changeLerpTime = 0.5f;

    Vector3 currentSpeed;
    Vector3 targetSpeed;
    Vector3 speedVelocity;
    float timer;

    public float SpeedX => currentSpeed.x;
    public float SpeedY => currentSpeed.y;
    public float SpeedZ => currentSpeed.z;

    void Start()
    {
        targetSpeed = new Vector3(
            Random.Range(-maxSpeed, maxSpeed),
            Random.Range(-maxSpeed, maxSpeed),
            Random.Range(-maxSpeed, maxSpeed)
        );
        currentSpeed = targetSpeed;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            PickNewTargets();
        }

        currentSpeed.x = Mathf.SmoothDamp(currentSpeed.x, targetSpeed.x, ref speedVelocity.x, changeLerpTime);
        currentSpeed.y = Mathf.SmoothDamp(currentSpeed.y, targetSpeed.y, ref speedVelocity.y, changeLerpTime);
        currentSpeed.z = Mathf.SmoothDamp(currentSpeed.z, targetSpeed.z, ref speedVelocity.z, changeLerpTime);

        transform.Rotate(currentSpeed * Time.deltaTime, Space.Self);
    }

    void PickNewTargets()
    {
        int count = Mathf.Clamp(axesToChange, 1, 3);

        int a = Random.Range(0, 3);
        int b = (a + Random.Range(1, 3)) % 3;
        int c = 3 - a - b;

        if (count >= 1) SetAxisTarget(a);
        if (count >= 2) SetAxisTarget(b);
        if (count >= 3) SetAxisTarget(c);
    }

    void SetAxisTarget(int axis)
    {
        float value = Random.Range(-maxSpeed, maxSpeed);
        if (axis == 0) targetSpeed.x = value;
        else if (axis == 1) targetSpeed.y = value;
        else targetSpeed.z = value;
    }
}
