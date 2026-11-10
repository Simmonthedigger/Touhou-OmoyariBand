using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Caixa de diálogo. Fica na cena de HUD (compartilhada por todas as fases).
/// Usa tempo NÃO escalado, então funciona normalmente com Time.timeScale = 0.
/// </summary>
public class DialoguePlayer : MonoBehaviour
{
    public static DialoguePlayer Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Painel da caixa de diálogo (filho deste objeto). Começa desligado.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("Opcional: retrato de quem fala.")]
    [SerializeField] private Image portraitImage;
    [Tooltip("Opcional: ícone (ex.: seta ▼) mostrado quando a fala termina de aparecer.")]
    [SerializeField] private GameObject continueIndicator;

    [Header("Texto")]
    [Tooltip("Letras por segundo no efeito de digitação. 0 = mostra a fala inteira de uma vez.")]
    [SerializeField] private float charsPerSecond = 45f;

    [Header("Áudio (Sons Estilo Undertale)")]
    [Tooltip("AudioSource para os bips de texto. Se vazio, será criado automaticamente.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Tooltip("Som de bip padrão caso nada seja configurado no DialogueData.")]
    [SerializeField] private AudioClip fallbackVoiceSound;

    [Tooltip("Toca o som a cada N caracteres revelados (1 = toda letra, 2 = letra sim/letra não).")]
    [SerializeField, Range(1, 5)] private int soundFrequency = 2;

    [Tooltip("Pitch base do som (1 = normal, 1.5 = mais agudo, 0.8 = mais grave).")]
    [SerializeField, Range(0.5f, 2f)] private float basePitch = 1.0f;

    public bool IsPlaying { get; private set; }

    private DialogueData data;
    private Action onFinished;
    private int lineIndex;
    private int totalChars;
    private float visibleChars;
    private bool lineComplete;
    private float inputUnlockTime;
    private CanvasGroup canvasGroup;

    private int lastPlayedCharIndex = -1;

    private void Awake()
    {
        if (panel == null || bodyText == null)
        {
            Debug.LogError("[DialoguePlayer] Atribua o Panel e o Body Text.");
            enabled = false;
            return;
        }

        if (panel == gameObject)
        {
            Debug.LogError("[DialoguePlayer] O Panel precisa ser um objeto FILHO, não o mesmo objeto do script.");
            enabled = false;
            return;
        }

        Instance = this;

        // Prepara o AudioSource e garante que ignore a pausa global (AudioListener.pause = true)
        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
        }
        voiceAudioSource.ignoreListenerPause = true;
        voiceAudioSource.playOnAwake = false;

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(DialogueData dialogue, Action finished)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            finished?.Invoke();
            return;
        }

        data = dialogue;
        onFinished = finished;
        lineIndex = -1;
        IsPlaying = true;

        inputUnlockTime = Time.unscaledTime + 0.25f;

        canvasGroup.alpha = 1f;
        panel.SetActive(true);
        ShowNextLine();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        bool gamePaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Pausado;
        canvasGroup.alpha = gamePaused ? 0f : 1f;
        if (gamePaused) return;

        // 1. Entrada de avanço
        if (Time.unscaledTime >= inputUnlockTime)
        {
            bool advance = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

            if (advance)
            {
                if (!lineComplete) CompleteLine();
                else ShowNextLine();
                return;
            }
        }

        // 2. Efeito de digitação com emissão de som
        if (!lineComplete)
        {
            if (charsPerSecond <= 0f)
            {
                CompleteLine();
                return;
            }

            visibleChars += charsPerSecond * Time.unscaledDeltaTime;
            int currentVisibleCount = Mathf.FloorToInt(visibleChars);

            bodyText.maxVisibleCharacters = currentVisibleCount;

            // Dispara o bip de voz sempre que um novo caractere for revelado
            if (currentVisibleCount > lastPlayedCharIndex)
            {
                PlayCharacterSound(currentVisibleCount);
                lastPlayedCharIndex = currentVisibleCount;
            }

            if (visibleChars >= totalChars) CompleteLine();
        }
    }

    private void PlayCharacterSound(int charIndex)
    {
        if (voiceAudioSource == null) return;
        if (charIndex % soundFrequency != 0) return; // Filtra a frequência para não encavalar o som

        DialogueData.Line currentLine = data.lines[lineIndex];

        // Determina qual som usar (Prioridade: Fala -> Diálogo -> Fallback)
        AudioClip clipToPlay = currentLine.voiceSound;
        if (clipToPlay == null && data != null) clipToPlay = data.defaultVoiceSound;
        if (clipToPlay == null) clipToPlay = fallbackVoiceSound;

        if (clipToPlay == null) return;

        // Ignora espaços e pontuações na hora de tocar o bip
        if (charIndex - 1 < bodyText.textInfo.characterInfo.Length)
        {
            char character = bodyText.textInfo.characterInfo[Mathf.Max(0, charIndex - 1)].character;
            if (char.IsWhiteSpace(character) || char.IsPunctuation(character)) return;
        }

        // Variação leve no pitch (estilo Undertale/Animal Crossing)
        float variance = currentLine.pitchVariance;
        float randomPitch = UnityEngine.Random.Range(basePitch - variance, basePitch + variance);

        voiceAudioSource.pitch = randomPitch;
        voiceAudioSource.PlayOneShot(clipToPlay);
    }

    private void ShowNextLine()
    {
        lineIndex++;

        if (lineIndex >= data.lines.Length)
        {
            Finish();
            return;
        }

        DialogueData.Line line = data.lines[lineIndex];

        if (speakerText != null)
        {
            speakerText.text = line.speaker;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.enabled = line.portrait != null;
        }

        bodyText.text = line.text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        totalChars = bodyText.textInfo.characterCount;
        visibleChars = 0f;
        lastPlayedCharIndex = 0;
        lineComplete = false;

        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    private void CompleteLine()
    {
        bodyText.maxVisibleCharacters = int.MaxValue;
        lineComplete = true;

        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void Finish()
    {
        IsPlaying = false;
        panel.SetActive(false);

        Action callback = onFinished;
        onFinished = null;
        data = null;

        callback?.Invoke();
    }
}