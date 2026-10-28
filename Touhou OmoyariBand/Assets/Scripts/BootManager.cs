using UnityEngine;
using UnityEngine.SceneManagement;

public class BootController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainMenu"; // Altere de "Gameplay" para "MainMenu"

    private void Start()
    {
        // Executa inicializações necessárias (save, áudio, etc.)
        InitializeGameSystems();

        // Carrega o Menu Principal
        SceneManager.LoadScene(nextSceneName);
    }

    private void InitializeGameSystems()
    {
        // Seu código de inicialização do projeto
    }
}