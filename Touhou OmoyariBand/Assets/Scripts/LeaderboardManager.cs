using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    public bool IsInitialized =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    /// <summary>
    /// Inicializa o Unity Services e realiza Login Anônimo.
    /// </summary>
    public async Task InitializeServicesAsync()
    {
        if (IsInitialized) return;

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UGS] Conectado! Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Falha ao inicializar: {e.Message}");
        }
    }

    /// <summary>
    /// Atualiza o nome público do jogador no Leaderboard.
    /// </summary>
    public async Task SetPlayerNameAsync(string playerName)
    {
        if (!IsInitialized) await InitializeServicesAsync();

        try
        {
            // A Unity não permite espaços no UpdatePlayerNameAsync
            string sanitizedName = playerName.Trim().Replace(" ", "_");
            await AuthenticationService.Instance.UpdatePlayerNameAsync(sanitizedName);
            Debug.Log($"[UGS] Nome atualizado para: {sanitizedName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Erro ao salvar nome: {e.Message}");
        }
    }

    /// <summary>
    /// Envia a pontuação da fase.
    /// </summary>
    public async Task SubmitScoreAsync(string leaderboardId, double score)
    {
        if (!IsInitialized) await InitializeServicesAsync();

        try
        {
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"[UGS] Pontuação enviada! Posição atual: #{response.Rank + 1}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Erro ao enviar pontuação: {e.Message}");
        }
    }

    /// <summary>
    /// Busca o Top N pontuações da fase.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetTopScoresAsync(string leaderboardId, int limit = 10)
    {
        if (!IsInitialized) await InitializeServicesAsync();

        try
        {
            var options = new GetScoresOptions { Limit = limit };
            var response = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            return response.Results;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Erro ao buscar placar: {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    /// <summary>
    /// Calcula o percentual do jogador (ex: 1.0 = Top 1%).
    /// </summary>
    public async Task<float> GetPlayerPercentileAsync(string leaderboardId)
    {
        if (!IsInitialized) await InitializeServicesAsync();

        try
        {
            var playerEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions { Limit = 1 });

            int totalPlayers = scoresResponse.Total;
            if (totalPlayers <= 0 || playerEntry == null) return 100f;

            int rank = playerEntry.Rank + 1;
            float percentile = ((float)rank / totalPlayers) * 100f;

            return Mathf.Clamp(percentile, 0.1f, 100f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Erro ao calcular percentual: {e.Message}");
            return 100f;
        }
    }
}