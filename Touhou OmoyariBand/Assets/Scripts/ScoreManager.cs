using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Configuração de Pontos (Inspector)")]
    [Tooltip("Quantidade de pontos ganhos por segundo decorrido.")]
    [SerializeField] private float pointsPerSecond = 10f;

    [Tooltip("Quantidade de pontos ganhos por cada tiro acertado no Boss.")]
    [SerializeField] private int pointsPerBossHit = 50;

    [Tooltip("Quantidade de pontos perdidos ao tomar dano.")]
    [SerializeField] private int pointsLostOnDamage = 100;

    [Tooltip("Permitir que a pontuação fique negativa ao perder pontos?")]
    [SerializeField] private bool allowNegativeScore = false;

    [Header("Vida Extra por Pontuação")]
    [Tooltip("A cada quantos pontos o jogador ganha +1 vida (0 = desativado).")]
    [SerializeField] private int pointsForExtraLife = 5000;

    private float currentScore;
    private int nextExtraLifeThreshold;
    private PlayerHealth playerHealth;

    public event System.Action<float, bool> PointsGained;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentScore = 0f;
        nextExtraLifeThreshold = pointsForExtraLife;
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        UpdateScoreUI();
    }

    private void Update()
    {
        float timePoints = pointsPerSecond * Time.deltaTime;
        currentScore += timePoints;
        PointsGained?.Invoke(timePoints, true);

        CheckExtraLife();
        UpdateScoreUI();
    }

    public void AddBossHitPoints()
    {
        currentScore += pointsPerBossHit;
        PointsGained?.Invoke(pointsPerBossHit, false);

        CheckExtraLife();
        UpdateScoreUI();
    }

    public void DeductDamagePoints()
    {
        if (PlayerAbilities.IsPlayerInvulnerable) return;

        currentScore -= pointsLostOnDamage;

        if (!allowNegativeScore && currentScore < 0f)
        {
            currentScore = 0f;
        }

        UpdateScoreUI();
    }

    private void CheckExtraLife()
    {
        if (pointsForExtraLife <= 0 || nextExtraLifeThreshold <= 0) return;

        while (currentScore >= nextExtraLifeThreshold)
        {
            nextExtraLifeThreshold += pointsForExtraLife;

            if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.AddLife(1);
            }
        }
    }

    public int GetCurrentScore()
    {
        return Mathf.FloorToInt(currentScore);
    }

    private void UpdateScoreUI()
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateScore(GetCurrentScore());
        }
    }
}