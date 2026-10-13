using UnityEngine;
using UnityEngine.Pool;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;

    private ObjectPool<GameObject> pool;
    private float timer;

    public void SetPool(ObjectPool<GameObject> bulletPool)
    {
        pool = bulletPool;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        // Move a bala para a frente / para cima
        transform.Translate(Vector3.up * speed * Time.deltaTime);

        // Devolve para a pool por tempo se sair da tela
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se acertar o Boss
        if (collision.CompareTag("Boss"))
        {
            // Adiciona os pontos de acerto
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddBossHitPoints();
            }

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (pool != null && gameObject.activeSelf)
        {
            pool.Release(gameObject);
        }
    }
}