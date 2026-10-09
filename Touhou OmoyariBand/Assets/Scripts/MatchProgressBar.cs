using UnityEngine;
using UnityEngine.UI;

public class MatchProgressBar : MonoBehaviour
{
    [Header("Música / Tempo da Partida")]
    [Tooltip("Arraste o AudioSource da música principal.")]
    [SerializeField] private AudioSource musicSource;

    [Header("Componentes de UI (Escolha um)")]
    [Tooltip("Arraste aqui se estiver usando um UI Slider.")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("Arraste aqui se estiver usando uma UI Image com Image Type = Filled.")]
    [SerializeField] private Image progressImage;

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
    }

    private void Update()
    {
        if (musicSource == null || totalMusicDuration <= 0f) return;

        // Calcula a porcentagem do progresso (0.0 a 1.0)
        float progress = Mathf.Clamp01(musicSource.time / totalMusicDuration);

        // Atualiza o Slider (se atribuído)
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        // Atualiza a Imagem Filled (se atribuída)
        if (progressImage != null)
        {
            progressImage.fillAmount = progress;
        }
    }
}