using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Caixa de diálogo. Fica na cena de HUD (compartilhada por todas as fases).
/// Usa tempo NÃO escalado, então funciona normalmente com Time.timeScale = 0.
/// IMPORTANTE: este script deve ficar em um objeto SEMPRE ativo; o 'panel' é um FILHO que é ligado/desligado.
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

    public bool IsPlaying { get; private set; }

    private DialogueData data;
    private Action onFinished;
    private int lineIndex;
    private int totalChars;
    private float visibleChars;
    private bool lineComplete;
    private float inputUnlockTime;

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
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Inicia um diálogo. 'finished' é chamado quando o jogador passa a última fala.</summary>
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

        // Pequena folga para ignorar um clique que ainda esteja "sobrando" da tela anterior
        inputUnlockTime = Time.unscaledTime + 0.25f;

        panel.SetActive(true);
        ShowNextLine();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // 1. Entrada: clique esquerdo, Espaço ou Enter
        if (Time.unscaledTime >= inputUnlockTime)
        {
            bool advance = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

            if (advance)
            {
                if (!lineComplete) CompleteLine(); // 1º clique: mostra a fala inteira
                else ShowNextLine();               // 2º clique: próxima fala
                return;
            }
        }

        // 2. Efeito de digitação
        if (!lineComplete)
        {
            if (charsPerSecond <= 0f)
            {
                CompleteLine();
                return;
            }

            visibleChars += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(visibleChars);

            if (visibleChars >= totalChars) CompleteLine();
        }
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
