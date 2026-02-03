using UnityEngine;
using UnityEngine.UI;

public class TextVelocity : MonoBehaviour
{
    public Text text;
    public MovementController movementController;


    void FixedUpdate()
    {
        text.text = movementController.Velocity.ToString("F2");
    }
}