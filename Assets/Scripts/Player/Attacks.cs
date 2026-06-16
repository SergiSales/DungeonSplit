using UnityEngine;

public class Attacks : MonoBehaviour
{
  [Header("Combat")]
  public bool autoAttack = false;
  [Header("BasicShot")]
  public GameObject basicShotPrefab;
  public float basicShotCooldown = 1f;
  public float basicShotRange = 20f;
  public int basicShotDamage = 25;
  public int basicShotSpeed = 10;
  public int basicShotPierce = 0;
  public int basicShotNumber = 1;
  float basicShotTimer;
  
  void Start()
  {
      basicShotTimer = 0f;
  }

  // Update is called once per frame
  void Update()
  {
      UpdateAutoAttackState();

      basicShotTimer += Time.deltaTime;
      if(basicShotTimer > basicShotCooldown && autoAttack)
      {
          basicShotTimer = 0f;
          BasicShot();
      }
  }

  void UpdateAutoAttackState()
  {
      // Verificar si estamos en una sala Wave
      bool isInWaveRoom = GameManager.instance != null && GameManager.instance.IsInWaveRoom();
      
      // Detectar si hay enemigos cerca
      bool hasEnemiesNearby = false;
      if (isInWaveRoom)
      {
          Collider[] hits = Physics.OverlapSphere(transform.position, 15f);
          foreach (Collider hit in hits)
          {
              if (hit.CompareTag("Enemy"))
              {
                  hasEnemiesNearby = true;
                  break;
              }
          }
      }
      
      // Actualizar autoAttack: true si estamos en Wave Y hay enemigos
      autoAttack = isInWaveRoom && hasEnemiesNearby;
  }

    void BasicShot()
    {
        Transform target = GetClosestEnemy();

        if (target == null)
            return;

        Vector3 direction =
            (target.position - transform.position).normalized;

        

        float spreadAngle = 15f;

        // Disparo central
        SpawnProjectile(direction);

        // Disparos extra
        int extras = basicShotNumber - 1;

        for (int i = 0; i < extras; i++)
        {
            int side = (i % 2 == 0) ? 1 : -1;

            int layer = (i / 2) + 1;

            float angle = spreadAngle * layer * side;

            Vector3 dir =
                Quaternion.Euler(0, angle, 0) * direction;

            SpawnProjectile(dir);
        }
    }

    void SpawnProjectile(Vector3 direction)
    {
        Quaternion rotation =
        Quaternion.LookRotation(direction) * Quaternion.Euler(0, 0, 0);

        GameObject p = Instantiate(
            basicShotPrefab,
            transform.position,
            rotation
        );

        p.GetComponent<Projectile>()
        .setStats(
            basicShotDamage,
            basicShotSpeed,
            basicShotPierce
        );
    }
  Transform GetClosestEnemy()
  {
      Collider[] hits = Physics.OverlapSphere(
          transform.position,
          basicShotRange
      );

      Transform closest = null;
      float closestDistance = float.MaxValue;

      foreach (Collider hit in hits)
      {
          if (!hit.CompareTag("Enemy"))
              continue;

          float distance = Vector3.Distance(
              hit.transform.position,
              transform.position
          );

          if (distance < closestDistance)
          {
              closestDistance = distance;
              closest = hit.transform;
          }
      }

      return closest;
  }


}
