using System;
using UnityEngine;

/// <summary>
/// Configuração da progressão: a ORDEM das fases da campanha e qual habilidade cada fase desbloqueia.
/// Crie pelo menu: Assets > Create > Progression > Config
/// e salve o asset em  Assets/Resources/ProgressionConfig.asset  (o nome e a pasta precisam ser exatamente esses).
/// Tudo é configurado por string, como no resto do projeto: o nome da cena e o ID da habilidade
/// devem ser idênticos aos usados no Build Settings e nos scripts do player.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Progression/Config")]
public class ProgressionConfig : ScriptableObject
{
    [Serializable]
    public class StageEntry
    {
        [Tooltip("Nome EXATO da cena (a mesma que está no Build Settings).")]
        public string sceneName;

        [Tooltip("Nome mostrado ao jogador (opcional, para a sua UI).")]
        public string displayName;

        [Tooltip("ID da habilidade desbloqueada ao COMPLETAR esta fase (ex.: Dash). Vazio = nenhuma.")]
        public string unlocksAbilityId;
    }

    [Tooltip("Na ordem da campanha: a primeira é a fase 1.")]
    public StageEntry[] stages = new StageEntry[0];

    [Tooltip("Ligado: a fase N só abre depois de completar a fase N-1 (fases já completadas podem ser repetidas).")]
    public bool lockStagesInOrder = true;

    [Tooltip("SÓ NO EDITOR: se você der Play direto numa fase (sem passar pelo menu), tudo fica desbloqueado para testar. " +
             "Se passar pelo menu e escolher um nível, a progressão normal vale. Não afeta o build.")]
    public bool editorUnlockAllWhenNoExperience = true;
}
