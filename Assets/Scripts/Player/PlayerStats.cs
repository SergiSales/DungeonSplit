using UnityEngine;
using UnityEngine.UI;


public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int expDrop = 50;
    private int exp;
    private int nextLevelExp;
    private int level = 1;
    private int[] expToNextLevel = {0, 400, 900, 1500, 2200, 3000, 3900, 4900, 6000, 7200, 8500, 9900, 11400, 13000, 14700};


    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider xpSlider;
    private float timerInvulnerable = 0f;
    private bool invulnerable = false;


    void Start()
    {
        level = 1;
        exp = 0;


        currentHealth = maxHealth;

        GameObject healthObj = GameObject.Find("PlayerHealth");
        healthSlider = healthObj.GetComponent<Slider>();

        GameObject xpObj = GameObject.Find("XP");
        xpSlider = xpObj.GetComponent<Slider>();

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;

        
        nextLevelExp = expToNextLevel[level];

        xpSlider.maxValue = nextLevelExp;
        xpSlider.value = 0;
    }

    void Update()
    {
        if (invulnerable == true)
        {
            timerInvulnerable += Time.deltaTime;
            if(timerInvulnerable >= 1f)
            {
                invulnerable = false;
                timerInvulnerable = 0;
            }
        }
        
    }

    public void TakeDamage(int amount)
    {
        if(invulnerable == false)
        {
            currentHealth -= amount;
            healthSlider.value = currentHealth;
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        
    }

    private void Die()
    {
        // Handle player death (e.g., respawn, game over, etc.)
        Debug.Log("Player has died.");
    }


    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            EnemyBase enemyAttack = other.GetComponent<EnemyBase>();
            if (enemyAttack != null)
            {
                TakeDamage(enemyAttack.damage);
            }
        }
    }
    public void addExp()
    {
        
        exp += expDrop;

        xpSlider.value = exp;

        if (exp >= nextLevelExp)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;

        exp = 0;

        currentHealth += maxHealth / 10;

        if (level < expToNextLevel.Length)
            nextLevelExp = expToNextLevel[level];
        else
            nextLevelExp = expToNextLevel[^1];

        xpSlider.maxValue = nextLevelExp;
        xpSlider.value = 0;

        GameManager.instance.SetLevelUpState();

        FindAnyObjectByType<Attacks>().StartUpgradeSelection();
    }





    




}
