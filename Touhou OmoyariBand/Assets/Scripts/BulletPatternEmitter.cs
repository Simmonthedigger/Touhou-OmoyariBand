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
    [SerializeField] private float fireRate = 0.15f;           // Tempo em segundos entre disparos
    [SerializeField] private float startAngle = 0f;             // Ângulo inicial
    [SerializeField] private float angleOffsetPerShot = 10f;    // Rotação incremental (Espiral)
    [SerializeField] private float fanSpreadAngle = 60f;        // Abertura do leque (Fan)

    [Header("Controle de Rajada")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private bool loopPattern = true;

    private Transform playerTransform;
    private Coroutine currentPatternCoroutine;
    private float currentSpiralAngle;

    private void Awake()
    {
        FindPlayer();
    }

    private void OnEnable()
    {
        FindPlayer();
        currentSpiralAngle = startAngle;

        if (autoStart)
        {
            StartFiring();
        }
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
        // Garante a busca do Player caso ele tenha renascido
        FindPlayer();

        // Limpa qualquer rotina anterior para evitar duplicação de disparos
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
            currentPatternCoroutine = null;
        }

        // Reseta o ângulo do padrão espiral ao iniciar uma nova rajada
        currentSpiralAngle = startAngle;

        // Inicia a corrotina de tiros contínuos
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

    private void OnDisable()
    {
        StopFiring();
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

            // Aguarda o tempo do intervalo configurado
            yield return new WaitForSeconds(fireRate);

        } while (loopPattern);
    }

    // --- MÉTODOS DE CÁLCULO E DISPARO DANMAKU ---

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
        if (ObjectPoolManager.Instance == null) return;

        GameObject bulletObj = ObjectPoolManager.Instance.SpawnFromPool(bulletPoolTag, transform.position, Quaternion.identity);
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