using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void OnClick_Play()
    {
        GameManager.Instance.StartGameplay();
    }

    public void OnClick_Quit()
    {
        GameManager.Instance.QuitGame();
    }
}