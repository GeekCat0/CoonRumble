using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;
    [SerializeField] private CharacterController playerControler;

    private EnemyAi[] enemies;
    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
        enemies = FindObjectsByType<EnemyAi>();
        playerControler = FindAnyObjectByType<CharacterController>();
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
        playerControler.enabled = false; // Disable the character controller to prevent physics issues when teleporting
        playerControler.gameObject.transform.position = currentCheckpoint.position;
        playerControler.enabled = true;
        playerControler.GetComponent<Health>().ResetHealth();
        ResetEnemies();
    }
}
