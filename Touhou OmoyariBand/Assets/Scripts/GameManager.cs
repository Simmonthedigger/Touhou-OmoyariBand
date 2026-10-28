using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState
{
    Iniciando,
    Splash,
    MenuPrincipal,
    Gameplay
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuração dos Estados e Cenas")]
    [SerializeField] private string splashSceneName = "Splash";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string hudSceneName = "HUD";

    [Header("Música e Boss (Gameplay)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private BossController boss;

    [Header("Player Input System")]
    [SerializeField] private PlayerInput playerInput;

    public GameState CurrentState { get; private set; }
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém o GameManager persitente entre as cenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inscreve no evento de carregamento de cena da Unity
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Inicia o estado e dispara a sequência inicial do jogo
        ChangeState(GameState.Iniciando);
        StartCoroutine(Routine_SplashSequence());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ==========================================
    // --- MÁQUINA DE ESTADOS E MUDANÇA DE CENAS ---
    // ==========================================

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] Estado alterado para: {CurrentState}");
    }

    private IEnumerator Routine_SplashSequence()
    {
        ChangeState(GameState.Splash);
        SceneManager.LoadScene(splashSceneName);

        // Aguarda os 2 segundos exigidos na tela de Splash
        yield return new WaitForSeconds(2.0f);

        // Transita automaticamente para o Menu Principal
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        ChangeState(GameState.MenuPrincipal);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void StartGameplay()
    {
        if (CurrentState != GameState.MenuPrincipal && CurrentState != GameState.Gameplay) return;

        Time.timeScale = 1f;
        isGameOver = false;

        ChangeState(GameState.Gameplay);
        StartCoroutine(Routine_LoadGameplayWithHUD());
    }

    private IEnumerator Routine_LoadGameplayWithHUD()
    {
        // 1. Carrega a cena de Gameplay
        AsyncOperation loadGameplay = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        while (!loadGameplay.isDone)
        {
            yield return null;
        }

        // 2. Carrega a cena da HUD de forma aditiva
        if (!string.IsNullOrEmpty(hudSceneName))
        {
            AsyncOperation loadHUD = SceneManager.LoadSceneAsync(hudSceneName, LoadSceneMode.Additive);
            while (!loadHUD.isDone)
            {
                yield return null;
            }
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // ==========================================
    // --- OBSERVAÇÃO DE CENA E ALOCAÇÃO INPUT ---
    // ==========================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Executa busca de referências específicas ao entrar na Gameplay
        if (scene.name == gameplaySceneName)
        {
            if (musicSource == null) musicSource = FindAnyObjectByType<AudioSource>();
            if (boss == null) boss = FindAnyObjectByType<BossController>();

            // Busca e aloca o PlayerInput do jogador na cena de Gameplay
            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.enabled = true;
                Debug.Log("[GameManager] Input alocado com sucesso ao PlayerInput.");
            }
        }
    }

    // ==========================================
    // --- LÓGICA DE GAMEPLAY (MANTIDA INTEGRAL) ---
    // ==========================================

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

    private void CleanUpEnemiesAndBullets()
    {
        if (boss != null)
        {
            boss.StopAllPatterns();
            Destroy(boss.gameObject);
            boss = null;
        }
        else
        {
            GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj != null) Destroy(bossObj);
        }

        PlayerBullet[] playerBullets = FindObjectsByType<PlayerBullet>(FindObjectsSortMode.None);
        foreach (PlayerBullet bullet in playerBullets)
        {
            if (bullet != null) Destroy(bullet.gameObject);
        }

        GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject bullet in enemyBullets)
        {
            if (bullet != null) Destroy(bullet);
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

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        StartGameplay();
    }
}