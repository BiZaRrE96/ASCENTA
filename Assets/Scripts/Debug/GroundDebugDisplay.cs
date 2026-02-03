using UnityEngine;
using UnityEngine.UI;

public class GroundDebugDisplay : MonoBehaviour
{
    [SerializeField] Text groundedText;
    [SerializeField] Text tractionText;
    [SerializeField] MovementController movementController;

    void Update()
    {
        if (movementController == null)
        {
            return;
        }

        if (groundedText != null)
        {
            groundedText.text = $"Grounded: {movementController.IsGrounded}\nNormal: {movementController.GroundNormal.ToString("F2")}";
        }

        if (tractionText != null)
        {
            tractionText.text = $"Traction: {movementController.CurrentTraction:F2}\nDamping: {movementController.CurrentSurfaceDamping:F2}";
        }
    }
}
