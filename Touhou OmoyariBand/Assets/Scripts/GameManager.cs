using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // IMPORTANTE: Adicionado para usar o TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Interface (UI)")]
    // Mudado de Text para TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel;

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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void UpdateLivesUI(int lives)
    {
        if (livesText != null)
        {
            livesText.text = "Vidas: " + lives;
        }
    }

    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}