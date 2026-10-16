using System.Collections;
using UnityEngine;

public class BulletPatternEmitter : MonoBehaviour
{
    public enum PatternType { Ring, Spiral, Fan, DirectToPlayer }

    [Header("Configurações do Pool e Prefab")]
    [SerializeField] private string bulletPoolTag = "BossBulletRed";
    [SerializeField] private GameObject fallbackBulletPrefab;

    [Header("Tipo de Ataque")]
    [SerializeField] private PatternType patternType = PatternType.Ring;

    [Header("Parâmetros do Padrão")]
    [SerializeField] private int bulletCount = 16;             // Quantidade de tiros por onda
    [SerializeField] private float bulletSpeed = 5f;            // Velocidade dos tiros
    [SerializeField] private float fireRate = 0.2f;            // Intervalo entre disparos contínuos
    [SerializeField] private float startAngle = 0f;             // Ângulo inicial
    [SerializeField] private float angleOffsetPerShot = 10f;    // Rotação incremental (Espiral)
    [SerializeField] private float fanSpreadAngle = 60f;        // Abertura do leque (Fan)

    private Transform playerTransform;
    private Coroutine currentPatternCoroutine;
    private float currentSpiralAngle;
    private bool isFiring = false;

    private void Awake()
    {
        FindPlayer();
    }

    private void OnEnable()
    {
        FindPlayer();
        currentSpiralAngle = startAngle;
        StartFiring(); // Inicia o disparo automaticamente assim que ativado pelo BossController
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    public void StartFiring()
    {
        FindPlayer();

        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }

        isFiring = true;
        currentSpiralAngle = startAngle;
        currentPatternCoroutine = StartCoroutine(FireRoutine());
    }

    public void StopFiring()
    {
        isFiring = false;
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
            currentPatternCoroutine = null;
        }
    }

    private void OnDisable()
    {
        StopFiring();
    }

    private IEnumerator FireRoutine()
    {
        // Roda continuamente enquanto a flag isFiring for verdadeira
        while (isFiring)
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

            // Aguarda o intervalo antes de disparar a próxima onda contínua
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void SpawnRing(float baseAngle)
    {
        float angleStep = 360f / Mathf.Max(1, bulletCount);

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

        float baseAngle = 270f;
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
        GameObject bulletObj = null;

        if (ObjectPoolManager.Instance != null)
        {
            bulletObj = ObjectPoolManager.Instance.SpawnFromPool(bulletPoolTag, transform.position, Quaternion.identity);
        }

        if (bulletObj == null && fallbackBulletPrefab != null)
        {
            bulletObj = Instantiate(fallbackBulletPrefab, transform.position, Quaternion.identity);
        }

        if (bulletObj != null && bulletObj.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.gameObject.SetActive(true);
            bullet.Initialize(direction, bulletSpeed);
        }
    }

    private Vector2 DegreeToVector2(float degree)
    {
        float radian = degree * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}