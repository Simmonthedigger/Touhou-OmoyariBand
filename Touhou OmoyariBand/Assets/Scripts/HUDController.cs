using DG.Tweening;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Textos da HUD (Partida)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Header("Painéis de UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Placar Final nos Painéis")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI victoryScoreText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Garante que todos os painéis comecem desativados
        if (pausePanel != null) pausePanel.SetActive(false);
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

    /// <summary>
    /// Ativa ou desativa a exibição do Painel de Pausa.
    /// </summary>
    public void TogglePauseMenu(bool isPaused)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
            Debug.Log($"[HUDController] Painel de Pausa alterado para: {isPaused}");
        }
        else
        {
            Debug.LogError("[HUDController] ERRO: O objeto 'pausePanel' não está atribuído no Inspector da cena HUD!");
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

    // --- MÉTODOS DE BOTÕES DE UI ---

    public void OnClickResume()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    public void OnClickRestart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void OnClickMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }
}