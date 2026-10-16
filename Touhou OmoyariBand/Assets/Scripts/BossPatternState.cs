using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Configuração da Fase")]
    [Range(1, 3)]
    [SerializeField] private int phaseIndex = 1;

    [Header("Ataque e Duração")]
    [SerializeField] private int patternIndex = 0;

    [Tooltip("Tempo em segundos que o Boss FICA ATIRANDO antes de trocar de comportamento.")]
    [SerializeField] private float stateDuration = 4.0f;

    private float timer;
    private MatchProgressBar progressBar;
    private bool hasTriggeredNext;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        hasTriggeredNext = false;

        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("GoToMedium");
        animator.ResetTrigger("GoToLow");

        if (progressBar == null)
        {
            progressBar = FindAnyObjectByType<MatchProgressBar>();
        }

        // Ativa o movimento e inicia o padrão de tiros contínuo
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.ActivatePatternIndex(patternIndex);

            int[] currentPattern = new int[] { patternIndex };
            boss.ExecuteRandomBehavior(phaseIndex, currentPattern);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasTriggeredNext) return;

        float currentProgress = (progressBar != null) ? progressBar.CurrentProgressNormalized : 0f;

        // 1. TROCA DE FASE (Se atingiu 33% ou 66% da música)
        if (phaseIndex == 1 && currentProgress >= 0.33f)
        {
            hasTriggeredNext = true;
            TriggerPhaseTransition(animator, "GoToMedium");
            return;
        }
        else if (phaseIndex == 2 && currentProgress >= 0.66f)
        {
            hasTriggeredNext = true;
            TriggerPhaseTransition(animator, "GoToLow");
            return;
        }

        // 2. TEMPO DO BEHAVIOR (Mantém atirando até completar a duração total)
        timer += Time.deltaTime;
        if (timer >= stateDuration)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextBehavior");
        }
    }

    private void TriggerPhaseTransition(Animator animator, string triggerName)
    {
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns();
            boss.StartInvulnerableBlink(2.0f);
        }

        animator.SetTrigger(triggerName);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("GoToMedium");
        animator.ResetTrigger("GoToLow");

        // Desliga os tiros apenas no momento da troca de estado
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns();
        }
    }
}