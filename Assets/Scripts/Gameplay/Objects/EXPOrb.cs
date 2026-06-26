using Unity.VisualScripting;
using UnityEngine;

public class EXPOrb : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float detectionRange = 6f;       // Rango en el que la bola detecta al jugador
    public float baseSpeed = 2f;            // Velocidad inicial de movimiento
    public float acceleration = 8f;         // Qué tan rápido acelera mientras vuela hacia ti

    [Header("Experience Value")]
    public int expAmount = 10;              // Cuánta experiencia da esta gema

    private Transform playerTransform;
    private bool isBeingAttracted = false;
    private float currentSpeed;


    void Start()
    {
        // Buscamos al jugador UNA SOLA VEZ al aparecer para no perder rendimiento
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. Calcular la distancia matemática entre la bola y el jugador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 2. Si el jugador entra en rango, se activa el imán para siempre
        if (distanceToPlayer <= detectionRange)
        {
            isBeingAttracted = true;
        }

        // 3. Si está activado el imán, vuela hacia el jugador
        if (isBeingAttracted)
        {
            // Aumenta la velocidad con el tiempo (efecto latigazo/imán)
            currentSpeed += acceleration * Time.deltaTime;

            // Mueve la bola directamente hacia la posición del jugador
            transform.position = Vector3.MoveTowards(
                transform.position, 
                playerTransform.position, 
                currentSpeed * Time.deltaTime
            );
        }
    }

    void OnTriggerEnter(Collider other){
        
        if (other.CompareTag("player"))
        {
            PlayerStats p = playerTransform.gameObject.GetComponent<PlayerStats>();
            p.addExp();
            Destroy(gameObject);
        }
    }
}