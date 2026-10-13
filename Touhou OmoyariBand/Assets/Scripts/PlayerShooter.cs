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

    [Header("Configurações do ObjectPool")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 50;

    private ObjectPool<GameObject> bulletPool;
    private float nextFireTime;

    private void Awake()
    {
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
        // Atira imediatamente no primeiro clique ou continuamente ao segurar
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null) return;

        // Pega uma bala da pool
        bulletPool.Get();
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