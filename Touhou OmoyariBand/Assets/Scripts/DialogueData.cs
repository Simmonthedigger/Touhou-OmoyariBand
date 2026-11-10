using System;
using UnityEngine;

/// <summary>
/// Conteúdo de um diálogo. Crie pelo menu: Assets > Create > Dialogue > Dialogue.
/// </summary>
[CreateAssetMenu(fileName = "Dialogue_", menuName = "Dialogue/Dialogue")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public class Line
    {
        [Tooltip("Nome de quem fala (pode ficar vazio).")]
        public string speaker;

        [TextArea(2, 5)]
        public string text;

        [Tooltip("Retrato de quem fala (opcional).")]
        public Sprite portrait;

        [Header("Áudio da Fala (Undertale Style)")]
        [Tooltip("Som da voz deste personagem. Se vazio, usa o som padrão do DialogueData ou do DialoguePlayer.")]
        public AudioClip voiceSound;

        [Tooltip("Variação aleatória de pitch (tom) a cada caractere para um som mais natural.")]
        [Range(0f, 0.5f)] public float pitchVariance = 0.1f;
    }

    [Header("Áudio Padrão do Diálogo")]
    [Tooltip("Som de voz padrão para todas as falas deste diálogo (caso o personagem não tenha um som específico).")]
    public AudioClip defaultVoiceSound;

    public Line[] lines;
}