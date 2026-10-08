using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Música e Duração")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("Duração deste padrão em segundos da música.")]
    [SerializeField] private float stateDuration = 10.0f;

    private float startTimeInMusic;
    private bool hasTriggeredNext;

    [Header("Tipo de Transição")]
    [Tooltip("Marque se este for o último padrão de uma fase (Ex: final do High_Behavior3).")]
    [SerializeField] private bool isPhaseEnd = false;
    [SerializeField] private string phaseTriggerName = "NextPhase";

    [Header("Ataque do Boss")]
    [SerializeField] private bool shootDuringState = true;
    [SerializeField] private int patternIndex = 0;

    [Header("Movimentação do Boss")]
    [SerializeField] private BossMovement.MovementState movementState = BossMovement.MovementState.IdleHover;
    [SerializeField] private string targetPointName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasTriggeredNext = false;
        animator.ResetTrigger("NextBehavior");
        if (!string.IsNullOrEmpty(phaseTriggerName)) animator.ResetTrigger(phaseTriggerName);

        if (musicSource == null)
        {
            musicSource = FindFirstObjectByType<AudioSource>();
        }

        startTimeInMusic = (musicSource != null && musicSource.isPlaying) ? musicSource.time : Time.time;

        BossController boss = animator.GetComponent<BossController>();
        BossMovement movement = animator.GetComponent<BossMovement>();

        if (boss != null)
        {
            boss.StopAllPatterns();
            if (shootDuringState && patternIndex >= 0)
            {
                boss.ActivatePatternIndex(patternIndex);
            }
        }

        if (movement != null)
        {
            if (!string.IsNullOrEmpty(targetPointName))
            {
                GameObject targetPoint = GameObject.Find(targetPointName);
                if (targetPoint != null) movement.MoveToPosition(targetPoint.transform.position);
            }
            else
            {
                switch (movementState)
                {
                    case BossMovement.MovementState.IdleHover: movement.StartHoverMode(); break;
                    case BossMovement.MovementState.Circle: movement.StartCircleMode(); break;
                    case BossMovement.MovementState.Static: movement.SetState(BossMovement.MovementState.Static); break;
                }
            }
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentMusicTime = (musicSource != null && musicSource.isPlaying) ? musicSource.time : Time.time;
        float elapsedTime = currentMusicTime - startTimeInMusic;

        if (elapsedTime >= stateDuration && !hasTriggeredNext)
        {
            hasTriggeredNext = true;

            if (isPhaseEnd)
            {
                animator.SetTrigger(phaseTriggerName);
            }
            else
            {
                animator.SetTrigger("NextBehavior");
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("NextBehavior");
        if (!string.IsNullOrEmpty(phaseTriggerName)) animator.ResetTrigger(phaseTriggerName);

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null) boss.StopAllPatterns();
    }
}