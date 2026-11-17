using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Progressão da sessão atual (modo exposição).
/// NADA é salvo em disco: tudo vive só na memória e some ao fechar o jogo.
/// Cada visitante escolhe seu nível de experiência no menu, e a progressão dele
/// (fases completadas) começa a partir daí. As habilidades são derivadas das fases completadas,
/// conforme o ProgressionConfig (Resources/ProgressionConfig).
/// </summary>
public static class ProgressionState
{
    private const string ConfigResourceName = "ProgressionConfig";

    private static readonly HashSet<string> completed = new HashSet<string>();
    private static readonly HashSet<string> warnedAbilities = new HashSet<string>();
    private static ProgressionConfig config;
    private static bool configSearched;

    /// <summary>True se o visitante atual já escolheu o nível de experiência.</summary>
    public static bool HasSelectedExperience { get; private set; }

    /// <summary>Disparado quando completar uma fase desbloqueia uma habilidade nova (parâmetro: ID da habilidade).
    /// Quem se inscrever deve se desinscrever no OnDisable.</summary>
    public static event Action<string> AbilityUnlocked;

    // Garante o reset ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        completed.Clear();
        warnedAbilities.Clear();
        HasSelectedExperience = false;
        config = null;
        configSearched = false;
        AbilityUnlocked = null;
    }

    // ---------------- Configuração ----------------

    public static ProgressionConfig Config
    {
        get
        {
            if (!configSearched)
            {
                configSearched = true;
                config = Resources.Load<ProgressionConfig>(ConfigResourceName);
                if (config == null)
                {
                    Debug.LogError("[ProgressionState] Não achei Assets/Resources/" + ConfigResourceName + ".asset. " +
                                   "Crie em Assets > Create > Progression > Config e salve nessa pasta com esse nome.");
                }
            }
            return config;
        }
    }

    /// <summary>Quantidade de fases da campanha.</summary>
    public static int TotalStages => Config != null ? Config.stages.Length : 0;

    /// <summary>Quantas fases o visitante atual já completou.</summary>
    public static int CompletedCount => completed.Count;

    // No editor, dar Play direto numa fase (sem escolher nível no menu) libera tudo para teste.
    private static bool EditorBypass =>
        Application.isEditor && !HasSelectedExperience && Config != null && Config.editorUnlockAllWhenNoExperience;

    private static int StageIndex(string sceneName)
    {
        ProgressionConfig cfg = Config;
        if (cfg == null || string.IsNullOrEmpty(sceneName)) return -1;

        for (int i = 0; i < cfg.stages.Length; i++)
        {
            if (cfg.stages[i].sceneName == sceneName) return i;
        }
        return -1;
    }

    // ---------------- Fases ----------------

    /// <summary>A fase (nome da cena) já foi completada nesta sessão?</summary>
    public static bool IsCompleted(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName) && completed.Contains(sceneName);
    }

    /// <summary>
    /// A fase pode ser jogada? Fora da campanha (cena que não está no Config) = sempre liberada.
    /// Com lockStagesInOrder, a fase N exige a N-1 completa (ou a própria já completa, para repetir).
    /// </summary>
    public static bool IsStageUnlocked(string sceneName)
    {
        if (EditorBypass) return true;

        ProgressionConfig cfg = Config;
        if (cfg == null || !cfg.lockStagesInOrder) return true;

        int index = StageIndex(sceneName);
        if (index <= 0) return true;                          // fase 1 ou cena fora da campanha

        return IsCompleted(sceneName) || IsCompleted(cfg.stages[index - 1].sceneName);
    }

    /// <summary>
    /// Registra uma fase como completa (chamado quando o jogador vence a fase).
    /// Retorna true se foi a PRIMEIRA vez nesta sessão (útil para mostrar "nova habilidade!").
    /// </summary>
    public static bool MarkCompleted(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (!completed.Add(sceneName)) return false;

        string ability = AbilityUnlockedBy(sceneName);
        if (!string.IsNullOrEmpty(ability)) AbilityUnlocked?.Invoke(ability);
        return true;
    }

    /// <summary>Atalho: marca a cena ativa como completa. Chame quando o jogador vencer a fase.</summary>
    public static bool MarkCurrentSceneCompleted()
    {
        return MarkCompleted(SceneManager.GetActiveScene().name);
    }

    // ---------------- Habilidades ----------------

    /// <summary>ID da habilidade que completar esta fase desbloqueia (null se nenhuma).</summary>
    public static string AbilityUnlockedBy(string sceneName)
    {
        int index = StageIndex(sceneName);
        if (index < 0) return null;

        string id = Config.stages[index].unlocksAbilityId;
        return string.IsNullOrEmpty(id) ? null : id;
    }

    /// <summary>A habilidade está desbloqueada para o visitante atual? Use isso nos scripts do player.</summary>
    public static bool IsAbilityUnlocked(string abilityId)
    {
        if (string.IsNullOrEmpty(abilityId)) return false;
        if (EditorBypass) return true;

        ProgressionConfig cfg = Config;
        if (cfg == null) return false;

        bool found = false;
        foreach (ProgressionConfig.StageEntry stage in cfg.stages)
        {
            if (stage.unlocksAbilityId != abilityId) continue;

            found = true;
            if (completed.Contains(stage.sceneName)) return true;
        }

        if (!found && warnedAbilities.Add(abilityId))
        {
            Debug.LogWarning("[ProgressionState] A habilidade '" + abilityId + "' não está em nenhuma fase do ProgressionConfig " +
                             "(confira se o ID está escrito igual).");
        }
        return false;
    }

    // ---------------- Nível de experiência ----------------

    /// <summary>Define a progressão a partir do nível de experiência escolhido pelo visitante.</summary>
    public static void SetExperience(IEnumerable<string> completedScenes)
    {
        completed.Clear();

        if (completedScenes != null)
        {
            foreach (string scene in completedScenes)
            {
                if (!string.IsNullOrEmpty(scene)) completed.Add(scene);
            }
        }

        HasSelectedExperience = true;
    }

    /// <summary>"Já joguei X fases": marca como completas as X primeiras fases do ProgressionConfig.
    /// 0 = primeira vez jogando.</summary>
    public static void SetExperienceStageCount(int stagesCompleted)
    {
        var scenes = new List<string>();
        ProgressionConfig cfg = Config;

        if (cfg != null)
        {
            int n = Mathf.Clamp(stagesCompleted, 0, cfg.stages.Length);
            for (int i = 0; i < n; i++) scenes.Add(cfg.stages[i].sceneName);
        }

        SetExperience(scenes);
    }

    /// <summary>"Já zerei o jogo": todas as fases completas (todas as habilidades).</summary>
    public static void SetExperienceFinishedGame()
    {
        SetExperienceStageCount(int.MaxValue);
    }

    /// <summary>Apaga tudo (novo visitante).</summary>
    public static void ResetAll()
    {
        completed.Clear();
        HasSelectedExperience = false;
    }
}
