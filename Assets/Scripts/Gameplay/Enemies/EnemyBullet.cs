using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 50;
    public int speed = 15;
    public float range = 10f;

    private PlayerStats enemy;

    public void SetStats(int d, int s)
    {
        
        damage = d;
        speed = s;
        range = 10f;
    }

    void Update()
    {
      if (!GameManager.instance.IsPlaying()) return;
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
          enemy.TakeDamage(damage);
          Destroy(gameObject);
        }
      }
      else if(other.CompareTag("Wall")){
        Destroy(gameObject);
      }
    }

    


}
