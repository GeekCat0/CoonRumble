using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;
    private CharacterController characterController;
    private PlayerControler playerController;

    private EnemyAi[] enemies = { };
    private bool allEnemiesDead = true;
    private GameObject blockWall;

    public void Start()
    {
        Application.targetFrameRate = 165;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        allEnemiesDead = true;
        foreach (EnemyAi enemy in enemies)
        {
            if (enemy.isActiveAndEnabled)
                allEnemiesDead = false;
        }
        if (allEnemiesDead)
        {
            if (blockWall != null)
                blockWall.SetActive(false);

            currentCheckpoint = checkpoint;
            characterController = FindAnyObjectByType<CharacterController>();
            playerController = FindAnyObjectByType<PlayerControler>();
        }
    }
    public void ResetEnemies()
    {
        foreach (EnemyAi enemy in enemies)
        {
            enemy.gameObject.SetActive(true);
            enemy.gameObject.transform.position = enemy.GetSpawnPoint();
            enemy.ResetAgrro();
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
    public void SetWall(GameObject wall)
    {
        blockWall = wall;
    }
}
