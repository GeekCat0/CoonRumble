using UnityEngine;
using System.Collections.Generic;

public class MeleWeapons : MonoBehaviour
{
    [SerializeField] private float meleForce;
    [SerializeField] private int meleDamage;
    [SerializeField] private float meleDelay;
    [SerializeField] private AudioSource hitMarker;
    private bool meleReady = true;
    PlayerActionsInput actionsInput;
    private List<Collider> enemiesInRange = new List<Collider>();

    private void Awake()
    {
        actionsInput = GetComponentInParent<PlayerActionsInput>();
    }
    private void Update()
    {
        if (actionsInput.MelePressed && meleReady)
        {
            for (int i = enemiesInRange.Count - 1; i >= 0; i--)
            {
                if (enemiesInRange[i] == null)
                {
                    enemiesInRange.RemoveAt(i);
                    continue;
                }
                hitMarker.Play();
                enemiesInRange[i].GetComponent<Health>().TakeDamage(meleDamage);
                StartCoroutine(enemiesInRange[i].GetComponent<EnemyAi>().GetKnockedBack(gameObject.transform.forward * meleForce + Vector3.up * meleForce));
                meleReady = false;
                Invoke(nameof(setAsReady), meleDelay);
            }
        }
    }
    private void setAsReady()
    { 
        meleReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !enemiesInRange.Contains(other))
        {
            enemiesInRange.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other);
        }
    }
}
