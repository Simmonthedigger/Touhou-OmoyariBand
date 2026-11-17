using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SplashFade : MonoBehaviour
{
    [Header("Tempos da Animação (Total ideal: <= 2.0s)")]
    [Tooltip("Tempo para a imagem surgir (Fade In).")]
    [SerializeField] private float fadeInDuration = 0.8f;

    [Tooltip("Tempo que a imagem fica totalmente visível antes de sumir.")]
    [SerializeField] private float holdDuration = 0.7f;

    [Tooltip("Tempo para a imagem desaparecer antes de trocar de cena (0 = não faz fade out).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Começa totalmente transparente
    }

    private void Start()
    {
        Sequence seq = DOTween.Sequence();

        // 1. Surge suavemente (Fade In)
        seq.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

        // 2. Fica parada na tela + Fade Out (opcional)
        if (fadeOutDuration > 0f)
        {
            seq.AppendInterval(holdDuration);
            seq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        }

        // Usa tempo real (ignora Time.timeScale)
        seq.SetUpdate(true);
    }

    private void OnDestroy()
    {
        canvasGroup.DOKill();
    }
}