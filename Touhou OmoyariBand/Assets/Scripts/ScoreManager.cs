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

    private float currentScore;

    /// <summary>
    /// Disparado sempre que o jogador GANHA pontos.
    /// Parâmetros: quantidade de pontos e se veio do tempo (true) ou de um acerto no Boss (false).
    /// Usado pelo ManaSystem para converter pontos em mana.
    /// </summary>
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
        UpdateScoreUI();
    }

    private void Update()
    {
        // Acumula pontos contínuos com base no tempo de jogo
        float timePoints = pointsPerSecond * Time.deltaTime;
        currentScore += timePoints;
        PointsGained?.Invoke(timePoints, true);
        UpdateScoreUI();
    }

    /// <summary>
    /// Chamado quando o tiro do jogador atinge o Boss.
    /// </summary>
    public void AddBossHitPoints()
    {
        currentScore += pointsPerBossHit;
        PointsGained?.Invoke(pointsPerBossHit, false);
        UpdateScoreUI();
    }

    /// <summary>
    /// Chamado quando o jogador toma dano.
    /// </summary>
    public void DeductDamagePoints()
    {
        // Durante a invulnerabilidade de uma habilidade (dash / escudo), não perde pontos
        if (PlayerAbilities.IsPlayerInvulnerable) return;

        currentScore -= pointsLostOnDamage;

        if (!allowNegativeScore && currentScore < 0f)
        {
            currentScore = 0f;
        }

        UpdateScoreUI();
    }

    /// <summary>
    /// Retorna a pontuação atual em formato inteiro.
    /// </summary>
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