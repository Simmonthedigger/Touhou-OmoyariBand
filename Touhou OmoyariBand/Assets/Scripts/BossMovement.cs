using System.Collections;
using UnityEngine;

public class BossMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private Coroutine moveCoroutine;

    public void MoveToPosition(Vector2 targetPosition)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    private IEnumerator MoveRoutine(Vector2 target)
    {
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}