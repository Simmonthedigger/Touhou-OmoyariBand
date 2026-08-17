using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Interface (UI)")]
    [SerializeField] private Text livesText; // Se estiver usando TMPro, altere para TextMeshProUGUI
    [SerializeField] private GameObject gameOverPanel;

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
        isGameOver = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // Método para ser chamado pelo botão "Jogar de Novo" na UI
    public void RestartGame()
    {
        Time.timeScale = 1f; // Garante que o tempo volte ao normal se estivesse pausado
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}