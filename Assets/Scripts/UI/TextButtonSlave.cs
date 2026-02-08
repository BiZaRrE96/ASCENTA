using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TextButtonSlave : ButtonHoverSlave
{
    [SerializeField] Graphic targetGraphic;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color hoveredColor = Color.white;
    [SerializeField] Color disabledColor = Color.gray;

    void Awake()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetGraphic != null)
        {
            normalColor = targetGraphic.color;
        }
    }

    public override void OnHoverStart()
    {
        if (!IsEnabled || targetGraphic == null)
        {
            return;
        }

        targetGraphic.color = hoveredColor;
    }

    public override void OnHoverEnd()
    {
        if (!IsEnabled || targetGraphic == null)
        {
            return;
        }

        targetGraphic.color = normalColor;
    }

    public override void OnButtonEnabled()
    {
        base.OnButtonEnabled();
        if (targetGraphic != null)
        {
            targetGraphic.color = normalColor;
        }
    }

    public override void OnButtonDisabled()
    {
        base.OnButtonDisabled();
        if (targetGraphic != null)
        {
            targetGraphic.color = disabledColor;
        }
    }
}
