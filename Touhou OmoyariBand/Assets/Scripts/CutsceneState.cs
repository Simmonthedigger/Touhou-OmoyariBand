using UnityEngine;

/// <summary>
/// Estado global de cutscene. Enquanto estiver ativo:
/// - Time.timeScale = 0: física, Animators, corrotinas com WaitForSeconds, timers com Time.deltaTime,
///   fundo rolando, tiros do boss etc. ficam todos parados;
/// - AudioListener.pause = true: a música (e qualquer áudio) fica parada;
/// - CutsceneState.IsActive = true: scripts de input (tiro, movimento, pause) devem se bloquear.
/// </summary>
public static class CutsceneState
{
    public static bool IsActive { get; private set; }

    private static float previousTimeScale = 1f;

    // Garante o reset ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsActive = false;
        previousTimeScale = 1f;
    }

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
        AudioListener.pause = false;
    }
}
