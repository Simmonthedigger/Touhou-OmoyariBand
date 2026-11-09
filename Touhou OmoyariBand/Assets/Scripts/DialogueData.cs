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
    }

    public Line[] lines;
}
