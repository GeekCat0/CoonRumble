using UnityEngine;

public class DashAttack : MonoBehaviour
{
    [SerializeField] private string enemyTag;
    [SerializeField] private int damage = 0;
    [SerializeField] private float jumpForce = 0;

    private PlayerState playerState;
    private PlayerMovement playerMovement;
    void Awake()
    {
        playerState = GetComponentInParent<PlayerState>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag(enemyTag))
        {
            if (playerState.CurrentPlayerActionState == PlayerActionState.Dashing)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                playerMovement.StopDash();
                collision.GetComponent<Health>().TakeDamage(damage);

                playerMovement.Jump(jumpForce);
            }
        }
    }
}
