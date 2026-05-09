using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class RailMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector3 heightOffset = new Vector3(0, 1, 0);

    private bool isGrinding = false;
    private int direction = 1; // 1 for forward, -1 for backward
    private int end = 1; // 1 for end of the spline, 0 for start of the spline

    private SplineContainer spline;
    private SplineAnimate animatedObj;
    private PlayerState playerState;

    private float calculatedSpeed = 1f;

    private PlayerLocomotionInput playerLocomotionInput;
    private PlayerControler playerControler;

    private void Start()
    {
        playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        playerState = GetComponent<PlayerState>();
        playerControler = GetComponent<PlayerControler>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Rail") && !isGrinding)
        {
            if (Vector3.Dot(transform.forward, other.transform.forward) >= 0f)
                direction = 1;
            else
                direction = -1;
            spline = other.GetComponent<splinePart>().spline;
            animatedObj = other.GetComponent<splinePart>().animatedObj;
            isGrinding = true;
            playerState.SetPlayerMovementState(PlayerMovementState.Grinding);
            Grind();
        }
    }
    private void Grind()
    {
        SplineUtility.GetNearestPoint(spline.Spline, spline.transform.InverseTransformPoint(transform.position), out float3 nearestPoint, out float normalisedCurvePos, SplineUtility.PickResolutionDefault, 2);
        animatedObj.NormalizedTime = normalisedCurvePos * animatedObj.Duration;
        calculatedSpeed = speed / spline.Spline.GetLength();
        StartCoroutine(Grinding());
    }

    IEnumerator Grinding()
    {
        if (direction > 0)
            end = 1;
        else
            end = 0;

        while ((animatedObj.NormalizedTime + Time.deltaTime * calculatedSpeed < end && direction > 0) || (animatedObj.NormalizedTime - Time.deltaTime * calculatedSpeed > end && direction < 0))
        {
            if (playerLocomotionInput.JumpPressed)
            {
                Invoke(nameof(cooldown), 0.5f);
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                playerControler.Jump(2);
                yield break;
            }
            animatedObj.NormalizedTime += (Time.deltaTime * calculatedSpeed * direction);
            transform.position = animatedObj.transform.position + heightOffset;
            yield return null;
        }
        Invoke(nameof(cooldown), 0.5f);
        playerState.SetPlayerMovementState(PlayerMovementState.Idling);
        playerControler.Jump(1);
    }

    private void cooldown()
    {
        isGrinding = false;
    }

}
