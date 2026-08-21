using System.Collections;
using UnityEngine;

public class BossMovement : MonoBehaviour
{
    public enum MovementState { Static, Linear, IdleHover, Circle }

    [Header("Estado Atual")]
    [SerializeField] private MovementState currentState = MovementState.IdleHover;

    [Header("Configuração de Velocidade")]
    [SerializeField] private float linearSpeed = 3f;
    [SerializeField] private float transitionSpeed = 3.5f; // Velocidade de transição suave entre estados

    [Header("Configuração de Hover (Idle)")]
    [SerializeField] private float hoverFrequency = 2f;
    [SerializeField] private float hoverAmplitude = 0.5f;

    [Header("Configuração de Movimento Circular")]
    [SerializeField] private float circleRadius = 2f;
    [SerializeField] private float circleSpeed = 2f;

    private Vector3 anchorPosition;
    private Coroutine currentMoveCoroutine;
    private float angleTimer;

    private void Start()
    {
        anchorPosition = transform.position;
    }

    private void Update()
    {
        switch (currentState)
        {
            case MovementState.IdleHover:
                ApplyHoverMovement();
                break;

            case MovementState.Circle:
                ApplyCircleMovement();
                break;

            case MovementState.Static:
            case MovementState.Linear:
                break;
        }
    }

    // --- LÓGICAS DE MOVIMENTO COM TRANSIÇÃO SUAVE ---

    private void ApplyHoverMovement()
    {
        float offsetY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float offsetX = Mathf.Cos(Time.time * (hoverFrequency * 0.5f)) * (hoverAmplitude * 0.5f);

        Vector3 targetHoverPos = anchorPosition + new Vector3(offsetX, offsetY, 0f);

        // Desloca suavemente a posição atual até se alinhar com o ciclo de oscilação
        transform.position = Vector3.MoveTowards(transform.position, targetHoverPos, transitionSpeed * Time.deltaTime);
    }

    private void ApplyCircleMovement()
    {
        angleTimer += Time.deltaTime * circleSpeed;

        float x = anchorPosition.x + Mathf.Cos(angleTimer) * circleRadius;
        float y = anchorPosition.y + Mathf.Sin(angleTimer) * circleRadius;

        Vector3 targetCirclePos = new Vector3(x, y, transform.position.z);

        // Transiciona suavemente até entrar no raio de órbita do círculo
        transform.position = Vector3.MoveTowards(transform.position, targetCirclePos, transitionSpeed * Time.deltaTime);
    }

    // --- MÉTODOS DE TRANSIÇÃO E COMANDO ---

    public void MoveToPosition(Vector2 targetPosition)
    {
        SetState(MovementState.Linear);
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
        currentMoveCoroutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    public void StartHoverMode()
    {
        // Define o ponto onde o Boss está no momento como a nova âncora do Idle
        anchorPosition = transform.position;
        SetState(MovementState.IdleHover);
    }

    public void StartCircleMode()
    {
        anchorPosition = transform.position;

        // Ajusta o ângulo inicial com base na posição atual para não dar "tranco" ao entrar no círculo
        Vector2 offset = transform.position - anchorPosition;
        if (offset.sqrMagnitude > 0.001f)
        {
            angleTimer = Mathf.Atan2(offset.y, offset.x);
        }
        else
        {
            angleTimer = 0f;
        }

        SetState(MovementState.Circle);
    }

    public void SetState(MovementState newState)
    {
        currentState = newState;
    }

    private IEnumerator MoveRoutine(Vector2 target)
    {
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, target, linearSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        anchorPosition = target;

        // Transiciona de forma limpa para o Idle após chegar ao destino
        StartHoverMode();
    }
}