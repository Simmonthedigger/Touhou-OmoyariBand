using UnityEngine;

public class TestBulletSpawner : MonoBehaviour
{
    [Header("Configuração do Spawner")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnBullet();
        }
    }

    private void SpawnBullet()
    {
        if (bulletPrefab == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(bulletPrefab, pos, Quaternion.identity);
    }
}