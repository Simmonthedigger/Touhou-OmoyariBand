using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coloque no botão de cada fase da tela de seleção. Ele só cuida do ESTADO visual:
/// desliga o botão (e mostra um cadeado) se a fase estiver bloqueada, e mostra um check se já foi completada.
/// O clique continua chamando MainMenuController.OnClick_LoadStageScene(nome da cena) como antes;
/// o 'Scene Name' aqui precisa ser igual ao que você passou nesse clique.
/// Atualiza sempre que o painel de seleção de fases é aberto.
/// </summary>
[RequireComponent(typeof(Button))]
public class StageButtonState : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [Tooltip("Opcional: objeto (cadeado) mostrado quando a fase está bloqueada.")]
    [SerializeField] private GameObject lockedIcon;
    [Tooltip("Opcional: objeto (check) mostrado quando a fase já foi completada.")]
    [SerializeField] private GameObject completedIcon;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (button == null) button = GetComponent<Button>();

        bool unlocked = ProgressionState.IsStageUnlocked(sceneName);
        button.interactable = unlocked;

        if (lockedIcon != null) lockedIcon.SetActive(!unlocked);
        if (completedIcon != null) completedIcon.SetActive(ProgressionState.IsCompleted(sceneName));
    }
}
