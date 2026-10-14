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
        if (musicSource == null) musicSource = FindAnyObjectByType<AudioSource>();

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
        if (musicSource == null || totalMusicDuration <= 0f) return;

        CurrentProgressNormalized = Mathf.Clamp01(musicSource.time / totalMusicDuration);

        if (progressSlider != null)
        {
            progressSlider.value = CurrentProgressNormalized;
        }

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

        // DISPARA A VITÓRIA E CONGELA O JOGO QUANDO A MÚSICA CHEGA AO FIM (100% DE PROGRESSO)
        if (CurrentProgressNormalized >= 0.99f || (!musicSource.isPlaying && musicSource.time > 0f))
        {
            if (!hasTriggeredVictory)
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