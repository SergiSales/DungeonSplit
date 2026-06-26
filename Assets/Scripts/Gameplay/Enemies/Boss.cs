using UnityEngine;
using UnityEngine.UI;

public class Boss : EnemyBase
{
    public Slider healthBar;
    public Image fill;
    public Image phase2Icon;
    public Image phase3Icon;


    int phase = 1;


    [Header("Minions")]
    public GameObject minionPrefab;
    public int maxMinions = 12;
    public float summonCooldown = 5f;
    float summonTimer;

    [Header("Attack")]
    public GameObject projectilePrefab;
    public float bossAttackCooldown = 1.5f;
    float attackTimer;

    [Header("Movement")]
    public float stopDistance = 7f;
    void Start()
    {
        maxHealth = 1000;
        currentHealth = maxHealth;
        damage = 40;
        moveSpeed = 2f;
        rotationSpeed = 10f;
        ranged = true;
        dead = false;
        preferredCombatDistance = 7f;
        TryEnsurePlayerTarget();

        GameObject bar = GameObject.FindGameObjectWithTag("HealthBoss");

        healthBar = bar.GetComponentInChildren<Slider>(true);
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
        healthBar.gameObject.SetActive(true);

        fill = healthBar.fillRect.GetComponent<Image>();

        phase2Icon = healthBar.transform.Find("Phase2").GetComponent<Image>();
        phase3Icon = healthBar.transform.Find("Phase3").GetComponent<Image>();

    }
    void Update()
    {
        if (!GameManager.instance.IsPlaying()) return;
        
        if (dead || playerTarget == null)
            return;

        
        HandleBehaviour();
        
    }

    void UpdatePhase()
    {
        float hp = (float)currentHealth / maxHealth;

        if (hp <= 0.33f)
        {
            phase = 3;
        }

        else if (hp <= 0.66f)
        {
            phase = 2;
        }

        else
        {
            phase = 1;
        }
            
    }


    void HandleBehaviour()
    {
        float distance =
            Vector3.Distance(transform.position, playerTarget.position);

        RotateToPlayer();

        switch (phase)
        {
            case 1:
                PhaseOne(distance);
                break;

            case 2:
                PhaseTwo(distance);
                break;

            case 3:
                PhaseThree(distance);
                break;
        }
    }

    void UpdateUI()
    {
        healthBar.value = currentHealth;
        float hp = (float)currentHealth / maxHealth;

        if (hp <= 0.33f)
        {
            Debug.Log("RED");
            fill.color = new Color(1f,0.05f,0);
            phase3Icon.transform.gameObject.SetActive(false);
        }

        else if (hp <= 0.66f)
        {
            Debug.Log("ORAGE");
            phase = 2;
            fill.color = new Color(1f, 0.5f, 0f);
            phase2Icon.transform.gameObject.SetActive(false);
        }

        else
        {
            Debug.Log("YELLOW");
            phase = 1;
        }
    }

    void Move(float distance)
    {
        Vector3 dir =
            (playerTarget.position - transform.position);

        dir.y = 0;

        if (distance > stopDistance)
        {
            transform.position +=
                dir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void RotateToPlayer()
    {
        Vector3 dir =
            playerTarget.position - transform.position;

        dir.y = 0;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    void PhaseOne(float distance)
    {
        Move(distance);

        attackTimer += Time.deltaTime;

        if (attackTimer >= bossAttackCooldown)
        {
            attackTimer = 0f;
            Shoot();
        }
    }

    void PhaseTwo(float distance)
    {
        Move(distance);

        summonTimer += Time.deltaTime;

        if (summonTimer >= summonCooldown)
        {
            summonTimer = 0f;
            SummonMinions();
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= bossAttackCooldown)
        {
            attackTimer = 0f;
            Shoot();
        }
    }

    void PhaseThree(float distance)
    {
        moveSpeed = 3.5f;

        Move(distance);

        attackTimer += Time.deltaTime;

        if (attackTimer >= bossAttackCooldown)
        {
            attackTimer = 0f;
            Shoot();
        }

        summonTimer += Time.deltaTime;

        if (summonTimer >= summonCooldown)
        {
            summonTimer = 0f;
            SummonMinions();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null)
            return;

        if(phase == 1)
        {
            PlayerStats target = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0;
            direction.Normalize();

            Vector3 dir = direction;
            Quaternion rotation = Quaternion.LookRotation(dir);

            Instantiate(projectilePrefab, transform.position, rotation);
            
        }
        else if (phase == 3)
        {
            PlayerStats target = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0;
            direction.Normalize();

            int bullets = 5;
            float spreadAngle = 45f;

            for (int i = 0; i < bullets; i++)
            {
                float angle = -spreadAngle + (spreadAngle * 2f / (bullets - 1)) * i;

                Vector3 dir = Quaternion.Euler(0, angle, 0) * direction;
                Quaternion rotation = Quaternion.LookRotation(dir);

                Instantiate(projectilePrefab, transform.position, rotation);
            }
        }

    }

    void SummonMinions()
    {
        if (minionPrefab == null)
            return;

        int current = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (current >= maxMinions)
            return;

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos =
                transform.position +
                Random.insideUnitSphere * 4f;

            spawnPos.y = 1;

            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        }
    }

    public override void Die()
    {
        dead = true;

        healthBar.gameObject.SetActive(false);

        GetComponent<Collider>().enabled = false;

        playerTarget
            .GetComponent<Attacks>()
            .GameOver();

        Destroy(gameObject, 3f);
    }

    public override void TakeDamage(int amount)
    {
        Debug.Log("Boss damaged: " + amount);
        currentHealth -= amount;
        if(currentHealth <= 0)
        {
            GetComponent<Collider>().enabled = false;
            Die();
        }
        UpdatePhase();
        UpdateUI();
    }

}
