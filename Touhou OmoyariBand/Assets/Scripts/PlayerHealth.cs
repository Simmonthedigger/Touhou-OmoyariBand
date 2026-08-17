using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;

    [Header("Invencibilidade")]
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float flashInterval = 0.1f;
    private bool isInvincible = false;

    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        currentLives = maxLives;

        // Notifica o GameManager sobre a vida inicial
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLivesUI(currentLives);
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (isInvincible) return;

        currentLives -= damage;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLivesUI(currentLives);
        }

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        isInvincible = false;
    }

    private void Die()
    {
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    public int GetCurrentLives() => currentLives;
}
