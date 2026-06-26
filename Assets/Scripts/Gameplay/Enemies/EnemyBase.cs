using UnityEngine;
using System.Collections;
public class EnemyBase : MonoBehaviour
{
    private const float MinPlanarMagnitude = 0.001f;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 10;

    [Header("Movement")]
    protected bool alive = true;
    public float moveSpeed = 3f;
    protected float acceleration = 10f;
    protected float rotationSpeed = 3f;
    protected float separationRadius = 1.5f;
    protected float separationForce = 2f;
    protected float preferredCombatDistance = 3f;
    protected float combatDistanceTolerance = 0.75f;
    protected float strafeStrength = 1f;
    protected float approachStrength = 1.35f;
    protected float retreatStrength = 1.6f;
    protected float crowdAvoidanceStrength = 2.25f;
    protected Vector3 velocity;
    protected static Collider[] buffer = new Collider[20];

    [Header("Combat")]
    public bool ranged = false;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float projectileSpeed = 5f;
    public GameObject expDropPrefab;

    protected bool dead = false;
    public Transform playerTarget;
    protected Rigidbody rb;
    protected float orbitDirection;


    public Renderer enemyRenderer;
    private Color originalColor;



    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        orbitDirection = Random.value < 0.5f ? -1f : 1f;
        originalColor = enemyRenderer.material.color;
    }

    protected bool TryEnsurePlayerTarget()
    {
        if (playerTarget != null)
        {
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return false;
        }

        playerTarget = player.transform;
        return true;
    }

    protected Vector3 GetEnemySeparationVector(float radiusOverride = -1f)
    {
        float radius = radiusOverride > 0f ? radiusOverride : separationRadius;
        if (radius <= MinPlanarMagnitude)
        {
            return Vector3.zero;
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer);
        Vector3 separation = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Collider candidate = buffer[i];
            if (candidate == null || candidate.gameObject == gameObject || !candidate.CompareTag("Enemy") || !candidate.CompareTag("Enemy"))
            if (candidate == null || candidate.gameObject == gameObject || !candidate.CompareTag("Enemy") || !candidate.CompareTag("Enemy"))
            {
                continue;
            }

            Vector3 diff = transform.position - candidate.transform.position;
            diff.y = 0f;
            float distance = diff.magnitude;
            if (distance <= MinPlanarMagnitude || distance > radius)
            {
                continue;
            }

            float strength = (radius - distance) / radius;
            separation += diff.normalized * strength;
        }

        return separation;
    }

    protected void MoveInDirection(Vector3 desiredDirection)
    {
        if (rb == null)
        {
            return;
        }

        desiredDirection.y = 0f;
        Vector3 currentPlanarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (desiredDirection.sqrMagnitude <= MinPlanarMagnitude * MinPlanarMagnitude)
        {
            Vector3 slowedVelocity = Vector3.Lerp(
                currentPlanarVelocity,
                Vector3.zero,
                acceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(slowedVelocity.x, rb.linearVelocity.y, slowedVelocity.z);
            velocity = rb.linearVelocity;
            return;
        }

        Vector3 targetVelocity = desiredDirection.normalized * moveSpeed;
        Vector3 blendedVelocity = Vector3.Lerp(
            currentPlanarVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(blendedVelocity.x, rb.linearVelocity.y, blendedVelocity.z);
        velocity = rb.linearVelocity;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized);
        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime));
    }

    public virtual void TakeDamage(int amount)
    {
    }

    public virtual void Attack()
    {
    }

    public virtual void Die()
    {
    }
    public virtual void ChasePlayer(Transform player)
    {
    }

    public IEnumerator DamageFlash()
    {
        enemyRenderer.material.color = Color.red;

        yield return new WaitForSeconds(0.15f);

        enemyRenderer.material.color = originalColor;
    }

}
