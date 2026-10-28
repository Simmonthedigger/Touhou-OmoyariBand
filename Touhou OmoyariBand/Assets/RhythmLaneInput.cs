using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Input de baixa latência para o RhythmController.
/// Em vez de perguntar "a tecla foi apertada neste frame?" (erro de até 1 frame), guarda o TIMESTAMP do
/// evento do Input System (o instante em que a tecla realmente foi registrada). O controller então calcula
/// há quantos segundos o aperto aconteceu e julga naquele instante, independente do FPS.
/// </summary>
public class RhythmLaneInput : IDisposable
{
    struct Press
    {
        public int lane;
        public double eventTime;
    }

    readonly List<InputAction> actions = new List<InputAction>();
    readonly Queue<Press> queue = new Queue<Press>();

    public RhythmLaneInput(IReadOnlyList<Key> keys)
    {
        int n = Math.Min(keys.Count, 4);
        for (int i = 0; i < n; i++)
        {
            if (keys[i] == Key.None) continue;

            int lane = i;
            string control = keys[i].ToString();
            if (control.StartsWith("Digit")) control = control.Substring(5);   // Key.Digit1 -> "1"

            var a = new InputAction("RhythmLane" + i, InputActionType.Button, "<Keyboard>/" + control);
            a.performed += ctx => queue.Enqueue(new Press { lane = lane, eventTime = ctx.time });
            a.Enable();
            actions.Add(a);
        }
    }

    /// <summary>Próximo aperto da fila. 'ageSeconds' = há quanto tempo (real) a tecla foi apertada.</summary>
    public bool TryDequeue(out int lane, out double ageSeconds)
    {
        if (queue.Count == 0)
        {
            lane = -1;
            ageSeconds = 0.0;
            return false;
        }

        Press p = queue.Dequeue();
        lane = p.lane;
        ageSeconds = InputState.currentTime - p.eventTime;
        if (ageSeconds < 0.0 || ageSeconds > 0.25) ageSeconds = 0.0;   // timestamp estranho: usa "agora"
        return true;
    }

    public void Clear() => queue.Clear();

    public void Dispose()
    {
        foreach (var a in actions)
        {
            a.Disable();
            a.Dispose();
        }
        actions.Clear();
    }
}
