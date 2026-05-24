using Unity.VisualScripting;
using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    private LevelManager levelManager;
    [SerializeField] private CharacterController playerControler;

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        playerControler = FindAnyObjectByType<CharacterController>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerControler.enabled = false; // Disable the character controller to prevent physics issues when teleporting
            playerControler.gameObject.transform.position = levelManager.GetCheckpoint().position;
            playerControler.enabled = true;
        }
    }
}
