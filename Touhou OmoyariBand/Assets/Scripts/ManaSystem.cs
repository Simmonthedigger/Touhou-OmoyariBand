using UnityEngine;

/// <summary>
/// Mana do player. Ganha mana sempre que o jogador GANHA pontos
/// (ouve o evento PointsGained do ScoreManager). Fica no objeto do Player.
/// Perder pontos (dano) não tira mana, e recuperar os pontos perdidos não dá mana de novo.
/// </summary>
public class ManaSystem : MonoBehaviour
{
    [Header("Mana")]
    [SerializeField, Min(1f)] private float maxMana = 100f;
    [Tooltip("Mana que o player já tem ao começar a fase.")]
    [SerializeField, Min(0f)] private float startingMana = 0f;

    [Header("Ganho de mana")]
    [Tooltip("Mana ganha por cada 1 ponto ganho. 1 = cada ponto vira 1 de mana. " +
             "Atenção: o ScoreManager dá pontos sozinho a cada segundo, então valores altos enchem a barra rápido.")]
    [SerializeField, Min(0f)] private float manaPerScorePoint = 1f;

    [Tooltip("Os pontos que o ScoreManager dá sozinhos a cada segundo também dão mana? " +
             "Desligue para só os acertos no Boss darem mana.")]
    [SerializeField] private bool countTimePoints = true;

    [Tooltip("Mana recuperada por segundo, sem precisar de pontos. 0 = desligado.")]
    [SerializeField, Min(0f)] private float passiveRegenPerSecond = 0f;

    public float Current { get; private set; }
    public float Max => maxMana;
    public float Normalized => Mathf.Clamp01(Current / Mathf.Max(0.0001f, maxMana));

    private ScoreManager subscribedTo;

    private void Awake()
    {
        Current = Mathf.Clamp(startingMana, 0f, maxMana);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        // Regeneração passiva (opcional)
        if (passiveRegenPerSecond > 0f)
        {
            AddMana(passiveRegenPerSecond * Time.deltaTime);
        }

        // O ScoreManager pode ser carregado depois (ex.: cena de HUD): tenta se conectar até conseguir
        if (subscribedTo == null)
        {
            ScoreManager score = ScoreManager.Instance;
            if (score != null)
            {
                score.PointsGained += OnPointsGained;
                subscribedTo = score;
            }
        }
    }

    private void Unsubscribe()
    {
        if (subscribedTo != null)
        {
            subscribedTo.PointsGained -= OnPointsGained;
        }
        subscribedTo = null;
    }

    private void OnPointsGained(float points, bool fromTime)
    {
        if (fromTime && !countTimePoints) return;

        AddMana(points * manaPerScorePoint);
    }

    public void AddMana(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(maxMana, Current + amount);
    }

    /// <summary>Gasta a mana se houver o suficiente. Retorna false (sem gastar nada) se não houver.</summary>
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (Current < amount) return false;

        Current -= amount;
        return true;
    }
}
