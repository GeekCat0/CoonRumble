using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosion;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private string enemyTag;

    [Header("Stats")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0;
    [SerializeField] private bool useGravity = false;

    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionRange = 1;

    [SerializeField] private int maxCollisions = 1;
    [SerializeField] private float maxLifeTime = 1;
    [SerializeField] private bool explodeOnTouch = true;

    private int collisions;
    private PhysicsMaterial physicsMat;

    private void Start()
    {
        physicsMat = new PhysicsMaterial();
        physicsMat.bounciness = bounciness;
        physicsMat.frictionCombine = PhysicsMaterialCombine.Minimum;
        physicsMat.bounceCombine = PhysicsMaterialCombine.Maximum;

        GetComponent<SphereCollider>().material = physicsMat;
        rb.useGravity = useGravity;
    }

    private void Update()
    {
        if (collisions > maxCollisions)
            Explode();

        maxLifeTime -= Time.deltaTime;
        if (maxLifeTime <= 0)
            Explode();
    }

    private void Explode()
    {
        if (explosion != null)
            Instantiate(explosion, transform.position, Quaternion.identity);

        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, enemyLayer);
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].GetComponent<Health>().TakeDamage(explosionDamage);
            Debug.Log(enemies[i]);
        }
        Destroy(gameObject);

    }

    private void OnCollisionEnter(Collision collision)
    {
        collisions++;

        if (collision.collider.CompareTag(enemyTag) && explodeOnTouch)
            Explode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
