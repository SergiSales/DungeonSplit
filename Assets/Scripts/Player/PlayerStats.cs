using UnityEngine;
using UnityEngine.UI;


public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int expDrop = 50;
    private int exp;
    private int level = 1;
    private int nextLevelExp;


    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider xpSlider;
    private float timerInvulnerable = 0f;
    private bool invulnerable = false;


    public Renderer r;
    private Color originalColor;

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

        
        nextLevelExp = GetNextLevelExp();

        xpSlider.maxValue = nextLevelExp;
        xpSlider.value = 0;

        originalColor = r.material.color;
    }

    void Update()
    {
        if (!GameManager.instance.IsPlaying()) return;
        if (invulnerable == true)
        {
            timerInvulnerable += Time.deltaTime;
            if(timerInvulnerable >= 1f)
            {
                invulnerable = false;
                timerInvulnerable = 0;
                r.material.color = originalColor;
            }
        }
        
    }

    public void TakeDamage(int amount)
    {
        if(invulnerable == false)
        {
            r.material.color = Color.red;
            currentHealth -= amount;
            healthSlider.value = currentHealth;
            invulnerable = true;
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        
    }

    private void Die()
    {
        GameManager.instance.state = GameState.GameEnd;
        StartCoroutine(GameManager.instance.SetGameOver(false));
    }


    void OnCollisionEnter(Collision other) {
        if(other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Boss"))
        {
            EnemyBase enemyAttack = other.gameObject.GetComponent<EnemyBase>();
            
            if (enemyAttack != null)
            {
                TakeDamage(enemyAttack.damage);
            }
        }
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
    private int GetNextLevelExp()
    {
        return Mathf.FloorToInt(400f * Mathf.Pow(level, 1.6f));
    }

    public void LevelUp()
    {
        level++;

        exp = 0;

        currentHealth += maxHealth / 10;
        if(currentHealth>maxHealth) currentHealth = maxHealth;

        nextLevelExp = GetNextLevelExp();

        xpSlider.maxValue = nextLevelExp;
        xpSlider.value = 0;

        GameManager.instance.SetLevelUpState();

        FindAnyObjectByType<Attacks>().StartUpgradeSelection();
    }





    




}
