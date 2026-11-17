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
        Selected = ability;
        HasChosen = true;
    }

    // Garante o reset ao entrar no Play Mode (caso o Domain Reload esteja desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Selected = null;
        HasChosen = false;
    }
}
