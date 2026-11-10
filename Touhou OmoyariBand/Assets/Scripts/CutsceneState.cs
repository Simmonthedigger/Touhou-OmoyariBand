using UnityEngine;

/// <summary>
/// Estados globais do início da fase.
///
/// IsActive (diálogo):
///   Time.timeScale = 0 (física, Animators, timers, fundo, tiros parados), música parada e
///   scripts de input devem se bloquear.
///
/// IsCountdown (contagem regressiva):
///   o jogo RODA (o player pode se mover), mas a música fica parada (AudioListener.pause)
///   e o boss/tiros são segurados pelo StageIntro.
///
/// A música só volta quando nenhum dos dois estados estiver ativo.
/// </summary>
public static class CutsceneState
{
    /// <summary>Diálogo em andamento: tudo congelado.</summary>
    public static bool IsActive { get; private set; }

    /// <summary>Contagem regressiva em andamento: player livre, música e boss parados.</summary>
    public static bool IsCountdown { get; private set; }

    private static float previousTimeScale = 1f;

    // Garante o reset ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsActive = false;
        IsCountdown = false;
        previousTimeScale = 1f;
    }

    // ---------- Diálogo ----------

    public static void Begin()
    {
        if (IsActive) return;

        IsActive = true;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public static void End()
    {
        if (!IsActive) return;

        IsActive = false;
        Time.timeScale = previousTimeScale;

        // Se ainda houver contagem regressiva, a música continua parada
        AudioListener.pause = IsCountdown;
    }

    // ---------- Contagem regressiva ----------

    public static void BeginCountdown()
    {
        if (IsCountdown) return;

        IsCountdown = true;
        AudioListener.pause = true;
    }

    public static void EndCountdown()
    {
        if (!IsCountdown) return;

        IsCountdown = false;

        // Se ainda houver diálogo, a música continua parada
        AudioListener.pause = IsActive;
    }
}