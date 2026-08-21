using System.Collections;
using UnityEngine;

public class BossMovement : MonoBehaviour
{
    public enum MovementState { Static, Linear, IdleHover, Circle }

    [Header("Estado Atual")]
    [SerializeField] private MovementState currentState = MovementState.IdleHover;

    [Header("Configuração de Velocidade")]
    [SerializeField] private float linearSpeed = 3f;

    [Header("Configuração de Hover (Idle)")]
    [SerializeField] private float hoverFrequency = 2f; // Velocidade da oscilação
    [SerializeField] private float hoverAmplitude = 0.5f; // Distância do balanço

    [Header("Configuração de Movimento Circular")]
    [SerializeField] private float circleRadius = 2f;
    [SerializeField] private float circleSpeed = 2f;

    private Vector3 anchorPosition;
    private Coroutine currentMoveCoroutine;
    private float angleTimer;

    private void Start()
    {
        // Define o ponto atual da cena como âncora inicial
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
                // O movimento linear é controlado pela Coroutine
                break;
        }
    }

    // --- LÓGICAS DE MOVIMENTO ---

    private void ApplyHoverMovement()
    {
        // Movimento senoidal suave para cima e para baixo / lados
        float offsetY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float offsetX = Mathf.Cos(Time.time * (hoverFrequency * 0.5f)) * (hoverAmplitude * 0.5f);

        transform.position = anchorPosition + new Vector3(offsetX, offsetY, 0f);
    }

    private void ApplyCircleMovement()
    {
        angleTimer += Time.deltaTime * circleSpeed;

        float x = anchorPosition.x + Mathf.Cos(angleTimer) * circleRadius;
        float y = anchorPosition.y + Mathf.Sin(angleTimer) * circleRadius;

        transform.position = new Vector3(x, y, transform.position.z);
    }

    // --- MÉTODOS PÚBLICOS (CHAMADOS PELO BOSS CONTROLLER / TIMELINE) ---

    public void MoveToPosition(Vector2 targetPosition)
    {
        SetState(MovementState.Linear);
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
        currentMoveCoroutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    public void StartHoverMode()
    {
        anchorPosition = transform.position;
        SetState(MovementState.IdleHover);
    }

    public void StartCircleMode()
    {
        anchorPosition = transform.position;
        angleTimer = 0f;
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
        anchorPosition = target; // Atualiza a âncora para o novo ponto

        // Transiciona automaticamente para o estado Idle após chegar ao destino
        SetState(MovementState.IdleHover);
    }
}