using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 50;
    public int speed = 15;
    public float range = 20f;

    private PlayerStats enemy;

    void Start()
    {
        
        damage = 50;
        speed = 15;
        range = 20f;
    }

    void Update()
    {
        // Mover el proyectil hacia adelante
        transform.Translate(Vector3.forward * Time.deltaTime * speed);
        

        if (Vector3.Distance(transform.position, transform.position) > range)
        {
            Destroy(gameObject);
        }
    }



    void OnTriggerEnter(Collider other){
      if(other.CompareTag("Player")){
        enemy = other.GetComponent<PlayerStats>();
        if(enemy!=null){
          Debug.Log("damage to: " + other.name);
          enemy.TakeDamage(damage);
          Destroy(gameObject);
        }
      }
      else if(other.CompareTag("Wall")){
        Destroy(gameObject);
      }
    }

    


}
