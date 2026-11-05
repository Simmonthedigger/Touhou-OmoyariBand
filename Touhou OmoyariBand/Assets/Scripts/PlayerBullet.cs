using UnityEngine;
using UnityEngine.Pool;

public class PlayerBullet : MonoBehaviour
{
    [Header("Movimento Teleguiado")]
    [SerializeField] private float speed = 16f;
    [SerializeField] private float rotateSpeed = 500f; // Velocidade de curva
    [SerializeField] private float lifeTime = 4f;

    private ObjectPool<GameObject> pool;
    private float timer;
    private Transform target; // alvo perseguido (Boss por padrão, ou o alvo clicado)
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetPool(ObjectPool<GameObject> bulletPool)
    {
        pool = bulletPool;
    }

    /// <summary>
    /// Define o alvo que a bala vai perseguir (chamado pelo PlayerShooter ao atirar em um alvo clicado).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        target = newTarget;
        AimAtTarget();
    }

    private void OnEnable()
    {
        timer = 0f;
        target = null;

        // Padrão: busca o Boss na cena pela Tag "Boss"
        GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
        if (bossObj != null)
        {
            target = bossObj.transform;
            AimAtTarget();
        }
    }

    // Aponta a bala imediatamente na direção do alvo
    private void AimAtTarget()
    {
        if (target == null) return;

        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void FixedUpdate()
    {
        // Se o alvo estiver ativo na cena, ajusta a trajetória via física
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 direction = (Vector2)target.position - rb.position;
            direction.Normalize();

            float rotateAmount = Vector3.Cross(direction, transform.up).z;
            rb.angularVelocity = -rotateAmount * rotateSpeed;
            rb.linearVelocity = transform.up * speed;
        }
        else
        {
            // Se não houver alvo ativo, segue reto na direção atual
            rb.angularVelocity = 0f;
            rb.linearVelocity = transform.up * speed;
        }
    }

    private void Update()
    {
        // Devolve para a pool se expirar o tempo
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Colisão com o Boss ou colliders em objetos filhos do Boss
        if (collision.CompareTag("Boss") || (collision.transform.parent != null && collision.transform.parent.CompareTag("Boss")))
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddBossHitPoints();
            }

            ReturnToPool();
        }
        // Outro alvo clicado (que não é o Boss): a bala é consumida ao atingi-lo
        else if (target != null && (collision.transform == target || collision.transform.IsChildOf(target)))
        {
            // TODO: aplicar dano/pontos ao alvo aqui, quando existirem outros alvos
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (pool != null && gameObject.activeSelf)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            pool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}