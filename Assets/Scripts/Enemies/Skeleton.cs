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
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        
        Vector3 lookDirection = player.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
    }
}