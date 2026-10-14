using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Textos da HUD (Partida)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Header("Painéis de Fim de Jogo")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Placar Final nos Painéis")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI victoryScoreText;

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    /// <summary>
    /// Atualiza o texto de pontuação contínua da HUD.
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Pontos: " + score;
        }
    }

    /// <summary>
    /// Atualiza o texto de vidas na HUD.
    /// </summary>
    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = "Vidas: " + lives;
        }
    }

    public void ShowGameOver(int score)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverScoreText != null) gameOverScoreText.text = "Pontuação Final: " + score;
    }

    public void ShowVictory(int score)
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (victoryScoreText != null) victoryScoreText.text = "Pontuação Final: " + score;
    }

    public void OnClickRestart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}