using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Música e Boss")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private BossController boss;

    [Header("Cena de Reinicialização")]
    [SerializeField] private string bootSceneName = "Boot";

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
    }

    private void Start()
    {
        if (musicSource == null) musicSource = FindAnyObjectByType<AudioSource>();
        if (boss == null) boss = FindAnyObjectByType<BossController>();
    }

    public void UpdateLivesUI(int lives)
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateLives(lives);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        StopGameElements();

        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowGameOver(finalScore);
        }
    }

    public void Victory()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 1. Destrói o Boss e todos os tiros em tela
        CleanUpEnemiesAndBullets();

        // 2. Para a música e congela o tempo
        StopGameElements();

        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowVictory(finalScore);
        }
    }

    /// <summary>
    /// Destrói o Boss e limpa todas as balas em tela ao vencer.
    /// </summary>
    private void CleanUpEnemiesAndBullets()
    {
        // Destrói o GameObject do Boss
        if (boss != null)
        {
            boss.StopAllPatterns();
            Destroy(boss.gameObject);
            boss = null;
        }
        else
        {
            // Busca caso o Boss tenha outro nome/tag e destrua
            GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj != null) Destroy(bossObj);
        }

        // Destrói todas as balas do Player ativas na cena
        PlayerBullet[] playerBullets = FindObjectsByType<PlayerBullet>(FindObjectsSortMode.None);
        foreach (PlayerBullet bullet in playerBullets)
        {
            if (bullet != null) Destroy(bullet.gameObject);
        }

        // Destrói todas as balas do Boss/Inimigos ativas na cena
        // (Busca pelo script de bala de inimigo se houver ou por Tag)
        GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject bullet in enemyBullets)
        {
            if (bullet != null) Destroy(bullet);
        }
    }

    private void StopGameElements()
    {
        // Para a música da partida
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        // Desativa o Boss caso ainda exista
        if (boss != null)
        {
            boss.StopAllPatterns();

            Animator bossAnimator = boss.GetComponent<Animator>();
            if (bossAnimator != null) bossAnimator.enabled = false;

            boss.gameObject.SetActive(false);
        }

        // Congela o tempo de jogo
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        // Descongela o tempo antes de carregar a cena
        Time.timeScale = 1f;
        SceneManager.LoadScene(bootSceneName);
    }
}