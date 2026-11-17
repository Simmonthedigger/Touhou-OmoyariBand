using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// OPCIONAL, para exposição: se ninguém mexer em teclado, mouse ou controle por 'idleSeconds',
/// assume que o visitante foi embora, apaga a progressão e volta ao menu, que pergunta o nível de experiência
/// do próximo visitante. Coloque UM objeto com este script na cena do menu: ele sobrevive às trocas de cena.
/// </summary>
public class ExhibitionIdleReset : MonoBehaviour
{
    [Tooltip("Nome EXATO da cena do menu principal.")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [Tooltip("Segundos sem nenhum input até resetar. Dica: use um valor maior que o tempo de uma cutscene/diálogo.")]
    [SerializeField] private float idleSeconds = 90f;

    private static ExhibitionIdleReset instance;
    private float lastInputTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        lastInputTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (AnyInput()) lastInputTime = Time.unscaledTime;
        if (Time.unscaledTime - lastInputTime < idleSeconds) return;

        lastInputTime = Time.unscaledTime;

        bool inMenu = SceneManager.GetActiveScene().name == menuSceneName;
        if (inMenu && !ProgressionState.HasSelectedExperience) return;   // já está na pergunta de nível

        // 1. Limpa a progressão e a habilidade escolhida pelo visitante anterior
        ProgressionState.ResetAll();
        AbilitySelection.ResetSelection();

        // 2. Garante que estados globais de cutscene/tempo/áudio estejam destravados
        CutsceneState.End();
        CutsceneState.EndCountdown();
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 3. Volta ao menu principal
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private static bool AnyInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.anyKey.isPressed) return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
                              mouse.delta.ReadValue().sqrMagnitude > 4f)) return true;

        Gamepad pad = Gamepad.current;   // só botões (o analógico pode ter drift e nunca deixaria resetar)
        if (pad != null && (pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed ||
                            pad.buttonNorth.isPressed || pad.startButton.isPressed ||
                            pad.dpad.ReadValue().sqrMagnitude > 0f)) return true;

        return false;
    }
}