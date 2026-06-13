using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;
    [SerializeField] private TextMeshProUGUI pointsCounter;
    private CharacterController characterController;
    private PlayerControler playerController;

    [SerializeField] private EnemyAi[] enemies = { };
    private bool allEnemiesDead = true;
    private GameObject blockWall;
    private int freebies = 0;

    public void Start()
    {
        Application.targetFrameRate = 60;
        pointsCounter.text = "Trash: " + freebies;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (allEnemiesDead)
        {
            if (blockWall != null)
                blockWall.SetActive(false);
            
            currentCheckpoint = checkpoint;
            characterController = FindAnyObjectByType<CharacterController>();
            playerController = FindAnyObjectByType<PlayerControler>();
        }
    }
    public void checkIfAllowed()
    {
        allEnemiesDead = true;
        foreach (EnemyAi enemy in enemies)
        {
            if (enemy.isActiveAndEnabled)
                allEnemiesDead = false;
        }
    }
    public void ResetEnemies()
    {
        foreach (EnemyAi enemy in enemies)
        {
            enemy.StopAllCoroutines();
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
        if (allEnemiesDead)
        {
            enemies = currentEnemies;
        }
    }
    public void SetWall(GameObject wall)
    {
        if (allEnemiesDead)
            blockWall = wall;
    }
    public void addFreebie()
    {
        freebies++;
        pointsCounter.text = "Trash: " + freebies;
    }
}
