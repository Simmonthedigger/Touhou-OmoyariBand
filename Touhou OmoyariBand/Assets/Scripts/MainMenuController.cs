using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis do Menu")]
    [SerializeField] private GameObject experiencePanel;  // Painel de nível de experiência (aparece assim que o jogo abre)
    [SerializeField] private GameObject mainPanel;        // Painel Principal com os botões "Jogar" e "Sair"
    [SerializeField] private GameObject stageSelectPanel; // Painel com os botões das fases

    private void Start()
    {
        // Visitante novo (ainda sem nível escolhido): pergunta a experiência primeiro.
        // Quem volta de uma fase já tem nível escolhido e cai direto no menu principal.
        if (ProgressionState.HasSelectedExperience) ShowMain();
        else ShowExperience();
    }

    // ---------------- Nível de experiência ----------------

    /// <summary>
    /// Botões de nível de experiência. Passe no Inspector quantas fases o visitante já completou:
    /// 0 = "Primeira vez jogando", 2 = "Já joguei 2 fases" etc.
    /// </summary>
    public void OnClick_SelectExperience(int stagesCompleted)
    {
        ProgressionState.SetExperienceStageCount(stagesCompleted);
        ShowMain();
    }

    /// <summary>Botão "Já zerei o jogo": libera todas as fases e habilidades.</summary>
    public void OnClick_SelectExperienceFinishedGame()
    {
        ProgressionState.SetExperienceFinishedGame();
        ShowMain();
    }

    /// <summary>Botão "Trocar jogador" no menu principal: apaga a progressão e pergunta o nível de novo.</summary>
    public void OnClick_ChangePlayer()
    {
        ProgressionState.ResetAll();
        AbilitySelection.ResetSelection();
        ShowExperience();
    }

    // ---------------- Navegação ----------------

    /// <summary>
    /// Botão "Jogar": Abre o painel de Seleção de Fases
    /// </summary>
    public void OnClick_OpenStageSelect()
    {
        SetPanels(false, false, true);
    }

    /// <summary>
    /// Botão "Voltar" (dentro da Seleção de Fases): Retorna ao menu principal
    /// </summary>
    public void OnClick_BackToMainMenu()
    {
        ShowMain();
    }

    /// <summary>
    /// Inicia uma fase passando o nome exato da cena no Inspector
    /// </summary>
    public void OnClick_LoadStageScene(string sceneName)
    {
        // Proteção extra: mesmo que o botão esteja ativo por engano, fase bloqueada não abre
        if (!ProgressionState.IsStageUnlocked(sceneName)) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameplay(sceneName); // Alterado aqui
        }
    }

    /// <summary>
    /// Botão de sair do jogo
    /// </summary>
    public void OnClick_Quit()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }

    // ---------------- Painéis ----------------

    private void ShowExperience() { SetPanels(true, false, false); }
    private void ShowMain() { SetPanels(false, true, false); }

    private void SetPanels(bool experience, bool main, bool stageSelect)
    {
        if (experiencePanel != null) experiencePanel.SetActive(experience);
        if (mainPanel != null) mainPanel.SetActive(main);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(stageSelect);
    }
}