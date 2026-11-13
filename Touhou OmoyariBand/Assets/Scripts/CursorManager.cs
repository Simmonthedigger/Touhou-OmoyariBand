using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cursor personalizado com 3 estilos:
///  - Default: cursor padrão do jogo;
///  - Aim:     mira, quando o mouse está sobre o Boss/inimigos E o player pode atirar;
///  - Button:  quando o mouse está sobre um botão (ou outro controle de UI clicável).
/// Prioridade: UI (botão) > alvo > padrão.
/// Coloque em um objeto vazio na PRIMEIRA cena do jogo. Ele persiste entre as cenas.
/// </summary>
public class CursorManager : MonoBehaviour
{
    public enum CursorState { Default, Aim, Button }

    [System.Serializable]
    public class CursorStyle
    {
        [Tooltip("Imagem do cursor (PNG com fundo transparente, Texture Type = Cursor). 32x32 é o tamanho mais seguro.")]
        public Texture2D texture;

        [Tooltip("Ponto da imagem que 'clica', em pixels a partir do canto SUPERIOR ESQUERDO. Ex.: (0,0) para uma seta. Ignorado se 'Center Hotspot' estiver ligado.")]
        public Vector2 hotspot = Vector2.zero;

        [Tooltip("Ligue para miras: o clique acontece no centro da imagem.")]
        public bool centerHotspot = false;
    }

    public static CursorManager Instance { get; private set; }

    [Header("Estilos (um estilo sem imagem usa o padrão)")]
    [SerializeField] private CursorStyle defaultStyle = new CursorStyle();
    [SerializeField] private CursorStyle aimStyle = new CursorStyle { centerHotspot = true };
    [SerializeField] private CursorStyle buttonStyle = new CursorStyle();

    [Header("Alvos da mira (partida)")]
    [Tooltip("Tags dos objetos que mudam o cursor para a mira. Mesmas tags usadas no Target Tags do PlayerShooter.")]
    [SerializeField] private string[] targetTags = { "Boss" };

    [Header("Opções")]
    [Tooltip("Auto = cursor do sistema (mais rápido, ideal até 32x32). ForceSoftware = desenhado pela Unity (aceita imagens maiores, mas com leve atraso).")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    private CursorState current = CursorState.Default;

    private Camera cam;
    private PlayerShooter shooter;
    private float nextShooterSearch;

    private ContactFilter2D contactFilter;
    private readonly List<Collider2D> colliderHits = new List<Collider2D>();
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // já existe um (ex.: voltou para a primeira cena)
            return;
        }

        Instance = this;

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Detecta também colliders marcados como Trigger
        contactFilter = new ContactFilter2D().NoFilter();
        contactFilter.useTriggers = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Apply(CursorState.Default);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Novas cenas: refaz as buscas e reaplica o cursor atual
        shooter = null;
        nextShooterSearch = 0f;
        cam = null;
        Apply(current);
    }

    // Ao voltar o foco para a janela do jogo, o sistema pode ter trocado o cursor
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) Apply(current);
    }

    private void Update()
    {
        if (Instance != this) return;

        CursorState wanted = DetectState();
        if (wanted != current) Apply(wanted);
    }

    // ---------------- Detecção ----------------

    private CursorState DetectState()
    {
        // 1. UI primeiro: botão clicável tem prioridade sobre qualquer coisa
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            if (pointerData == null || pointerSystem != es)
            {
                pointerSystem = es;
                pointerData = new PointerEventData(es);
            }

            pointerData.position = Input.mousePosition;
            uiHits.Clear();
            es.RaycastAll(pointerData, uiHits);

            foreach (RaycastResult hit in uiHits)
            {
                // Só interessa UI de Canvas (ignora raycasts de física). O 1º resultado é o que está por cima.
                if (!(hit.module is GraphicRaycaster)) continue;

                Selectable selectable = hit.gameObject.GetComponentInParent<Selectable>();
                return (selectable != null && selectable.IsInteractable()) ? CursorState.Button : CursorState.Default;
            }
        }

        // 2. Alvos (Boss/inimigos), só se o player puder atirar agora
        if (CanAim() && IsOverTarget()) return CursorState.Aim;

        return CursorState.Default;
    }

    // A mira só aparece quando o clique realmente atiraria:
    // fora de pausa/diálogo (timeScale > 0) e com o PlayerShooter ligado (desligado na contagem regressiva).
    private bool CanAim()
    {
        if (Time.timeScale <= 0f) return false;

        if (shooter == null)
        {
            if (Time.unscaledTime >= nextShooterSearch)
            {
                shooter = FindAnyObjectByType<PlayerShooter>();
                nextShooterSearch = Time.unscaledTime + 1f; // nos menus, tenta no máximo 1x por segundo
            }

            if (shooter == null) return false;
        }

        return shooter.isActiveAndEnabled;
    }

    private bool IsOverTarget()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z); // necessário se a câmera for perspectiva
        Vector2 worldPoint = cam.ScreenToWorldPoint(mousePos);

        colliderHits.Clear();
        Physics2D.OverlapPoint(worldPoint, contactFilter, colliderHits);

        foreach (Collider2D col in colliderHits)
        {
            if (HasTargetTag(col.transform)) return true;
        }

        return false;
    }

    // Aceita a tag no próprio objeto ou no pai (mesma regra do PlayerShooter)
    private bool HasTargetTag(Transform t)
    {
        foreach (string tag in targetTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            if (t.CompareTag(tag)) return true;
            if (t.parent != null && t.parent.CompareTag(tag)) return true;
        }

        return false;
    }

    // ---------------- Aplicação do cursor ----------------

    private void Apply(CursorState state)
    {
        current = state;

        CursorStyle style = GetStyle(state);

        // Estilo sem imagem: usa o padrão do jogo
        if (style == null || style.texture == null) style = defaultStyle;

        // Sem nenhuma imagem: volta ao cursor do sistema
        if (style == null || style.texture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            return;
        }

        Vector2 hotspot = style.centerHotspot
            ? new Vector2(style.texture.width * 0.5f, style.texture.height * 0.5f)
            : style.hotspot;

        Cursor.SetCursor(style.texture, hotspot, cursorMode);
    }

    private CursorStyle GetStyle(CursorState state)
    {
        switch (state)
        {
            case CursorState.Aim: return aimStyle;
            case CursorState.Button: return buttonStyle;
            default: return defaultStyle;
        }
    }
}
