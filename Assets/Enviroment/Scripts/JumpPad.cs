using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float force = 1;
    private PlayerMovement playerMovement;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerMovement = other.GetComponent<PlayerMovement>();
            playerMovement.Jump(playerMovement.GetVelocity().magnitude * force * 0.01f);
            playerMovement.StopDash();
        }
    }
}
