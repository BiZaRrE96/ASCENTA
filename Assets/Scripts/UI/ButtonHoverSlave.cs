using UnityEngine;
using UnityEngine.UI;

public class ButtonHoverSlave : MonoBehaviour
{
    protected bool IsEnabled { get; private set; } = true;

    public virtual void OnHoverStart()
    {
    }

    public virtual void OnHoverEnd()
    {
    }

    public virtual void OnClick()
    {
    }

    public virtual void OnButtonEnabled()
    {
        IsEnabled = true;
    }

    public virtual void OnButtonDisabled()
    {
        IsEnabled = false;
    }
}
