using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool; // Namespace obrigatório para a exigência da atividade

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public struct PoolConfig
    {
        public string tag;
        public GameObject prefab;
        public int defaultCapacity;
        public int maxSize;
    }

    [SerializeField] private List<PoolConfig> poolConfigs;

    // Dicionário mapeando a Tag para a Interface da classe nativa ObjectPool
    private Dictionary<string, IObjectPool<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, IObjectPool<GameObject>>();

        foreach (var config in poolConfigs)
        {
            // Criação do ObjectPool nativo da Unity para cada Tag
            var newPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(config.prefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: config.defaultCapacity > 0 ? config.defaultCapacity : 50,
                maxSize: config.maxSize > 0 ? config.maxSize : 500
            );

            poolDictionary.Add(config.tag, newPool);
        }
    }

    // Pega um objeto do Pool nativo
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool com a tag '{tag}' não existe!");
            return null;
        }

        IObjectPool<GameObject> pool = poolDictionary[tag];
        GameObject obj = pool.Get();

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        // Injeta a referência da pool e da tag no próprio tiro
        if (obj.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.SetPoolReference(pool);
        }

        return obj;
    }

    // Devolve o objeto ao Pool nativo
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out IObjectPool<GameObject> pool))
        {
            pool.Release(obj);
        }
        else
        {
            obj.SetActive(false);
        }
    }
}