using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Desenha o "note highway" (4 lanes, notas caindo até a linha de acerto).
/// O painel fica desligado e só aparece durante os trechos jogáveis.
/// Todo o visual é criado por código: você só precisa de um painel (RectTransform) vazio.
/// IMPORTANTE: este script deve ficar em um objeto que está SEMPRE ativo; o painel é um FILHO dele.
/// </summary>
public class NoteHighwayUI : MonoBehaviour
{
    private const int LaneCount = 4;

    private class NoteVisual
    {
        public RectTransform rt;
        public Image image;
    }

    [Header("Referências")]
    [SerializeField] private RhythmController controller;
    [Tooltip("Painel que será ligado/desligado. Deve ser um objeto FILHO diferente deste.")]
    [SerializeField] private RectTransform highwayPanel;

    [Header("Layout")]
    [Tooltip("Altura da linha de acerto, de 0 (base) a 1 (topo) do painel.")]
    [SerializeField, Range(0.05f, 0.4f)] private float hitLineHeight = 0.15f;
    [SerializeField] private float noteHeight = 28f;

    [Header("Lanes (G, D, Em, C)")]
    [SerializeField] private string[] laneNames = { "G", "D", "Em", "C" };
    [SerializeField]
    private Color[] laneColors =
    {
        new Color(0.25f, 0.85f, 0.35f),
        new Color(0.95f, 0.30f, 0.30f),
        new Color(1.00f, 0.85f, 0.25f),
        new Color(0.30f, 0.60f, 1.00f)
    };
    [Tooltip("Opcional. Se vazio, usa a fonte padrão da Unity.")]
    [SerializeField] private Font labelFont;

    private readonly Image[] targets = new Image[LaneCount];
    private readonly Dictionary<RhythmController.RuntimeNote, NoteVisual> visuals = new Dictionary<RhythmController.RuntimeNote, NoteVisual>();
    private readonly Stack<NoteVisual> pool = new Stack<NoteVisual>();

    private Image hitLineImage;
    private RectTransform notesLayer;
    private bool built;
    private bool showing;
    private float lookAhead = 2f;
    private float missFlash;

    private void Awake()
    {
        if (controller == null || highwayPanel == null)
        {
            Debug.LogError("[NoteHighwayUI] Atribua o RhythmController e o Highway Panel.");
            enabled = false;
            return;
        }

        if (highwayPanel.gameObject == gameObject)
        {
            Debug.LogError("[NoteHighwayUI] O Highway Panel precisa ser um objeto FILHO, não o mesmo objeto do script.");
            enabled = false;
            return;
        }

        highwayPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (controller == null) return;
        controller.SegmentStarted += HandleSegmentStarted;
        controller.SegmentFinished += HandleSegmentFinished;
        controller.NoteMissed += HandleNoteMissed;
    }

    private void OnDisable()
    {
        if (controller == null) return;
        controller.SegmentStarted -= HandleSegmentStarted;
        controller.SegmentFinished -= HandleSegmentFinished;
        controller.NoteMissed -= HandleNoteMissed;
    }

    // ---------------- Eventos ----------------

    private void HandleSegmentStarted(RhythmSegment segment)
    {
        EnsureBuilt();
        lookAhead = Mathf.Max(0.1f, segment.approachTime);
        highwayPanel.gameObject.SetActive(true);
        showing = true;
    }

    private void HandleSegmentFinished(int hits, int total)
    {
        showing = false;

        foreach (KeyValuePair<RhythmController.RuntimeNote, NoteVisual> kv in visuals)
        {
            kv.Value.rt.gameObject.SetActive(false);
            pool.Push(kv.Value);
        }
        visuals.Clear();

        highwayPanel.gameObject.SetActive(false);
    }

    private void HandleNoteMissed(int lane)
    {
        missFlash = 0.25f;
    }

    // ---------------- Atualização visual ----------------

    private void Update()
    {
        if (!showing) return;

        Rect rect = highwayPanel.rect;
        float laneWidth = rect.width / LaneCount;
        float hitY = rect.height * hitLineHeight;
        float pixelsPerSecond = (rect.height - hitY) / lookAhead;
        double songTime = controller.SongTime;

        IReadOnlyList<RhythmController.RuntimeNote> notes = controller.ActiveNotes;
        for (int i = 0; i < notes.Count; i++)
        {
            RhythmController.RuntimeNote note = notes[i];
            float dt = (float)(note.time - songTime);
            float y = hitY + dt * pixelsPerSecond;

            // Acertada, ainda longe no futuro ou já fora da tela: sem visual
            if (note.state == RhythmController.NoteState.Hit || dt > lookAhead || y < -noteHeight)
            {
                Release(note);
                continue;
            }

            NoteVisual v = Acquire(note);
            v.rt.anchoredPosition = new Vector2((note.lane + 0.5f) * laneWidth, y);
            v.rt.sizeDelta = new Vector2(laneWidth * 0.7f, noteHeight);
            v.image.color = note.state == RhythmController.NoteState.Missed ? new Color(0.45f, 0.45f, 0.45f, 0.8f) : LaneColor(note.lane);
        }

        // Feedback das teclas: alvo acende quando a tecla está pressionada
        Keyboard kb = Keyboard.current;
        for (int lane = 0; lane < LaneCount; lane++)
        {
            bool pressed = kb != null && lane < controller.LaneKeys.Count && controller.LaneKeys[lane] != Key.None && kb[controller.LaneKeys[lane]].isPressed;
            Color c = LaneColor(lane);
            c.a = pressed ? 1f : 0.45f;
            targets[lane].color = c;
        }

        // Linha de acerto pisca em vermelho quando erra
        missFlash = Mathf.Max(0f, missFlash - Time.unscaledDeltaTime);
        hitLineImage.color = Color.Lerp(Color.white, Color.red, missFlash / 0.25f);
    }

    // ---------------- Notas (pool) ----------------

    private NoteVisual Acquire(RhythmController.RuntimeNote note)
    {
        if (visuals.TryGetValue(note, out NoteVisual existing)) return existing;

        NoteVisual v;
        if (pool.Count > 0)
        {
            v = pool.Pop();
            v.rt.gameObject.SetActive(true);
        }
        else
        {
            RectTransform rt = CreateRect("Note", notesLayer);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            v = new NoteVisual { rt = rt, image = AddImage(rt, Color.white) };
        }

        visuals[note] = v;
        return v;
    }

    private void Release(RhythmController.RuntimeNote note)
    {
        if (!visuals.TryGetValue(note, out NoteVisual v)) return;

        visuals.Remove(note);
        v.rt.gameObject.SetActive(false);
        pool.Push(v);
    }

    // ---------------- Construção do layout ----------------

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        // Fundo
        RectTransform bg = CreateRect("Background", highwayPanel);
        SetAnchors(bg, Vector2.zero, Vector2.one);
        AddImage(bg, new Color(0f, 0f, 0f, 0.6f));

        // Faixas (lanes)
        for (int lane = 0; lane < LaneCount; lane++)
        {
            RectTransform laneRt = CreateRect("Lane" + lane, highwayPanel);
            SetAnchors(laneRt, new Vector2(lane / (float)LaneCount, 0f), new Vector2((lane + 1) / (float)LaneCount, 1f));
            laneRt.offsetMin = new Vector2(2f, 0f);
            laneRt.offsetMax = new Vector2(-2f, 0f);

            Color c = LaneColor(lane);
            c.a = 0.12f;
            AddImage(laneRt, c);
        }

        // Linha de acerto
        RectTransform line = CreateRect("HitLine", highwayPanel);
        line.anchorMin = new Vector2(0f, hitLineHeight);
        line.anchorMax = new Vector2(1f, hitLineHeight);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(0f, 4f);
        hitLineImage = AddImage(line, Color.white);

        // Alvos (onde a nota deve estar quando você aperta)
        for (int lane = 0; lane < LaneCount; lane++)
        {
            RectTransform t = CreateRect("Target" + lane, highwayPanel);
            t.anchorMin = new Vector2(lane / (float)LaneCount + 0.02f, hitLineHeight);
            t.anchorMax = new Vector2((lane + 1) / (float)LaneCount - 0.02f, hitLineHeight);
            t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero;
            t.sizeDelta = new Vector2(0f, noteHeight + 8f);

            Color c = LaneColor(lane);
            c.a = 0.45f;
            targets[lane] = AddImage(t, c);
        }

        // Camada das notas
        notesLayer = CreateRect("Notes", highwayPanel);
        SetAnchors(notesLayer, Vector2.zero, Vector2.one);

        // Rótulos (tecla + acorde) abaixo da linha
        Font font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int lane = 0; lane < LaneCount; lane++)
        {
            RectTransform l = CreateRect("Label" + lane, highwayPanel);
            SetAnchors(l, new Vector2(lane / (float)LaneCount, 0f), new Vector2((lane + 1) / (float)LaneCount, hitLineHeight));
            l.offsetMax = new Vector2(0f, -(noteHeight * 0.5f + 8f));

            Text text = l.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = LaneColor(lane);
            text.raycastTarget = false;
            text.text = KeyLabel(lane) + "\n" + LaneName(lane);
        }
    }

    private string KeyLabel(int lane)
    {
        return lane < controller.LaneKeys.Count ? controller.LaneKeys[lane].ToString() : "?";
    }

    private string LaneName(int lane)
    {
        return (laneNames != null && lane < laneNames.Length) ? laneNames[lane] : "";
    }

    private Color LaneColor(int lane)
    {
        return (laneColors != null && lane < laneColors.Length) ? laneColors[lane] : Color.white;
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

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
