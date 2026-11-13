using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Fundo da fase. Você escolhe o tipo no Inspector:
///  - ScrollingImage: fundo infinito em ciclo que ajusta a largura EXATA para caber na tela com margem de segurança.
///  - Video: toca um vídeo em loop como fundo, ocupando a tela toda.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AutoFitScrollingBackground : MonoBehaviour
{
    public enum BackgroundMode { ScrollingImage, Video }

    [Header("Tipo de Fundo")]
    [Tooltip("ScrollingImage = imagem que rola em ciclo (usa o SpriteRenderer deste objeto). Video = vídeo em loop.")]
    [SerializeField] private BackgroundMode mode = BackgroundMode.ScrollingImage;

    [Header("Vídeo (só vale se o Tipo de Fundo for Video)")]
    [Tooltip("Arraste aqui o arquivo de vídeo (mp4, webm...) que está no Project.")]
    [SerializeField] private VideoClip videoClip;

    [SerializeField] private bool loopVideo = true;

    [Tooltip("Velocidade de reprodução do vídeo (1 = normal).")]
    [SerializeField, Range(0.1f, 3f)] private float playbackSpeed = 1f;

    [Header("Ajuste de Tela")]
    [Tooltip("Imagem: marque se a imagem deve ajustar a largura para caber na tela.")]
    [SerializeField] private bool fitToScreenWidth = true;

    [Tooltip("Imagem: mantém a proporção (Aspect Ratio) ao escalar para não esticar. Vídeo: ligado = preenche a tela cortando as sobras; desligado = estica o vídeo para a tela.")]
    [SerializeField] private bool preserveAspectRatio = true;

    [Tooltip("Imagem: margem extra em % para cobrir artefatos e bordas cinzas (ex: 1.03 = 3% mais largo que a tela).")]
    [SerializeField, Range(1f, 1.1f)] private float safetyMarginWidth = 1.03f;

    [Header("Rolagem (só imagem)")]
    [Tooltip("Velocidade em unidades por segundo. Positivo = desce. Negativo = sobe.")]
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Ajustes Finos (só imagem)")]
    [Tooltip("Sobreposição (em unidades) entre as cópias, para evitar fresta fina.")]
    [SerializeField] private float tileOverlap = 0.01f;

    [Tooltip("Cópias extras de segurança para telas muito altas.")]
    [SerializeField] private int extraTiles = 1;

    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    // --- Imagem rolando ---
    private Transform[] tiles;
    private int[] tileIndices;
    private float spacing;
    private float offset;
    private Vector3 startPos;
    private Camera cam;

    // --- Vídeo ---
    private bool useVideo;
    private VideoPlayer videoPlayer;
    private GameObject videoCanvasObj;
    private RawImage videoImage;
    private bool pausedByTimeScale;
    private float lastAspect = -1f;

    private void Start()
    {
        cam = Camera.main;
        SpriteRenderer source = GetComponent<SpriteRenderer>();

        // Modo vídeo: não cria cópias da imagem, só toca o vídeo
        if (mode == BackgroundMode.Video)
        {
            if (videoClip != null && cam != null)
            {
                StartVideoBackground(source);
                return;
            }

            Debug.LogWarning("[AutoFitScrollingBackground] Tipo 'Video' escolhido, mas falta o Video Clip (ou a câmera principal). Usando a imagem rolando no lugar.");
        }

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
        if (useVideo)
        {
            UpdateVideo();
            return;
        }

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

    // =====================================================
    // --- FUNDO EM VÍDEO ---
    // =====================================================

    private void StartVideoBackground(SpriteRenderer source)
    {
        useVideo = true;

        // A imagem deste objeto não é usada no modo vídeo
        if (source != null) source.enabled = false;

        // Canvas atrás de tudo: usa a mesma Sorting Layer / Order in Layer do SpriteRenderer deste objeto
        videoCanvasObj = new GameObject(name + "_VideoCanvas", typeof(Canvas));
        videoCanvasObj.layer = gameObject.layer;

        Canvas canvas = videoCanvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.95f);
        canvas.sortingLayerID = source != null ? source.sortingLayerID : 0;
        canvas.sortingOrder = source != null ? source.sortingOrder : 0;

        // Imagem que mostra o vídeo, esticada na tela inteira (invisível até o vídeo ficar pronto)
        GameObject imageObj = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage));
        imageObj.layer = gameObject.layer;
        imageObj.transform.SetParent(videoCanvasObj.transform, false);

        RectTransform rt = (RectTransform)imageObj.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        videoImage = imageObj.GetComponent<RawImage>();
        videoImage.raycastTarget = false; // nunca bloqueia cliques
        videoImage.enabled = false;

        // Player do vídeo: sem som (a música do jogo é a trilha) e sem criar AudioSource
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.playbackSpeed = playbackSpeed;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoImage == null) return;

        videoImage.texture = vp.texture;
        videoImage.enabled = true;
        ApplyVideoFit();

        // Se o jogo estiver congelado (diálogo/pausa), o vídeo começa quando o tempo voltar
        if (Time.timeScale > 0f)
        {
            vp.Play();
        }
        else
        {
            pausedByTimeScale = true;
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("[AutoFitScrollingBackground] Erro no vídeo de fundo: " + message);
    }

    private void UpdateVideo()
    {
        if (videoPlayer == null) return;

        // O vídeo acompanha o tempo do jogo: congela com Time.timeScale = 0 (pausa, diálogo) e volta depois
        bool frozen = Time.timeScale <= 0f;

        if (frozen && !pausedByTimeScale && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            pausedByTimeScale = true;
        }
        else if (!frozen && pausedByTimeScale && videoPlayer.isPrepared)
        {
            videoPlayer.Play();
            pausedByTimeScale = false;
        }

        // Reajusta o enquadramento se a proporção da tela mudar
        if (cam != null && !Mathf.Approximately(cam.aspect, lastAspect))
        {
            ApplyVideoFit();
        }
    }

    // Ligado = "cover": preenche a tela inteira cortando o que sobrar, sem esticar. Desligado = estica.
    private void ApplyVideoFit()
    {
        if (videoImage == null || videoClip == null) return;

        Rect uv = new Rect(0f, 0f, 1f, 1f);

        if (cam != null)
        {
            lastAspect = cam.aspect;

            if (preserveAspectRatio && videoClip.height > 0)
            {
                float videoAspect = (float)videoClip.width / videoClip.height;
                float screenAspect = cam.aspect;

                if (videoAspect > screenAspect)
                {
                    // Vídeo mais largo que a tela: corta as laterais
                    uv.width = screenAspect / videoAspect;
                    uv.x = (1f - uv.width) * 0.5f;
                }
                else
                {
                    // Vídeo mais alto que a tela: corta em cima e embaixo
                    uv.height = videoAspect / screenAspect;
                    uv.y = (1f - uv.height) * 0.5f;
                }
            }
        }

        videoImage.uvRect = uv;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        if (videoCanvasObj != null) Destroy(videoCanvasObj);

        if (tiles == null) return;

        foreach (Transform t in tiles)
        {
            if (t != null && t != transform) Destroy(t.gameObject);
        }
    }
}