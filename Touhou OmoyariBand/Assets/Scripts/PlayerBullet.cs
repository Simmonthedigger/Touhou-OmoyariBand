using UnityEngine;
using UnityEngine.Pool;

public class PlayerBullet : MonoBehaviour
{
    [Header("Movimento Teleguiado")]
    [SerializeField] private float speed = 16f;
    [SerializeField] private float rotateSpeed = 500f; // Ajusta a curva se o Boss se mover no ar
    [SerializeField] private float lifeTime = 4f;

    private ObjectPool<GameObject> pool;
    private float timer;
    private Transform targetBoss;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetPool(ObjectPool<GameObject> bulletPool)
    {
        pool = bulletPool;
    }

    private void OnEnable()
    {
        timer = 0f;

        // Busca o Boss na cena pela Tag "Boss"
        GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
        if (bossObj != null)
        {
            targetBoss = bossObj.transform;

            // --- APONTAR IMEDIATAMENTE PARA O BOSS AO NASCER ---
            Vector2 direction = (Vector2)targetBoss.position - (Vector2)transform.position;

            if (direction != Vector2.zero)
            {
                // Calcula o ângulo em graus para girar a bala na direção do Boss
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // -90 para alinhar o topo (Up) da bala
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    private void FixedUpdate()
    {
        // Se o Boss estiver ativo e se movendo, continua corrigindo a trajetória em tempo real
        if (targetBoss != null && targetBoss.gameObject.activeInHierarchy)
        {
            Vector2 direction = (Vector2)targetBoss.position - rb.position;
            direction.Normalize();

            float rotateAmount = Vector3.Cross(direction, transform.up).z;
            rb.angularVelocity = -rotateAmount * rotateSpeed;
            rb.linearVelocity = transform.up * speed;
        }
        else
        {
            // Se não houver Boss, segue em frente na direção em que nasceu
            rb.linearVelocity = transform.up * speed;
        }
    }

    private void Update()
    {
        // Devolve para a pool se expirar o tempo sem acertar nada
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se colidiu com o Boss (ou qualquer collider filho do Boss)
        if (collision.CompareTag("Boss") || (collision.transform.parent != null && collision.transform.parent.CompareTag("Boss")))
        {
            // Adiciona pontos no ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddBossHitPoints();
            }

            // Recolhe a bala de volta para a Pool
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