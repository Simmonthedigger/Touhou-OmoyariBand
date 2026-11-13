using System.Collections;
using TMPro;
using UnityEngine;

public class VictoryScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text percentileText;

    [Header("ID da Fase")]
    [SerializeField] private string currentStageLeaderboardId = "fase_01";

    public void DisplayVictoryScreen(int finalScore)
    {
        if (scoreText != null) scoreText.text = $"Pontuação: {finalScore:N0}";
        if (percentileText != null) percentileText.text = "Calculando colocação global...";

        StartCoroutine(Routine_SubmitAndDisplayPercentile(finalScore));
    }

    private IEnumerator Routine_SubmitAndDisplayPercentile(int score)
    {
        var submitTask = LeaderboardManager.Instance.SubmitScoreAsync(currentStageLeaderboardId, score);
        yield return new WaitUntil(() => submitTask.IsCompleted);

        var percentileTask = LeaderboardManager.Instance.GetPlayerPercentileAsync(currentStageLeaderboardId);
        yield return new WaitUntil(() => percentileTask.IsCompleted);

        float percentile = percentileTask.Result;

        if (percentileText != null)
        {
            if (percentile <= 1.0f)
                percentileText.text = "Incrível! Você está no <color=#FFD700>TOP 1%</color> dos melhores jogadores!";
            else if (percentile <= 5.0f)
                percentileText.text = "Excelente! Você está no <color=#C0C0C0>TOP 5%</color> global!";
            else if (percentile <= 10.0f)
                percentileText.text = "Muito bem! Você está no <color=#CD7F32>TOP 10%</color> dos melhores!";
            else
                percentileText.text = $"Você está entre os <b>TOP {Mathf.CeilToInt(percentile)}%</b> jogadores nesta fase.";
        }
    }

    /// <summary>
    /// Vincule este método ao evento OnEndEdit do TMP_InputField na Tela de Vitória.
    /// </summary>
    public async void OnPlayerNameChanged(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        if (LeaderboardManager.Instance != null)
        {
            await LeaderboardManager.Instance.SetPlayerNameAsync(newName);
        }
    }
}