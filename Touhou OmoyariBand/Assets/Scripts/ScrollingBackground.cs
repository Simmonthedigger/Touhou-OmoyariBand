using UnityEngine;

/// <summary>
/// Fundo infinito que rola para baixo em ciclo.
/// Coloque em um objeto com SpriteRenderer (o sprite do fundo). O script cria sozinho as cópias
/// necessárias para cobrir a tela e as reposiciona para a imagem se repetir sem cortes.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    [Header("Rolagem")]
    [Tooltip("Velocidade em unidades por segundo. Positivo = desce. Negativo = sobe.")]
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Ajustes finos")]
    [Tooltip("Sobreposição (em unidades) entre as cópias, para evitar fresta fina entre elas.")]
    [SerializeField] private float tileOverlap = 0.005f;

    [Tooltip("Cópias extras de segurança. Aumente se aparecer espaço vazio em telas muito altas.")]
    [SerializeField] private int extraTiles = 0;

    /// <summary>Permite mudar a velocidade por outros scripts (ex.: acelerar na troca de fase).</summary>
    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    private Transform[] tiles;
    private int[] tileIndices;
    private float spacing;
    private float offset;
    private Vector3 startPos;

    private void Start()
    {
        SpriteRenderer source = GetComponent<SpriteRenderer>();

        float height = source.bounds.size.y;
        spacing = Mathf.Max(0.01f, height - tileOverlap);
        startPos = transform.position;

        // Altura visível da câmera (para saber quantas cópias são necessárias)
        float viewHeight = height;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            viewHeight = cam.orthographicSize * 2f;
        }

        // Se a imagem for menor que a tela, precisamos de cópias extras acima e abaixo
        int m = Mathf.Max(0, Mathf.CeilToInt((viewHeight - height) / (2f * spacing))) + Mathf.Max(0, extraTiles);
        int kMin = -m;
        int kMax = 1 + m;

        int count = kMax - kMin + 1;
        tiles = new Transform[count];
        tileIndices = new int[count];

        for (int i = 0; i < count; i++)
        {
            int k = kMin + i;
            tileIndices[i] = k;
            tiles[i] = (k == 0) ? transform : CreateTile(source, k);
        }

        UpdateTiles();
    }

    private void Update()
    {
        // Mathf.Repeat faz o ciclo: ao percorrer a altura de uma imagem, volta a 0 sem emenda visível
        offset = Mathf.Repeat(offset + scrollSpeed * Time.deltaTime, spacing);
        UpdateTiles();
    }

    private void UpdateTiles()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            Vector3 pos = startPos;
            pos.y += tileIndices[i] * spacing - offset;
            tiles[i].position = pos;
        }
    }

    private Transform CreateTile(SpriteRenderer source, int index)
    {
        GameObject go = new GameObject(name + "_Tile" + index);
        go.layer = gameObject.layer;

        Transform t = go.transform;
        t.SetParent(transform.parent, false);
        t.rotation = transform.rotation;
        t.localScale = transform.localScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = source.color;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder;
        sr.maskInteraction = source.maskInteraction;

        if (source.drawMode != SpriteDrawMode.Simple)
        {
            sr.drawMode = source.drawMode;
            sr.size = source.size;
        }

        return t;
    }

    private void OnDestroy()
    {
        // Remove as cópias criadas pelo script
        if (tiles == null) return;

        foreach (Transform t in tiles)
        {
            if (t != null && t != transform) Destroy(t.gameObject);
        }
    }
}