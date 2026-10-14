using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Componentes do Boss")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private BulletPatternEmitter[] patternEmitters;

    [Header("Waypoints Por Fase")]
    [SerializeField] private Transform[] phase1Waypoints;
    [SerializeField] private Transform[] phase2Waypoints;
    [SerializeField] private Transform[] phase3Waypoints;

    private Coroutine blinkCoroutine;
    private int lastPatternIndex = -1; // Guarda o último padrão usado para não repetir

    private void Awake()
    {
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossMovement == null) bossMovement = GetComponent<BossMovement>();
        if (patternEmitters == null || patternEmitters.Length == 0)
            patternEmitters = GetComponentsInChildren<BulletPatternEmitter>();
    }

    /// <summary>
    /// Sorteia um Waypoint e um Padrão de Tiro SEM REPETIR o padrão anterior.
    /// </summary>
    public void ExecuteRandomBehavior(int phaseIndex, int[] allowedPatternIndices)
    {
        StopAllPatterns();

        // 1. Sorteia e move para um Waypoint da fase
        Transform[] currentWaypoints = GetWaypointsForPhase(phaseIndex);
        if (currentWaypoints != null && currentWaypoints.Length > 0 && bossMovement != null)
        {
            int randomWpIndex = Random.Range(0, currentWaypoints.Length);
            if (currentWaypoints[randomWpIndex] != null)
            {
                bossMovement.MoveToPosition(currentWaypoints[randomWpIndex].position);
            }
        }

        // 2. Sorteia um padrão SEM REPETIR o anterior
        if (allowedPatternIndices != null && allowedPatternIndices.Length > 0)
        {
            int selectedPattern = GetRandomNonRepeatingPattern(allowedPatternIndices);
            lastPatternIndex = selectedPattern; // Guarda o novo escolhido
            ActivatePatternIndex(selectedPattern);
        }
    }

    private int GetRandomNonRepeatingPattern(int[] allowedIndices)
    {
        // Se houver apenas 1 padrão configurado para a fase, não há como alternar
        if (allowedIndices.Length <= 1) return allowedIndices[0];

        int newPattern;
        do
        {
            newPattern = allowedIndices[Random.Range(0, allowedIndices.Length)];
        }
        while (newPattern == lastPatternIndex); // Repete o sorteio se for igual ao último disparado

        return newPattern;
    }

    private Transform[] GetWaypointsForPhase(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 1: return phase1Waypoints;
            case 2: return phase2Waypoints;
            case 3: return phase3Waypoints;
            default: return phase1Waypoints;
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

    public void StopAllPatterns()
    {
        foreach (var emitter in patternEmitters)
        {
            if (emitter != null) emitter.StopFiring();
        }
    }

    // --- CONTROLE DE INVULNERABILIDADE (PISCAR) ---

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
        float blinkInterval = 0.1f;

        while (elapsedTime < duration)
        {
            Color c = bossSpriteRenderer.color;
            c.a = (c.a >= 0.9f) ? 0.2f : 1f;
            bossSpriteRenderer.color = c;

            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }

        Color finalColor = bossSpriteRenderer.color;
        finalColor.a = 1f;
        bossSpriteRenderer.color = finalColor;
    }
}