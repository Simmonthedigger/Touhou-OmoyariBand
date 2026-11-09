using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Coloque em um objeto vazio na cena de gameplay de cada fase.
/// Ao entrar na cena: congela o jogo (e a música), segura o Animator do boss, toca o diálogo
/// na caixa da cena de HUD e só libera tudo depois da última fala.
/// Sem diálogo atribuído, não faz nada.
/// </summary>
[DefaultExecutionOrder(-1000)] // roda antes dos outros scripts da cena
public class StageIntro : MonoBehaviour
{
    [Header("Diálogo")]
    [SerializeField] private DialogueData dialogue;

    [Tooltip("Se ligado, o diálogo só toca na 1ª vez que a fase abre. Ao reiniciar após Game Over, ele é pulado.")]
    [SerializeField] private bool playOnlyOncePerSession = false;

    [Tooltip("Tempo máximo (s) esperando a cena de HUD carregar o DialoguePlayer. Evita travar o jogo se ele não existir.")]
    [SerializeField] private float waitForUITimeout = 3f;

    [Header("Eventos")]
    [Tooltip("Chamado quando o diálogo termina e o jogo é liberado (ex.: iniciar a música do RhythmMusic).")]
    public UnityEvent onDialogueFinished;

    private static readonly HashSet<string> playedScenes = new HashSet<string>();

    private bool startedCutscene;
    private Animator bossAnimator;

    // Limpa a lista ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        playedScenes.Clear();
    }

    private void Awake()
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0) return;

        string sceneName = gameObject.scene.name;
        if (playOnlyOncePerSession && playedScenes.Contains(sceneName)) return;
        playedScenes.Add(sceneName);

        // Congela imediatamente, antes de qualquer Start() dos outros scripts
        CutsceneState.Begin();
        startedCutscene = true;

        // Segura o Animator do boss: o comportamento dele só começa depois do diálogo
        BossController boss = FindAnyObjectByType<BossController>();
        if (boss != null && boss.TryGetComponent(out bossAnimator))
        {
            bossAnimator.enabled = false;
        }
    }

    private IEnumerator Start()
    {
        if (!startedCutscene) yield break;

        // A caixa de diálogo fica na cena de HUD (carregada de forma aditiva): espera ela existir
        float waited = 0f;
        while (DialoguePlayer.Instance == null && waited < waitForUITimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (DialoguePlayer.Instance == null)
        {
            Debug.LogWarning("[StageIntro] DialoguePlayer não encontrado (a cena de HUD está carregada?). Pulando o diálogo.");
        }
        else
        {
            bool finished = false;
            DialoguePlayer.Instance.Play(dialogue, () => finished = true);

            while (!finished) yield return null;

            // Espera soltar o botão do mouse para o clique final não virar um tiro
            while (Input.GetMouseButton(0)) yield return null;
        }

        EndCutscene();
        onDialogueFinished?.Invoke();
    }

    private void EndCutscene()
    {
        if (!startedCutscene) return;

        startedCutscene = false;
        CutsceneState.End();

        // Libera o boss: ao religar, o Animator recomeça do primeiro behavior
        if (bossAnimator != null)
        {
            bossAnimator.enabled = true;
            bossAnimator = null;
        }
    }

    private void OnDestroy()
    {
        // Se a cena for descarregada no meio do diálogo, não deixa o jogo congelado
        EndCutscene();
    }
}