using UnityEngine;
using UnityEngine.AI;

public enum EnemyState 
{
    Idling = 0,
    Patrolling = 1,
    Chasing = 2,
    Attacking = 3
}

public class EnemyAi : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private EnemyState enemyState = EnemyState.Idling;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject enemyBullet;
    private Health healthScript;

    [Header("Basic Stats")]
    [SerializeField] private float runSpeed;
    [SerializeField] private float walkSpeed;

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
    private bool alreadyAttacked;

    [Header("Range")]
    [SerializeField] private float sightRange;
    [SerializeField] private float attackRange;
    private bool playerInSightRange = false;
    private bool playerInAttackRange = false;
    private bool gotAttacked = false;


    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        healthScript = GetComponent<Health>();
        agent.speed = walkSpeed;
    }

    private void Update()
    {
        transform.LookAt(player);
        HandleEnemyState();
        HandleAgrro();

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
                AttackPlayer();
                break;
        }
    }

    private void HandleEnemyState()
    {
        // Check for sight and attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerLayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);

        if (!playerInSightRange && !playerInAttackRange && !canPatrol) enemyState = EnemyState.Idling;
        if (!playerInSightRange && !playerInAttackRange && canPatrol) enemyState = EnemyState.Patrolling;
        if (playerInSightRange && !playerInAttackRange) enemyState = EnemyState.Chasing;
        if (playerInAttackRange) enemyState = EnemyState.Attacking;

        if (gotAttacked)
        {
            if (playerInAttackRange)
                enemyState = EnemyState.Attacking;
            else
                enemyState = EnemyState.Chasing;
        }
    }

    private void Idling()
    {
        // Just chilling
        agent.SetDestination(transform.position);
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
        agent.SetDestination(player.position);
    }
    private void AttackPlayer()
    {
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            alreadyAttacked = true;
            Rigidbody rb = Instantiate(enemyBullet, transform.position, Quaternion.identity).GetComponent<Rigidbody>();

            Vector3 direction = player.position - rb.position;

            rb.AddForce(direction.normalized * forwardForce, ForceMode.Impulse);
            rb.AddForce(transform.up * upwardForce, ForceMode.Impulse);

            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    private void ResetAttack()
    {
        alreadyAttacked = false;
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
    private void ResetAgrro()
    {
        gotAttacked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }

}
