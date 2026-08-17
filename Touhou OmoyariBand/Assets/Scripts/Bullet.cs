using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetimeTimer;
    private float maxLifetime = 8f; // Tempo limite para desativar caso saia da tela

    public void Initialize(Vector2 dir, float spd, float lifetime = 8f)
    {
        direction = dir.normalized;
        speed = spd;
        maxLifetime = lifetime;
        lifetimeTimer = 0f;
        gameObject.SetActive(true);
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
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Colisão com o Player
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
            Deactivate();
        }
    }
}