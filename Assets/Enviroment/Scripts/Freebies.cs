using UnityEngine;

public class Freebies : MonoBehaviour
{
    [SerializeField] AudioSource soundEffect;
    [SerializeField] int pickableType = 0;
    [SerializeField] int healthAmount = 0;
    private LevelManager levelManager;
    void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (pickableType == 0)
            {
                levelManager.addFreebie();
                soundEffect.Play();
                Destroy(gameObject);
            }
            if (pickableType == 1)
            {
                other.GetComponent<Health>().addHealth(healthAmount);
                soundEffect.Play();
                Destroy(gameObject);
            }
        }
    }

}
