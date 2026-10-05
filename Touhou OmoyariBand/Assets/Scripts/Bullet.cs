using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetimeTimer;
    private float maxLifetime = 8f;

    // Guardamos a referência do pool nativo de onde o tiro veio
    private IObjectPool<GameObject> myPool;

    public void SetPoolReference(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }

    public void Initialize(Vector2 dir, float spd, float lifetime = 8f)
    {
        direction = dir.normalized;
        speed = spd;
        maxLifetime = lifetime;
        lifetimeTimer = 0f;
    }

    private void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime), Space.World);

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Deactivate();
        }
    }

    public void Deactivate()
    {
        // Se temos a referência nativa da pool, usamos Release(), caso contrário apenas desativamos
        if (myPool != null)
        {
            myPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
            Deactivate();
        }
    }
}