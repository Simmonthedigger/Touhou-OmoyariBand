using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Configuração da Fase")]
    [Tooltip("Fase do Boss (1, 2 ou 3).")]
    [Range(1, 3)]
    [SerializeField] private int phaseIndex = 1;

    [Tooltip("Índices dos Padrões de Tiro permitidos para este comportamento específico.")]
    [SerializeField] private int[] allowedPatternIndices = new int[] { 0, 1 };

    [Header("Duração e Transições")]
    [Tooltip("Tempo em segundos que o Boss permanece neste nó de comportamento antes de avançar.")]
    [SerializeField] private float stateDuration = 4.0f;

    [Tooltip("Marque APENAS no último nó de cada fase (ex: High_Behavior3 e Medium_Behavior3).")]
    [SerializeField] private bool isPhaseEnd = false;

    private float timer;
    private MatchProgressBar progressBar;
    private bool hasTriggeredNext;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        hasTriggeredNext = false;

        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("NextPhase");

        if (progressBar == null)
        {
            progressBar = FindAnyObjectByType<MatchProgressBar>();
        }

        // Sorteia o waypoint da fase e dispara um padrão de tiro permitido para este nó
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.ExecuteRandomBehavior(phaseIndex, allowedPatternIndices);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasTriggeredNext) return;

        float currentProgress = (progressBar != null) ? progressBar.CurrentProgressNormalized : 0f;

        // 1. MUDANÇA DE FASE (Se for o nó final da fase ou se a porcentagem já atingiu o limite)
        if (phaseIndex == 1 && currentProgress >= 0.33f && isPhaseEnd)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextPhase");
            return;
        }
        else if (phaseIndex == 2 && currentProgress >= 0.66f && isPhaseEnd)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextPhase");
            return;
        }

        // 2. TROCA DE COMPORTAMENTO DENTRO DA MESMA FASE
        timer += Time.deltaTime;
        if (timer >= stateDuration)
        {
            hasTriggeredNext = true;

            // Se for o último nó da fase, mas ainda não deu o tempo percentual, faz um loop na fase
            if (isPhaseEnd)
            {
                animator.SetTrigger("NextBehavior");
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
        animator.ResetTrigger("NextPhase");

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns();
        }
    }
}