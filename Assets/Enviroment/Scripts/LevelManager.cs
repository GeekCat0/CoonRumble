using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;
    private CharacterController characterController;
    private PlayerControler playerController;

    private EnemyAi[] enemies;
    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
        characterController = FindAnyObjectByType<CharacterController>();
        playerController = FindAnyObjectByType<PlayerControler>();
    }
    public void ResetEnemies()
    {
        foreach (EnemyAi enemy in enemies)
        {
            enemy.gameObject.SetActive(true);
            enemy.GetComponent<Health>().ResetHealth();
        }
    }
    public void LoadCheckpoint()
    {
        characterController.enabled = false; // Disable the character controller to prevent physics issues when teleporting
        characterController.gameObject.transform.position = currentCheckpoint.position;
        characterController.enabled = true;
        characterController.GetComponent<Health>().ResetHealth();
        playerController.SetPlatform(null);
        playerController.SetObjectToFollow(null);
        ResetEnemies();
    }
    public void SetEnemies(EnemyAi[] currentEnemies)
    {
        enemies = currentEnemies;
    }
}
