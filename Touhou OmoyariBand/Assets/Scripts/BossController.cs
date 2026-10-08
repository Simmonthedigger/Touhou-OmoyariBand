using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Componentes do Boss")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private BulletPatternEmitter[] patternEmitters; // Lista de padrões no Boss

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossMovement == null) bossMovement = GetComponent<BossMovement>();
        if (patternEmitters == null || patternEmitters.Length == 0)
            patternEmitters = GetComponentsInChildren<BulletPatternEmitter>();
    }

    // --- MÉTODOS CHAMADOS PELO STAGE TIMELINE / ANIMATOR ---

    public void SetBossSprite(Sprite newSprite)
    {
        if (bossSpriteRenderer != null && newSprite != null)
        {
            bossSpriteRenderer.sprite = newSprite;
        }
    }

    public void MoveToPoint(Transform targetTransform)
    {
        if (targetTransform != null && bossMovement != null)
        {
            bossMovement.MoveToPosition(targetTransform.position);
        }
    }

    public void ActivatePatternIndex(int index)
    {
        StopAllPatterns();
        if (index >= 0 && index < patternEmitters.Length)
        {
            patternEmitters[index].StartFiring();
        }
    }

    public void SetMovementStateIdle()
    {
        if (bossMovement != null) bossMovement.StartHoverMode();
    }

    public void SetMovementStateCircle()
    {
        if (bossMovement != null) bossMovement.StartCircleMode();
    }

    public void StopAllPatterns()
    {
        foreach (var emitter in patternEmitters)
        {
            emitter.StopFiring();
        }
    }

    // --- CONTROLE DE PISCAR (INVULNERABILIDADE) ---

    public void StartInvulnerableBlink(float duration)
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine(duration));
    }

    public void StopInvulnerableBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // Restaura a cor e opacidade originais do Sprite
        if (bossSpriteRenderer != null)
        {
            Color c = bossSpriteRenderer.color;
            c.a = 1f;
            bossSpriteRenderer.color = c;
        }
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        if (bossSpriteRenderer == null) yield break;

        float elapsedTime = 0f;
        float blinkInterval = 0.1f; // Intervalo entre visível/transparente

        while (elapsedTime < duration)
        {
            Color c = bossSpriteRenderer.color;
            c.a = (c.a >= 0.9f) ? 0.2f : 1f; // Alterna a transparência
            bossSpriteRenderer.color = c;

            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }

        // Garante que termina visível
        Color finalColor = bossSpriteRenderer.color;
        finalColor.a = 1f;
        bossSpriteRenderer.color = finalColor;
    }
}