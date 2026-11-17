using UnityEngine;

/// <summary>
/// Guarda a habilidade escolhida no painel de seleção de fase.
/// É estático, então sobrevive à troca de cena: o menu escolhe e a fase lê.
/// </summary>
public static class AbilitySelection
{
    /// <summary>Habilidade escolhida (null = nenhuma).</summary>
    public static AbilityData Selected { get; private set; }

    /// <summary>True se o jogador já fez uma escolha no seletor (mesmo que tenha sido "sem habilidade").</summary>
    public static bool HasChosen { get; private set; }

    public static void Select(AbilityData ability)
    {
        // Se uma habilidade foi passada, só permite selecionar se estiver desbloqueada na sessão atual
        if (ability != null && !PlayerAbilities.IsCurrentAbilityUnlocked(ability.type))
        {
            Debug.LogWarning($"[AbilitySelection] Tentativa de selecionar habilidade bloqueada: {ability.type}");
            return;
        }

        Selected = ability;
        HasChosen = true;
    }

    /// <summary>Limpa a habilidade escolhida (útil ao trocar de visitante no modo exposição).</summary>
    public static void ResetSelection()
    {
        Selected = null;
        HasChosen = false;
    }

    // Garante o reset ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ResetSelection();
    }
}