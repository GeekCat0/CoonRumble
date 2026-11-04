using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float force = 1;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerMovement>().Jump(other.GetComponent<PlayerMovement>().GetVelocity().magnitude * force * 0.01f);

        }
    }
}
