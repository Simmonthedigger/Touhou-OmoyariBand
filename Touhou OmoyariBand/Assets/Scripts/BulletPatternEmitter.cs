using System.Collections;
using UnityEngine;

public class BulletPatternEmitter : MonoBehaviour
{
    public enum PatternType { Ring, Spiral, Fan, DirectToPlayer }

    [Header("Configurações do Pool")]
    [SerializeField] private string bulletPoolTag = "BossBulletRed";

    [Header("Tipo de Ataque")]
    [SerializeField] private PatternType patternType = PatternType.Ring;

    [Header("Parâmetros do Padrão")]
    [SerializeField] private int bulletCount = 16;             // Quantidade de tiros por onda
    [SerializeField] private float bulletSpeed = 5f;            // Velocidade dos tiros
    [SerializeField] private float fireRate = 0.2f;             // Tempo entre disparos
    [SerializeField] private float startAngle = 0f;             // Ângulo inicial
    [SerializeField] private float angleOffsetPerShot = 10f;    // Rotação incremental (usado na Espiral)
    [SerializeField] private float fanSpreadAngle = 60f;        // Abertura do leque (usado no Fan)

    [Header("Controle de Rajada")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private bool loopPattern = true;

    private Transform playerTransform;
    private Coroutine currentPatternCoroutine;
    private float currentSpiralAngle;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        currentSpiralAngle = startAngle;

        if (autoStart)
        {
            StartFiring();
        }
    }

    public void StartFiring()
    {
        if (currentPatternCoroutine != null) StopCoroutine(currentPatternCoroutine);
        currentPatternCoroutine = StartCoroutine(FireRoutine());
    }

    public void StopFiring()
    {
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
            currentPatternCoroutine = null;
        }
    }

    private IEnumerator FireRoutine()
    {
        do
        {
            switch (patternType)
            {
                case PatternType.Ring:
                    SpawnRing(startAngle);
                    break;

                case PatternType.Spiral:
                    SpawnRing(currentSpiralAngle);
                    currentSpiralAngle += angleOffsetPerShot;
                    break;

                case PatternType.Fan:
                    SpawnFan();
                    break;

                case PatternType.DirectToPlayer:
                    SpawnDirectToPlayer();
                    break;
            }

            yield return new WaitForSeconds(fireRate);

        } while (loopPattern);
    }

    // --- MÉTODOS DE CÁLCULO DE ÂNGULO E MATEMÁTICA DANMAKU ---

    private void SpawnRing(float baseAngle)
    {
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = baseAngle + (i * angleStep);
            Vector2 dir = DegreeToVector2(angle);
            FireBullet(dir);
        }
    }

    private void SpawnFan()
    {
        float startFan = -fanSpreadAngle / 2f;
        float angleStep = fanSpreadAngle / Mathf.Max(1, bulletCount - 1);

        // Se houver player, dispara em direção ao player com o espalhamento em leque
        float baseAngle = 270f; // Direção para baixo por padrão
        if (playerTransform != null)
        {
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = baseAngle + startFan + (i * angleStep);
            Vector2 dir = DegreeToVector2(angle);
            FireBullet(dir);
        }
    }

    private void SpawnDirectToPlayer()
    {
        Vector2 dir = Vector2.down;
        if (playerTransform != null)
        {
            dir = (playerTransform.position - transform.position).normalized;
        }
        FireBullet(dir);
    }

    private void FireBullet(Vector2 direction)
    {
        GameObject bulletObj = ObjectPoolManager.Instance.SpawnFromPool(bulletPoolTag, transform.position, Quaternion.identity);
        if (bulletObj != null && bulletObj.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.Initialize(direction, bulletSpeed);
        }
    }

    private Vector2 DegreeToVector2(float degree)
    {
        float radian = degree * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}