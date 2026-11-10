using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Coloque em um objeto vazio na cena de gameplay de cada fase. Sequência ao entrar na cena:
///   1. (opcional) Diálogo: tudo congelado, caixa de texto na cena de HUD.
///   2. (opcional) Contagem regressiva: o player se move, mas sem música, sem boss e sem tiro.
///   3. Início da luta: a música volta, o boss aparece e o player pode atirar.
/// Sem diálogo e com contagem 0, não faz nada.
/// </summary>
[DefaultExecutionOrder(-1000)] // roda antes dos outros scripts da cena
public class StageIntro : MonoBehaviour
{
    [Header("Diálogo (opcional)")]
    [SerializeField] private DialogueData dialogue;

    [Tooltip("Se ligado, o diálogo só toca na 1ª vez que a fase abre. Ao reiniciar após Game Over, ele é pulado. (A contagem sempre acontece.)")]
    [SerializeField] private bool playOnlyOncePerSession = false;

    [Tooltip("Tempo máximo (s) esperando a cena de HUD carregar o DialoguePlayer. Evita travar o jogo se ele não existir.")]
    [SerializeField] private float waitForUITimeout = 3f;

    [Header("Contagem regressiva (opcional)")]
    [Tooltip("Segundos de contagem antes da luta começar. 0 = sem contagem.")]
    [SerializeField, Min(0)] private int countdownSeconds = 3;

    [Tooltip("Texto mostrado quando a contagem termina.")]
    [SerializeField] private string goText = "GO!";

    [Tooltip("Quanto tempo (s) o texto final fica na tela.")]
    [SerializeField] private float goDuration = 0.8f;

    [Tooltip("Esconde o boss durante a contagem. Ele aparece quando a luta começa.")]
    [SerializeField] private bool hideBossDuringCountdown = true;

    [Header("Texto da contagem")]
    [Tooltip("Opcional. Se vazio, o texto é criado sozinho no centro da tela.")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float fontSize = 180f;
    [SerializeField] private Color textColor = Color.white;

    [Header("Sons da contagem (opcional)")]
    [Tooltip("AudioSource que toca os sons da contagem. Use um AudioSource SÓ para isso (nunca o da música). Desmarque Play On Awake nele.")]
    [SerializeField] private AudioSource countdownAudioSource;

    [Tooltip("Som tocado a cada número (3, 2, 1).")]
    [SerializeField] private AudioClip tickClip;

    [Tooltip("Som tocado quando aparece o GO!.")]
    [SerializeField] private AudioClip goClip;

    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Eventos")]
    [Tooltip("Chamado quando o diálogo termina.")]
    public UnityEvent onDialogueFinished;

    [Tooltip("Chamado quando a luta começa (fim da contagem). Ex.: iniciar a música do RhythmMusic.")]
    public UnityEvent onStageStarted;

    /// <summary>
    /// AudioSource usado nos sons da contagem. Scripts que procuram "o AudioSource da música"
    /// (ex.: GameManager) devem ignorar este.
    /// </summary>
    public static AudioSource CountdownAudioSource { get; private set; }

    private static readonly HashSet<string> playedScenes = new HashSet<string>();

    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();

    private bool playDialogue;
    private bool playCountdown;
    private bool sequenceActive;

    private BossController bossController;
    private Animator bossAnimator;
    private PlayerShooter shooter;
    private GameObject runtimeCanvas;

    // Limpa a lista ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        playedScenes.Clear();
        CountdownAudioSource = null;
    }

    private void Awake()
    {
        // Registra já no Awake (antes de o GameManager procurar a música) para ele poder ignorar este AudioSource
        CountdownAudioSource = countdownAudioSource;

        bool hasDialogue = dialogue != null && dialogue.lines != null && dialogue.lines.Length > 0;

        string sceneName = gameObject.scene.name;
        if (hasDialogue && playOnlyOncePerSession && playedScenes.Contains(sceneName)) hasDialogue = false;
        if (hasDialogue) playedScenes.Add(sceneName);

        bool hasCountdown = countdownSeconds > 0;

        if (!hasDialogue && !hasCountdown) return;

        playDialogue = hasDialogue;
        playCountdown = hasCountdown;
        sequenceActive = true;

        if (playCountdown) PrepareCountdownAudio();

        // Segura o boss: o comportamento dele só começa quando a luta começar
        bossController = FindAnyObjectByType<BossController>();
        if (bossController != null && bossController.TryGetComponent(out bossAnimator))
        {
            bossAnimator.enabled = false;
        }

        // Segura o tiro do player (o clique do diálogo e a contagem não podem virar tiro)
        shooter = FindAnyObjectByType<PlayerShooter>();
        if (shooter != null) shooter.enabled = false;

        // Congela / segura a música imediatamente, antes de qualquer Start() dos outros scripts
        if (playDialogue) CutsceneState.Begin();
        if (playCountdown) CutsceneState.BeginCountdown();
    }

    private IEnumerator Start()
    {
        if (!sequenceActive) yield break;

        // Garante que nenhum padrão de tiro do boss tenha começado antes da hora
        if (bossController != null) bossController.StopAllPatterns();

        if (playDialogue) yield return StartCoroutine(RunDialogue());

        if (playCountdown) yield return StartCoroutine(RunCountdown());
        else ReleaseGame(true);
    }

    // =====================================================
    // --- 1. DIÁLOGO ---
    // =====================================================

    private IEnumerator RunDialogue()
    {
        // A caixa de diálogo fica na cena de HUD (carregada de forma aditiva): espera ela existir
        float waited = 0f;
        while (DialoguePlayer.Instance == null && waited < waitForUITimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        // Não achou? Tenta recuperar (objeto desativado) e, se não der, explica o motivo no Console
        if (DialoguePlayer.Instance == null)
        {
            TryRecoverDialoguePlayer();
        }

        if (DialoguePlayer.Instance != null)
        {
            bool finished = false;
            DialoguePlayer.Instance.Play(dialogue, () => finished = true);

            while (!finished) yield return null;

            // Espera soltar o botão do mouse (o clique final do diálogo)
            while (Input.GetMouseButton(0)) yield return null;
        }

        // Descongela o jogo. Se houver contagem, a música continua parada.
        CutsceneState.End();
        onDialogueFinished?.Invoke();
    }

    // =====================================================
    // --- 2. CONTAGEM REGRESSIVA ---
    // =====================================================

    private IEnumerator RunCountdown()
    {
        if (hideBossDuringCountdown) HideBossVisuals();

        TMP_Text text = GetCountdownText();

        for (int n = countdownSeconds; n >= 1; n--)
        {
            ShowText(text, n.ToString());
            PlaySound(tickClip);

            float t = 0f;
            while (t < 1f)
            {
                // Tempo escalado: se o jogador pausar com ESC, a contagem também pausa
                t += Time.deltaTime;
                PopEffect(text, t);
                yield return null;
            }
        }

        // Fim da contagem: libera a música, o boss e o tiro
        ReleaseGame(true);

        ShowText(text, goText);
        PlaySound(goClip);

        float g = 0f;
        while (g < goDuration)
        {
            g += Time.deltaTime;
            PopEffect(text, g);
            yield return null;
        }

        CleanupText();
    }

    // Efeito simples: o texto "estoura" de 1.5x para 1x no início de cada número
    private static void PopEffect(TMP_Text text, float t)
    {
        if (text == null) return;
        text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(t * 4f));
    }

    private static void ShowText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value;
        text.gameObject.SetActive(true);
    }

    private TMP_Text GetCountdownText()
    {
        if (countdownText != null) return countdownText;

        // Cria um Canvas simples só para a contagem (nada para montar no editor)
        runtimeCanvas = new GameObject("CountdownCanvas", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(runtimeCanvas, gameObject.scene);

        Canvas canvas = runtimeCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // abaixo da caixa de diálogo (100)

        CanvasScaler scaler = runtimeCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObj = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(runtimeCanvas.transform, false);

        RectTransform rt = (RectTransform)textObj.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1000f, 400f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.raycastTarget = false;
        tmp.text = "";

        return tmp;
    }

    private void PrepareCountdownAudio()
    {
        if (countdownAudioSource == null) return;

        // Sem isto, a pausa de áudio usada durante a contagem (que segura a música) também silenciaria este som
        countdownAudioSource.ignoreListenerPause = true;

        if (countdownAudioSource.playOnAwake && countdownAudioSource.clip != null)
        {
            Debug.LogWarning("[StageIntro] O AudioSource da contagem ('" + countdownAudioSource.name +
                             "') parece ser o da música (tem clip e Play On Awake). Use um AudioSource separado, só para os sons da contagem.");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (countdownAudioSource == null || clip == null) return;
        countdownAudioSource.PlayOneShot(clip, soundVolume);
    }

    private void CleanupText()
    {
        if (runtimeCanvas != null)
        {
            Destroy(runtimeCanvas);
            runtimeCanvas = null;
        }
        else if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    // =====================================================
    // --- 3. LIBERAÇÃO DO JOGO ---
    // =====================================================

    private void ReleaseGame(bool notify)
    {
        if (!sequenceActive) return;
        sequenceActive = false;

        CutsceneState.End();          // por segurança (não faz nada se o diálogo já terminou)
        CutsceneState.EndCountdown(); // a música volta a tocar aqui

        ShowBossVisuals();

        // Libera o boss: ao religar, o Animator recomeça do primeiro behavior
        if (bossAnimator != null)
        {
            bossAnimator.enabled = true;
            bossAnimator = null;
        }

        if (shooter != null)
        {
            shooter.enabled = true;
            shooter = null;
        }

        if (notify) onStageStarted?.Invoke();
    }

    private void HideBossVisuals()
    {
        if (bossController == null) return;

        foreach (Renderer r in bossController.GetComponentsInChildren<Renderer>(true))
        {
            if (r.enabled)
            {
                r.enabled = false;
                hiddenRenderers.Add(r);
            }
        }
    }

    private void ShowBossVisuals()
    {
        foreach (Renderer r in hiddenRenderers)
        {
            if (r != null) r.enabled = true;
        }
        hiddenRenderers.Clear();
    }

    private void OnDestroy()
    {
        // Se a cena for descarregada no meio da sequência, não deixa o jogo congelado ou sem som
        ReleaseGame(false);
        CleanupText();

        if (CountdownAudioSource == countdownAudioSource) CountdownAudioSource = null;
    }

    // =====================================================
    // --- DIAGNÓSTICO DO DIALOGUEPLAYER ---
    // =====================================================

    private void TryRecoverDialoguePlayer()
    {
        DialoguePlayer found = FindAnyObjectByType<DialoguePlayer>(FindObjectsInactive.Include);

        if (found == null)
        {
            Debug.LogWarning("[StageIntro] Não existe nenhum DialoguePlayer nas cenas carregadas (" + LoadedScenes() +
                             "). Monte a caixa de diálogo na cena de HUD e SALVE a cena. Pulando o diálogo.");
            return;
        }

        if (!found.gameObject.activeInHierarchy)
        {
            // Se o próprio objeto estava desativado, isto roda o Awake e registra o DialoguePlayer
            found.gameObject.SetActive(true);

            if (found.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[StageIntro] O objeto '" + found.name + "' (DialoguePlayer) estava desativado e foi ativado automaticamente. " +
                                 "Deixe-o ativo na cena de HUD para não depender disso.");
            }
            else
            {
                Debug.LogWarning("[StageIntro] O DialoguePlayer está em '" + GetPath(found.transform) +
                                 "', mas um objeto pai está desativado. Ative o pai na cena de HUD. Pulando o diálogo.");
            }
            return;
        }

        Debug.LogWarning("[StageIntro] O DialoguePlayer existe e está ativo, mas não inicializou. " +
                         "Procure erros em vermelho no Console (Panel ou Body Text sem atribuir?). Pulando o diálogo.");
    }

    private static string LoadedScenes()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(SceneManager.GetSceneAt(i).name);
        }
        return sb.ToString();
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}