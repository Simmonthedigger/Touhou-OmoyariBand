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

    /// <summary>
    /// True enquanto o Boss está piscando (invulnerável) na troca de fase.
    /// </summary>
    public bool IsInvulnerable => blinkCoroutine != null;

    private void Awake()
    {
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossMovement == null) bossMovement = GetComponent<BossMovement>();
        if (patternEmitters == null || patternEmitters.Length == 0)
            patternEmitters = GetComponentsInChildren<BulletPatternEmitter>(true);
    }

    /// <summary>
    /// Move o Boss para um waypoint aleatório da fase.
    /// </summary>
    public void MoveBossForPhase(int phaseIndex)
    {
        Transform[] currentWaypoints = GetWaypointsForPhase(phaseIndex);
        if (currentWaypoints != null && currentWaypoints.Length > 0 && bossMovement != null)
        {
            int randomWpIndex = Random.Range(0, currentWaypoints.Length);
            if (currentWaypoints[randomWpIndex] != null)
            {
                bossMovement.MoveToPosition(currentWaypoints[randomWpIndex].position);
            }
        }
    }

    /// <summary>
    /// Ativa APENAS o emissor do índice informado e desliga os demais.
    /// </summary>
    public void ActivatePatternIndex(int index)
    {
        if (patternEmitters == null) return;

        for (int i = 0; i < patternEmitters.Length; i++)
        {
            if (patternEmitters[i] == null) continue;

            if (i == index)
            {
                // Se já estava ativo, reinicia explicitamente; se estava inativo,
                // o OnEnable do emissor já chama StartFiring().
                if (patternEmitters[i].gameObject.activeSelf)
                {
                    patternEmitters[i].StartFiring();
                }
                else
                {
                    patternEmitters[i].gameObject.SetActive(true);
                }
            }
            else
            {
                patternEmitters[i].StopFiring();
                patternEmitters[i].gameObject.SetActive(false);
            }
        }
    }

    public void StopAllPatterns()
    {
        if (patternEmitters == null) return;

        foreach (var emitter in patternEmitters)
        {
            if (emitter != null)
            {
                emitter.StopFiring();
            }
        }
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

    // --- CONTROLE DE INVULNERABILIDADE (PISCAR) ---

    public void StartInvulnerableBlink(float duration)
    {
        // Para os tiros
        StopAllPatterns();

        // Para o movimento: manda o boss "ir" para a própria posição atual
        if (bossMovement != null)
        {
            bossMovement.MoveToPosition(transform.position);
        }

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
        if (bossSpriteRenderer == null)
        {
            blinkCoroutine = null;
            yield break;
        }

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

        blinkCoroutine = null; // invulnerabilidade acabou
    }
}