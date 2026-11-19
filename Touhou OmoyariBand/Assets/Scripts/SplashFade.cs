using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SplashFade : MonoBehaviour
{
    [Header("Tempos da Animação")]
    [Tooltip("Atraso antes de começar (0 para o 1º Canvas; 4.0 para o 2º Canvas).")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("Tempo para a imagem surgir (Fade In).")]
    [SerializeField] private float fadeInDuration = 0.8f;

    [Tooltip("Tempo que a imagem fica totalmente visível antes de sumir.")]
    [SerializeField] private float holdDuration = 2.5f;

    [Tooltip("Tempo para a imagem desaparecer (0 = não faz fade out).")]
    [SerializeField] private float fadeOutDuration = 0.7f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private IEnumerator Start()
    {
        // Aguarda 1 frame para absorver o lag de carregamento da cena e inicialização do DOTween
        yield return null;

        Sequence seq = DOTween.Sequence();

        if (startDelay > 0f)
        {
            seq.AppendInterval(startDelay);
        }

        // 1. Surge suavemente (Fade In)
        seq.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

        // 2. Fica parada na tela + Fade Out
        if (fadeOutDuration > 0f)
        {
            seq.AppendInterval(holdDuration);
            seq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        }

        seq.SetUpdate(true);
    }

    private void OnDestroy()
    {
        canvasGroup.DOKill();
    }
}