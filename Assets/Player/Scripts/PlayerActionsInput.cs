using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2)]
public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions // Here we create methods for all specified Inputs in the Actions Input Map 
{
    public PlayerControls PlayerControls { get; private set; }

    // Variables that get set every time a select input is activated
    public bool AttackPressed { get; private set; }
    public float AttackHeld { get; private set; }
    public int WeaponNumber { get; private set; } = 0;

    private void OnEnable()
    {
        PlayerControls = new PlayerControls();
        PlayerControls.Enable();

        PlayerControls.PlayerActionMap.Enable();
        PlayerControls.PlayerActionMap.SetCallbacks(this);
    }

    private void OnDisable()
    {
        PlayerControls.PlayerActionMap.Disable();
        PlayerControls.PlayerActionMap.RemoveCallbacks(this);
    }
    private void LateUpdate()
    {
        AttackPressed = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        AttackHeld = context.ReadValue<float>();
        if (!context.performed)
            return;
        AttackPressed = true;
    }

    public void OnSwitchWeapon1(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        WeaponNumber = 0;
    }

    public void OnSwitchWeapon2(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        WeaponNumber = 1;
    }

    public void OnSwitchWeapon3(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        WeaponNumber = 2;
    }
}
