using DG.Tweening;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Textos da HUD (Partida)")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Ícones de Vida (Coloque os 5 ícones na ordem)")]
    [SerializeField] private GameObject[] lifeIcons;

    [Header("Painéis de UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Placar Final nos Painéis")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI victoryScoreText;

    private int lastKnownLives = -1;

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

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Start()
    {
        // Sincroniza caso o PlayerHealth tenha iniciado antes da cena HUD terminar de carregar aditivamente
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null)
        {
            UpdateLives(player.GetCurrentLives());
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Pontos: " + score;
        }
    }

    /// <summary>
    /// Ativa/desativa os ícones de vida na HUD conforme a quantidade atual.
    /// </summary>
    public void UpdateLives(int lives)
    {
        if (lifeIcons == null || lifeIcons.Length == 0) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;

            bool shouldBeActive = i < lives;
            bool wasActive = lifeIcons[i].activeSelf;

            lifeIcons[i].SetActive(shouldBeActive);

            // Anima o ícone recém-ganho com um "pop" suave
            if (shouldBeActive && !wasActive && lastKnownLives >= 0)
            {
                lifeIcons[i].transform.DOKill();
                lifeIcons[i].transform.localScale = Vector3.zero;
                lifeIcons[i].transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        lastKnownLives = lives;
    }

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

    public void OnClick_LoadStageScene(string sceneName)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameplay(sceneName);
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