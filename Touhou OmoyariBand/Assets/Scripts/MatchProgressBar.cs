using UnityEngine;
using UnityEngine.UI;

public class MatchProgressBar : MonoBehaviour
{
    [Header("Música / Tempo da Partida")]
    [Tooltip("Arraste o AudioSource da música principal.")]
    [SerializeField] private AudioSource musicSource;

    [Header("Componentes do Slider")]
    [Tooltip("Arraste o seu componente Slider.")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("Arraste a imagem 'Fill' que fica dentro de Slider -> Fill Area -> Fill.")]
    [SerializeField] private Image fillImage;

    [Header("Configuração de Cores por Fase (em Segundos)")]
    [Tooltip("Cor inicial da barra de progresso (Fase 1).")]
    [SerializeField] private Color phase1Color = Color.green;

    [Tooltip("Segundo exato da música em que entra a Fase 2.")]
    [SerializeField] private float phase2StartSecond = 30.0f;
    [Tooltip("Cor da barra na Fase 2.")]
    [SerializeField] private Color phase2Color = Color.yellow;

    [Tooltip("Segundo exato da música em que entra a Fase 3.")]
    [SerializeField] private float phase3StartSecond = 60.0f;
    [Tooltip("Cor da barra na Fase 3.")]
    [SerializeField] private Color phase3Color = Color.red;

    private float totalMusicDuration;

    private void Start()
    {
        if (musicSource == null)
        {
            musicSource = FindFirstObjectByType<AudioSource>();
        }

        if (musicSource != null && musicSource.clip != null)
        {
            totalMusicDuration = musicSource.clip.length;
        }
        else
        {
            Debug.LogWarning("[MatchProgressBar] AudioSource ou AudioClip não encontrado!");
        }

        // Tenta encontrar a imagem 'Fill' automaticamente se não tiver sido arrastada
        if (fillImage == null && progressSlider != null && progressSlider.fillRect != null)
        {
            fillImage = progressSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (musicSource == null || totalMusicDuration <= 0f) return;

        float currentTime = musicSource.time;

        // 1. Atualiza o valor do Slider (0.0 a 1.0)
        float progress = Mathf.Clamp01(currentTime / totalMusicDuration);

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        // 2. Troca a cor do Fill com base no segundo exato da música
        if (fillImage != null)
        {
            if (currentTime >= phase3StartSecond)
            {
                fillImage.color = phase3Color;
            }
            else if (currentTime >= phase2StartSecond)
            {
                fillImage.color = phase2Color;
            }
            else
            {
                fillImage.color = phase1Color;
            }
        }
    }
}