using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int maxLives = 5;
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
        currentLives = Mathf.Clamp(startingLives, 1, maxLives);

        // Notifica o GameManager sobre a vida inicial
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLivesUI(currentLives);
        }
    }

    /// <summary>
    /// Adiciona vida ao jogador até o limite máximo configurado (padrão: 5).
    /// </summary>
    public void AddLife(int amount = 1)
    {
        if (currentLives <= 0 || currentLives >= maxLives) return;

        currentLives = Mathf.Min(currentLives + amount, maxLives);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLivesUI(currentLives);
        }

        Debug.Log($"[PlayerHealth] Vida extra ganha! Vidas atuais: {currentLives}/{maxLives}");
    }

    public void TakeDamage(int damage = 1)
    {
        if (isInvincible) return;

        // Invulnerabilidade das habilidades (dash / escudo)
        if (PlayerAbilities.IsPlayerInvulnerable) return;

        currentLives = Mathf.Max(0, currentLives - damage);

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