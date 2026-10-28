using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a submecânica de ritmo:
/// - ativa cada trecho (RhythmSegment) no momento certo da música;
/// - julga acertos e erros (janelas de tempo), usando o instante REAL do aperto (baixa latência);
/// - ACERTOU: a stem do player fica audível. ERROU: a stem é cortada + SFX de erro, até o próximo acerto.
/// </summary>
public class RhythmController : MonoBehaviour
{
    public enum Judgement { Perfect, Good }
    public enum NoteState { Pending, Hit, Missed }

    public class RuntimeNote
    {
        public float time;
        public int lane;
        public NoteState state;
    }

    [Header("Referências")]
    [SerializeField] private RhythmMusic music;
    [Tooltip("AudioSource usado para os sons de erro (pode ser um novo, sem clip).")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("Sons de erro (palhetada arranhada, estática...). Um é sorteado a cada erro.")]
    [SerializeField] private AudioClip[] missClips;
    [SerializeField, Range(0f, 1f)] private float missSfxVolume = 0.7f;

    [Header("Trechos jogáveis")]
    [Tooltip("Não deixe trechos se sobreporem no tempo.")]
    [SerializeField] private RhythmSegment[] segments;

    [Header("Teclas (uma por lane: G, D, Em, C)")]
    [Tooltip("Evite teclas que o Touhou já usa (setas, Z, X, Shift).")]
    [SerializeField] private Key[] laneKeys = { Key.M, Key.K, Key.O, Key.P };

    [Header("Janelas de acerto (segundos, para cada lado) = o 'respiro' do jogador")]
    [SerializeField] private float perfectWindow = 0.07f;
    [SerializeField] private float goodWindow = 0.15f;
    [Tooltip("Apertar uma tecla sem nota por perto também conta como erro.")]
    [SerializeField] private bool penalizeWrongPress = true;

    [Header("Volume da stem do player")]
    [SerializeField, Range(0f, 1f)] private float playerVolume = 1f;
    [Tooltip("Volume da stem do player fora dos trechos jogáveis (0 = silêncio).")]
    [SerializeField, Range(0f, 1f)] private float idleVolume = 0f;
    [Tooltip("Velocidade do fade do volume (maior = mais brusco).")]
    [SerializeField] private float fadeSpeed = 20f;

    [Header("Eventos (use no Inspector para dar recompensas, etc.)")]
    public UnityEvent onSegmentStarted;
    [Tooltip("Parâmetros: acertos, total de notas.")]
    public UnityEvent<int, int> onSegmentFinished;

    // Eventos em código (usados pela UI)
    public event Action<RhythmSegment> SegmentStarted;
    public event Action<int, int> SegmentFinished;
    public event Action<int, Judgement> NoteHit;
    public event Action<int> NoteMissed;

    public double SongTime => music != null ? music.SongTime : 0.0;
    public bool IsSegmentActive => activeSegment != null;
    public RhythmSegment ActiveSegment => activeSegment;
    public IReadOnlyList<RuntimeNote> ActiveNotes => activeNotes;
    public IReadOnlyList<Key> LaneKeys => laneKeys;

    /// <summary>Facilita/dificulta em tempo de execução. 1 = normal, 2 = o dobro de respiro.</summary>
    public void SetWindowScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.25f, 4f);
        perfectWindow = basePerfect * scale;
        goodWindow = baseGood * scale;
    }

    private readonly List<RuntimeNote> activeNotes = new List<RuntimeNote>();
    private List<RhythmSegment> sortedSegments;
    private RhythmLaneInput input;
    private RhythmSegment activeSegment;
    private int nextSegmentIndex;
    private float activeEnd;
    private int hitCount;
    private bool playerAudible;
    private float currentVolume;
    private float lastSfxTime = -10f;
    private float basePerfect, baseGood;

    private void Awake()
    {
        basePerfect = perfectWindow;
        baseGood = goodWindow;

        sortedSegments = new List<RhythmSegment>(segments ?? new RhythmSegment[0]);
        sortedSegments.RemoveAll(s => s == null || s.notes == null || s.notes.Length == 0);
        sortedSegments.Sort((a, b) => a.ActiveStart.CompareTo(b.ActiveStart));
    }

    private void OnEnable() => input = new RhythmLaneInput(laneKeys);

    private void OnDisable()
    {
        input?.Dispose();
        input = null;
    }

    private void Start()
    {
        currentVolume = idleVolume;
        if (music != null) music.SetPlayerVolume(currentVolume);
    }

    private void Update()
    {
        if (music == null || !music.IsPlaying)
        {
            input?.Clear();
            return;
        }

        double t = music.SongTime;

        if (activeSegment == null)
        {
            input.Clear();               // apertos fora de um trecho não contam
            TryBeginSegment(t);
        }

        if (activeSegment != null)
        {
            ProcessInput(t);   // primeiro as teclas...
            ProcessMisses(t);  // ...depois as notas que passaram da janela
            if (t > activeEnd) EndSegment();
        }

        UpdatePlayerVolume();
    }

    // ---------------- API pública ----------------

    /// <summary>
    /// Volta ao primeiro trecho (use ao reiniciar a luta/música). Se houver um trecho ativo, ele é cancelado:
    /// a UI esconde o painel (SegmentFinished com 0 acertos) e o UnityEvent de recompensa NÃO é disparado.
    /// </summary>
    public void ResetProgress()
    {
        if (activeSegment != null)
        {
            int total = activeNotes.Count;
            activeSegment = null;
            playerAudible = false;
            activeNotes.Clear();
            SegmentFinished?.Invoke(0, total);
        }

        nextSegmentIndex = 0;
        input?.Clear();
    }

    // ---------------- Trechos ----------------

    private void TryBeginSegment(double t)
    {
        // Pula trechos que já passaram (ex.: se a música foi adiantada)
        while (nextSegmentIndex < sortedSegments.Count && t > sortedSegments[nextSegmentIndex].ActiveEnd)
        {
            nextSegmentIndex++;
        }

        if (nextSegmentIndex >= sortedSegments.Count) return;

        RhythmSegment seg = sortedSegments[nextSegmentIndex];
        if (t >= seg.ActiveStart) BeginSegment(seg);
    }

    private void BeginSegment(RhythmSegment seg)
    {
        nextSegmentIndex++;
        activeSegment = seg;
        // garante que a última nota sempre tenha tempo de ser julgada (mesmo com janelas grandes)
        activeEnd = Mathf.Max(seg.ActiveEnd, seg.LastNoteTime + goodWindow + 0.05f);
        hitCount = 0;
        playerAudible = false; // a stem só toca a partir do primeiro acerto
        input.Clear();

        activeNotes.Clear();
        foreach (RhythmSegment.NoteData data in seg.notes)
        {
            activeNotes.Add(new RuntimeNote
            {
                time = seg.GetTime(data),
                lane = Mathf.Clamp(data.lane, 0, 3),
                state = NoteState.Pending
            });
        }
        activeNotes.Sort((a, b) => a.time.CompareTo(b.time));

        onSegmentStarted?.Invoke();
        SegmentStarted?.Invoke(seg);
    }

    private void EndSegment()
    {
        int total = activeNotes.Count;
        int hits = hitCount;

        activeSegment = null;
        playerAudible = false;

        SegmentFinished?.Invoke(hits, total);
        onSegmentFinished?.Invoke(hits, total);

        activeNotes.Clear();
    }

    // ---------------- Julgamento ----------------

    private void ProcessInput(double now)
    {
        // Cada aperto é julgado no instante em que a tecla foi realmente registrada, não no do frame.
        while (input.TryDequeue(out int lane, out double age))
        {
            HandlePress(lane, now - age);
        }
    }

    private void HandlePress(int lane, double t)
    {
        RuntimeNote best = null;
        float bestDiff = float.MaxValue;

        foreach (RuntimeNote n in activeNotes)
        {
            if (n.state != NoteState.Pending || n.lane != lane) continue;

            float diff = Mathf.Abs((float)(n.time - t));
            if (diff <= goodWindow && diff < bestDiff)
            {
                best = n;
                bestDiff = diff;
            }
        }

        if (best != null)
        {
            best.state = NoteState.Hit;
            hitCount++;
            playerAudible = true; // ACERTOU: stem do player volta a tocar

            NoteHit?.Invoke(lane, bestDiff <= perfectWindow ? Judgement.Perfect : Judgement.Good);
        }
        else if (penalizeWrongPress)
        {
            RegisterMiss(lane);
        }
    }

    private void ProcessMisses(double t)
    {
        foreach (RuntimeNote n in activeNotes)
        {
            if (n.state == NoteState.Pending && t - n.time > goodWindow)
            {
                n.state = NoteState.Missed;
                RegisterMiss(n.lane);
            }
        }
    }

    private void RegisterMiss(int lane)
    {
        playerAudible = false; // ERROU: corta a stem do player até o próximo acerto
        PlayMissSfx();
        NoteMissed?.Invoke(lane);
    }

    // ---------------- Áudio ----------------

    private void UpdatePlayerVolume()
    {
        float target = activeSegment != null ? (playerAudible ? playerVolume : 0f) : idleVolume;
        currentVolume = Mathf.MoveTowards(currentVolume, target, fadeSpeed * Time.unscaledDeltaTime);
        music.SetPlayerVolume(currentVolume);
    }

    private void PlayMissSfx()
    {
        if (sfxSource == null || missClips == null || missClips.Length == 0) return;
        if (Time.unscaledTime - lastSfxTime < 0.08f) return; // evita empilhar vários sons de erro

        lastSfxTime = Time.unscaledTime;
        AudioClip clip = missClips[UnityEngine.Random.Range(0, missClips.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, missSfxVolume);
    }
}
