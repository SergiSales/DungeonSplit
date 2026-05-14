using UnityEngine;

public class Skeleton : EnemyBase
{
    
    void Start()
    {
        currentHealth = maxHealth;
        damage = 15;
        moveSpeed = 2.5f;
        ranged = false;
        playerTarget = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        ChasePlayer(playerTarget);
    }

    public override void TakeDamage(int amount)
    {
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
        //animation die 
        Destroy(gameObject, 2f);
    }
    public override void ChasePlayer(Transform player)
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f; // Mantener solo la dirección horizontal

        Vector3 separation = Vector3.zero;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            buffer
        );

        for (int i = 0; i < count; i++)
        {
            Collider c = buffer[i];

            if (c.gameObject == gameObject) continue;

            Vector3 diff = transform.position - c.transform.position;
            diff.y = 0f; // Mantener solo la separación horizontal

            float dist = diff.magnitude;
            if (dist > 0.001f)
            {
                separation += diff / dist;
            }
        }

        Vector3 finalDir = (toPlayer + separation * separationForce).normalized;

        transform.position += finalDir * moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}