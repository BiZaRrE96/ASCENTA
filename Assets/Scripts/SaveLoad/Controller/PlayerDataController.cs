using UnityEngine;


public class PlayerDataController : IDataPersistence
{
    [SerializeField] Transform playerTransform;
    [SerializeField] MovementController movementController;
    [SerializeField] Rigidbody rb;
    [SerializeField] Collider[] colliders;

    bool cachedUseGravity;
    bool cachedDetectCollisions;
    bool[] cachedColliderStates;
    bool cachedStateValid;
    bool inputLockedForLoad;

    void Awake()
    {
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (movementController == null)
        {
            movementController = GetComponent<MovementController>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public override void BeforeLoadData()
    {
        // lock player input, turn off gravity and collision
        CacheStateIfNeeded();

        if (movementController != null)
        {
            movementController.SetPlayerInputAllowed(false);
            inputLockedForLoad = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
    }

    public override void LoadData(GameData data)
    {
        if (data == null || data.playerData == null || playerTransform == null)
        {
            return;
        }

        playerTransform.SetPositionAndRotation(data.playerData.position, data.playerData.rotation);
    }

    public override void LoadDataComplete()
    {
        // reenable input, gravity, and collision
        if (rb != null && cachedStateValid)
        {
            rb.useGravity = cachedUseGravity;
            rb.detectCollisions = cachedDetectCollisions;
        }

        if (colliders != null && cachedColliderStates != null)
        {
            int count = Mathf.Min(colliders.Length, cachedColliderStates.Length);
            for (int i = 0; i < count; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                {
                    collider.enabled = cachedColliderStates[i];
                }
            }
        }

        if (movementController != null && inputLockedForLoad)
        {
            movementController.SetPlayerInputAllowed(true);
        }

        cachedStateValid = false;
        inputLockedForLoad = false;
    }

    public override void SaveData(ref GameData data)
    {
        if (playerTransform == null)
        {
            return;
        }

        if (data == null)
        {
            data = new GameData();
        }

        if (data.playerData == null)
        {
            data.playerData = new PlayerData();
        }

        data.playerData.position = playerTransform.position;
        data.playerData.rotation = playerTransform.rotation;
    }

    void CacheStateIfNeeded()
    {
        if (cachedStateValid)
        {
            return;
        }

        if (colliders == null || colliders.Length == 0)
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }

        if (rb != null)
        {
            cachedUseGravity = rb.useGravity;
            cachedDetectCollisions = rb.detectCollisions;
        }

        if (colliders != null)
        {
            cachedColliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                cachedColliderStates[i] = collider != null && collider.enabled;
            }
        }

        cachedStateValid = true;
    }
}
