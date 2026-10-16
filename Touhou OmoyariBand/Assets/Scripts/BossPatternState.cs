using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Configuração da Fase")]
    [Range(1, 3)]
    [SerializeField] private int phaseIndex = 1;

    [Header("Ataque e Duração")]
    [Tooltip("Índice EXATO do padrão no array do Boss (0, 1, 2...).")]
    [SerializeField] private int patternIndex = 0;

    [Tooltip("Tempo em segundos que o Boss FICA ATIRANDO antes de trocar de comportamento.")]
    [SerializeField] private float stateDuration = 4.0f;

    private float timer;
    private MatchProgressBar progressBar;
    private bool hasTriggeredNext;
    private bool hasStartedBehavior;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        hasTriggeredNext = false;
        hasStartedBehavior = false;

        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("GoToMedium");
        animator.ResetTrigger("GoToLow");

        if (progressBar == null)
        {
            progressBar = FindAnyObjectByType<MatchProgressBar>();
        }

        BossController boss = animator.GetComponent<BossController>();
        if (boss == null) return;

        // Se o boss está invulnerável (troca de fase), espera o piscar acabar
        if (boss.IsInvulnerable) return;

        StartBehavior(boss);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasTriggeredNext) return;

        // Ainda não começou: aguarda o fim da invulnerabilidade, parado e sem atirar
        if (!hasStartedBehavior)
        {
            BossController boss = animator.GetComponent<BossController>();
            if (boss == null || boss.IsInvulnerable) return;

            StartBehavior(boss);
        }

        float currentProgress = (progressBar != null) ? progressBar.CurrentProgressNormalized : 0f;

        // 1. TROCA DE FASE (33% e 66% da música)
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

        // 2. DURAÇÃO DO BEHAVIOR
        timer += Time.deltaTime;
        if (timer >= stateDuration)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextBehavior");
        }
    }

    private void StartBehavior(BossController boss)
    {
        hasStartedBehavior = true;
        timer = 0f; // os 4s só começam a contar quando o boss realmente começa a atirar

        // 1. Move o Boss para um waypoint da fase
        boss.MoveBossForPhase(phaseIndex);

        // 2. Liga somente o padrão deste estado (desliga os outros)
        boss.ActivatePatternIndex(patternIndex);
    }

    private void TriggerPhaseTransition(Animator animator, string triggerName)
    {
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            // Para os tiros, congela o boss e inicia o piscar (feito dentro do controller)
            boss.StartInvulnerableBlink(2.0f);
        }

        animator.SetTrigger(triggerName);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("NextBehavior");
        animator.ResetTrigger("GoToMedium");
        animator.ResetTrigger("GoToLow");

        // NÃO chamar StopAllPatterns() aqui: o OnStateExit do estado antigo roda
        // DEPOIS do OnStateEnter do novo e mataria o padrão que acabou de começar.
        // O ActivatePatternIndex já desliga os outros emissores ao entrar no próximo estado.
    }
}