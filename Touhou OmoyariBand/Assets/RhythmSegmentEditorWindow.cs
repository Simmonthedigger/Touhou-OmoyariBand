using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor visual de RhythmSegment. Menu: Tools > Rhythm > Segment Editor.
/// - Mostra a onda da música, a grade de batidas (a partir do "Beat 0") e as 4 lanes.
/// - A faixa azul clara nas lanes é onde o painel do jogo ficará visível (ActiveStart até ActiveEnd).
/// - Clique esquerdo numa lane: coloca/remove nota (sempre na grade). Clique direito: remove.
/// - Espaço: tocar/pausar. Roda do mouse: rola. Ctrl + roda: zoom. Clique na faixa da onda: move o cursor.
/// - Botão "Beat 0 = cursor": põe o início da grade no ponto onde você parou o cursor.
/// - Modo Gravar: toque D F J K ao ritmo da música; as notas entram quantizadas na grade
///   (o teclado do editor tem precisão de frame, então confira depois e corrija com cliques).
/// Este arquivo precisa estar numa pasta chamada "Editor".
/// </summary>
public class RhythmSegmentEditorWindow : EditorWindow
{
    [MenuItem("Tools/Rhythm/Segment Editor")]
    static void Open() => GetWindow<RhythmSegmentEditorWindow>("Segment Editor");

    const float TOP = 74f, WAVE = 90f;
    const int PeaksPerSec = 200;
    const int Lanes = 4;
    static readonly int[] SnapDivs = { 1, 2, 3, 4, 6, 8 };
    static readonly string[] SnapLabels = { "1/4", "1/8", "1/12", "1/16", "1/24", "1/32" };
    static readonly KeyCode[] Hotkeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
    static readonly string[] LaneNames = { "G", "D", "Em", "C" };
    static readonly Color[] Pal =
    {
        new Color(0.25f, 0.85f, 0.35f), new Color(0.95f, 0.30f, 0.30f),
        new Color(1.00f, 0.85f, 0.25f), new Color(0.30f, 0.60f, 1.00f)
    };

    RhythmSegment seg;
    AudioClip clip;
    int snapIndex = 1;
    float pxPerSec = 200f;
    float scroll;
    float playhead;
    float[] peaks;
    AudioSource preview;
    bool playing, recordMode;

    int Div => SnapDivs[snapIndex];

    void OnEnable()
    {
        EditorApplication.update += Tick;
        Selection.selectionChanged += OnSelect;
        OnSelect();
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
        Selection.selectionChanged -= OnSelect;
        StopPlay();
        if (preview != null) DestroyImmediate(preview.gameObject);
    }

    void OnSelect()
    {
        if (Selection.activeObject is RhythmSegment s && s != seg)
        {
            seg = s;
            scroll = Mathf.Max(0f, seg.ActiveStart - 1f);
            playhead = scroll;
            Repaint();
        }
    }

    // ---------- Conversões ----------
    float BeatOf(float t) => (t - seg.songTimeOfBeatZero) * seg.bpm / 60f;
    float SnapBeat(float t) => Mathf.Round(BeatOf(t) * Div) / Div;
    float TimeOfBeat(float beat) => seg.songTimeOfBeatZero + beat * 60f / Mathf.Max(1f, seg.bpm);

    // ---------- Áudio ----------
    void BuildPeaks()
    {
        peaks = null;
        if (clip == null) return;

        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0))
        {
            Debug.LogWarning("Não consegui ler a música. Nas configurações do AudioClip use Load Type = Decompress On Load.");
            return;
        }

        int frames = clip.samples, ch = clip.channels;
        int perBucket = Mathf.Max(1, clip.frequency / PeaksPerSec);
        peaks = new float[frames / perBucket + 1];
        for (int f = 0; f < frames; f++)
        {
            float m = 0f;
            for (int c = 0; c < ch; c++) m = Mathf.Max(m, Mathf.Abs(data[f * ch + c]));
            int b = f / perBucket;
            if (m > peaks[b]) peaks[b] = m;
        }
    }

    void TogglePlay()
    {
        if (playing) StopPlay(); else StartPlay();
    }

    void StartPlay()
    {
        if (clip == null) return;
        if (preview == null)
            preview = EditorUtility.CreateGameObjectWithHideFlags("SegmentPreview", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        preview.clip = clip;
        preview.time = Mathf.Clamp(playhead, 0f, clip.length - 0.01f);
        preview.Play();
        playing = true;
    }

    void StopPlay()
    {
        if (preview != null) preview.Pause();
        playing = false;
    }

    void Tick()
    {
        if (!playing || preview == null) return;
        if (!preview.isPlaying) { playing = false; Repaint(); return; }

        playhead = preview.time;
        float visible = position.width / pxPerSec;
        if (playhead > scroll + visible * 0.9f || playhead < scroll) scroll = playhead - visible * 0.1f;
        Repaint();
    }

    // ---------- Notas ----------
    void EditNotes(System.Action<List<RhythmSegment.NoteData>> edit)
    {
        Undo.RecordObject(seg, "Editar notas");
        var list = new List<RhythmSegment.NoteData>(seg.notes ?? new RhythmSegment.NoteData[0]);
        edit(list);
        list.Sort((a, b) => a.beat.CompareTo(b.beat));
        seg.notes = list.ToArray();
        EditorUtility.SetDirty(seg);
    }

    void ToggleNote(int lane, float beat)
    {
        EditNotes(list =>
        {
            int i = list.FindIndex(n => n.lane == lane && Mathf.Abs(n.beat - beat) < 0.5f / Div);
            if (i >= 0) list.RemoveAt(i);
            else list.Add(new RhythmSegment.NoteData { beat = beat, lane = lane });
        });
    }

    void AddNoteIfFree(int lane, float beat)
    {
        EditNotes(list =>
        {
            if (list.FindIndex(n => n.lane == lane && Mathf.Abs(n.beat - beat) < 0.5f / Div) < 0)
                list.Add(new RhythmSegment.NoteData { beat = beat, lane = lane });
        });
    }

    void RemoveNear(int lane, float time, float tolSeconds)
    {
        EditNotes(list =>
        {
            int i = list.FindIndex(n => n.lane == lane && Mathf.Abs(seg.GetTime(n) - time) <= tolSeconds);
            if (i >= 0) list.RemoveAt(i);
        });
    }

    // ---------- GUI ----------
    void OnGUI()
    {
        var e = Event.current;

        GUILayout.BeginArea(new Rect(4, 3, position.width - 8, TOP));
        EditorGUIUtility.labelWidth = 60;

        GUILayout.BeginHorizontal();
        seg = (RhythmSegment)EditorGUILayout.ObjectField("Trecho", seg, typeof(RhythmSegment), false, GUILayout.Width(280));
        var newClip = (AudioClip)EditorGUILayout.ObjectField("Música", clip, typeof(AudioClip), false, GUILayout.Width(280));
        if (newClip != clip) { StopPlay(); clip = newClip; playhead = 0f; BuildPeaks(); }
        GUILayout.EndHorizontal();

        if (seg != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            float bpm = EditorGUILayout.FloatField("BPM", seg.bpm, GUILayout.Width(120));
            float beat0 = EditorGUILayout.FloatField("Beat 0 (s)", seg.songTimeOfBeatZero, GUILayout.Width(160));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(seg, "Sincronia do trecho");
                seg.bpm = Mathf.Max(1f, bpm);
                seg.songTimeOfBeatZero = beat0;
                EditorUtility.SetDirty(seg);
            }
            if (GUILayout.Button("Beat 0 = cursor", GUILayout.Width(110)))
            {
                Undo.RecordObject(seg, "Beat 0 = cursor");
                seg.songTimeOfBeatZero = playhead;
                EditorUtility.SetDirty(seg);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(playing ? "Pausar (Espaço)" : "Tocar (Espaço)", GUILayout.Width(110))) TogglePlay();
        if (GUILayout.Button("Cursor no trecho", GUILayout.Width(110)) && seg != null)
        {
            playhead = Mathf.Max(0f, seg.ActiveStart);
            scroll = Mathf.Max(0f, playhead - 1f);
            if (playing && preview != null) preview.time = Mathf.Clamp(playhead, 0f, clip.length - 0.01f);
        }
        snapIndex = EditorGUILayout.Popup("Snap", snapIndex, SnapLabels, GUILayout.Width(120));
        recordMode = GUILayout.Toggle(recordMode, "Gravar (D F J K)", "Button", GUILayout.Width(120));
        if (GUILayout.Button("Salvar", GUILayout.Width(60))) AssetDatabase.SaveAssets();
        GUILayout.Label(seg != null ? (seg.notes != null ? seg.notes.Length : 0) + " notas   cursor " + playhead.ToString("F3") + "s" : "");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        var area = new Rect(0, TOP + 4, position.width, position.height - TOP - 4);
        if (seg == null)
        {
            GUI.Label(area, "Selecione (ou crie em Assets > Create > Rhythm > Segment) um RhythmSegment.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        var waveR = new Rect(area.x, area.y, area.width, WAVE);
        float lanesTop = waveR.yMax + 16f;
        float laneH = Mathf.Max(28f, (area.yMax - lanesTop) / Lanes);

        HandleEvents(e, area, waveR, lanesTop, laneH);
        if (e.type != EventType.Repaint) return;

        EditorGUI.DrawRect(area, new Color(0.13f, 0.13f, 0.15f));
        DrawLanes(area, lanesTop, laneH);
        DrawActiveRange(area, lanesTop, laneH);
        DrawGrid(area, lanesTop, laneH);
        DrawWave(waveR);
        DrawNotes(area, lanesTop, laneH);

        // Beat 0
        float bx = area.x + (seg.songTimeOfBeatZero - scroll) * pxPerSec;
        if (bx >= area.x && bx <= area.xMax)
        {
            EditorGUI.DrawRect(new Rect(bx - 1, waveR.y, 2, area.yMax - waveR.y), new Color(1f, 0.9f, 0.2f, 0.9f));
            GUI.Label(new Rect(bx + 3, waveR.yMax, 60, 14), "beat 0", EditorStyles.miniLabel);
        }

        // Cursor
        float px = area.x + (playhead - scroll) * pxPerSec;
        if (px >= area.x && px <= area.xMax)
            EditorGUI.DrawRect(new Rect(px - 1, area.y, 2, area.height), new Color(1f, 0.25f, 0.25f));

        // Nota fantasma (prévia do snap)
        var mp = e.mousePosition;
        if (mp.y >= lanesTop && mp.y < lanesTop + laneH * Lanes && mp.x >= area.x)
        {
            int lane = Mathf.Clamp(Mathf.FloorToInt((mp.y - lanesTop) / laneH), 0, Lanes - 1);
            float gt = TimeOfBeat(SnapBeat(scroll + (mp.x - area.x) / pxPerSec));
            float gx = area.x + (gt - scroll) * pxPerSec;
            var c = Pal[lane]; c.a = 0.4f;
            EditorGUI.DrawRect(new Rect(gx - 8, lanesTop + lane * laneH + laneH * 0.5f - 8, 16, 16), c);
        }
    }

    void HandleEvents(Event e, Rect area, Rect waveR, float lanesTop, float laneH)
    {
        if (e.type == EventType.MouseDown)
        {
            float t = scroll + (e.mousePosition.x - area.x) / pxPerSec;
            if (waveR.Contains(e.mousePosition))
            {
                playhead = Mathf.Max(0f, t);
                if (playing && preview != null) preview.time = Mathf.Clamp(playhead, 0f, clip.length - 0.01f);
                e.Use(); Repaint();
            }
            else if (e.mousePosition.y >= lanesTop && e.mousePosition.y < lanesTop + laneH * Lanes)
            {
                int lane = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y - lanesTop) / laneH), 0, Lanes - 1);
                if (e.button == 0) ToggleNote(lane, SnapBeat(t));
                else if (e.button == 1) RemoveNear(lane, t, 10f / pxPerSec);
                e.Use(); Repaint();
            }
        }
        else if (e.type == EventType.ScrollWheel)
        {
            if (e.control || e.command)
            {
                float mouseT = scroll + (e.mousePosition.x - area.x) / pxPerSec;
                pxPerSec = Mathf.Clamp(pxPerSec * (1f - e.delta.y * 0.05f), 20f, 2000f);
                scroll = mouseT - (e.mousePosition.x - area.x) / pxPerSec;
            }
            else scroll += e.delta.y * 40f / pxPerSec;
            e.Use(); Repaint();
        }
        else if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Space) { TogglePlay(); e.Use(); }
            else if (recordMode && playing)
            {
                for (int l = 0; l < Lanes; l++)
                {
                    if (e.keyCode != Hotkeys[l]) continue;
                    AddNoteIfFree(l, SnapBeat(preview.time));
                    e.Use(); Repaint();
                    break;
                }
            }
        }
    }

    void DrawLanes(Rect area, float top, float h)
    {
        for (int l = 0; l < Lanes; l++)
        {
            var r = new Rect(area.x, top + l * h, area.width, h);
            EditorGUI.DrawRect(r, l % 2 == 0 ? new Color(0.16f, 0.16f, 0.19f) : new Color(0.19f, 0.19f, 0.22f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, 4, h), Pal[l]);
            GUI.Label(new Rect(r.x + 8, r.y + 2, 40, 14), LaneNames[l], EditorStyles.miniLabel);
        }
    }

    void DrawActiveRange(Rect area, float top, float h)
    {
        if (seg.notes == null || seg.notes.Length == 0) return;
        float x0 = area.x + (seg.ActiveStart - scroll) * pxPerSec;
        float x1 = area.x + (seg.ActiveEnd - scroll) * pxPerSec;
        x0 = Mathf.Max(x0, area.x);
        x1 = Mathf.Min(x1, area.xMax);
        if (x1 <= x0) return;
        EditorGUI.DrawRect(new Rect(x0, top, x1 - x0, h * Lanes), new Color(0.4f, 0.8f, 1f, 0.08f));
    }

    void DrawGrid(Rect area, float top, float h)
    {
        float step = 60f / Mathf.Max(1f, seg.bpm) / Div;
        float tMin = scroll, tMax = scroll + area.width / pxPerSec;
        int stride = Mathf.Max(1, Mathf.CeilToInt(6f / (step * pxPerSec)));
        int first = Mathf.FloorToInt((tMin - seg.songTimeOfBeatZero) / step);
        first -= ((first % stride) + stride) % stride;

        for (int i = first; seg.songTimeOfBeatZero + i * step <= tMax; i += stride)
        {
            float t = seg.songTimeOfBeatZero + i * step;
            if (t < tMin) continue;
            float x = area.x + (t - scroll) * pxPerSec;
            bool beat = i % Div == 0;
            bool bar = i % (Div * 4) == 0;
            var c = bar ? new Color(1f, 1f, 1f, 0.35f) : beat ? new Color(1f, 1f, 1f, 0.18f) : new Color(1f, 1f, 1f, 0.06f);
            EditorGUI.DrawRect(new Rect(x, top, 1, h * Lanes), c);
            if (bar) GUI.Label(new Rect(x + 2, top - 14, 70, 14), "beat " + (i / Div), EditorStyles.miniLabel);
        }
    }

    void DrawWave(Rect r)
    {
        EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.12f));
        if (peaks == null)
        {
            GUI.Label(r, "Arraste a música (stem do boss ou mix) no campo 'Música'", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Handles.color = new Color(0.4f, 0.75f, 1f, 0.9f);
        float mid = r.center.y, half = r.height * 0.45f;
        for (int x = 0; x < (int)r.width; x++)
        {
            float t0 = scroll + x / pxPerSec, t1 = scroll + (x + 1) / pxPerSec;
            int i0 = Mathf.Max(0, Mathf.FloorToInt(t0 * PeaksPerSec));
            int i1 = Mathf.Min(peaks.Length - 1, Mathf.Max(i0, Mathf.FloorToInt(t1 * PeaksPerSec)));
            if (i0 >= peaks.Length) break;
            float m = 0f;
            for (int i = i0; i <= i1; i++) m = Mathf.Max(m, peaks[i]);
            Handles.DrawLine(new Vector3(r.x + x, mid - m * half), new Vector3(r.x + x, mid + m * half));
        }
    }

    void DrawNotes(Rect area, float top, float h)
    {
        if (seg.notes == null) return;
        float tMin = scroll - 0.1f, tMax = scroll + area.width / pxPerSec + 0.1f;
        foreach (var n in seg.notes)
        {
            float t = seg.GetTime(n);
            int lane = Mathf.Clamp(n.lane, 0, Lanes - 1);
            if (t < tMin || t > tMax) continue;
            float x = area.x + (t - scroll) * pxPerSec;
            float y = top + lane * h + h * 0.5f;
            EditorGUI.DrawRect(new Rect(x - 9, y - 9, 18, 18), Color.black);
            EditorGUI.DrawRect(new Rect(x - 7, y - 7, 14, 14), Pal[lane]);
        }
    }
}
