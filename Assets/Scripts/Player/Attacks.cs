using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Attacks : MonoBehaviour
{
  [Header("Combat")]
  public bool autoAttack = false;
  public GameObject[] attackPrefabs;
  
  [Header("BasicShot")]
    public float basicShotCooldown = 1f;
    public float basicShotRange = 20f;
    public int basicShotDamage = 25;
    public int basicShotSpeed = 10;
    public int basicShotPierce = 0;
    public int basicShotNumber = 1;
    float basicShotTimer;

    [Header("OrbitBall")]
    private float orbitBallRadius = 3f;
    public float orbitBallHeight = 1f;
    public int orbitBallDamage = 10;
    public int orbitBallSpeed = 180;
    public int orbitBallNumber = 0;

    private List<OrbitBall> orbitBalls = new List<OrbitBall>();

    [Header("Chain Lightning")]
    public bool chainLightningEnabled = false;
    public int lightningDamage = 20;
    public int segments = 5;
    public float lightningRange = 6f;
    public float lightningCooldown = 3f;
    public float lightningLifetime = 0.2f;
    float lightningTimer;
  
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

        if(orbitBallNumber > orbitBalls.Count && autoAttack)
        {
            UpdateOrbitBalls();
        }

        if (chainLightningEnabled)
        {
            lightningTimer += Time.deltaTime;

            if(lightningTimer >= lightningCooldown && autoAttack)
            {
                lightningTimer = 0f;
                StartCoroutine(ChainLightning());
            }
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

#region Basic Shot
    void BasicShot()
    {
        EnemyBase target = GetClosestEnemy(transform);

        if (target == null)
            return;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0; // Mantener el disparo en el plano horizontal
        direction.Normalize();

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
            attackPrefabs[0],
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

#endregion

#region Orbit Balls
    public void UpdateOrbitBalls()
    {
        // Añadir una nueva bola si es necesario
        if (orbitBalls.Count < orbitBallNumber)
        {
            SpawnOrbitBall(orbitBalls.Count);
        }
        
        // Actualizar todas las bolas con sus parámetros correctos
        for (int i = 0; i < orbitBalls.Count; i++)
        {
            if (orbitBalls[i] != null)
            {
                orbitBalls[i].ballIndex = i;
                orbitBalls[i].number = orbitBallNumber;
                orbitBalls[i].orbitRadius = orbitBallRadius;
                orbitBalls[i].orbitSpeed = orbitBallSpeed;
                orbitBalls[i].damage = orbitBallDamage;
            }
        }
    }
    
    void SpawnOrbitBall(int ballIndex)
    {
        if (attackPrefabs[1] == null)
        {
            Debug.LogError("OrbitBall prefab no asignado en Attacks.cs");
            return;
        }
        
        // Instanciar la bola
        GameObject newBall = Instantiate(
            attackPrefabs[1],
            transform.position,
            Quaternion.identity,
            transform
        );
        
        // Configurar el script OrbitBall
        OrbitBall orbitScript = newBall.GetComponent<OrbitBall>();
        if (orbitScript != null)
        {
            orbitScript.player = transform;
            orbitScript.ballIndex = ballIndex;
            orbitScript.orbitRadius = orbitBallRadius;
            orbitScript.orbitSpeed = orbitBallSpeed;
            orbitScript.damage = orbitBallDamage;
            orbitScript.number = orbitBallNumber;
        }
        else
        {
            Debug.LogWarning("El prefab de OrbitBall no tiene el script OrbitBall.cs");
        }
        
        orbitBalls.Add(orbitScript);
    }
  
#endregion



#region Chain Lightning
    IEnumerator ChainLightning()
    {
        EnemyBase current = GetClosestEnemy(transform);

        if (current == null)
            yield break;

        HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

        Vector3 fromPos = transform.position;

        for (int i = 0; i < segments; i++)
        {
            if (current == null)
                yield break;

            Vector3 targetPos = current.transform.position;

            CreateLightning(fromPos, targetPos);

            current.TakeDamage(lightningDamage);
            hitEnemies.Add(current);

            fromPos = targetPos;

            current = FindNextTarget(fromPos, hitEnemies);

            
            yield return new WaitForSeconds(lightningLifetime/2);
        }
    }
    void CreateLightning(Vector3 start, Vector3 end)
    {
        GameObject obj = Instantiate(attackPrefabs[2], start, Quaternion.identity);

        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.positionCount = segments;

        StartCoroutine(LightningTravel(lr, start, end));
    }

    Vector3 GetJitter(float intensity = 0.2f)
    {
        return new Vector3(
            Random.Range(-intensity, intensity),
            Random.Range(-intensity, intensity),
            Random.Range(-intensity, intensity)
        );
    }


    IEnumerator LightningTravel(LineRenderer lr, Vector3 start, Vector3 end)
    {
        int segments = lr.positionCount;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            Vector3 point = Vector3.Lerp(start, end, t);
            point += GetJitter(0.15f);

            lr.SetPosition(i, point);
        }

        yield return new WaitForSeconds(lightningLifetime);

        Destroy(lr.gameObject);
    }

#endregion
    
    
    
    
    EnemyBase GetClosestEnemy(Transform t)
    {
        Collider[] hits =
            Physics.OverlapSphere(t.position, 20f);

        EnemyBase closest = null;
        float closestDistance = Mathf.Infinity;

        foreach(Collider hit in hits)
        {
            if(!hit.CompareTag("Enemy"))
                continue;

            EnemyBase enemy =
                hit.GetComponent<EnemyBase>();

            if(enemy == null)
                continue;

            float dist =
                Vector3.SqrMagnitude(
                    hit.transform.position - t.position
                );

            if(dist < closestDistance)
            {
                closestDistance = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    EnemyBase FindNextTarget(Vector3 position, HashSet<EnemyBase> alreadyHit)
    {
        Collider[] hits =
            Physics.OverlapSphere(
                position,
                lightningRange
            );

        EnemyBase closest = null;

        float closestDistance = Mathf.Infinity;

        foreach(Collider hit in hits)
        {
            if(!hit.CompareTag("Enemy"))
                continue;

            EnemyBase enemy =
                hit.GetComponent<EnemyBase>();

            if(enemy == null)
                continue;

            if(alreadyHit.Contains(enemy))
                continue;

            float dist =
                Vector3.SqrMagnitude(
                    hit.transform.position - position
                );

            if(dist < closestDistance)
            {
                closestDistance = dist;
                closest = enemy;
            }
        }

        return closest;
    }






}

