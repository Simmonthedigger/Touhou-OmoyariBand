using UnityEngine;

/// <summary>
/// Fundo infinito em ciclo que ajusta a largura EXATA para caber na tela com margem de segurança.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AutoFitScrollingBackground : MonoBehaviour
{
    [Header("Ajuste de Tela")]
    [Tooltip("Marque se a imagem deve ajustar a largura para caber na tela.")]
    [SerializeField] private bool fitToScreenWidth = true;

    [Tooltip("Se ativo, mantém a proporção (Aspect Ratio) ao escalar a imagem para não esticar.")]
    [SerializeField] private bool preserveAspectRatio = true;

    [Tooltip("Margem extra em % para cobrir artefatos e bordas cinzas (ex: 1.03 = 3% mais largo que a tela).")]
    [SerializeField, Range(1f, 1.1f)] private float safetyMarginWidth = 1.03f;

    [Header("Rolagem")]
    [Tooltip("Velocidade em unidades por segundo. Positivo = desce. Negativo = sobe.")]
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Ajustes Finos")]
    [Tooltip("Sobreposição (em unidades) entre as cópias, para evitar fresta fina.")]
    [SerializeField] private float tileOverlap = 0.01f;

    [Tooltip("Cópias extras de segurança para telas muito altas.")]
    [SerializeField] private int extraTiles = 1;

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
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        SpriteRenderer source = GetComponent<SpriteRenderer>();

        if (source == null || source.sprite == null) return;

        // 1. Ajusta a escala com base nas dimensões da tela + margem de segurança
        if (fitToScreenWidth && cam != null)
        {
            AjustarEscalaExataTela(source);
        }

        // 2. Calcula as dimensões FINAIS (após o ajuste de escala)
        float height = source.bounds.size.y;
        spacing = Mathf.Max(0.01f, height - tileOverlap);
        startPos = transform.position;

        // Altura visível da câmera
        float viewHeight = height;
        if (cam != null && cam.orthographic)
        {
            viewHeight = cam.orthographicSize * 2f;
        }

        // 3. Calcula e cria os tiles necessários
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

    private void AjustarEscalaExataTela(SpriteRenderer sr)
    {
        // Tamanho real da imagem sem nenhuma escala aplicada
        float larguraRealSprite = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;

        // Largura total da câmera no mundo 2D
        float alturaCamWorld = cam.orthographicSize * 2f;
        float larguraCamWorld = alturaCamWorld * cam.aspect;

        // Multiplica por safetyMarginWidth para cobrir completamente a lateral da tela
        float escalaX = (larguraCamWorld / larguraRealSprite) * safetyMarginWidth;

        Vector3 novaEscala = transform.localScale;
        novaEscala.x = escalaX;

        if (preserveAspectRatio)
        {
            novaEscala.y = escalaX;
        }

        // Aplica a escala direta
        transform.localScale = novaEscala;
    }

    private void Update()
    {
        // Se o jogo estiver em pausa de física/cutscene, o Time.deltaTime vai tratar corretamente
        offset = Mathf.Repeat(offset + scrollSpeed * Time.deltaTime, spacing);
        UpdateTiles();
    }

    private void UpdateTiles()
    {
        if (tiles == null) return;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;

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
        if (tiles == null) return;

        foreach (Transform t in tiles)
        {
            if (t != null && t != transform) Destroy(t.gameObject);
        }
    }
}