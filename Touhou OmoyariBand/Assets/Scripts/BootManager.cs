using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    [Tooltip("Nome da cena principal de Gameplay.")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Tooltip("Nome da cena que contém a UI/HUD.")]
    [SerializeField] private string hudSceneName = "HUD";

    private void Start()
    {
        // Carrega a Gameplay e em seguida carrega a HUD por cima em modo Additive
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        SceneManager.LoadScene(hudSceneName, LoadSceneMode.Additive);
    }
}