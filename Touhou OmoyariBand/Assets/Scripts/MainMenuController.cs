using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis do Menu")]
    [SerializeField] private GameObject mainPanel;        // Painel Principal com os botões "Jogar" e "Sair"
    [SerializeField] private GameObject stageSelectPanel; // Painel com os botões das fases

    private void Start()
    {
        // Garante que o menu principal comece visível e a seleção de fases oculta
        if (mainPanel != null) mainPanel.SetActive(true);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
    }

    /// <summary>
    /// Botão "Jogar": Abre o painel de Seleção de Fases
    /// </summary>
    public void OnClick_OpenStageSelect()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(true);
    }

    /// <summary>
    /// Botão "Voltar" (dentro da Seleção de Fases): Retorna ao menu principal
    /// </summary>
    public void OnClick_BackToMainMenu()
    {
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    /// <summary>
    /// Inicia uma fase passando o nome exato da cena no Inspector
    /// </summary>
    public void OnClick_LoadStageScene(string sceneName)
    {
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
}