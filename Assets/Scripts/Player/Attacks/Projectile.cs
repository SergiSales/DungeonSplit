using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage;
    public int speed;
    public int pierce;
    public float range;

    private GameObject player;
    private EnemyBase enemy;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
    }

    void Update()
    {
        // Mover el proyectil hacia adelante
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        if (Vector3.Distance(transform.position, player.transform.position) > range)
        {
            Destroy(gameObject);
        }
        
    }

    public void setStats(int damage, int speed, int pierce)
    {
        this.damage = damage;
        this.speed = speed;
        this.pierce = pierce;
        this.range = 50f;
    }


    void OnTriggerEnter(Collider other){
      if(other.CompareTag("Enemy") || other.CompareTag("Boss")){
        enemy = other.GetComponent<EnemyBase>();
        if(enemy!=null){
          Debug.Log("damage to: " + other.name);
          enemy.TakeDamage(damage);
          pierce--;
          if(pierce <= 0){
            Destroy(gameObject);
          }
        }
      }
      else if(other.CompareTag("Wall")){
        Destroy(gameObject);
      }
    }
   


}
