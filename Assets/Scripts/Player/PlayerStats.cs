using Unity.VisualScripting;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    private int maxHealth = 100;
    private int currentHealth;
    private int expDrop = 50;
    private int exp;
    private int level = 1;
    private int[] levelsExp = { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500 }; // Experience required for each level

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Handle player death (e.g., respawn, game over, etc.)
        Debug.Log("Player has died.");
    }


    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            EnemyBase enemyAttack = other.GetComponent<EnemyBase>();
            if (enemyAttack != null)
            {
                TakeDamage(enemyAttack.damage);
            }
        }
        else if (other.CompareTag("reward"))
        {
            addExp();
            Destroy(other.gameObject);
        }
    }

    void addExp()
    {
        exp += expDrop;
        if (level < levelsExp.Length && exp >= levelsExp[level-1])
        {
            level++;
            Debug.Log("Player leveled up to level " + level);
        }
    }
}
