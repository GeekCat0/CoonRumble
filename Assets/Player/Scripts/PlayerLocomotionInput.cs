using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2)]
public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions  // Here we create methods for all specified Inputs in the Locomotion Input Map 
{
    public PlayerControls PlayerControls {get; private set;}

    // Variables that get set every time a select input is activated
    public Vector2 MovementInput { get; private set;}
    public Vector2 LookInput { get; private set;}
    public bool JumpPressed { get; private set;}
    public bool DashPressed { get; private set; }
    public bool SlideHeld { get; private set; }

    private void OnEnable() 
    {
        PlayerControls = new PlayerControls();
        PlayerControls.Enable();

        PlayerControls.PlayerLocomotionMap.Enable();
        PlayerControls.PlayerLocomotionMap.SetCallbacks(this);
    }

    private void OnDisable()
    {
        PlayerControls.PlayerLocomotionMap.Disable();
        PlayerControls.PlayerLocomotionMap.RemoveCallbacks(this);
    }

    private void LateUpdate()
    {
        JumpPressed = false;
        DashPressed = false;
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        MovementInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        JumpPressed = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        DashPressed = true;
    }

    public void OnSlide(InputAction.CallbackContext context)
    {
        SlideHeld = context.ReadValue<float>() > 0;
    }
}
