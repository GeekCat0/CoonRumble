using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState 
{
    Idling = 0,
    Patrolling = 1,
    Chasing = 2,
    Attacking = 3,
    Stunned = 4
}

public class EnemyAi : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private EnemyState enemyState = EnemyState.Idling;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform playerLocation;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject enemyBullet;
    [SerializeField] private LayerMask rayLayerMask;
    private Health healthScript;
    private PlayerControler playerControler;

    [Header("Basic Stats")]
    [SerializeField] private float runSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private bool canWalk;
    [SerializeField] private bool melee;
    [SerializeField] private float maxVelocityToRecover;

    [Header("Patrolling stats")]
    [SerializeField] private Vector3 walkPoint;
    [SerializeField] private float walkPointRange;
    [SerializeField] private bool canPatrol;
    private bool walkPointSet;

    [Header("Attacking stats")]
    [SerializeField] private float timeBetweenAttacks;
    [SerializeField] private float aggroDuration;
    [SerializeField] private float forwardForce;
    [SerializeField] private float upwardForce;
    [SerializeField] private float attackDelay;
    [SerializeField] private int meleDamage;
    [SerializeField] private float spread;
    private bool alreadyAttacked;

    [Header("Range")]
    [SerializeField] private float sightRange;
    [SerializeField] private float attackRange;
    private bool playerInSightRange = false;
    private bool playerInAttackRange = false;
    private bool gotAttacked = false;
    private Vector3 spawnPoint;
    private Rigidbody rb;
    private bool stunned = false;

    private void Start()
    {
        spawnPoint = transform.position;
        playerLocation = FindAnyObjectByType<PlayerControler>().transform;
        playerControler = FindAnyObjectByType<PlayerControler>();
        agent = GetComponent<NavMeshAgent>();
        healthScript = GetComponent<Health>();
        agent.speed = walkSpeed;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        transform.LookAt(playerLocation);
        HandleEnemyState();
        HandleAgrro();
        if (!stunned)
        {
            switch (enemyState)
            {
                case EnemyState.Idling:
                    Idling();
                    break;

                case EnemyState.Patrolling:
                    Patroling();
                    break;

                case EnemyState.Chasing:
                    ChasePlayer();
                    break;

                case EnemyState.Attacking:
                    StartCoroutine(AttackPlayer());
                    break;
            }
        }
    }

    private void HandleEnemyState()
    {
        // Check for sight and attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerLayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);

        if (!playerInSightRange && !playerInAttackRange && !canPatrol) enemyState = EnemyState.Idling;
        if (!playerInSightRange && !playerInAttackRange && canPatrol && canWalk) enemyState = EnemyState.Patrolling;
        if (playerInSightRange && !playerInAttackRange && canWalk) enemyState = EnemyState.Chasing;
        if (playerInAttackRange) enemyState = EnemyState.Attacking;

        if (gotAttacked)
        {
            if (playerInAttackRange)
                enemyState = EnemyState.Attacking;
            else if (canWalk)
                enemyState = EnemyState.Chasing;
        }
    }

    private void Idling()
    {
        // Just chilling
        if (canWalk)
        {
            agent.SetDestination(transform.position);
        }
    }
    private void Patroling()
    {
        agent.speed = walkSpeed;
        // Searches for a random point in range and if it's on a ground layer then sets destination to it
        if (!walkPointSet) 
            SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }
    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
            walkPointSet = true;
    }
    private void ChasePlayer()
    {
        agent.speed = runSpeed;
        agent.SetDestination(playerLocation.position);
    }
    private IEnumerator AttackPlayer()
    {

        if (!alreadyAttacked)
        {
            if (!melee)
            {
                if (canWalk)
                    agent.SetDestination(transform.position);
                bool isHit = Physics.Raycast(transform.position, playerLocation.position - transform.position, out RaycastHit hit, attackRange, rayLayerMask, QueryTriggerInteraction.Ignore);
                alreadyAttacked = true;

                if (isHit && hit.collider.CompareTag("Hitbox"))
                {

                    Rigidbody rb = Instantiate(enemyBullet, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
                    Vector3 direction = playerLocation.position - rb.position;

                    direction += new Vector3(Random.Range(-spread, spread), Random.Range(-spread / 2, spread / 2), Random.Range(-spread, spread));

                    rb.AddForce(direction.normalized * forwardForce, ForceMode.Impulse);
                    rb.AddForce(transform.up * upwardForce, ForceMode.Impulse);
                }
            }
            else
            {
                alreadyAttacked = true;
                yield return new WaitForSeconds(attackDelay);
                agent.SetDestination(transform.position);
                if (playerInAttackRange)
                {
                    playerControler.GetComponent<Health>().TakeDamage(meleDamage);
                    Debug.Log("mele");
                }
            }
            yield return new WaitForSeconds(timeBetweenAttacks);
            alreadyAttacked = false;
        }
        yield return null;
    }
    public IEnumerator GetKnockedBack(Vector3 force)
    {
        stunned = true;
        enemyState = EnemyState.Idling;

        agent.enabled = false;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForFixedUpdate();
        yield return new WaitUntil(() => rb.linearVelocity.magnitude < maxVelocityToRecover);

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        agent.Warp(transform.position);
        agent.enabled = true;
        stunned = false;

        yield return null;
    }
    public void HandleAgrro()
    {
        if (healthScript.tookDamage)
        {
            healthScript.tookDamage = false;
            CancelInvoke(nameof(ResetAgrro));
            gotAttacked = true;
            Invoke(nameof(ResetAgrro), aggroDuration);
        }
    }
    public void ResetAgrro()
    {
        stunned = false;
        alreadyAttacked = false;
        gotAttacked = false;
        enemyState = EnemyState.Idling;
        StopAllCoroutines();
        rb.useGravity = false;
        rb.isKinematic = true;
        agent.enabled = true;
        agent.Warp(transform.position);
        if (canWalk)
            agent.SetDestination(transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
    public Vector3 GetSpawnPoint()
    {
        return spawnPoint;
    }

}
