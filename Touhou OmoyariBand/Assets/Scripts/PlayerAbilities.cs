using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Executa a habilidade escolhida no seletor de fase. Vai no objeto do Player.
/// Gasta mana do ManaSystem e controla foco, dash invulnerável e invulnerabilidade segurada.
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(ManaSystem))]
public class PlayerAbilities : MonoBehaviour
{
    public static bool IsPlayerInvulnerable { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPlayerInvulnerable = false;
    }

    [Header("Habilidade")]
    [Tooltip("Usada só para TESTAR (ex.: Play direto na cena da fase). Quando você entra pelo menu, vale a habilidade escolhida no seletor.")]
    [SerializeField] private AbilityData testAbility;

    [Header("Tecla da habilidade")]
    [Tooltip("Padrão: Espaço. Clique no + para adicionar outras teclas/botões (ex.: botão de gamepad).")]
    [SerializeField] private InputAction abilityAction = new InputAction("Habilidade", InputActionType.Button, "<Keyboard>/space");

    [Header("Invulnerabilidade")]
    [Tooltip("Desliga os Collider2D do player enquanto invulnerável. Funciona sem mexer no PlayerHealth. " +
             "Normalmente deixe DESLIGADO: o PlayerHealth já respeita a invulnerabilidade e, assim, o player ainda coleta itens. Ligue só como plano B.")]
    [SerializeField] private bool disableCollidersWhileInvulnerable = false;

    [Tooltip("Transparência do player enquanto invulnerável (1 = sem efeito).")]
    [SerializeField, Range(0.1f, 1f)] private float invulnerableAlpha = 0.45f;

    public AbilityData Current { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public bool IsHoldActive => holdActive;
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    private PlayerController controller;
    private ManaSystem mana;
    private PlayerShooter shooter;

    private bool holdActive;
    private bool holdInvulnerable;
    private float invulnerableTimer;
    private float cooldownTimer;

    private SpriteRenderer[] sprites;
    private Color[] originalColors;
    private readonly List<Collider2D> disabledColliders = new List<Collider2D>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        mana = GetComponent<ManaSystem>();

        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[sprites.Length];
    }

    private void Start()
    {
        // Pelo menu: vale a escolha do seletor. Sem passar pelo menu (teste): vale a Test Ability.
        AbilityData chosen = AbilitySelection.HasChosen ? AbilitySelection.Selected : testAbility;

        // Trava de Progressão: só equipa a habilidade se ela estiver desbloqueada na sessão atual
        if (chosen != null && IsCurrentAbilityUnlocked(chosen.type))
        {
            Current = chosen;
        }
        else
        {
            Current = null;
        }

        shooter = GetComponentInChildren<PlayerShooter>(true);
        if (shooter == null) shooter = FindAnyObjectByType<PlayerShooter>();
    }

    /// <summary>
    /// Mapeia o AbilityType do jogo para os IDs configurados no ProgressionConfig.asset.
    /// </summary>
    public static bool IsCurrentAbilityUnlocked(AbilityType type)
    {
        switch (type)
        {
            case AbilityType.Focus:
                return ProgressionState.IsAbilityUnlocked("Focus");

            case AbilityType.InvulnerableDash:
                return ProgressionState.IsAbilityUnlocked("Dash");

            default:
                // Qualquer outro enum de invulnerabilidade segurada
                return ProgressionState.IsAbilityUnlocked("Invulnerabilidade");
        }
    }

    private void OnEnable()
    {
        abilityAction.Enable();
    }

    private void OnDisable()
    {
        abilityAction.Disable();

        StopHold();
        invulnerableTimer = 0f;
        RefreshInvulnerability();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        if (invulnerableTimer > 0f) invulnerableTimer -= dt;

        if (Current != null && !IsBlocked())
        {
            if (Current.type == AbilityType.InvulnerableDash) HandleDash();
            else HandleHold();
        }
        else if (holdActive)
        {
            StopHold();
        }

        RefreshInvulnerability();
    }

    private static bool IsBlocked()
    {
        return Time.timeScale <= 0f || CutsceneState.IsActive || CutsceneState.IsCountdown;
    }

    // ---------------- Dash invulnerável ----------------

    private void HandleDash()
    {
        if (!abilityAction.WasPressedThisFrame()) return;
        if (cooldownTimer > 0f || controller.IsDashing) return;

        Vector2 direction = controller.MoveInput;
        if (direction.sqrMagnitude < 0.01f) return;

        if (!mana.TrySpend(Current.manaCost)) return;

        controller.StartDash(direction, Current.dashDistance, Current.dashDuration);

        invulnerableTimer = Current.dashDuration + Current.extraInvulnerability;
        cooldownTimer = Current.dashDuration + Current.cooldown;
    }

    // ---------------- Habilidades seguradas (Foco / Invulnerabilidade) ----------------

    private void HandleHold()
    {
        if (!holdActive)
        {
            bool canStart = mana.Current >= Current.minManaToStart && mana.Current > 0f;
            if (abilityAction.WasPressedThisFrame() && canStart) StartHold();
            return;
        }

        if (!abilityAction.IsPressed() || !mana.TrySpend(Current.manaCostPerSecond * Time.deltaTime))
        {
            StopHold();
        }
    }

    private void StartHold()
    {
        holdActive = true;

        if (Current.type == AbilityType.Focus)
        {
            controller.SpeedMultiplier = Current.focusSpeedMultiplier;
            controller.ForceShowHitbox = true;
            if (shooter != null) shooter.FireRateMultiplier = Current.fireRateMultiplier;
        }
        else
        {
            controller.SpeedMultiplier = Current.holdSpeedMultiplier;
            holdInvulnerable = true;
        }
    }

    private void StopHold()
    {
        if (!holdActive && !holdInvulnerable) return;

        holdActive = false;
        holdInvulnerable = false;

        if (controller != null)
        {
            controller.SpeedMultiplier = 1f;
            controller.ForceShowHitbox = false;
        }

        if (shooter != null) shooter.FireRateMultiplier = 1f;
    }

    // ---------------- Invulnerabilidade ----------------

    private void RefreshInvulnerability()
    {
        bool wanted = holdInvulnerable || invulnerableTimer > 0f;
        if (wanted == IsInvulnerable) return;

        IsInvulnerable = wanted;
        IsPlayerInvulnerable = wanted;

        SetTint(wanted);
        if (disableCollidersWhileInvulnerable) SetColliders(!wanted);
    }

    private void SetTint(bool invulnerable)
    {
        if (sprites == null) return;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;

            if (invulnerable)
            {
                originalColors[i] = sprites[i].color;
                Color c = originalColors[i];
                c.a *= invulnerableAlpha;
                sprites[i].color = c;
            }
            else
            {
                sprites[i].color = originalColors[i];
            }
        }
    }

    private void SetColliders(bool enable)
    {
        if (enable)
        {
            foreach (Collider2D c in disabledColliders)
            {
                if (c != null) c.enabled = true;
            }
            disabledColliders.Clear();
            return;
        }

        disabledColliders.Clear();
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            if (!c.enabled) continue;

            c.enabled = false;
            disabledColliders.Add(c);
        }
    }

    public string GetKeyLabel()
    {
        string label = abilityAction.GetBindingDisplayString();
        return string.IsNullOrEmpty(label) ? "?" : label;
    }
}