using UnityEngine;

/// <summary>
/// Toca as duas stems (boss e player) perfeitamente sincronizadas e fornece
/// o relógio da música (SongTime) usado para julgar as notas.
/// A stem do boss toca sempre; o volume da stem do player é controlado pelo RhythmController.
/// </summary>
[DefaultExecutionOrder(-100)] // atualiza o relógio antes dos outros scripts lerem
public class RhythmMusic : MonoBehaviour
{
    [Header("Stems (mesma duração e alinhadas no tempo!)")]
    [SerializeField] private AudioSource bossSource;
    [SerializeField] private AudioSource playerSource;

    [Header("Controle")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Atraso (s) antes de a música começar. Garante que as duas stems iniciem juntas.")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField, Range(0f, 1f)] private float bossVolume = 1f;

    [Header("Calibração")]
    [Tooltip("Compensa a latência de áudio. Se você sente que acerta sempre 'atrasado', aumente (ex.: 0.05).")]
    [SerializeField] private float latencyCompensation = 0f;

    private double dspStart;
    private double pauseDsp;
    private double smoothedTime;
    private bool started;
    private bool paused;

    public bool IsPlaying => started && !paused;

    /// <summary>Tempo atual da música em segundos (já com a compensação de latência).</summary>
    public double SongTime => started ? smoothedTime - latencyCompensation : 0.0;

    public float Length => (bossSource != null && bossSource.clip != null) ? bossSource.clip.length : 0f;

    /// <summary>Progresso 0..1 da música (pode ser usado pela barra de progresso da partida).</summary>
    public float NormalizedProgress => Length > 0f ? Mathf.Clamp01((float)SongTime / Length) : 0f;

    private void Awake()
    {
        if (bossSource != null)
        {
            bossSource.playOnAwake = false;
            bossSource.loop = false;
            bossSource.volume = bossVolume;
        }

        if (playerSource != null)
        {
            playerSource.playOnAwake = false;
            playerSource.loop = false;
            playerSource.volume = 0f;
        }
    }

    private void Start()
    {
        if (playOnStart) StartMusic();
    }

    public void StartMusic()
    {
        if (bossSource == null || playerSource == null || bossSource.clip == null || playerSource.clip == null)
        {
            Debug.LogError("[RhythmMusic] Atribua os AudioSources e os clips das duas stems.");
            return;
        }

        if (Mathf.Abs(bossSource.clip.length - playerSource.clip.length) > 0.05f)
        {
            Debug.LogWarning("[RhythmMusic] As stems têm durações diferentes. Exporte as duas com o mesmo tamanho para não perder a sincronia.");
        }

        bossSource.Stop();
        playerSource.Stop();

        dspStart = AudioSettings.dspTime + startDelay;
        bossSource.PlayScheduled(dspStart);
        playerSource.PlayScheduled(dspStart);

        smoothedTime = -startDelay;
        started = true;
        paused = false;
    }

    public void SetPlayerVolume(float volume)
    {
        if (playerSource != null) playerSource.volume = volume;
    }

    /// <summary>Use estes métodos no seu menu de pause (não use AudioListener.pause, ele desincroniza o relógio).</summary>
    public void Pause()
    {
        if (!started || paused) return;

        paused = true;
        pauseDsp = AudioSettings.dspTime;
        bossSource.Pause();
        playerSource.Pause();
    }

    public void Resume()
    {
        if (!started || !paused) return;

        paused = false;
        dspStart += AudioSettings.dspTime - pauseDsp;
        bossSource.UnPause();
        playerSource.UnPause();
    }

    private void Update()
    {
        if (!started || paused) return;

        // AudioSettings.dspTime avança em "degraus" (por buffer de áudio), o que faz as notas tremerem.
        // Então avançamos suavemente com o deltaTime e corrigimos aos poucos usando o dspTime.
        double raw = AudioSettings.dspTime - dspStart;
        smoothedTime += Time.unscaledDeltaTime;

        double error = raw - smoothedTime;
        if (Mathf.Abs((float)error) > 0.1f) smoothedTime = raw; // desvio grande: ressincroniza de vez
        else smoothedTime += error * 0.1;                        // desvio pequeno: corrige suavemente
    }
}
