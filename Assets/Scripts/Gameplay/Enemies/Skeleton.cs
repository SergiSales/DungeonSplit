using UnityEngine;

public class Skeleton : EnemyBase
{
    private const float EngageStrafeFactor = 0.35f;
    private const float PressureFactor = 0.25f;
    private const float MinDistanceToPlayer = 0.001f;

    private void Start()
    {
        maxHealth = 100;
        currentHealth = maxHealth;
        damage = 15;
        moveSpeed = 2.5f;
        rotationSpeed = 10f;
        ranged = false;
        dead = false;
        separationRadius = 1.8f;
        separationForce = 2f;
        preferredCombatDistance = 1.4f;
        combatDistanceTolerance = 0.4f;
        strafeStrength = 1.25f;
        approachStrength = 1f;
        retreatStrength = 1f;
        crowdAvoidanceStrength = 2.4f;
        TryEnsurePlayerTarget();
    }

    private void FixedUpdate()
    {
        if (dead || !TryEnsurePlayerTarget())
        {
            return;
        }

        ChasePlayer(playerTarget);
    }

    public override void TakeDamage(int amount)
    {
        UnityEngine.Debug.Log($"Skeleton took {amount} damage.");
        if (dead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public override void Die()
    {
        dead = true;
        //TODO: Implement death animation
        Destroy(gameObject, 1f);
        UnityEngine.Debug.Log("Skeleton died.");
    }


    public override void ChasePlayer(Transform player)
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= MinDistanceToPlayer)
        {
            MoveInDirection(Vector3.zero);
            return;
        }

        Vector3 radialDirection = toPlayer / distanceToPlayer;
        Vector3 strafeDirection = Vector3.Cross(Vector3.up, radialDirection) * orbitDirection;
        Vector3 avoidance = GetEnemySeparationVector() * separationForce * crowdAvoidanceStrength;
        Vector3 desiredDirection;

        float minPreferredDistance = Mathf.Max(0.1f, preferredCombatDistance - combatDistanceTolerance);
        float maxPreferredDistance = preferredCombatDistance + combatDistanceTolerance;

        if (distanceToPlayer > maxPreferredDistance)
        {
            desiredDirection =
                radialDirection * approachStrength +
                strafeDirection * strafeStrength * EngageStrafeFactor;
        }
        else if (distanceToPlayer < minPreferredDistance)
        {
            desiredDirection =
                -radialDirection * retreatStrength +
                strafeDirection * strafeStrength;
        }
        else
        {
            desiredDirection =
                strafeDirection * strafeStrength +
                radialDirection * PressureFactor;
        }

        MoveInDirection(desiredDirection + avoidance);
    }
}
