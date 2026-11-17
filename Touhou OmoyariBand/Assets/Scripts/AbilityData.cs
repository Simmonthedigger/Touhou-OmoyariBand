using UnityEngine;

public enum AbilityType
{
    /// <summary>Segurando: player mais lento, hitbox visível e tiro mais rápido. Gasta mana por segundo.</summary>
    Focus,

    /// <summary>Apertando: dash rápido e invulnerável. Gasta uma quantidade fixa de mana.</summary>
    InvulnerableDash,

    /// <summary>Segurando: invulnerável enquanto o botão estiver apertado. Gasta mana por segundo.</summary>
    InvulnerableHold
}

/// <summary>
/// Uma habilidade do player. Crie pelo menu: Assets > Create > Abilities > Ability.
/// Todos os valores podem ser ajustados no Inspector.
/// </summary>
[CreateAssetMenu(fileName = "Ability_", menuName = "Abilities/Ability")]
public class AbilityData : ScriptableObject
{
    [Header("Informações")]
    public string displayName = "Habilidade";

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Ícone mostrado no botão do seletor (opcional).")]
    public Sprite icon;

    public AbilityType type = AbilityType.InvulnerableDash;

    [Header("Custo de mana")]
    [Tooltip("Custo FIXO ao ativar. Usado pelo Dash.")]
    public float manaCost = 30f;

    [Tooltip("Custo por SEGUNDO enquanto o botão está segurado. Usado por Foco e Invulnerabilidade segurada.")]
    public float manaCostPerSecond = 20f;

    [Tooltip("Mana mínima necessária para COMEÇAR uma habilidade segurada (evita ligar e desligar sem parar).")]
    public float minManaToStart = 10f;

    [Header("Dash (só para o tipo Dash)")]
    [Tooltip("Distância percorrida pelo dash, em unidades do jogo.")]
    public float dashDistance = 3f;

    [Tooltip("Duração do dash em segundos. Menor = mais rápido.")]
    public float dashDuration = 0.18f;

    [Tooltip("Tempo de espera, depois do dash terminar, para poder usar de novo.")]
    public float cooldown = 0.5f;

    [Tooltip("Tempo extra de invulnerabilidade depois que o dash termina.")]
    public float extraInvulnerability = 0.1f;

    [Header("Foco (só para o tipo Foco)")]
    [Tooltip("Multiplica a velocidade do player. 1 = igual, 0.5 = metade.")]
    public float focusSpeedMultiplier = 0.6f;

    [Tooltip("Multiplica o intervalo entre tiros. 0.5 = atira 2x mais rápido.")]
    public float fireRateMultiplier = 0.5f;

    [Header("Invulnerabilidade segurada (só para esse tipo)")]
    [Tooltip("Multiplica a velocidade do player enquanto está invulnerável. 1 = igual.")]
    public float holdSpeedMultiplier = 1f;

    /// <summary>Texto curto do custo, para mostrar na interface.</summary>
    public string GetCostText()
    {
        if (type == AbilityType.InvulnerableDash)
        {
            return manaCost.ToString("0.#") + " mana";
        }

        return manaCostPerSecond.ToString("0.#") + " mana/s";
    }
}
