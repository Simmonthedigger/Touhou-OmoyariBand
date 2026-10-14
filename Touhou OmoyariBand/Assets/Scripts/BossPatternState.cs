using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Configuração de Fase")]
    [Tooltip("Fase do Boss (1, 2 ou 3).")]
    [Range(1, 3)]
    [SerializeField] private int phaseIndex = 1;

    [Tooltip("Índices dos Padrões de Tiro permitidos para esta fase.")]
    [SerializeField] private int[] allowedPatternIndices = new int[] { 0, 1 };

    [Header("Tempo do Ciclo")]
    [Tooltip("Tempo em segundos antes de sortear o próximo comportamento/waypoint.")]
    [SerializeField] private float cycleDuration = 4.0f;

    private float timer;
    private MatchProgressBar progressBar;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("NextPhase");

        if (progressBar == null)
        {
            progressBar = FindAnyObjectByType<MatchProgressBar>();
        }

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.ExecuteRandomBehavior(phaseIndex, allowedPatternIndices);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentProgress = (progressBar != null) ? progressBar.CurrentProgressNormalized : 0f;

        // 1. Verifica se deve mudar para a próxima fase com base na porcentagem
        if (phaseIndex == 1 && currentProgress >= 0.33f)
        {
            animator.SetTrigger("NextPhase");
            return;
        }
        else if (phaseIndex == 2 && currentProgress >= 0.66f)
        {
            animator.SetTrigger("NextPhase");
            return;
        }

        // 2. Controla o ciclo de alternância dentro da mesma fase
        timer += Time.deltaTime;
        if (timer >= cycleDuration)
        {
            timer = 0f;

            // Re-executa um sorteio de Waypoint e Padrão na mesma fase
            BossController boss = animator.GetComponent<BossController>();
            if (boss != null)
            {
                boss.ExecuteRandomBehavior(phaseIndex, allowedPatternIndices);
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