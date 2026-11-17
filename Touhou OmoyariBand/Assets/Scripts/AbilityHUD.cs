using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Barra de mana + nome e tecla da habilidade. Vai no objeto do Player (junto do PlayerAbilities).
/// A interface é criada sozinha em tempo de execução, então não precisa montar nada no editor.
/// Fica escondida quando a fase não tem habilidade escolhida e quando o jogo está congelado
/// (pausa, diálogo, game over), para não aparecer por cima dos painéis.
/// </summary>
[RequireComponent(typeof(PlayerAbilities), typeof(ManaSystem))]
public class AbilityHUD : MonoBehaviour
{
    private const float LabelHeight = 34f;

    [Header("Posição na tela")]
    [Tooltip("(0,0) = canto inferior esquerdo, (1,0) = inferior direito, (0,1) = superior esquerdo, (1,1) = superior direito.")]
    [SerializeField] private Vector2 screenAnchor = new Vector2(0f, 0f);
    [Tooltip("Distância da borda da tela, em pixels (na resolução de referência 1920x1080).")]
    [SerializeField] private Vector2 offset = new Vector2(30f, 30f);
    [SerializeField] private Vector2 barSize = new Vector2(380f, 34f);

    [Header("Cores")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [Tooltip("Habilidade pronta para usar.")]
    [SerializeField] private Color readyColor = new Color(0.25f, 0.8f, 1f);
    [Tooltip("Sem mana suficiente ou em espera.")]
    [SerializeField] private Color notReadyColor = new Color(0.35f, 0.45f, 0.6f);
    [Tooltip("Habilidade em uso.")]
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.3f);

    private PlayerAbilities abilities;
    private ManaSystem mana;

    private GameObject canvasObj;
    private RectTransform fillRect;
    private Image fillImage;
    private TextMeshProUGUI label;
    private TextMeshProUGUI manaText;

    private string lastLabel;
    private bool lastVisible = true;

    private void Awake()
    {
        abilities = GetComponent<PlayerAbilities>();
        mana = GetComponent<ManaSystem>();
    }

    private void Start()
    {
        BuildUI();
    }

    private void Update()
    {
        if (canvasObj == null) return;

        AbilityData ability = abilities.Current;

        // Sem habilidade escolhida: esconde a barra
        bool visible = ability != null && Time.timeScale > 0f;
        if (visible != lastVisible)
        {
            canvasObj.SetActive(visible);
            lastVisible = visible;
        }
        if (!visible) return;

        // Barra
        float n = mana.Normalized;
        fillImage.enabled = n > 0.001f;
        fillRect.anchorMax = new Vector2(Mathf.Max(0.0001f, n), 1f);

        bool active = abilities.IsHoldActive || abilities.IsInvulnerable;
        bool ready = ability.type == AbilityType.InvulnerableDash
            ? mana.Current >= ability.manaCost && abilities.CooldownRemaining <= 0f
            : mana.Current >= ability.minManaToStart;

        fillImage.color = active ? activeColor : (ready ? readyColor : notReadyColor);

        string manaString = Mathf.FloorToInt(mana.Current) + " / " + Mathf.FloorToInt(mana.Max);
        if (manaText.text != manaString) manaText.text = manaString;

        // Nome, tecla, custo e estado
        string newLabel = ability.displayName + " [" + abilities.GetKeyLabel() + "]  " + ability.GetCostText();
        if (abilities.CooldownRemaining > 0f) newLabel += "  " + abilities.CooldownRemaining.ToString("0.0") + "s";
        else if (active) newLabel += "  ATIVA";

        if (newLabel != lastLabel)
        {
            label.text = newLabel;
            lastLabel = newLabel;
        }
    }

    private void OnDestroy()
    {
        if (canvasObj != null) Destroy(canvasObj);
    }

    // ---------------- Construção da interface ----------------

    private void BuildUI()
    {
        canvasObj = new GameObject("AbilityHUD_Canvas", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObj, gameObject.scene);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // abaixo da contagem (90) e da caixa de diálogo (100)

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Painel ancorado no canto escolhido
        RectTransform panel = CreateRect("Panel", canvasObj.transform);
        panel.anchorMin = screenAnchor;
        panel.anchorMax = screenAnchor;
        panel.pivot = screenAnchor;
        panel.sizeDelta = new Vector2(barSize.x, barSize.y + LabelHeight);
        panel.anchoredPosition = new Vector2(
            screenAnchor.x > 0.5f ? -offset.x : offset.x,
            screenAnchor.y > 0.5f ? -offset.y : offset.y);

        // Nome da habilidade (em cima da barra)
        RectTransform labelRt = CreateRect("Label", panel);
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0.5f, 1f);
        labelRt.sizeDelta = new Vector2(0f, LabelHeight);
        labelRt.anchoredPosition = Vector2.zero;
        label = AddText(labelRt, 22f, TextAlignmentOptions.Left);

        // Fundo da barra
        RectTransform bar = CreateRect("Bar", panel);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, barSize.y);
        bar.anchoredPosition = Vector2.zero;
        AddImage(bar, backgroundColor);

        // Preenchimento (a largura é controlada pela âncora direita)
        fillRect = CreateRect("Fill", bar);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        fillImage = AddImage(fillRect, readyColor);

        // Número da mana no centro da barra
        RectTransform textRt = CreateRect("ManaText", bar);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        manaText = AddText(textRt, 20f, TextAlignmentOptions.Center);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static Image AddImage(RectTransform rt, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = alignment;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
