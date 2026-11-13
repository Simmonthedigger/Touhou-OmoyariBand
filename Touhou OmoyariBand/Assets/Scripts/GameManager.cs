using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Iniciando,
    Splash,
    MenuPrincipal,
    Gameplay,
    Pausado
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuração dos Estados e Cenas")]
    [SerializeField] private string splashSceneName = "Splash";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "SampleScene"; // Nome padrão ou da fase atual
    [SerializeField] private string hudSceneName = "HUD";

    [Header("Música e Boss (Gameplay)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private BossController boss;

    [Header("Player Input System")]
    [SerializeField] private PlayerInput playerInput;

    public GameState CurrentState { get; private set; }
    private GameState previousState;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém o GameManager persistente entre as cenas
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

    private void Update()
    {
        // Pressionar ESC alterna a pausa se o jogo estiver em Gameplay ou Pausado
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Gameplay || CurrentState == GameState.Pausado)
            {
                TogglePause();
            }
        }
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

        // Para a música da gameplay ao voltar para o menu
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        ChangeState(GameState.MenuPrincipal);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Inicia uma fase passando o nome da cena. Se nenhum nome for fornecido, usa a cena padrão.
    /// </summary>
    public void StartGameplay(string targetSceneName = null)
    {
        if (CurrentState != GameState.MenuPrincipal && CurrentState != GameState.Gameplay && CurrentState != GameState.Pausado) return;

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            gameplaySceneName = targetSceneName;
        }

        Time.timeScale = 1f;
        isGameOver = false;

        ChangeState(GameState.Gameplay);
        StartCoroutine(Routine_LoadGameplayWithHUD());
    }

    private IEnumerator Routine_LoadGameplayWithHUD()
    {
        // 1. Carrega a cena de Gameplay atual
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

    /// <summary>
    /// Alterna entre o estado de Pausa e Gameplay.
    /// </summary>
    public void TogglePause()
    {
        if (isGameOver) return;

        // Durante a cutscene (diálogo) não deixa pausar/despausar: despausar religaria o tempo no meio do diálogo
        if (CutsceneState.IsActive) return;

        HUDController hud = HUDController.Instance ?? FindAnyObjectByType<HUDController>();

        if (CurrentState == GameState.Gameplay)
        {
            previousState = CurrentState;
            ChangeState(GameState.Pausado);

            Time.timeScale = 0f; // Congela a física e o tempo do jogo

            // Pausa a música da partida
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }

            if (hud != null)
            {
                hud.TogglePauseMenu(true);
            }
        }
        else if (CurrentState == GameState.Pausado)
        {
            ChangeState(previousState);

            Time.timeScale = 1f; // Descongela o tempo do jogo

            // Despausa/retoma a música de onde parou
            if (musicSource != null)
            {
                musicSource.UnPause();
            }

            if (hud != null)
            {
                hud.TogglePauseMenu(false);
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
        // Executa busca de referências específicas ao carregar qualquer cena de Gameplay
        if (scene.name == gameplaySceneName)
        {
            // Reobtém os componentes da nova cena carregada
            musicSource = FindMusicSource();
            boss = FindAnyObjectByType<BossController>();

            // Busca e aloca o PlayerInput do jogador na cena
            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.enabled = true;
                Debug.Log($"[GameManager] Input alocado com sucesso na cena '{scene.name}'.");
            }
        }
    }

    // ==========================================
    // --- LÓGICA DE GAMEPLAY (MANTIDA INTEGRAL) ---
    // ==========================================

    // Procura o AudioSource da música, ignorando o AudioSource dos sons da contagem
    private AudioSource FindMusicSource()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
        foreach (AudioSource source in sources)
        {
            if (source != StageIntro.CountdownAudioSource) return source;
        }
        return null;
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

    /// <summary>
    /// Atalho caso o script do Boss chame OnBossDefeated() ao zerar a vida.
    /// </summary>
    public void OnBossDefeated()
    {
        Victory();
    }

    public void Victory()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 1. Destrói o Boss e todos os tiros em tela
        CleanUpEnemiesAndBullets();

        // 2. Para a música e congela o tempo
        StopGameElements();

        // 3. Pega a pontuação final do jogador
        int finalScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetCurrentScore() : 0;

        // 4. Mantém a chamada original do HUDController (se houver)
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowVictory(finalScore);
        }

        // 5. Localiza o painel de Vitória com Leaderboard (mesmo se estiver desativado na cena/HUD) e envia os dados para a nuvem
        VictoryScreenUI victoryScreenUI = FindAnyObjectByType<VictoryScreenUI>(FindObjectsInactive.Include);
        if (victoryScreenUI != null)
        {
            victoryScreenUI.gameObject.SetActive(true);
            victoryScreenUI.DisplayVictoryScreen(finalScore);
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

        PlayerBullet[] playerBullets = FindObjectsByType<PlayerBullet>(FindObjectsInactive.Exclude);
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
        // Reinicia a mesma fase que estava sendo jogada
        StartGameplay(gameplaySceneName);
    }
}