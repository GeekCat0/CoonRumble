using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private EnemyAi[] enemies;
    [SerializeField] private GameObject blockWall;
    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelManager.checkIfAllowed();
            levelManager.SetCheckpoint(transform);
            levelManager.SetEnemies(enemies);

            if (blockWall != null)
                levelManager.SetWall(blockWall);
        }
    }
    public GameObject GetWall()
    {
        return blockWall;
    }
}
