using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDSceneLoader : MonoBehaviour
{
    [Tooltip("Nome exato da cena que contém a UI/HUD.")]
    [SerializeField] private string hudSceneName = "HUDScene";

    private void Start()
    {
        // Verifica se a cena de HUD já não está carregada para não duplicar
        Scene hudScene = SceneManager.GetSceneByName(hudSceneName);
        if (!hudScene.isLoaded)
        {
            SceneManager.LoadScene(hudSceneName, LoadSceneMode.Additive);
        }
    }
}