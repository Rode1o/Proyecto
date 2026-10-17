using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Genera la escena MainMenu desde cero con el arte del menú. Se puede volver a ejecutar: reemplaza la escena.
/// Botones a la izquierda; Controles y Configuración se despliegan en un pergamino a la derecha.
/// </summary>
static class MainMenuSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    const string ArtPath = "Assets/Art/UI/MainMenu/";
    const string IconPath = "Assets/Art/UI/Icons/";
    // Primer fotograma del vídeo: se ve mientras carga (o si el vídeo falla).
    const string BackgroundPath = "Assets/Art/Backgrounds/JardinEncantadoDia_Poster.jpg";
    const string BackgroundVideo = "Video/JardinEncantadoDia.mp4";
    const string BodyFontFolder = "Assets/Art/Fonts/Alegreya/";
    const string TitleFontFolder = "Assets/Art/Fonts/Cinzel/";
    const string ShineShaderName = "UI/Menu Button Shine";
    const string ShineMaterialPath = "Assets/Art/Shaders/MenuButtonShine.mat";

    // Diseño a 2K. Los botones principales se ven a su tamaño nativo (520x175) en 2560x1440.
    static readonly Vector2 ReferenceResolution = new Vector2(2560f, 1440f);
    static readonly Vector2 ButtonSize = new Vector2(520f, 175f);
    static readonly Vector2 TabSize = new Vector2(357f, 120f);
    static readonly Vector2 BackButtonSize = new Vector2(360f, 121f);

    // Bordes 9-slice de los marcos (izq., abajo, der., arriba) en píxeles del sprite de 520x175.
    static readonly Vector4 FrameBorder = new Vector4(78f, 58f, 78f, 58f);

    // Columna de botones y panel desplegable (en unidades de la resolución de referencia).
    const float ColumnCenterX = 420f;
    const float PanelLeft = 780f;
    const float PanelWidth = 1700f;

    static readonly Color CreamColor = new Color32(243, 230, 207, 255);
    static readonly Color GoldColor = new Color32(232, 184, 100, 255);
    static readonly Color DarkBrownColor = new Color32(43, 26, 16, 255);
    static readonly Color InkColor = new Color32(59, 36, 20, 255);
    static readonly Color InkSoftColor = new Color32(104, 68, 40, 255);
    static readonly Color AmberColor = new Color32(196, 128, 40, 255);
    static readonly Color TrackColor = new Color(0.35f, 0.22f, 0.12f, 0.28f);
    static readonly Color ShadeColor = new Color(0.06f, 0.03f, 0.08f, 0.22f);

    static Sprite buttonActive;
    static Sprite buttonInactive;
    static Sprite leaf;
    static Sprite ornamental;
    static Sprite background;
    static Sprite parchment;
    static Sprite roundedSprite;
    static Sprite knobSprite;
    static TMP_FontAsset regularFont;
    static TMP_FontAsset boldFont;
    static TMP_FontAsset titleFont;
    static Material titleMaterial;
    static Material shineMaterial;

    [MenuItem("Tools/Menú principal/Generar escena")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        LoadArt();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas();
        CreateBackground(canvas, camera);

        var root = new GameObject("MainMenu");
        var menu = root.AddComponent<MainMenu>();

        // --- Columna de botones (izquierda) ---
        var column = CreateColumn(canvas, "MainPanel", ButtonSize.x, 22f);
        column.anchorMin = column.anchorMax = new Vector2(0f, 0.5f);
        column.anchoredPosition = new Vector2(ColumnCenterX, 40f);
        var title = CreateText(column, "Title", PlayerSettings.productName, 130, CreamColor, titleFont);
        title.fontSharedMaterial = titleMaterial;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        CreateDivider(column, 520f);
        CreateSpacer(column, 20f);
        var playButton = CreateMenuButton(column, "PlayButton", "Jugar");
        var controlsButton = CreateMenuButton(column, "ControlsButton", "Controles");
        var settingsButton = CreateMenuButton(column, "SettingsButton", "Configuración");
        var exitButton = CreateMenuButton(column, "ExitButton", "Salir");

        var backButton = CreateBackButton(canvas);

        // --- Configuración (pergamino desplegable) ---
        var settingsPanel = CreateParchmentPanel(canvas, "SettingsPanel", "Configuración", out var settingsContent);

        var tabsRow = CreateRow(settingsContent, "Tabs", 28f, TextAnchor.MiddleLeft);
        tabsRow.gameObject.AddComponent<LayoutElement>().minHeight = TabSize.y;
        var audioTab = CreateTabButton(tabsRow, "AudioTab", "Audio", "volume");
        var graphicsTab = CreateTabButton(tabsRow, "GraphicsTab", "Gráficos", "monitor");

        var pages = CreateRect(settingsContent, "Pages");
        pages.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var audioPage = CreatePage(pages, "AudioPage");
        var graphicsPage = CreatePage(pages, "GraphicsPage");

        // Audio
        var masterRow = CreateVolumeRow(audioPage, "MasterVolume", "Volumen general", "volume");
        var musicRow = CreateVolumeRow(audioPage, "MusicVolume", "Música", "music");
        var sfxRow = CreateVolumeRow(audioPage, "SfxVolume", "Efectos de sonido", "leaf");
        var voiceRow = CreateVolumeRow(audioPage, "VoiceVolume", "Voces", "communication");

        var settingsMenu = audioPage.gameObject.AddComponent<SettingsMenu>();
        var settingsSo = new SerializedObject(settingsMenu);
        var rows = settingsSo.FindProperty("volumeRows");
        rows.arraySize = 4;
        SetVolumeRow(rows.GetArrayElementAtIndex(0), AudioChannel.Master, masterRow);
        SetVolumeRow(rows.GetArrayElementAtIndex(1), AudioChannel.Music, musicRow);
        SetVolumeRow(rows.GetArrayElementAtIndex(2), AudioChannel.Sfx, sfxRow);
        SetVolumeRow(rows.GetArrayElementAtIndex(3), AudioChannel.Voice, voiceRow);
        settingsSo.ApplyModifiedPropertiesWithoutUndo();

        // Gráficos
        var graphicsMenu = graphicsPage.gameObject.AddComponent<GraphicsSettingsMenu>();
        var graphicsSo = new SerializedObject(graphicsMenu);
        graphicsSo.FindProperty("displayMode").objectReferenceValue = CreateOptionRow(graphicsPage, "DisplayMode", "Modo de pantalla");
        graphicsSo.FindProperty("resolution").objectReferenceValue = CreateOptionRow(graphicsPage, "Resolution", "Resolución");
        graphicsSo.FindProperty("quality").objectReferenceValue = CreateOptionRow(graphicsPage, "Quality", "Calidad");
        graphicsSo.FindProperty("vSync").objectReferenceValue = CreateOptionRow(graphicsPage, "VSync", "Sincronización vertical");
        graphicsSo.FindProperty("frameRate").objectReferenceValue = CreateOptionRow(graphicsPage, "FrameRate", "Límite de FPS");
        graphicsSo.ApplyModifiedPropertiesWithoutUndo();

        var tabs = settingsContent.gameObject.AddComponent<SettingsTabs>();
        var tabsSo = new SerializedObject(tabs);
        var tabList = tabsSo.FindProperty("tabs");
        tabList.arraySize = 2;
        SetTab(tabList.GetArrayElementAtIndex(0), audioTab, audioPage.gameObject);
        SetTab(tabList.GetArrayElementAtIndex(1), graphicsTab, graphicsPage.gameObject);
        tabsSo.ApplyModifiedPropertiesWithoutUndo();

        // --- Controles (pergamino desplegable) ---
        var controlsPanel = CreateParchmentPanel(canvas, "ControlsPanel", "Controles", out var controlsContent);
        var controlsList = CreateText(controlsContent, "ControlsList", "", 36, InkColor, regularFont, TextAlignmentOptions.TopLeft);
        controlsList.lineSpacing = 0f;

        var controlsMenu = controlsContent.gameObject.AddComponent<ControlsMenu>();
        var controlsSo = new SerializedObject(controlsMenu);
        controlsSo.FindProperty("actions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        controlsSo.FindProperty("listText").objectReferenceValue = controlsList;
        controlsSo.ApplyModifiedPropertiesWithoutUndo();

        // --- Referencias del MainMenu ---
        var menuSo = new SerializedObject(menu);
        menuSo.FindProperty("gameSceneName").stringValue = Path.GetFileNameWithoutExtension(GameScenePath);
        menuSo.FindProperty("playButton").objectReferenceValue = playButton;
        menuSo.FindProperty("controlsButton").objectReferenceValue = controlsButton;
        menuSo.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        menuSo.FindProperty("exitButton").objectReferenceValue = exitButton;
        menuSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        menuSo.FindProperty("controlsPanel").objectReferenceValue = controlsPanel;
        menuSo.FindProperty("settingsFirstSelected").objectReferenceValue = audioTab;
        menuSo.FindProperty("controlsFirstSelected").objectReferenceValue = backButton;
        menuSo.FindProperty("backButton").objectReferenceValue = backButton;
        menuSo.ApplyModifiedPropertiesWithoutUndo();

        graphicsPage.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        controlsPanel.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScenesToBuild();
        Debug.Log($"Escena del menú generada en {ScenePath}");
    }

    // ---------- Recursos ----------

    static void LoadArt()
    {
        buttonActive = ImportSprite(ArtPath + "MenuButtonActive.png", FrameBorder);
        buttonInactive = ImportSprite(ArtPath + "MenuButtonInactive.png", FrameBorder);
        leaf = ImportSprite(ArtPath + "Hojita.png", Vector4.zero);
        ornamental = ImportSprite(ArtPath + "Ornamental.png", Vector4.zero, 1024);
        parchment = ImportSprite(ArtPath + "FondoConfiguracion.png", Vector4.zero);
        background = ImportSprite(BackgroundPath, Vector4.zero);
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        foreach (var name in new[] { "volume", "music", "leaf", "communication", "monitor" })
        {
            ImportSprite(IconPath + name + ".png", Vector4.zero, 256, mipmaps: true);
            ImportSprite(IconPath + name + "_white.png", Vector4.zero, 256, mipmaps: true);
        }

        regularFont = GetOrCreateFontAsset(BodyFontFolder + "Alegreya-Regular.ttf");
        boldFont = GetOrCreateFontAsset(BodyFontFolder + "Alegreya-Bold.ttf");
        titleFont = GetOrCreateFontAsset(TitleFontFolder + "Cinzel-Bold.ttf");
        titleMaterial = GetOrCreateTitleMaterial(titleFont);
        shineMaterial = GetOrCreateShineMaterial();
    }

    static Sprite Icon(string name, bool white) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(IconPath + name + (white ? "_white" : "") + ".png");

    static Sprite ImportSprite(string path, Vector4 border, int maxSize = 2048, bool mipmaps = false)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border
            || importer.mipmapEnabled != mipmaps || importer.maxTextureSize != maxSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            // Los iconos son grandes (512-1254 px) y se muestran pequeños: con mipmaps no se ven dentados.
            importer.mipmapEnabled = mipmaps;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static TMP_FontAsset GetOrCreateFontAsset(string ttfPath)
    {
        var assetPath = ttfPath.Replace(".ttf", " SDF.asset");
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        // Mismo camino que Assets > Create > TextMeshPro > Font Asset > SDF (font asset dinámico).
        var previousSelection = Selection.objects;
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF");
        Selection.objects = previousSelection;

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
    }

    static Material GetOrCreateTitleMaterial(TMP_FontAsset font)
    {
        var path = TitleFontFolder + "Cinzel-Bold SDF - Title.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetFloat("_OutlineWidth", 0.18f);
        material.SetColor("_OutlineColor", new Color32(58, 30, 14, 255));
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0.12f, 0.05f, 0.02f, 0.7f));
        material.SetFloat("_UnderlayOffsetX", 0.6f);
        material.SetFloat("_UnderlayOffsetY", -0.7f);
        material.SetFloat("_UnderlaySoftness", 0.4f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material GetOrCreateShineMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ShineMaterialPath);
        if (material != null)
            return material;

        material = new Material(Shader.Find(ShineShaderName));
        AssetDatabase.CreateAsset(material, ShineMaterialPath);
        return material;
    }

    // ---------- Escena ----------

    static Camera CreateCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
        return camera;
    }

    static void CreateEventSystem()
    {
        // InputSystemUIInputModule asigna sus acciones por defecto al activarse.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static RectTransform CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    static void CreateBackground(Transform canvas, Camera camera)
    {
        var rect = CreateRect(canvas, "Background");
        Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = background;
        image.raycastTarget = false;

        // Cubre toda la pantalla sin deformar la imagen.
        var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = background.rect.width / background.rect.height;

        // El vídeo se dibuja detrás de la UI, en la cámara; la imagen hace de respaldo mientras carga.
        rect.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        var video = rect.gameObject.AddComponent<VideoBackground>();
        var so = new SerializedObject(video);
        so.FindProperty("videoFile").stringValue = BackgroundVideo;
        so.FindProperty("targetCamera").objectReferenceValue = camera;
        so.FindProperty("poster").objectReferenceValue = image;
        so.ApplyModifiedPropertiesWithoutUndo();

        var shade = CreateRect(canvas, "Shade");
        Stretch(shade);
        var shadeImage = shade.gameObject.AddComponent<Image>();
        shadeImage.color = ShadeColor;
        shadeImage.raycastTarget = false;
    }

    /// <summary>
    /// Panel con el pergamino de fondo que se despliega hacia la derecha (PanelReveal).
    /// Devuelve el panel y, en <paramref name="content"/>, la columna donde va el contenido.
    /// </summary>
    static PanelReveal CreateParchmentPanel(Transform canvas, string name, string titleText, out RectTransform content)
    {
        var rect = CreateRect(canvas, name);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        var height = PanelWidth * parchment.rect.height / parchment.rect.width;
        rect.sizeDelta = new Vector2(PanelWidth, height);
        rect.anchoredPosition = new Vector2(PanelLeft, 20f);
        rect.gameObject.AddComponent<RectMask2D>();
        rect.gameObject.AddComponent<CanvasGroup>();
        var reveal = rect.gameObject.AddComponent<PanelReveal>();

        var paper = CreateRect(rect, "Parchment");
        Stretch(paper);
        var paperImage = paper.gameObject.AddComponent<Image>();
        paperImage.sprite = parchment;

        content = CreateRect(rect, "Content");
        Stretch(content);
        content.offsetMin = new Vector2(150f, 115f);
        content.offsetMax = new Vector2(-150f, -80f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 26f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var header = CreateRow(content, "Header", 22f, TextAnchor.MiddleCenter);
        var leafIcon = CreateIcon(header, "LeafIcon", Icon("leaf", false), 56f);
        leafIcon.color = AmberColor;
        CreateText(header, "Title", titleText, 68, InkColor, titleFont);
        CreateDivider(content, 520f);
        return reveal;
    }

    static RectTransform CreatePage(Transform parent, string name)
    {
        var page = CreateRect(parent, name);
        Stretch(page);
        var layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(10, 10, 10, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return page;
    }

    static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static RectTransform CreateColumn(Transform parent, string name, float width, float spacing)
    {
        var rect = CreateRect(parent, name);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0f);

        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    static RectTransform CreateRow(Transform parent, string name, float spacing, TextAnchor alignment)
    {
        var row = CreateRect(parent, name);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color,
        TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = CreateRect(parent, name);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Image CreateIcon(Transform parent, string name, Sprite sprite, float size)
    {
        var rect = CreateRect(parent, name);
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = size;
        element.minHeight = element.preferredHeight = size;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    static void CreateSpacer(Transform parent, float height)
    {
        var rect = CreateRect(parent, "Spacer");
        rect.gameObject.AddComponent<LayoutElement>().minHeight = height;
    }

    /// <summary>Divisor ornamental: dos lanzas doradas enfrentadas con la hojita en el centro.</summary>
    static void CreateDivider(Transform parent, float width)
    {
        var row = CreateRect(parent, "Divider");
        row.gameObject.AddComponent<LayoutElement>().minHeight = 54f;

        // El sprite de Ornamental tiene mucho margen vacío: la lanza ocupa ~90% del ancho y está centrada en vertical.
        const float spearHeight = 94f;
        var spearWidth = spearHeight * ornamental.rect.width / ornamental.rect.height;
        var gap = 44f;

        for (var side = -1; side <= 1; side += 2)
        {
            var spear = CreateRect(row, side < 0 ? "OrnamentLeft" : "OrnamentRight");
            spear.anchorMin = spear.anchorMax = new Vector2(0.5f, 0.5f);
            spear.pivot = new Vector2(1f, 0.5f);
            var length = Mathf.Min(spearWidth, width * 0.5f - gap);
            spear.sizeDelta = new Vector2(length, spearHeight * length / spearWidth);
            spear.anchoredPosition = new Vector2(side * gap, 0f);
            // La punta del sprite mira a la izquierda; el lado derecho se refleja para que apunte hacia fuera.
            spear.localScale = new Vector3(side < 0 ? 1f : -1f, 1f, 1f);
            var image = spear.gameObject.AddComponent<Image>();
            image.sprite = ornamental;
            image.raycastTarget = false;
        }

        var leafRect = CreateRect(row, "Leaf");
        leafRect.anchorMin = leafRect.anchorMax = new Vector2(0.5f, 0.5f);
        leafRect.sizeDelta = new Vector2(61f, 41f);
        var leafImage = leafRect.gameObject.AddComponent<Image>();
        leafImage.sprite = leaf;
        leafImage.raycastTarget = false;
    }

    // ---------- Botones ----------

    /// <summary>
    /// Base común de los botones con dos estados: área de clic invisible, marcos inactivo/activo
    /// (el activo con el shader de brillo) y MenuButtonVisual.
    /// </summary>
    static (Button button, MenuButtonVisual visual, RectTransform rect) CreateFramedButton(
        Transform parent, string name, Vector2 size)
    {
        var rect = CreateRect(parent, name);
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = size.x;
        element.minHeight = element.preferredHeight = size.y;
        rect.sizeDelta = size;

        var hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;

        var button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitArea;

        // Escala los bordes del 9-slice al alto del botón (el sprite mide 175 de alto).
        var borderScale = ButtonSize.y / size.y;
        var inactive = CreateFrame(rect, "InactiveFrame", buttonInactive, borderScale);
        var active = CreateFrame(rect, "ActiveFrame", buttonActive, borderScale);
        active.material = shineMaterial;

        var visual = rect.gameObject.AddComponent<MenuButtonVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("inactiveFrame").objectReferenceValue = inactive;
        so.FindProperty("activeFrame").objectReferenceValue = active;
        so.ApplyModifiedPropertiesWithoutUndo();
        return (button, visual, rect);
    }

    static Button CreateMenuButton(Transform parent, string name, string label)
    {
        // Contenedor que ocupa todo el ancho y centra el botón a su tamaño fijo.
        var slot = CreateRow(parent, name + "Slot", 0f, TextAnchor.MiddleCenter);
        var (button, visual, rect) = CreateFramedButton(slot, name, ButtonSize);

        // El texto deja libre la zona derecha donde va la hojita.
        var text = CreateButtonLabel(rect, label, 50f, new Vector2(80f, 0f), new Vector2(-140f, 0f));

        var iconRect = CreateRect(rect, "Leaf");
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.sizeDelta = new Vector2(61f, 41f);
        iconRect.anchoredPosition = new Vector2(-100f, 0f);
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = leaf;
        icon.raycastTarget = false;

        var so = new SerializedObject(visual);
        so.FindProperty("label").objectReferenceValue = text;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    /// <summary>Pestaña: los dos estados del botón, sin hoja, con el icono blanco (inactivo) y negro (activo).</summary>
    static Button CreateTabButton(Transform parent, string name, string label, string iconName)
    {
        var (button, visual, rect) = CreateFramedButton(parent, name, TabSize);

        Image Icon(string iconObjectName, bool white)
        {
            var iconRect = CreateRect(rect, iconObjectName);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            iconRect.anchoredPosition = new Vector2(88f, 0f);
            var image = iconRect.gameObject.AddComponent<Image>();
            image.sprite = MainMenuSceneBuilder.Icon(iconName, white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        var inactiveIcon = Icon("IconInactive", true);
        var activeIcon = Icon("IconActive", false);
        activeIcon.color = DarkBrownColor;

        var text = CreateButtonLabel(rect, label, 40f, new Vector2(130f, 0f), new Vector2(-40f, 0f));
        text.alignment = TextAlignmentOptions.Left;

        var so = new SerializedObject(visual);
        so.FindProperty("label").objectReferenceValue = text;
        so.FindProperty("inactiveIcon").objectReferenceValue = inactiveIcon;
        so.FindProperty("activeIcon").objectReferenceValue = activeIcon;
        so.FindProperty("activeScale").floatValue = 1.03f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    static Button CreateBackButton(Transform canvas)
    {
        var (button, visual, rect) = CreateFramedButton(canvas, "BackButton", BackButtonSize);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(ColumnCenterX, 110f);

        var text = CreateButtonLabel(rect, "‹  Volver", 44f, new Vector2(40f, 0f), new Vector2(-40f, 0f));

        var so = new SerializedObject(visual);
        so.FindProperty("label").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    static TextMeshProUGUI CreateButtonLabel(RectTransform button, string label, float maxSize, Vector2 offsetMin, Vector2 offsetMax)
    {
        var text = CreateText(button, "Label", label, maxSize, CreamColor, boldFont);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = offsetMin;
        text.rectTransform.offsetMax = offsetMax;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 30f;
        text.fontSizeMax = maxSize;
        return text;
    }

    static Image CreateFrame(Transform parent, string name, Sprite sprite, float borderScale)
    {
        var rect = CreateRect(parent, name);
        Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = borderScale;
        image.raycastTarget = false;
        return image;
    }

    static void SetTab(SerializedProperty element, Button button, GameObject page)
    {
        element.FindPropertyRelative("button").objectReferenceValue = button;
        element.FindPropertyRelative("page").objectReferenceValue = page;
    }

    // ---------- Filas de ajustes ----------

    struct VolumeRowParts
    {
        public Slider slider;
        public TextMeshProUGUI valueLabel;
    }

    static VolumeRowParts CreateVolumeRow(Transform parent, string name, string label, string iconName)
    {
        var row = CreateRow(parent, name, 30f, TextAnchor.MiddleLeft);
        row.gameObject.AddComponent<LayoutElement>().minHeight = 84f;

        var icon = CreateIcon(row, "Icon", Icon(iconName, false), 56f);
        icon.color = InkSoftColor;

        var labelText = CreateText(row, "Label", label, 44, InkColor, regularFont, TextAlignmentOptions.Left);
        labelText.gameObject.AddComponent<LayoutElement>().minWidth = 380f;

        var slider = CreateSlider(row);
        var sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minHeight = 56f;

        var valueText = CreateText(row, "Value", "100%", 42, InkSoftColor, regularFont, TextAlignmentOptions.Right);
        valueText.gameObject.AddComponent<LayoutElement>().minWidth = 130f;

        return new VolumeRowParts { slider = slider, valueLabel = valueText };
    }

    static Slider CreateSlider(Transform parent)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources
        {
            standard = roundedSprite,
            background = roundedSprite,
            knob = knobSprite,
        });
        go.name = "Slider";
        go.transform.SetParent(parent, false);
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("UI");

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var track = go.transform.Find("Background").GetComponent<Image>();
        track.color = TrackColor;
        track.rectTransform.anchorMin = new Vector2(0f, 0.38f);
        track.rectTransform.anchorMax = new Vector2(1f, 0.62f);

        var fillArea = (RectTransform)slider.fillRect.parent;
        fillArea.anchorMin = new Vector2(0f, 0.38f);
        fillArea.anchorMax = new Vector2(1f, 0.62f);
        slider.fillRect.GetComponent<Image>().color = AmberColor;

        var handle = slider.handleRect.GetComponent<Image>();
        handle.color = Color.white;
        slider.handleRect.sizeDelta = new Vector2(44f, 0f);
        ((RectTransform)slider.handleRect.parent).offsetMin = new Vector2(22f, 4f);
        ((RectTransform)slider.handleRect.parent).offsetMax = new Vector2(-22f, -4f);

        // El tinte de selección se aplica al tirador.
        var colors = slider.colors;
        colors.normalColor = new Color32(250, 240, 220, 255);
        colors.highlightedColor = new Color32(255, 214, 140, 255);
        colors.selectedColor = new Color32(255, 214, 140, 255);
        colors.pressedColor = AmberColor;
        slider.colors = colors;
        return slider;
    }

    static void SetVolumeRow(SerializedProperty element, AudioChannel channel, VolumeRowParts parts)
    {
        element.FindPropertyRelative("channel").enumValueIndex = (int)channel;
        element.FindPropertyRelative("slider").objectReferenceValue = parts.slider;
        element.FindPropertyRelative("valueLabel").objectReferenceValue = parts.valueLabel;
    }

    /// <summary>Fila "Etiqueta    ‹ valor ›" para los ajustes de gráficos.</summary>
    static OptionSelector CreateOptionRow(Transform parent, string name, string label)
    {
        var row = CreateRow(parent, name, 30f, TextAnchor.MiddleLeft);
        row.gameObject.AddComponent<LayoutElement>().minHeight = 64f;

        var labelText = CreateText(row, "Label", label, 44, InkColor, regularFont, TextAlignmentOptions.Left);
        labelText.gameObject.AddComponent<LayoutElement>().minWidth = 520f;

        var selectorRect = CreateRect(row, "Selector");
        var element = selectorRect.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.minHeight = 60f;

        var background = selectorRect.gameObject.AddComponent<Image>();
        background.sprite = roundedSprite;
        background.type = Image.Type.Sliced;
        background.color = Color.white;

        var selector = selectorRect.gameObject.AddComponent<OptionSelector>();
        selector.targetGraphic = background;
        var colors = selector.colors;
        colors.normalColor = new Color(0.35f, 0.22f, 0.12f, 0.10f);
        colors.highlightedColor = new Color(0.35f, 0.22f, 0.12f, 0.22f);
        colors.selectedColor = new Color(0.35f, 0.22f, 0.12f, 0.22f);
        colors.pressedColor = new Color(0.35f, 0.22f, 0.12f, 0.30f);
        colors.fadeDuration = 0.08f;
        selector.colors = colors;

        var previous = CreateArrowButton(selectorRect, "Previous", "‹", 0f);
        var next = CreateArrowButton(selectorRect, "Next", "›", 1f);

        var value = CreateText(selectorRect, "Value", "", 42, InkColor, boldFont);
        Stretch(value.rectTransform);
        value.rectTransform.offsetMin = new Vector2(90f, 0f);
        value.rectTransform.offsetMax = new Vector2(-90f, 0f);

        var so = new SerializedObject(selector);
        so.FindProperty("valueLabel").objectReferenceValue = value;
        so.FindProperty("previousButton").objectReferenceValue = previous;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.ApplyModifiedPropertiesWithoutUndo();
        return selector;
    }

    static Button CreateArrowButton(RectTransform parent, string name, string glyph, float side)
    {
        var rect = CreateRect(parent, name);
        rect.anchorMin = new Vector2(side, 0f);
        rect.anchorMax = new Vector2(side, 1f);
        rect.pivot = new Vector2(side, 0.5f);
        rect.sizeDelta = new Vector2(90f, 0f);

        var hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        // Las flechas no reciben foco: con teclado/mando se usa izquierda/derecha sobre el selector.
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var text = CreateText(rect, "Glyph", glyph, 64, AmberColor, boldFont);
        Stretch(text.rectTransform);
        var buttonColors = button.colors;
        buttonColors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        button.colors = buttonColors;
        return button;
    }

    static void AddScenesToBuild()
    {
        var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GameScenePath);
        EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            }
            .Concat(others)
            .ToArray();
    }
}
