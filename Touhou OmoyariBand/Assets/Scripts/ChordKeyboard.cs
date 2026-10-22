using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>
/// "Teclado" de acordes: cada tecla toca um AudioClip de acorde de violão.
/// Teclas padrão: M = G, K = D, O = Em, P = C (editáveis no Inspector).
/// </summary>
public class ChordKeyboard : MonoBehaviour
{
    [Serializable]
    public class ChordKey
    {
        public string chordName = "G";
        public Key key = Key.M;
        public AudioClip clip;
        [HideInInspector] public AudioSource source;
    }

    [Header("Acordes (um por tecla)")]
    [SerializeField] private ChordKey[] chords =
    {
        new ChordKey { chordName = "G (Sol Maior)", key = Key.M },
        new ChordKey { chordName = "D (Ré Maior)",  key = Key.K },
        new ChordKey { chordName = "Em (Mi Menor)", key = Key.O },
        new ChordKey { chordName = "C (Dó Maior)",  key = Key.P },
    };

    [Header("Áudio")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [Tooltip("Se ligado, tocar um acorde novo abafa o anterior (como num violão de verdade).")]
    [SerializeField] private bool monophonic = true;
    [Tooltip("Tempo (s) do fade-out ao abafar o acorde anterior.")]
    [SerializeField] private float fadeOutTime = 0.15f;
    [Tooltip("Opcional: Audio Mixer Group (ex.: grupo 'SFX' ou 'Instrumento').")]
    [SerializeField] private AudioMixerGroup outputGroup;

    /// <summary>Disparado quando um acorde é tocado (índice do acorde). Útil para efeitos/balas reagirem.</summary>
    public event Action<int> ChordPlayed;

    private bool[] fading;

    private void Awake()
    {
        fading = new bool[chords.Length];

        // Um AudioSource por acorde: permite sobreposição e fade individual
        foreach (ChordKey chord in chords)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f; // 2D
            src.clip = chord.clip;
            src.outputAudioMixerGroup = outputGroup;
            chord.source = src;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        for (int i = 0; i < chords.Length; i++)
        {
            if (kb[chords[i].key].wasPressedThisFrame)
            {
                PlayChord(i);
            }
        }

        UpdateFades();
    }

    public void PlayChord(int index)
    {
        if (index < 0 || index >= chords.Length) return;

        AudioSource src = chords[index].source;
        if (src == null || src.clip == null) return;

        if (monophonic)
        {
            for (int i = 0; i < chords.Length; i++)
            {
                if (i != index && chords[i].source.isPlaying) fading[i] = true;
            }
        }

        fading[index] = false;
        src.volume = volume;
        src.Stop();
        src.Play();

        ChordPlayed?.Invoke(index);
    }

    /// <summary>Troca a tecla de um acorde em tempo de execução (para menu de configurações).</summary>
    public void Rebind(int index, Key newKey)
    {
        if (index >= 0 && index < chords.Length) chords[index].key = newKey;
    }

    private void UpdateFades()
    {
        float step = volume / Mathf.Max(0.01f, fadeOutTime) * Time.unscaledDeltaTime;

        for (int i = 0; i < chords.Length; i++)
        {
            if (!fading[i]) continue;

            AudioSource src = chords[i].source;
            src.volume -= step;

            if (src.volume <= 0f)
            {
                src.Stop();
                fading[i] = false;
            }
        }
    }
}
