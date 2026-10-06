using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Duração do Estado")]
    [Tooltip("Tempo em segundos que o Boss ficará neste estado antes de avançar.")]
    [SerializeField] private float stateDuration = 6.0f;
    private float timer;
    private bool hasTriggeredNext;

    [Header("Ataque do Boss")]
    [Tooltip("Marque como TRUE para atirar. Desmarque (FALSE) para o Boss apenas se mover sem atirar.")]
    [SerializeField] private bool shootDuringState = true;

    [Tooltip("Índice do PatternEmitter nos objetos filhos do Boss (0, 1, 2...). Use -1 para nenhum.")]
    [SerializeField] private int patternIndex = 0;

    [Header("Movimentação do Boss")]
    [SerializeField] private BossMovement.MovementState movementState = BossMovement.MovementState.IdleHover;

    [Header("Destino do Movimento (Opcional)")]
    [Tooltip("Nome do GameObject Waypoint na Hierarchy. Deixe em branco se não quiser mudar de posição.")]
    [SerializeField] private string targetPointName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        hasTriggeredNext = false;

        // Limpa Triggers antigos para evitar pular de estado precocemente
        animator.ResetTrigger("NextBehavior");

        BossController boss = animator.GetComponent<BossController>();
        BossMovement movement = animator.GetComponent<BossMovement>();

        // --- CONTROLE DE TIRO ---
        if (boss != null)
        {
            // Sempre desliga os tiros do estado anterior primeiro
            boss.StopAllPatterns();

            // Só ativa o tiro se a caixinha 'shootDuringState' estiver marcada E o índice for válido
            if (shootDuringState && patternIndex >= 0)
            {
                boss.ActivatePatternIndex(patternIndex);
            }
        }

        // --- CONTROLE DE MOVIMENTO ---
        if (movement != null)
        {
            if (!string.IsNullOrEmpty(targetPointName))
            {
                GameObject targetPoint = GameObject.Find(targetPointName);
                if (targetPoint != null)
                {
                    movement.MoveToPosition(targetPoint.transform.position);
                }
                else
                {
                    Debug.LogWarning($"[BossPatternState] Objeto de destino '{targetPointName}' não foi encontrado!");
                }
            }
            else
            {
                switch (movementState)
                {
                    case BossMovement.MovementState.IdleHover:
                        movement.StartHoverMode();
                        break;
                    case BossMovement.MovementState.Circle:
                        movement.StartCircleMode();
                        break;
                    case BossMovement.MovementState.Static:
                        movement.SetState(BossMovement.MovementState.Static);
                        break;
                }
            }
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;

        if (timer >= stateDuration && !hasTriggeredNext)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextBehavior");
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("NextBehavior");

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns();
        }
    }
}