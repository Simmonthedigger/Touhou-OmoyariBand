using TMPro;
using UnityEngine;

public class LeaderboardEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;

    public void Setup(int rank, string playerName, double score)
    {
        if (rankText != null) rankText.text = $"#{rank}";
        if (nameText != null) nameText.text = string.IsNullOrEmpty(playerName) ? "Jogador" : playerName;
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }
}