using UnityEngine;

public class OrbitBall : MonoBehaviour
{
    public Transform player;
    public float orbitRadius;
    public float orbitSpeed;
    public int ballIndex; // Índice para determinar posición inicial
    public int number; // Número total de bolas orbitando
    public int damage;
    public float attackCooldown;
    public float attackRange;
    
    private float orbitAngle;
    private EnemyBase enemy;
    private int lastKnownNumber = -1;

    void Start()
    {
        if (player == null)
        {
            player = transform.parent;
        }
        
        // Calcular ángulo inicial basado en el índice
        RecalculateAngle();
    }

    void Update()
    {
        // Recalcular ángulo si el número total de bolas cambió
        if (number != lastKnownNumber)
        {
            RecalculateAngle();
        }

        if (player == null)
            return;

        // Orbitar alrededor del jugador
        OrbitAroundPlayer();
    }

    void RecalculateAngle()
    {
        if (number > 0)
        {
            orbitAngle = (360f / number) * ballIndex;
            lastKnownNumber = number;
        }
    }

    void OrbitAroundPlayer()
    {
        // Incrementar ángulo basado en velocidad
        orbitAngle += orbitSpeed * Time.deltaTime;
        
        // Calcular posición en órbita
        float x = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        
        transform.position = player.position + new Vector3(x, 0.5f, z);
        
        // Rotar para mirar hacia el centro (hacia el jugador)
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }
    }

    void OnTriggerEnter(Collider other){
      if(other.CompareTag("Enemy")){
        enemy = other.GetComponent<EnemyBase>();
        if(enemy!=null){
          enemy.TakeDamage(damage);
        }
      }
    }

}
