using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Boss : EnemyBase
{
    public Slider healthBar;
    int phase = 1;
    public GameObject minionPrefab;
    int minionCount = 5;
    int maxMinions = 20;
    void Start()
    {
        maxHealth = 5000;
        currentHealth = maxHealth;
        damage = 40;
        moveSpeed = 2f;
        rotationSpeed = 10f;
        ranged = true;
        dead = false;
        preferredCombatDistance = 7f;
        TryEnsurePlayerTarget();
    }
    void Update()
    {
        float hpPercentage = (float)currentHealth / maxHealth;
        if (hpPercentage <= 0.75f && phase == 1)
        {
            phase = 2;
            EnterPhaseTwo();
        }
        else if (hpPercentage <= 0.5f && phase == 2)
        {
            phase = 3;
            EnterPhaseThree();
        }
    }


    void SummonMinions()
    {
        
    }

    IEnumerator Slam()
    {
        yield return new WaitForSeconds(1f);

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                6f
            );

        foreach (Collider hit in hits)
        {
            if(hit.CompareTag("Player"))
            {
                // daño
            }
        }
    }

    public override void Die()
    {
        dead = true;

        // Terminar la partida o mostrar la pantalla de victoria

        Destroy(gameObject, 5f);
    }


    public override void TakeDamage(int amount)
    {
        if (dead) return;

        currentHealth -= amount;
        healthBar.value = (float)currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void EnterPhaseTwo()
    {
        SummonMinions();
    }

    void EnterPhaseThree()
    {
        // Ataques mas rapidos y mas fuertes
    }

}
