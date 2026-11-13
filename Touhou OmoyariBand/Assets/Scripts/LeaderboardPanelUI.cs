using TMPro;
using UnityEngine;

public class LeaderboardPanelUI : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private string leaderboardId = "fase_01";
    [SerializeField] private Transform container;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private GameObject loadingIndicator;

    private async void OnEnable()
    {
        await RefreshLeaderboard();
    }

    public async void RefreshButton()
    {
        await RefreshLeaderboard();
    }

    public async System.Threading.Tasks.Task RefreshLeaderboard()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        var scores = await LeaderboardManager.Instance.GetTopScoresAsync(leaderboardId, 10);

        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        foreach (var entry in scores)
        {
            GameObject item = Instantiate(entryPrefab, container);
            if (item.TryGetComponent(out LeaderboardEntryUI entryUI))
            {
                // Remove o sufixo automático "#1234" que a Unity adiciona ao PlayerName
                string cleanName = entry.PlayerName;
                if (!string.IsNullOrEmpty(cleanName) && cleanName.Contains("#"))
                {
                    cleanName = cleanName.Split('#')[0];
                }

                entryUI.Setup(entry.Rank + 1, cleanName, entry.Score);
            }
        }
    }

    public async void OnSaveNameSubmitted(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        await LeaderboardManager.Instance.SetPlayerNameAsync(newName);
        await RefreshLeaderboard();
    }
}