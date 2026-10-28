using System;
using UnityEngine;

/// <summary>
/// Um trecho da música em que o jogador precisa tocar.
/// Crie pelo menu: Assets > Create > Rhythm > Segment.
/// As notas são definidas em BATIDAS; o tempo real é calculado a partir do BPM.
/// </summary>
[CreateAssetMenu(fileName = "RhythmSegment", menuName = "Rhythm/Segment")]
public class RhythmSegment : ScriptableObject
{
    [Serializable]
    public struct NoteData
    {
        [Tooltip("Posição da nota em batidas, contada a partir da batida 0 do trecho. Aceita frações (0.5 = meia batida).")]
        public float beat;

        [Tooltip("0 = 1ª tecla (G), 1 = 2ª (D), 2 = 3ª (Em), 3 = 4ª (C).")]
        [Range(0, 3)] public int lane;
    }

    [Header("Sincronia com a música")]
    [Tooltip("BPM da música neste trecho.")]
    public float bpm = 120f;

    [Tooltip("Tempo (em segundos, contado do início da música) em que acontece a batida 0 deste trecho.")]
    public float songTimeOfBeatZero = 30f;

    [Header("Exibição")]
    [Tooltip("Quantos segundos ANTES da primeira nota o painel aparece. Também é o tempo que cada nota leva para cair até a linha.")]
    public float approachTime = 2f;

    [Tooltip("Tempo extra (s) com o painel visível depois da última nota.")]
    public float endPadding = 0.75f;

    [Header("Notas")]
    public NoteData[] notes = new NoteData[0];

    /// <summary>Tempo da nota, em segundos, na linha do tempo da música.</summary>
    public float GetTime(NoteData note)
    {
        return songTimeOfBeatZero + note.beat * 60f / Mathf.Max(1f, bpm);
    }

    public float FirstNoteTime
    {
        get
        {
            if (notes == null || notes.Length == 0) return songTimeOfBeatZero;
            float min = float.MaxValue;
            foreach (NoteData n in notes) min = Mathf.Min(min, GetTime(n));
            return min;
        }
    }

    public float LastNoteTime
    {
        get
        {
            if (notes == null || notes.Length == 0) return songTimeOfBeatZero;
            float max = float.MinValue;
            foreach (NoteData n in notes) max = Mathf.Max(max, GetTime(n));
            return max;
        }
    }

    /// <summary>Momento em que o painel aparece.</summary>
    public float ActiveStart => FirstNoteTime - approachTime;

    /// <summary>Momento em que o trecho termina.</summary>
    public float ActiveEnd => LastNoteTime + Mathf.Max(endPadding, 0.3f);
}
