using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerControler playerControler;
    [SerializeField] private Transform camTransform;
    [Header("Dash Stats")]
    [SerializeField] private float baseFOV = 60;
    [SerializeField] private float dashFOVBonus = 10;
    [SerializeField] private float FOVChangeSpeed = 30;
    [Header("WallRun Stats")]
    [SerializeField] private float rotationAmount = 5;
    [SerializeField] private float rotationSpeed = 10;
    private float targetRotation = 0;



    void LateUpdate()
    {
        WallRunCamAngle();
        DashFOVChange();
    }

    private void WallRunCamAngle()
    {
        camTransform.rotation = Quaternion.Euler(camTransform.rotation.eulerAngles.x, camTransform.rotation.eulerAngles.y, Mathf.LerpAngle(camTransform.rotation.eulerAngles.z, targetRotation, rotationSpeed * Time.deltaTime));

        if (playerState.CurrentPlayerMovementState == PlayerMovementState.WallRunning)
        {
            if (playerControler.wallRight)
            {
                targetRotation = rotationAmount;
            }
            else
            {
                targetRotation = -rotationAmount;
            }
        }
        else
            targetRotation = 0;
    }

    private void DashFOVChange()
    {
        if (playerState.CurrentPlayerActionState == PlayerActionState.Dashing)
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, baseFOV + dashFOVBonus, FOVChangeSpeed * Time.deltaTime);
        else
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, baseFOV, FOVChangeSpeed * Time.deltaTime);
    }
}
