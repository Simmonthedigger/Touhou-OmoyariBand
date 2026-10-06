using UnityEngine;

public class BossInvulnerableState : StateMachineBehaviour
{
    [SerializeField] private float invincibilityDuration = 2.0f;
    private float timer;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns();
            // Desativa temporariamente a vulnerabilidade (se houver BossHealth)
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;

        // Após passar os 2 segundos, avança no Animator
        if (timer >= invincibilityDuration)
        {
            animator.SetTrigger("NextPhase");
        }
    }
}