using System.Collections;
using UnityEngine;

public class DropBox : MonoBehaviour
{
    [Header("Configurações do Lançamento")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 6, 0); // Ponto fora da tela no topo
    [SerializeField] private Vector3 targetPosition = new Vector3(0, 2, 0); // Onde ela vai parar
    [SerializeField] private float dropSpeed = 5f;

    [Header("Ações ao Chegar/Destruir")]
    [SerializeField] private bool destroyOnArrival = false;
    [SerializeField] private GameObject impactEffectPrefab; // Opcional: efeito visual

    private bool isDropping = false;

    private void Awake()
    {
        // Começa escondida ou desativada
        gameObject.SetActive(false);
    }

    // Método PÚBLICO que a Timeline vai chamar!
    public void Drop()
    {
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        isDropping = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, dropSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        isDropping = false;

        OnReachTarget();
    }

    private void OnReachTarget()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        if (destroyOnArrival)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se a caixa cair em cima do Player, causa dano
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
    }
}