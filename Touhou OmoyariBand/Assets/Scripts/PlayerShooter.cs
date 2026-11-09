using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PlayerShooter : MonoBehaviour
{
    [Header("Configuração do Tiro")]
    [Tooltip("Prefab do projetil do Player (deve conter um Collider2D em Trigger e o script PlayerBullet).")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Ponto de origem de onde os tiros saem.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Intervalo entre disparos ao manter o botão pressionado (entre 0.2s e 1.0s).")]
    [Range(0.2f, 1.0f)]
    [SerializeField] private float fireRate = 0.25f;

    [Header("Mira por Clique")]
    [Tooltip("Se ligado, o Player só atira quando o cursor está sobre um alvo. Desligado = atira em qualquer lugar (comportamento antigo).")]
    [SerializeField] private bool requireClickOnTarget = true;

    [Tooltip("Tags dos objetos que podem ser alvo do clique. Adicione novas tags aqui (e crie-as antes em Tags & Layers).")]
    [SerializeField] private string[] targetTags = { "Boss" };

    [Header("Configurações do ObjectPool")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 50;

    private ObjectPool<GameObject> bulletPool;
    private float nextFireTime;

    private Camera cam;
    private ContactFilter2D contactFilter;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    private void Awake()
    {
        cam = Camera.main;

        // Detecta também colliders marcados como Trigger (o collider do Boss pode ser Trigger)
        contactFilter = new ContactFilter2D().NoFilter();
        contactFilter.useTriggers = true;

        // Inicializa o ObjectPool exigido pela atividade usando UnityEngine.Pool
        bulletPool = new ObjectPool<GameObject>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize
        );
    }

    private void Update()
    {
        // Durante a cutscene (diálogo) o Player não atira
        if (CutsceneState.IsActive) return;

        // Atira no primeiro clique ou continuamente ao segurar
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Transform target = null;

            if (requireClickOnTarget)
            {
                target = GetTargetUnderCursor();
                if (target == null) return; // clicou fora de qualquer alvo: não atira
            }

            nextFireTime = Time.time + fireRate;
            Shoot(target);
        }
    }

    /// <summary>
    /// Retorna o alvo (Transform com uma das tags) que está sob o cursor, ou null se não houver.
    /// </summary>
    private Transform GetTargetUnderCursor()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z); // necessário se a câmera for perspectiva
        Vector2 worldPoint = cam.ScreenToWorldPoint(mousePos);

        hits.Clear();
        Physics2D.OverlapPoint(worldPoint, contactFilter, hits);

        foreach (Collider2D col in hits)
        {
            Transform tagged = FindTaggedTransform(col.transform);
            if (tagged != null) return tagged;
        }

        return null;
    }

    // Aceita a tag no próprio objeto ou no pai (mesma lógica usada pelo PlayerBullet para o Boss)
    private Transform FindTaggedTransform(Transform t)
    {
        foreach (string tag in targetTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            if (t.CompareTag(tag)) return t;
            if (t.parent != null && t.parent.CompareTag(tag)) return t.parent;
        }

        return null;
    }

    private void Shoot(Transform target)
    {
        if (bulletPrefab == null) return;

        // Pega uma bala da pool
        GameObject bullet = bulletPool.Get();

        // Faz a bala perseguir o alvo clicado
        if (target != null && bullet.TryGetComponent(out PlayerBullet bulletScript))
        {
            bulletScript.SetTarget(target);
        }
    }

    // --- MÉTODOS DO OBJECT POOL ---

    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab);
        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();

        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<PlayerBullet>();
        }

        bulletScript.SetPool(bulletPool);
        return bullet;
    }

    private void OnGetBullet(GameObject bullet)
    {
        bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
        bullet.transform.rotation = firePoint != null ? firePoint.rotation : transform.rotation;
        bullet.SetActive(true);
    }

    private void OnReleaseBullet(GameObject bullet)
    {
        bullet.SetActive(false);
    }

    private void OnDestroyBullet(GameObject bullet)
    {
        Destroy(bullet);
    }
}
