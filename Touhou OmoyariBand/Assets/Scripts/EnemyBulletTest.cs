using UnityEngine;

public class EnemyBulletTest : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Vector2 direction = Vector2.down;

    private void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se colidiu com o Player ou com a Hitbox do Player
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
            Destroy(gameObject); // Destrói o tiro de teste
        }
    }
}