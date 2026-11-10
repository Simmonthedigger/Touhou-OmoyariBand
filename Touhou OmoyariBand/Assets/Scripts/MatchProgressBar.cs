using UnityEngine;
using UnityEngine.UI;

public class MatchProgressBar : MonoBehaviour
{
    [Header("Música / Tempo da Partida")]
    [SerializeField] private AudioSource musicSource;

    [Header("Componentes do Slider")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image fillImage;

    [Header("Cores por Fase (% da Partida)")]
    [SerializeField] private Color phase1Color = Color.green;
    [SerializeField] private Color phase2Color = Color.yellow;
    [SerializeField] private Color phase3Color = Color.red;

    private float totalMusicDuration;
    private bool hasTriggeredVictory = false;

    public float CurrentProgressNormalized { get; private set; } // Retorna de 0.0 a 1.0

    private void Start()
    {
        InitializeComponents();
    }

    private AudioSource FindMusicAudioSource()
    {
        GameObject musicObj = GameObject.FindGameObjectWithTag("MusicSource");
        if (musicObj != null)
        {
            return musicObj.GetComponent<AudioSource>();
        }
        return null;
    }
    private void InitializeComponents()
    {
        if (musicSource == null)
        {
            musicSource = FindMusicAudioSource();
        }

        if (musicSource != null && musicSource.clip != null)
        {
            totalMusicDuration = musicSource.clip.length;
        }

        if (fillImage == null && progressSlider != null && progressSlider.fillRect != null)
        {
            fillImage = progressSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        // 1. Garante que a referência da música e sua duração estejam configuradas
        if (musicSource == null || totalMusicDuration <= 0f)
        {
            InitializeComponents();
            if (musicSource == null || totalMusicDuration <= 0f) return;
        }

        // 2. Atualiza a porcentagem de progresso da música
        CurrentProgressNormalized = Mathf.Clamp01(musicSource.time / totalMusicDuration);

        // 3. Atualiza o Slider no Canvas
        if (progressSlider != null)
        {
            progressSlider.value = CurrentProgressNormalized;
        }

        // 4. Atualiza as cores do preenchimento da barra por fase
        if (fillImage != null)
        {
            if (CurrentProgressNormalized >= 0.66f)
            {
                fillImage.color = phase3Color;
            }
            else if (CurrentProgressNormalized >= 0.33f)
            {
                fillImage.color = phase2Color;
            }
            else
            {
                fillImage.color = phase1Color;
            }
        }

        // 5. DISPARA A VITÓRIA APENAS SE A MÚSICA REALMENTE CHEGAR AO FIM E O JOGO NÃO ESTIVER PAUSADO
        bool isGamePaused = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Pausado);

        if (!isGamePaused && !hasTriggeredVictory)
        {
            // A vitória só é acionada se o progresso atingir 99% ou se o áudio parar perto do fim da música
            if (CurrentProgressNormalized >= 0.99f || (!musicSource.isPlaying && musicSource.time >= totalMusicDuration - 0.5f))
            {
                hasTriggeredVictory = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Victory();
                }
            }
        }
    }
}