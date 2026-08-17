using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool com a tag '{tag}' não existe!");
            return null;
        }

        Queue<GameObject> poolQueue = poolDictionary[tag];

        // Se a fila estiver vazia, cria um novo objeto dinamicamente para expandir o pool
        GameObject objToSpawn;
        if (poolQueue.Count > 0 && !poolQueue.Peek().activeSelf)
        {
            objToSpawn = poolQueue.Dequeue();
        }
        else
        {
            // Busca o prefab correspondente para instanciar extra
            Pool pool = pools.Find(p => p.tag == tag);
            objToSpawn = Instantiate(pool.prefab, transform);
        }

        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        poolQueue.Enqueue(objToSpawn);
        return objToSpawn;
    }
}