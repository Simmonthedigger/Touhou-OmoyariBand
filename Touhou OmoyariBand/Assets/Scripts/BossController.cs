using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Componentes do Boss")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private BulletPatternEmitter[] patternEmitters; // Lista de padrões no Boss

    private void Awake()
    {
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossMovement == null) bossMovement = GetComponent<BossMovement>();
        if (patternEmitters == null || patternEmitters.Length == 0)
            patternEmitters = GetComponentsInChildren<BulletPatternEmitter>();
    }

    // --- MÉTODOS CHAMADOS PELO STAGE TIMELINE ---

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

    // Adicione estes métodos ao BossController.cs

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
}