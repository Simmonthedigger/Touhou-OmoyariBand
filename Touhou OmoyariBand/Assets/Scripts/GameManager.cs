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
        if (musicSource == null) musicSource = FindFirstObjectByType<AudioSource>();
        if (boss == null) boss = FindFirstObjectByType<BossController>();
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

        StopGameElements();

        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowVictory(finalScore);
        }
    }

    private void StopGameElements()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        if (boss != null)
        {
            boss.StopAllPatterns();

            Animator bossAnimator = boss.GetComponent<Animator>();
            if (bossAnimator != null) bossAnimator.enabled = false;

            boss.gameObject.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(bootSceneName);
    }
}