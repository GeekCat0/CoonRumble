using TMPro;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health = 100;
    [SerializeField] private bool canGetKilled = true;
    [SerializeField] private float deathDelay = 1.0f;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private bool isPlayer = false;
    [SerializeField] private Animator anim;
    public bool tookDamage = false;
    private LevelManager levelManager;

    public void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        ResetHealth();
        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
    }

    public void TakeDamage(int damage)
    {
        if (anim != null && health > 0) anim.Play("EnemyHurt");
        tookDamage = true;
        health -= damage;

        if (!isPlayer && canGetKilled && health <= 0)
        {
            GetComponent<EnemyAi>().enabled = false;
            if (anim != null) anim.SetBool("Dead", true);
            Invoke(nameof(Delay), deathDelay);
        }

        if (isPlayer && health <= 0 && canGetKilled)
        {
            levelManager.LoadCheckpoint();
            ResetHealth();
        }

        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
    }
    public void ResetHealth()
    {
        if (!isPlayer) GetComponent<EnemyAi>().enabled = true;
        if (anim != null) anim.SetBool("Dead", false);
        health = maxHealth;
        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
    }

    public void Delay()
    {
        gameObject.SetActive(false);
        ResetHealth();
    }
}
