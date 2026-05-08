using TMPro;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private bool canGetKilled = true;
    [SerializeField] private float deathDelay = 1.0f;
    [SerializeField] private TextMeshProUGUI healthText;
    public bool tookDamage = false;

    public void Start()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
    }

    public void TakeDamage(int damage)
    {
        tookDamage = true;
        health -= damage;

        if (canGetKilled && health <= 0)
            Invoke(nameof(Delay), deathDelay);
        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
    }

    public void Delay()
    {
        Destroy(gameObject, deathDelay);
    }
}
