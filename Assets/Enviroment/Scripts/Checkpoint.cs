using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private EnemyAi[] enemies;
    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelManager.SetCheckpoint(transform);
            levelManager.SetEnemies(enemies);
        }
    }
}
