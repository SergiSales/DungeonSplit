using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 10;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float acceleration = 10f;
    public float rotationSpeed = 5f;

    [Header("Combat")]
    public bool ranged = false;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float projectileSpeed = 5f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float loseTargetRange = 20f;

    [Header("Rewards")]
    public int xpReward = 10;
    public int goldReward = 5;
    public float dropChance = 0.5f;

    [Header("Runtime")]
    public bool dead = false;
    public Transform playerTarget;

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
}
