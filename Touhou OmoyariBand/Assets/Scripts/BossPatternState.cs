using UnityEngine;

public class BossPatternState : StateMachineBehaviour
{
    [Header("Ataque do Boss")]
    [Tooltip("Índice do PatternEmitter nos objetos filhos do Boss (0, 1, 2...)")]
    [SerializeField] private int patternIndex = 0;

    [Header("Movimentação do Boss")]
    [SerializeField] private BossMovement.MovementState movementState = BossMovement.MovementState.IdleHover;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        BossController boss = animator.GetComponent<BossController>();
        BossMovement movement = animator.GetComponent<BossMovement>();

        if (boss != null)
        {
            // Ativa o padrão de tiro correspondente a este estado
            boss.ActivatePatternIndex(patternIndex);
        }

        if (movement != null)
        {
            // Aplica o tipo de movimento desejado para este estado
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

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            // Para os tiros ao transicionar para o próximo estado
            boss.StopAllPatterns();
        }
    }
}