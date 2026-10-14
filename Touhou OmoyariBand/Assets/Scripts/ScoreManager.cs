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
        currentScore += pointsPerSecond * Time.deltaTime;
        UpdateScoreUI();
    }

    /// <summary>
    /// Chamado quando o tiro do jogador atinge o Boss.
    /// </summary>
    public void AddBossHitPoints()
    {
        currentScore += pointsPerBossHit;
        UpdateScoreUI();
    }

    /// <summary>
    /// Chamado quando o jogador toma dano.
    /// </summary>
    public void DeductDamagePoints()
    {
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