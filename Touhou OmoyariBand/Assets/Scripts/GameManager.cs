using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Interface (UI)")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Placar Final (UI)")]
    [Tooltip("Texto no Painel de Vitória para exibir os pontos finais.")]
    [SerializeField] private TextMeshProUGUI victoryScoreText;
    [Tooltip("Texto no Painel de Derrota para exibir os pontos finais.")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;

    [Header("Música e Boss")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private BossController boss;

    private bool isGameOver = false;

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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Start()
    {
        // Procura componentes na cena se não forem arrastados no Inspector
        if (musicSource == null) musicSource = FindFirstObjectByType<AudioSource>();
        if (boss == null) boss = FindFirstObjectByType<BossController>();
    }

    public void UpdateLivesUI(int lives)
    {
        if (livesText != null)
        {
            livesText.text = "Vidas: " + lives;
        }
    }

    /// <summary>
    /// Chamado quando o jogador perde todas as vidas.
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        StopGameElements();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Pontuação Final: " + finalScore;
        }
    }

    /// <summary>
    /// Chamado quando o Boss morre ou a partida/música chega ao fim com sucesso.
    /// </summary>
    public void Victory()
    {
        if (isGameOver) return;
        isGameOver = true;

        StopGameElements();

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;
        if (victoryScoreText != null)
        {
            victoryScoreText.text = "Pontuação Final: " + finalScore;
        }
    }

    /// <summary>
    /// Para a música, desativa os padrões de tiro e movimentação do Boss.
    /// </summary>
    private void StopGameElements()
    {
        // Para a música da partida
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        // Para ataques do Boss
        if (boss != null)
        {
            boss.StopAllPatterns();

            // Pausa o Animator do Boss para parar animações
            Animator bossAnimator = boss.GetComponent<Animator>();
            if (bossAnimator != null) bossAnimator.enabled = false;
        }
    }

    /// <summary>
    /// Recarrega a cena de Gameplay e a cena de HUD.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        // Recarrega a cena ativa (Gameplay)
        string activeSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(activeSceneName);
    }
}