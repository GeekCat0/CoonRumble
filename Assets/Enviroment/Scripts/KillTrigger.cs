using Unity.VisualScripting;
using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelManager.LoadCheckpoint();
        }
    }
}
