using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageEvent
{
    public string eventName = "Ataque 1 - Espiral";
    [Tooltip("Tempo em segundos na música/fase para disparar")]
    public float timeInSeconds;

    public UnityEngine.Events.UnityEvent onEventTriggered;
    [HideInInspector] public bool hasBeenTriggered = false;
}

public class StageTimelineManager : MonoBehaviour
{
    [Header("Áudio e Sincronização")]
    [SerializeField] private AudioSource musicSource;

    [Header("Linha do Tempo da Fase")]
    [SerializeField] private List<StageEvent> stageTimeline = new List<StageEvent>();

    private float currentTime = 0f;

    private void Start()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void Update()
    {
        // Usa o tempo exato do AudioSource se a música estiver tocando
        if (musicSource != null && musicSource.isPlaying)
        {
            currentTime = musicSource.time;
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        CheckTimelineEvents();
    }

    private void CheckTimelineEvents()
    {
        foreach (var evt in stageTimeline)
        {
            if (!evt.hasBeenTriggered && currentTime >= evt.timeInSeconds)
            {
                evt.hasBeenTriggered = true;
                evt.onEventTriggered?.Invoke();
            }
        }
    }

    // Método para resetar eventos se reiniciar a música/fase
    public void ResetTimeline()
    {
        currentTime = 0f;
        foreach (var evt in stageTimeline)
        {
            evt.hasBeenTriggered = false;
        }
    }
}