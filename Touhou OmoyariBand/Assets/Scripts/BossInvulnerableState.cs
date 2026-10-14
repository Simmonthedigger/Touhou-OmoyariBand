using UnityEngine;

public class BossInvulnerableState : StateMachineBehaviour
{
    [Header("Duração")]
    [Tooltip("Tempo em segundos (da música ou do jogo) que o Boss fica invulnerável.")]
    [SerializeField] private float invincibilityDuration = 2.0f;

    [Header("Música (Opcional)")]
    [Tooltip("Arraste o AudioSource da música para usar o tempo do áudio. Se vazio, usa o tempo normal.")]
    [SerializeField] private AudioSource musicSource;

    private float startTime;
    private bool hasTriggeredNext;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasTriggeredNext = false;

        // IMPORTANTE: Reseta o Trigger para evitar saltos acidentais de estado
        animator.ResetTrigger("NextPhase");

        // Procura o AudioSource caso não tenha sido atribuído no Inspector
        if (musicSource == null)
        {
            musicSource = FindAnyObjectByType<AudioSource>();
        }

        // Guarda o tempo inicial
        startTime = (musicSource != null && musicSource.isPlaying) ? musicSource.time : Time.time;

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopAllPatterns(); // Para todos os tiros
            boss.StartInvulnerableBlink(invincibilityDuration); // Inicia o efeito de piscar
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = (musicSource != null && musicSource.isPlaying) ? musicSource.time : Time.time;
        float elapsedTime = currentTime - startTime;

        // Dispara o Trigger apenas UMA VEZ ao atingir a duração
        if (elapsedTime >= invincibilityDuration && !hasTriggeredNext)
        {
            hasTriggeredNext = true;
            animator.SetTrigger("NextPhase");
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Consome o Trigger para garantir limpeza
        animator.ResetTrigger("NextPhase");

        BossController boss = animator.GetComponent<BossController>();
        if (boss != null)
        {
            boss.StopInvulnerableBlink(); // Restaura a visibilidade normal
        }
    }
}