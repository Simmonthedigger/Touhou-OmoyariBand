using UnityEngine;
using UnityEngine.InputSystem; // Importante para o novo Input System

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Velocidade")]
    [SerializeField] private float normalSpeed = 8f;
    [SerializeField] private float focusSpeed = 3.5f;

    [Header("Hitbox e Foco")]
    [Tooltip("Arraste o GameObject Filho da Hitbox aqui")]
    [SerializeField] private GameObject hitboxObject;

    [Header("Configuração de Inputs (New Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private InputActionProperty focusAction;

    [Header("Limites da Tela")]
    [SerializeField] private bool limitToScreen = true;
    [SerializeField] private Vector2 padding = new Vector2(0.5f, 0.5f);

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isFocused;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        if (hitboxObject != null)
        {
            hitboxObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Ativa as ações de Input ao habilitar o objeto
        moveAction.action?.Enable();
        focusAction.action?.Enable();
    }

    private void OnDisable()
    {
        // Desativa as ações ao desabilitar para evitar vazamento de memória
        moveAction.action?.Disable();
        focusAction.action?.Disable();
    }

    private void Update()
    {
        ProcessInputs();
        HandleFocus();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void ProcessInputs()
    {
        // Leitura do vetor 2D usando o novo Input System
        moveInput = moveAction.action.ReadValue<Vector2>();

        // Evita velocidade diagonal maior se não estiver usando composite normalizado no Input Asset
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void HandleFocus()
    {
        // Verifica se o botão de foco (ex: Shift) está sendo pressionado
        isFocused = focusAction.action.IsPressed();

        if (hitboxObject != null)
        {
            hitboxObject.SetActive(isFocused);
        }
    }

    private void MovePlayer()
    {
        float currentSpeed = isFocused ? focusSpeed : normalSpeed;
        Vector2 targetPosition = rb.position + moveInput * (currentSpeed * Time.fixedDeltaTime);

        if (limitToScreen && mainCamera != null)
        {
            targetPosition = ClampToScreen(targetPosition);
        }

        rb.MovePosition(targetPosition);
    }

    private Vector2 ClampToScreen(Vector2 targetPos)
    {
        Vector2 minBounds = mainCamera.ViewportToWorldPoint(new Vector2(0, 0)) + (Vector3)padding;
        Vector2 maxBounds = mainCamera.ViewportToWorldPoint(new Vector2(1, 1)) - (Vector3)padding;

        targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
        targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);

        return targetPos;
    }
}