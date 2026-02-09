using System.Collections;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StudioBankLoaderService : MonoBehaviour, IService
{
    [SerializeField] StudioBankLoader bankLoader;
    bool serviceInitialized;

    void Awake()
    {
        if (bankLoader == null)
        {
            bankLoader = GetComponent<StudioBankLoader>();
        }

        if (bankLoader == null)
        {
            Debug.LogWarning("StudioBankLoaderService has no StudioBankLoader reference.", this);
        }
    }

    public IEnumerator InitializeService()
    {
        if (serviceInitialized)
        {
            yield break;
        }

        serviceInitialized = true;
        if (bankLoader != null)
        {
            bankLoader.Load();
        }
    }
}
