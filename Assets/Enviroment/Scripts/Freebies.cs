using UnityEngine;

public class Freebies : MonoBehaviour
{
    [SerializeField] AudioSource soundEffect;
    private LevelManager levelManager;
    void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelManager.addFreebie();
            soundEffect.Play();
            Destroy(gameObject);
        }
    }

}
