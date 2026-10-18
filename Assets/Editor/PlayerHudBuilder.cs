using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Genera el prefab PlayerHUD (Canvas propio, se arrastra a cualquier escena) y una escena de prueba
/// con una cámara orbital. Se puede volver a ejecutar: reemplaza el prefab y la escena.
/// </summary>
static class PlayerHudBuilder
{
    const string PrefabPath = "Assets/Prefabs/UI/PlayerHUD.prefab";
    const string HudMessagePrefabPath = "Assets/Prefabs/UI/HudMessage.prefab";
    const string ConnectDotsPrefabPath = "Assets/Prefabs/UI/ConnectDotsMinigame.prefab";
    const string MemoryPairsPrefabPath = "Assets/Prefabs/UI/MemoryPairsMinigame.prefab";

    // Orden de dibujo: HUD < avisos < minijuegos.
    const int HudSortingOrder = 10;
    const int MessageSortingOrder = 15;
    const int MinigameSortingOrder = 20;
    const string ScenePath = "Assets/Scenes/PlayerHUD.unity";

    const string ArtPath = "Assets/Art/UI/HUD/";
    const string QuestIconPath = ArtPath + "Quests/";
    const string PortraitPath = ArtPath + "Portrait/";
    const string PortraitShaderName = "UI/Portrait Mood Fill";
    const string PortraitMaterialPath = "Assets/Art/Shaders/PortraitMoodFill.mat";
    const string DiaryBackgroundPath = "Assets/Art/UI/MainMenu/FondoConfiguracion.png";
    const string TitleFontPath = "Assets/Art/Fonts/Cinzel/Cinzel-Bold SDF.asset";
    const string BodyFontPath = "Assets/Art/Fonts/Alegreya/Alegreya-Regular SDF.asset";
    const string BodyBoldFontPath = "Assets/Art/Fonts/Alegreya/Alegreya-Bold SDF.asset";
    const string SampleQuestsPath = "Assets/Data/Quests/IslaDePrueba.asset";
    const string LeafCollectablePath = "Assets/Data/Collectables/Hoja.asset";

    // Diseño a 2K, igual que el menú principal.
    static readonly Vector2 ReferenceResolution = new Vector2(2560f, 1440f);

    // Widget de girar, arriba a la derecha. El resto escala con la brújula (IslandRotateLayout).
    const float CompassSize = 250f;
    static readonly Vector2 CornerMargin = new Vector2(60f, 50f);

    // Panel de encargos (arriba a la izquierda), botón y pergamino del diario.
    const float QuestPanelWidth = 460f;
    const float DiaryButtonSize = 150f;
    const float DiaryWidth = 1300f;
    const float NotificationWidth = 430f;

    // Retrato de la oveja, abajo a la izquierda. Proporciones respecto al marco de 1254 px.
    const float PortraitSize = 260f;
    const float PortraitFillRatio = 0.8f;    // El aro va de ~0.74 a ~0.84: ambos bordes quedan debajo.
    const float PortraitMaskRatio = 0.78f;
    const float PortraitZoom = 1.1f;         // Algo de zoom a la cara, dejando ver el relleno alrededor.
    const string CircleMaskPath = PortraitPath + "CircleMask.png";

    // Diálogo de la oveja, abajo al centro. Botones: los del menú principal (520x175, 9-slice).
    const string DialogPath = ArtPath + "Dialog/";
    const string MenuArtPath = "Assets/Art/UI/MainMenu/";
    const string MenuShineMaterialPath = "Assets/Art/Shaders/MenuButtonShine.mat";
    const float MenuButtonSpriteHeight = 175f;
    const float DialogPortraitSize = 320f;
    const float DialogBubbleWidth = 1150f;
    const float DialogBubbleOverlap = 10f;  // La cola del bocadillo toca el marco del retrato.
    const float DialogButtonGap = 10f;
    const float DialogBottomMargin = 50f;
    // Escala de todo el diálogo (retrato, bocadillo, textos y botones) para que no tape tanto.
    const float DialogScale = 0.78f;
    static readonly Vector2 DialogButtonSize = new Vector2(360f, 121f);

    // Sonidos de interfaz ("guid|ruta"), los mismos que usa el menú principal.
    const string HoverEvent = "53b09ab4-2495-47cf-a353-a0d77118469e|event:/UI/Play_ui_button_hover";
    const string DiaryOpenEvent = "5e7131c8-e60c-48bb-a974-5e0e63098b8a|event:/UI/Play_ui_button_open";
    const string DiaryCloseEvent = "de5ea2e4-e387-4144-9b13-797275da5bbc|event:/UI/Play_ui_button_close";

    // Eventos de FMOD del diálogo ("guid|ruta"). En el proyecto el del Sí se llama "buttom".
    const string DialogOpenEvent = "0cae445c-797d-4df7-b33c-01029ae10bdd|event:/DX/Witch/Play_dx_witch";
    static readonly string[] DialogOptionEvents =
    {
        "27f611cc-2f0c-4adb-9b29-55ef8da0737d|event:/UI/Play_ui_buttom_yes",
        "a6684730-199f-4b74-935c-ada813aa0e8b|event:/UI/Play_ui_button_no",
    };

    // Aviso de "no puedes todavía" (barra de pergamino arriba al centro) y ventanas de minijuego.
    const float MessageWidth = 860f;
    const float MinigameWindowWidth = 1400f;
    const float DotNodeSize = 140f;
    static readonly Vector2 MinigameButtonSize = new Vector2(320f, 108f);
    static readonly Vector2 CardSize = new Vector2(205f, 179f);   // Proporción del marco de 1341x1173.
    static readonly Color BlockerColor = new Color(0.04f, 0.02f, 0.06f, 0.5f);

    static readonly Color InkColor = new Color32(59, 36, 20, 255);
    static readonly Color InkSoftColor = new Color32(104, 68, 40, 255);
    static readonly Color IdleTint = new Color32(228, 228, 228, 255);
    static readonly Color BackdropColor = new Color(0.04f, 0.02f, 0.06f, 0.55f);

    static Sprite compass;
    static Sprite plaque;
    static Sprite mouseRightClick;
    static Sprite questBackground;
    static Sprite check;
    static Sprite diaryButton;
    static Sprite diaryButtonHover;
    static Sprite diaryBackground;
    static Sprite portraitFrame;
    static Sprite sheepHappy;
    static Sprite sheepSly;
    static Sprite sheepAngry;
    static Material portraitMaterial;
    static Sprite notificationBar;
    static Sprite cardFrame;
    static Sprite circleSprite;
    static Sprite dialogBubble;
    static Sprite dialogPortrait;
    static Sprite menuButtonActive;
    static Sprite menuButtonInactive;
    static Material menuShineMaterial;
    static TMP_FontAsset titleFont;
    static TMP_FontAsset bodyFont;
    static TMP_FontAsset bodyBoldFont;

    [MenuItem("Tools/HUD/Generar prefab y escena")]
    public static void Build()
    {
        // La escena de prueba se regenera entera: solo se pregunta si hay otra escena abierta con cambios.
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != ScenePath && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        LoadArt();

        // NewScene descarga los objetos sin usar: los assets se cargan después.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sampleQuests = GetOrCreateSampleQuests();
        var camera = CreateTestEnvironment();
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Cada isla lleva su QuestLog con su asset de encargos; el HUD lo busca solo.
        var questLog = new GameObject("QuestLog").AddComponent<QuestLog>();
        var questLogSo = new SerializedObject(questLog);
        questLogSo.FindProperty("quests").objectReferenceValue = sampleQuests;
        questLogSo.ApplyModifiedPropertiesWithoutUndo();

        var hud = CreateHud();
        EnsureFolder("Assets/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(hud, PrefabPath, InteractionMode.AutomatedAction);

        // Pantallas sueltas: cada escena añade solo las que usa (los mecanismos las referencian).
        PrefabUtility.SaveAsPrefabAssetAndConnect(CreateHudMessage(), HudMessagePrefabPath, InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(CreateConnectDots(), ConnectDotsPrefabPath, InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(CreateMemoryPairs(), MemoryPairsPrefabPath, InteractionMode.AutomatedAction);

        Selection.activeGameObject = camera.gameObject;
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"HUD generado en {PrefabPath} y escena de prueba en {ScenePath}");
    }

    // ---------- Recursos ----------

    static void LoadArt()
    {
        // Las imágenes miden 1254-2172 px y se ven pequeñas: con mipmaps no se ven dentadas.
        compass = ImportSprite(ArtPath + "MinimapaGirar.png");
        plaque = ImportSprite(ArtPath + "BarraPrompt.png");
        mouseRightClick = ImportSprite(ArtPath + "MouseClicDerecho.png");
        questBackground = ImportSprite(ArtPath + "FondoTareas.png");
        check = ImportSprite(ArtPath + "Check.png");
        diaryButton = ImportSprite(ArtPath + "BotonDiario.png");
        diaryButtonHover = ImportSprite(ArtPath + "BotonDiarioHover.png");
        foreach (var icon in new[] { "IconoHoja", "IconoCerca", "IconoRegadera" })
            ImportSprite(QuestIconPath + icon + ".png");
        portraitFrame = ImportSprite(PortraitPath + "MarcoCircular.png");
        sheepHappy = ImportSprite(PortraitPath + "OvejaFeliz.png");
        sheepSly = ImportSprite(PortraitPath + "OvejaAstuta.png");
        sheepAngry = ImportSprite(PortraitPath + "OvejaEnfurecida.png");
        portraitMaterial = GetOrCreateMaterial(PortraitMaterialPath, PortraitShaderName);
        notificationBar = ImportSprite(ArtPath + "BarraNotificacion.png");
        cardFrame = ImportSprite(ArtPath + "Minigames/MarcoCarta.png");
        circleSprite = GetOrCreateCircleSprite();
        dialogBubble = ImportSprite(DialogPath + "Bocadillo.png");
        dialogPortrait = ImportSprite(DialogPath + "RetratoOvejaAstuta.png");
        // Importados (con su 9-slice) por el builder del menú principal.
        menuButtonActive = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArtPath + "MenuButtonActive.png");
        menuButtonInactive = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArtPath + "MenuButtonInactive.png");
        menuShineMaterial = AssetDatabase.LoadAssetAtPath<Material>(MenuShineMaterialPath);
        // Ya importado por el menú principal (sin mipmaps): se carga tal cual.
        diaryBackground = AssetDatabase.LoadAssetAtPath<Sprite>(DiaryBackgroundPath);

        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
        bodyBoldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyBoldFontPath);
    }

    /// <summary>
    /// Encargos de ejemplo para la escena de prueba. Solo se crean si no existen, para no pisar
    /// lo que se haya editado a mano.
    /// </summary>
    static IslandQuests GetOrCreateSampleQuests()
    {
        var existing = AssetDatabase.LoadAssetAtPath<IslandQuests>(SampleQuestsPath);
        if (existing != null)
            return existing;

        EnsureFolder(System.IO.Path.GetDirectoryName(SampleQuestsPath).Replace('\\', '/'));
        var asset = ScriptableObject.CreateInstance<IslandQuests>();
        AssetDatabase.CreateAsset(asset, SampleQuestsPath);

        var so = new SerializedObject(asset);
        so.FindProperty("islandName").stringValue = "Isla de prueba";
        var quests = so.FindProperty("quests");
        quests.arraySize = 3;
        SetQuest(quests.GetArrayElementAtIndex(0), "recoger_hojas", "Recoger hojas",
            "El viento ha esparcido las hojas mágicas por la isla. Recógelas antes de que se las lleve.",
            "IconoHoja", 5, AssetDatabase.LoadAssetAtPath<CollectableDefinition>(LeafCollectablePath));
        SetQuest(quests.GetArrayElementAtIndex(1), "reparar_cerca", "Reparar la cerca",
            "La cerca del huerto está rota y las ovejas se escapan. Busca la forma de arreglarla.",
            "IconoCerca", 1, null);
        SetQuest(quests.GetArrayElementAtIndex(2), "regar_flores", "Regar las flores",
            "Las flores del jardín llevan días sin agua. Encuentra la regadera.",
            "IconoRegadera", 1, null);
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<IslandQuests>(SampleQuestsPath);
    }

    static void SetQuest(SerializedProperty quest, string id, string title, string description,
        string iconName, int goal, CollectableDefinition collectable)
    {
        quest.FindPropertyRelative("id").stringValue = id;
        quest.FindPropertyRelative("title").stringValue = title;
        quest.FindPropertyRelative("description").stringValue = description;
        quest.FindPropertyRelative("icon").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>(QuestIconPath + iconName + ".png");
        quest.FindPropertyRelative("goal").intValue = goal;
        quest.FindPropertyRelative("collectable").objectReferenceValue = collectable;
    }

    static Sprite ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || !importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material GetOrCreateMaterial(string path, string shaderName)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(Shader.Find(shaderName));
        AssetDatabase.CreateAsset(material, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    /// <summary>Círculo blanco a sangre de 512 px para la máscara (el Knob de Unity es pequeño y tiene margen).</summary>
    static Sprite GetOrCreateCircleSprite()
    {
        if (!System.IO.File.Exists(CircleMaskPath))
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude;
                var alpha = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels);
            System.IO.File.WriteAllBytes(CircleMaskPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(CircleMaskPath);
        }

        return ImportSprite(CircleMaskPath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    // ---------- Escena de prueba ----------

    /// <summary>Isla provisional y cámara orbital para probar el HUD sin cargar la escena de juego.</summary>
    static Camera CreateTestEnvironment()
    {
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var island = new GameObject("TestIsland").transform;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        ground.SetParent(island, false);
        ground.localScale = new Vector3(12f, 0.5f, 12f);
        // Un bloque descentrado para que se note el giro.
        var house = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        house.SetParent(island, false);
        house.localPosition = new Vector3(2f, 1.5f, 1f);
        house.localScale = new Vector3(3f, 2.5f, 2f);

        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(36, 26, 60, 255);
        go.AddComponent<AudioListener>();

        var orbit = go.AddComponent<OrbitCamera>();
        var so = new SerializedObject(orbit);
        so.FindProperty("target").objectReferenceValue = island;
        so.ApplyModifiedPropertiesWithoutUndo();
        return camera;
    }

    // ---------- HUD ----------

    /// <summary>Canvas a pantalla completa con la misma escala 2K que el resto de la UI.</summary>
    static GameObject CreateOverlayCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    static GameObject CreateHud()
    {
        var go = CreateOverlayCanvas("PlayerHUD", HudSortingOrder);

        CreateRotateWidget(go.transform);
        CreateQuestPanel(go.transform);
        var portrait = CreateSheepPortrait(go.transform);
        CreateItemNotification(go.transform);
        CreateDiary(go.transform);
        // El diálogo va el último para quedar por encima de todo.
        var dialogFill = CreateDialog(go.transform);

        var portraitSo = new SerializedObject(portrait);
        var linked = portraitSo.FindProperty("linkedFills");
        linked.arraySize = 1;
        linked.GetArrayElementAtIndex(0).objectReferenceValue = dialogFill;
        portraitSo.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // ---------- Aviso de objeto recogido ----------

    /// <summary>
    /// Barra a la izquierda, centrada en vertical, con el icono y el nombre del objeto en blanco.
    /// ItemNotification la mete y la saca deslizando desde fuera de la pantalla.
    /// </summary>
    static void CreateItemNotification(Transform canvas)
    {
        // El componente va en un contenedor siempre activo; la barra se activa solo al avisar.
        var holder = CreateRect(canvas, "ItemNotification");
        Stretch(holder);

        var height = NotificationWidth * notificationBar.rect.height / notificationBar.rect.width;
        var bar = CreateRect(holder, "Bar");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 0.5f);
        bar.sizeDelta = new Vector2(NotificationWidth, height);
        bar.anchoredPosition = new Vector2(CornerMargin.x, 0f);
        var group = bar.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        var barImage = bar.gameObject.AddComponent<Image>();
        barImage.sprite = notificationBar;
        barImage.raycastTarget = false;

        // Zona oscura del sprite (2172x724), sin el marco ni las hojas de los lados.
        var content = CreateRect(bar, "Content");
        SetAnchors(content, new Vector2(0.11f, 0.31f), new Vector2(0.89f, 0.75f));
        var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var bandHeight = height * 0.44f;
        var icon = CreateIcon(content, "Icon", null, bandHeight * 0.95f);

        var label = CreateText(content, "Label", "Objeto", bandHeight * 0.5f, Color.white, bodyBoldFont,
            TextAlignmentOptions.Left);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var notification = holder.gameObject.AddComponent<ItemNotification>();
        var so = new SerializedObject(notification);
        so.FindProperty("bar").objectReferenceValue = bar;
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("shownX").floatValue = CornerMargin.x;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Diálogo de la oveja ----------

    /// <summary>
    /// Cuadro de diálogo abajo al centro: retrato de la oveja, bocadillo con el texto y los botones
    /// del menú principal debajo. Bloquea los clics al mundo mientras está abierto.
    /// Devuelve el relleno del retrato para enlazarlo con el clima de SheepPortrait.
    /// </summary>
    static Graphic CreateDialog(Transform canvas)
    {
        var holder = CreateRect(canvas, "DialogBox");
        Stretch(holder);
        var root = CreateRect(holder, "Root");
        Stretch(root);

        var blockerImage = root.gameObject.AddComponent<Image>();
        blockerImage.color = Color.clear;
        var blocker = root.gameObject.AddComponent<Button>();
        blocker.transition = Selectable.Transition.None;
        blocker.navigation = new Navigation { mode = Navigation.Mode.None };

        var bubbleHeight = DialogBubbleWidth * dialogBubble.rect.height / dialogBubble.rect.width;
        var groupWidth = DialogPortraitSize + DialogBubbleWidth - DialogBubbleOverlap;
        var groupHeight = bubbleHeight + DialogButtonGap + DialogButtonSize.y;

        var group = CreateRect(root, "Group");
        group.anchorMin = group.anchorMax = group.pivot = new Vector2(0.5f, 0f);
        group.sizeDelta = new Vector2(groupWidth, groupHeight);
        group.anchoredPosition = new Vector2(0f, DialogBottomMargin);
        group.localScale = new Vector3(DialogScale, DialogScale, 1f);

        // Retrato centrado en la altura del bocadillo; el relleno del clima va detrás del marco.
        var portrait = CreateRect(group, "Portrait");
        portrait.anchorMin = portrait.anchorMax = portrait.pivot = new Vector2(0f, 1f);
        portrait.sizeDelta = Vector2.one * DialogPortraitSize;
        portrait.anchoredPosition = new Vector2(0f, -(bubbleHeight - DialogPortraitSize) * 0.5f);

        var fillRect = CreateRect(portrait, "Fill");
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = Vector2.one * DialogPortraitSize * 0.82f;
        var fill = fillRect.gameObject.AddComponent<Image>();
        fill.material = portraitMaterial;
        fill.maskable = false;
        fill.raycastTarget = false;

        var portraitImageRect = CreateRect(portrait, "Image");
        Stretch(portraitImageRect);
        var portraitImage = portraitImageRect.gameObject.AddComponent<Image>();
        portraitImage.sprite = dialogPortrait;
        portraitImage.raycastTarget = false;

        // Bocadillo a la derecha; la cola apunta al retrato.
        var bubble = CreateRect(group, "Bubble");
        bubble.anchorMin = bubble.anchorMax = bubble.pivot = new Vector2(1f, 1f);
        bubble.sizeDelta = new Vector2(DialogBubbleWidth, bubbleHeight);
        var bubbleImage = bubble.gameObject.AddComponent<Image>();
        bubbleImage.sprite = dialogBubble;
        bubbleImage.raycastTarget = false;

        // Zona de pergamino del sprite (2172x724), sin el marco ni la cola.
        var text = CreateRect(bubble, "Text");
        SetAnchors(text, new Vector2(0.13f, 0.2f), new Vector2(0.89f, 0.8f));
        var textLayout = text.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 6f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        var speaker = CreateText(text, "Speaker", "Oveja", 32f, InkSoftColor, titleFont, TextAlignmentOptions.Left);
        var body = CreateText(text, "Body", "", 40f, InkColor, bodyFont, TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = true;
        body.fontSizeMin = 28f;
        body.fontSizeMax = 40f;
        body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        // Botones del menú principal, centrados bajo el pergamino del bocadillo.
        var buttons = CreateRect(group, "Options");
        buttons.anchorMin = new Vector2(1f - DialogBubbleWidth / groupWidth, 0f);
        buttons.anchorMax = new Vector2(1f, 0f);
        buttons.pivot = new Vector2(0.5f, 0f);
        buttons.sizeDelta = new Vector2(0f, DialogButtonSize.y);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 60f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;

        var optionButtons = new Button[2];
        var optionLabels = new TMP_Text[2];
        for (var i = 0; i < optionButtons.Length; i++)
            (optionButtons[i], optionLabels[i]) = CreateMenuStyleButton(buttons, "Option" + (i + 1), DialogButtonSize);

        var dialog = holder.gameObject.AddComponent<DialogBox>();
        var so = new SerializedObject(dialog);
        so.FindProperty("root").objectReferenceValue = root.gameObject;
        so.FindProperty("speakerText").objectReferenceValue = speaker;
        so.FindProperty("bodyText").objectReferenceValue = body;
        var buttonsProp = so.FindProperty("optionButtons");
        var labelsProp = so.FindProperty("optionLabels");
        buttonsProp.arraySize = labelsProp.arraySize = optionButtons.Length;
        for (var i = 0; i < optionButtons.Length; i++)
        {
            buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionButtons[i];
            labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionLabels[i];
        }
        so.FindProperty("portrait").objectReferenceValue = portraitImage;
        so.FindProperty("defaultPortrait").objectReferenceValue = dialogPortrait;
        so.FindProperty("dismissArea").objectReferenceValue = blocker;
        // Voz de la oveja al abrirse y sonido de cada opción (Sí, No).
        SetEventReference(so.FindProperty("openSound"), DialogOpenEvent);
        var sounds = so.FindProperty("optionSounds");
        sounds.arraySize = DialogOptionEvents.Length;
        for (var i = 0; i < DialogOptionEvents.Length; i++)
            SetEventReference(sounds.GetArrayElementAtIndex(i), DialogOptionEvents[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
        return fill;
    }

    /// <summary>Sonido al pasar el ratón, como en el menú principal: StudioEventEmitter en UIMouseEnter.</summary>
    static void AddHoverSound(GameObject button)
    {
        var emitter = button.AddComponent<FMODUnity.StudioEventEmitter>();
        var so = new SerializedObject(emitter);
        SetEventReference(so.FindProperty("EventReference"), HoverEvent);
        so.FindProperty("EventPlayTrigger").enumValueIndex = (int)FMODUnity.EmitterGameEvent.UIMouseEnter;
        so.FindProperty("EventStopTrigger").enumValueIndex = (int)FMODUnity.EmitterGameEvent.None;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Rellena un EventReference de FMOD (Guid en 4 ints + ruta) desde "guid|ruta".</summary>
    static void SetEventReference(SerializedProperty property, string guidAndPath)
    {
        var parts = guidAndPath.Split('|');
        var bytes = new System.Guid(parts[0]).ToByteArray();
        var guid = property.FindPropertyRelative("Guid");
        for (var i = 0; i < 4; i++)
            guid.FindPropertyRelative("Data" + (i + 1)).intValue = System.BitConverter.ToInt32(bytes, i * 4);
        property.FindPropertyRelative("Path").stringValue = parts[1];
    }

    /// <summary>Botón con los marcos inactivo/activo del menú principal (9-slice) y MenuButtonVisual.</summary>
    static (Button, TMP_Text) CreateMenuStyleButton(Transform parent, string name, Vector2 size)
    {
        var rect = CreateRect(parent, name);
        rect.sizeDelta = size;
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = size.x;
        element.minHeight = element.preferredHeight = size.y;

        var hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        var button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitArea;

        // Los bordes del 9-slice se escalan al alto del botón (el sprite mide 175 de alto).
        var borderScale = MenuButtonSpriteHeight / size.y;
        var inactive = CreateSlicedFrame(rect, "InactiveFrame", menuButtonInactive, borderScale);
        var active = CreateSlicedFrame(rect, "ActiveFrame", menuButtonActive, borderScale);
        active.material = menuShineMaterial;

        var maxFont = size.y * 0.36f;
        var label = CreateText(rect, "Label", "Opción", maxFont, Color.white, bodyBoldFont);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(40f, 0f);
        label.rectTransform.offsetMax = new Vector2(-40f, 0f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = maxFont * 0.65f;
        label.fontSizeMax = maxFont;

        AddHoverSound(rect.gameObject);

        var visual = rect.gameObject.AddComponent<MenuButtonVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("inactiveFrame").objectReferenceValue = inactive;
        so.FindProperty("activeFrame").objectReferenceValue = active;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        return (button, label);
    }

    // ---------- Aviso de interacción bloqueada ----------

    /// <summary>
    /// HudMessage: la barra de pergamino arriba al centro con el motivo (p. ej. "Recoge todos los
    /// objetos de la isla primero"). Prefab con su propio Canvas.
    /// </summary>
    static GameObject CreateHudMessage()
    {
        var canvas = CreateOverlayCanvas("HudMessage", MessageSortingOrder);

        var height = MessageWidth * plaque.rect.height / plaque.rect.width;
        var panel = CreateRect(canvas.transform, "Panel");
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
        panel.sizeDelta = new Vector2(MessageWidth, height);
        // La placa tiene ~24% de margen transparente arriba: se sube para que la franja quede a 40 px.
        panel.anchoredPosition = new Vector2(0f, height * 0.24f - 40f);
        var image = panel.gameObject.AddComponent<Image>();
        image.sprite = plaque;
        image.raycastTarget = false;

        // Franja de pergamino del sprite (sin el borde ni el adorno inferior).
        var text = CreateText(panel, "Text", "", 34f, InkColor, bodyBoldFont);
        SetAnchors(text.rectTransform, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.76f));
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = 34f;

        var message = canvas.AddComponent<HudMessage>();
        var so = new SerializedObject(message);
        so.FindProperty("root").objectReferenceValue = panel.gameObject;
        so.FindProperty("text").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
        return canvas;
    }

    // ---------- Minijuegos ----------

    /// <summary>
    /// Base de las ventanas de minijuego: Root (lo que Minigame activa) con un bloqueador a pantalla
    /// completa y el pergamino centrado. Devuelve la zona de contenido dentro del marco.
    /// </summary>
    static (GameObject canvas, GameObject root, RectTransform content) CreateMinigameWindow(string name, string title)
    {
        var canvas = CreateOverlayCanvas(name, MinigameSortingOrder);
        var root = CreateRect(canvas.transform, "Root");
        Stretch(root);

        // El bloqueador frena los clics al mundo mientras el minijuego está abierto.
        var blocker = CreateRect(root, "Blocker");
        Stretch(blocker);
        blocker.gameObject.AddComponent<Image>().color = BlockerColor;

        var height = MinigameWindowWidth * diaryBackground.rect.height / diaryBackground.rect.width;
        var window = CreateRect(root, "Window");
        window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(MinigameWindowWidth, height);
        window.gameObject.AddComponent<Image>().sprite = diaryBackground;

        // Mismos márgenes interiores que el pergamino del menú, a esta escala.
        var scale = MinigameWindowWidth / 1700f;
        var content = CreateRect(window, "Content");
        Stretch(content);
        content.offsetMin = new Vector2(150f, 115f) * scale;
        content.offsetMax = new Vector2(-150f, -80f) * scale;

        var titleText = CreateText(content, "Title", title, 60f, InkColor, titleFont);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = Vector2.one;
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(0f, 76f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        return (canvas, root.gameObject, content);
    }

    /// <summary>Fila inferior con Reiniciar y Cerrar (botones del menú principal) y el texto de resultado encima.</summary>
    static (Button restart, Button close, TMP_Text result) CreateMinigameFooter(RectTransform content)
    {
        var row = CreateRect(content, "Buttons");
        row.anchorMin = Vector2.zero;
        row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.sizeDelta = new Vector2(0f, MinigameButtonSize.y);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 70f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var (restart, restartLabel) = CreateMenuStyleButton(row, "RestartButton", MinigameButtonSize);
        restartLabel.text = "Reiniciar";
        var (close, closeLabel) = CreateMenuStyleButton(row, "CloseButton", MinigameButtonSize);
        closeLabel.text = "Cerrar";

        var result = CreateText(content, "Result", "", 32f, InkColor, bodyBoldFont);
        result.rectTransform.anchorMin = Vector2.zero;
        result.rectTransform.anchorMax = new Vector2(1f, 0f);
        result.rectTransform.pivot = new Vector2(0.5f, 0f);
        result.rectTransform.sizeDelta = new Vector2(0f, 44f);
        result.rectTransform.anchoredPosition = new Vector2(0f, MinigameButtonSize.y + 8f);
        return (restart, close, result);
    }

    /// <summary>
    /// Conectar los puntos: nodos A, B y C con el marco circular dorado sobre un círculo que cambia
    /// de color, guías tenues del triángulo y las líneas del jugador debajo de los nodos.
    /// </summary>
    static GameObject CreateConnectDots()
    {
        var (canvas, root, content) = CreateMinigameWindow("ConnectDotsMinigame", "Conecta los puntos");

        var hint = CreateText(content, "Hint", "", 36f, InkSoftColor, bodyFont);
        hint.fontStyle = FontStyles.Italic;
        hint.rectTransform.anchorMin = new Vector2(0f, 1f);
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.pivot = new Vector2(0.5f, 1f);
        hint.rectTransform.sizeDelta = new Vector2(0f, 50f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -80f);

        // Tablero entre la pista y el resultado.
        var board = CreateRect(content, "Board");
        board.anchorMin = Vector2.zero;
        board.anchorMax = Vector2.one;
        board.offsetMin = new Vector2(0f, MinigameButtonSize.y + 56f);
        board.offsetMax = new Vector2(0f, -136f);

        var positions = new[]
        {
            new Vector2(-260f, -70f), // A
            new Vector2(0f, 90f),     // B
            new Vector2(260f, -70f),  // C
        };

        // Guías del triángulo, tenues, para que se vea qué se puede unir.
        var guides = CreateRect(board, "Guides");
        Stretch(guides);
        for (var i = 0; i < positions.Length; i++)
            CreateGuideLine(guides, positions[i], positions[(i + 1) % positions.Length]);

        // Las líneas del jugador van antes que los nodos para quedar debajo.
        var lines = CreateRect(board, "Lines");
        Stretch(lines);

        var nodes = new Button[3];
        var letters = new[] { "A", "B", "C" };
        for (var i = 0; i < nodes.Length; i++)
            nodes[i] = CreateDotNode(board, letters[i], positions[i]);

        var (restart, close, result) = CreateMinigameFooter(content);

        var minigame = canvas.AddComponent<ConnectDotsMinigame>();
        var so = new SerializedObject(minigame);
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("linesContainer").objectReferenceValue = lines;
        var nodesProp = so.FindProperty("nodes");
        nodesProp.arraySize = nodes.Length;
        for (var i = 0; i < nodes.Length; i++)
            nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.FindProperty("resultText").objectReferenceValue = result;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("lineThickness").floatValue = 10f;
        so.FindProperty("idleColor").colorValue = new Color32(246, 236, 214, 255);
        so.FindProperty("selectedColor").colorValue = new Color32(255, 210, 120, 255);
        so.FindProperty("errorColor").colorValue = new Color32(214, 92, 76, 255);
        so.FindProperty("successColor").colorValue = new Color32(140, 200, 110, 255);
        so.FindProperty("lineColor").colorValue = new Color32(0x92, 0x70, 0xC7, 0xFF);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        return canvas;
    }

    /// <summary>Nodo: círculo que cambia de color (destino del clic) bajo el marco dorado, con su letra.</summary>
    static Button CreateDotNode(Transform board, string letter, Vector2 position)
    {
        var node = CreateRect(board, "Node" + letter);
        node.anchorMin = node.anchorMax = new Vector2(0.5f, 0.5f);
        node.sizeDelta = Vector2.one * DotNodeSize;
        node.anchoredPosition = position;

        // El aro del marco va de ~0.74 a ~0.84: el círculo de color queda debajo de su borde.
        var fillRect = CreateRect(node, "Fill");
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = Vector2.one * DotNodeSize * 0.8f;
        var fill = fillRect.gameObject.AddComponent<Image>();
        fill.sprite = circleSprite;

        var frameRect = CreateRect(node, "Frame");
        Stretch(frameRect);
        var frame = frameRect.gameObject.AddComponent<Image>();
        frame.sprite = portraitFrame;
        frame.raycastTarget = false;

        var text = CreateText(node, "Letter", letter, DotNodeSize * 0.36f, InkColor, titleFont);
        Stretch(text.rectTransform);

        var button = node.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        // El minijuego pinta el color de cada nodo: el botón no tiñe.
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        AddHoverSound(node.gameObject);
        return button;
    }

    static void CreateGuideLine(Transform parent, Vector2 from, Vector2 to)
    {
        var delta = to - from;
        var rect = CreateRect(parent, "Guide");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = from;
        rect.sizeDelta = new Vector2(delta.magnitude, 4f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(InkSoftColor.r, InkSoftColor.g, InkSoftColor.b, 0.25f);
        image.raycastTarget = false;
    }

    /// <summary>Encuentra las parejas: seis cartas en 3x2 con el marco dorado ornamental.</summary>
    static GameObject CreateMemoryPairs()
    {
        var (canvas, root, content) = CreateMinigameWindow("MemoryPairsMinigame", "Encuentra las parejas");

        var grid = CreateRect(content, "Cards");
        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = new Vector2(0f, MinigameButtonSize.y + 56f);
        grid.offsetMax = new Vector2(0f, -86f);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = CardSize;
        layout.spacing = new Vector2(34f, 14f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.MiddleCenter;

        var cards = new MemoryCardView[6];
        for (var i = 0; i < cards.Length; i++)
            cards[i] = CreateMemoryCard(grid, "Card" + (i + 1));

        var (restart, close, result) = CreateMinigameFooter(content);

        var minigame = canvas.AddComponent<MemoryPairsMinigame>();
        var so = new SerializedObject(minigame);
        so.FindProperty("root").objectReferenceValue = root;
        var cardsProp = so.FindProperty("cards");
        cardsProp.arraySize = cards.Length;
        for (var i = 0; i < cards.Length; i++)
            cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        so.FindProperty("resultText").objectReferenceValue = result;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("backColor").colorValue = new Color32(46, 92, 52, 255);
        so.FindProperty("faceColor").colorValue = new Color32(248, 238, 214, 255);
        so.FindProperty("matchedColor").colorValue = new Color32(196, 228, 160, 255);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        return canvas;
    }

    /// <summary>Carta: fondo de color en el hueco del marco, reverso y símbolo encima, y el marco dorado.</summary>
    static MemoryCardView CreateMemoryCard(Transform grid, string name)
    {
        var card = CreateRect(grid, name);

        // Hueco interior del marco (1341x1173): x 0.2–0.8, y 0.21–0.79. Algo más grande para que no se vean juntas.
        var backgroundRect = CreateRect(card, "Background");
        SetAnchors(backgroundRect, new Vector2(0.18f, 0.19f), new Vector2(0.82f, 0.81f));
        var background = backgroundRect.gameObject.AddComponent<Image>();

        Image Face(string faceName)
        {
            var rect = CreateRect(card, faceName);
            SetAnchors(rect, new Vector2(0.26f, 0.27f), new Vector2(0.74f, 0.73f));
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        var back = Face("Back");
        var symbol = Face("Symbol");

        var frameRect = CreateRect(card, "Frame");
        Stretch(frameRect);
        var frame = frameRect.gameObject.AddComponent<Image>();
        frame.sprite = cardFrame;
        frame.raycastTarget = false;

        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        // MemoryCardView pinta el fondo según el estado: el botón no tiñe.
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        AddHoverSound(card.gameObject);

        var view = card.gameObject.AddComponent<MemoryCardView>();
        var so = new SerializedObject(view);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("background").objectReferenceValue = background;
        so.FindProperty("symbol").objectReferenceValue = symbol;
        so.FindProperty("back").objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    static Image CreateSlicedFrame(Transform parent, string name, Sprite sprite, float borderScale)
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

    // ---------- Retrato de la oveja ----------

    /// <summary>
    /// Marco dorado con la cara de la oveja recortada en círculo sobre el relleno del clima.
    /// Orden: relleno → máscara con la cara → marco encima tapando los bordes.
    /// </summary>
    static SheepPortrait CreateSheepPortrait(Transform canvas)
    {
        var root = CreateRect(canvas, "SheepPortrait");
        root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
        root.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        root.anchoredPosition = CornerMargin;

        // El relleno no es enmascarable: así anima su propio material sin copias de stencil.
        var fillRect = CreateRect(root, "Fill");
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = Vector2.one * PortraitSize * PortraitFillRatio;
        var fill = fillRect.gameObject.AddComponent<Image>();
        fill.material = portraitMaterial;
        fill.maskable = false;
        fill.raycastTarget = false;

        var maskSize = PortraitSize * PortraitMaskRatio;
        var maskRect = CreateRect(root, "Mask");
        maskRect.anchorMin = maskRect.anchorMax = new Vector2(0.5f, 0.5f);
        maskRect.sizeDelta = Vector2.one * maskSize;
        var maskImage = maskRect.gameObject.AddComponent<Image>();
        maskImage.sprite = GetOrCreateCircleSprite();
        maskImage.raycastTarget = false;
        maskRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        // La cabeza está en el tercio superior del sprite: se baja para centrarla en el círculo.
        var characterRect = CreateRect(maskRect, "Character");
        characterRect.anchorMin = characterRect.anchorMax = new Vector2(0.5f, 0.5f);
        characterRect.sizeDelta = Vector2.one * maskSize * PortraitZoom;
        characterRect.anchoredPosition = new Vector2(0f, -maskSize * 0.08f);
        var character = characterRect.gameObject.AddComponent<Image>();
        character.sprite = sheepHappy;
        character.raycastTarget = false;

        var frameRect = CreateRect(root, "Frame");
        Stretch(frameRect);
        var frame = frameRect.gameObject.AddComponent<Image>();
        frame.sprite = portraitFrame;
        frame.raycastTarget = false;

        var portrait = root.gameObject.AddComponent<SheepPortrait>();
        var so = new SerializedObject(portrait);
        so.FindProperty("character").objectReferenceValue = character;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("happy").objectReferenceValue = sheepHappy;
        so.FindProperty("sly").objectReferenceValue = sheepSly;
        so.FindProperty("angry").objectReferenceValue = sheepAngry;
        so.ApplyModifiedPropertiesWithoutUndo();
        return portrait;
    }

    // ---------- Encargos ----------

    /// <summary>
    /// Panel "Encargos" arriba a la izquierda. El fondo trae tres renglones separados por líneas:
    /// la cabecera ocupa el primero y cada encargo uno de los de abajo.
    /// </summary>
    static void CreateQuestPanel(Transform canvas)
    {
        var height = QuestPanelWidth * questBackground.rect.height / questBackground.rect.width;
        var root = CreateRect(canvas, "QuestPanel");
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(QuestPanelWidth, height);
        root.anchoredPosition = new Vector2(CornerMargin.x, -CornerMargin.y);
        var background = root.gameObject.AddComponent<Image>();
        background.sprite = questBackground;
        background.raycastTarget = false;

        // Zonas en fracciones del sprite de 1422x1106: cabecera hasta la 1.ª línea (y=250),
        // filas entre esa línea y el borde inferior (y≈1000). Margen lateral dentro del marco.
        var header = CreateText(root, "Title", "ENCARGOS", 0f, InkColor, titleFont);
        SetAnchors(header.rectTransform, new Vector2(0.1f, 0.775f), new Vector2(0.9f, 0.95f));
        header.fontSize = height * 0.1f;

        var list = CreateRect(root, "Rows");
        SetAnchors(list, new Vector2(0.1f, 0.095f), new Vector2(0.9f, 0.775f));
        // Altura fija por fila para que cada encargo caiga en su renglón aunque haya menos de tres.
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var rowHeight = height * 0.68f / 3f;
        var row = CreateQuestRow(list, "QuestRow", rowHeight, withDescription: false);

        var panel = root.gameObject.AddComponent<QuestPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("rowTemplate").objectReferenceValue = row;
        so.FindProperty("rowContainer").objectReferenceValue = list;
        so.FindProperty("root").objectReferenceValue = background.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Fila: icono · nombre (y descripción en el diario) · progreso · check.</summary>
    static QuestRow CreateQuestRow(Transform parent, string name, float height, bool withDescription)
    {
        var rect = CreateRect(parent, name);
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = height;
        var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = height * 0.18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconSize = height * 0.72f;
        var icon = CreateIcon(rect, "Icon", null, iconSize);

        // Columna de textos: ocupa el espacio sobrante.
        var texts = CreateRect(rect, "Texts");
        texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var textLayout = texts.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        var titleSize = withDescription ? height * 0.3f : height * 0.34f;
        var title = CreateText(texts, "Title", "Encargo", titleSize, InkColor,
            withDescription ? bodyBoldFont : bodyFont, TextAlignmentOptions.Left);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text description = null;
        if (withDescription)
        {
            description = CreateText(texts, "Description", "", height * 0.22f, InkSoftColor, bodyFont,
                TextAlignmentOptions.TopLeft);
            description.lineSpacing = -10f;
        }

        var progress = CreateText(rect, "Progress", "", height * 0.32f, InkSoftColor, bodyBoldFont,
            TextAlignmentOptions.Right);
        progress.textWrappingMode = TextWrappingModes.NoWrap;
        progress.gameObject.AddComponent<LayoutElement>().minWidth = height * 0.75f;

        var checkImage = CreateIcon(rect, "Check", check, height * 0.5f);

        var row = rect.gameObject.AddComponent<QuestRow>();
        var so = new SerializedObject(row);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("description").objectReferenceValue = description;
        so.FindProperty("progressLabel").objectReferenceValue = progress;
        so.FindProperty("check").objectReferenceValue = checkImage;
        so.ApplyModifiedPropertiesWithoutUndo();
        return row;
    }

    // ---------- Diario ----------

    /// <summary>Botón abajo a la derecha que abre el diario: pergamino centrado con todos los encargos.</summary>
    static void CreateDiary(Transform canvas)
    {
        var root = CreateRect(canvas, "Diary");
        Stretch(root);

        // El pergamino va debajo del botón para que el botón siga a la vista y sirva para cerrar.
        var panel = CreateRect(root, "DiaryPanel");
        Stretch(panel);

        var backdropImage = panel.gameObject.AddComponent<Image>();
        backdropImage.color = BackdropColor;
        var backdrop = panel.gameObject.AddComponent<Button>();
        backdrop.transition = Selectable.Transition.None;
        backdrop.navigation = new Navigation { mode = Navigation.Mode.None };

        var height = DiaryWidth * diaryBackground.rect.height / diaryBackground.rect.width;
        var paper = CreateRect(panel, "Parchment");
        paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.5f);
        paper.sizeDelta = new Vector2(DiaryWidth, height);
        // Recibe los clics para que no lleguen al fondo (que cierra el diario).
        var paperImage = paper.gameObject.AddComponent<Image>();
        paperImage.sprite = diaryBackground;

        // Mismos márgenes interiores que el pergamino del menú, a esta escala.
        var scale = DiaryWidth / 1700f;
        var content = CreateRect(paper, "Content");
        Stretch(content);
        content.offsetMin = new Vector2(150f, 115f) * scale;
        content.offsetMax = new Vector2(-150f, -80f) * scale;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(content, "Title", "DIARIO", 60f, InkColor, titleFont);
        var island = CreateText(content, "Island", "Isla", 36f, InkSoftColor, bodyFont);
        island.fontStyle = FontStyles.Italic;
        CreateText(content, "Section", "Encargos", 40f, InkColor, bodyBoldFont, TextAlignmentOptions.Left);

        var list = CreateRect(content, "Rows");
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        var row = CreateQuestRow(list, "QuestRow", 110f, withDescription: true);

        var close = CreateTextButton(paper, "CloseButton", "×", 64f);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(90f, 90f);
        // Dentro del marco: la esquina del pergamino está cubierta de hojas.
        closeRect.anchoredPosition = new Vector2(-190f, -120f) * scale;

        // Botón del diario: cambia al sprite iluminado al pasar el ratón.
        var buttonRect = CreateRect(root, "DiaryButton");
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = new Vector2(DiaryButtonSize, DiaryButtonSize);
        buttonRect.anchoredPosition = new Vector2(-CornerMargin.x, CornerMargin.y);
        var buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = diaryButton;
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = diaryButtonHover,
            pressedSprite = diaryButtonHover,
            selectedSprite = diaryButton,
        };
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        AddHoverSound(button.gameObject);
        AddHoverSound(close.gameObject);

        var diary = root.gameObject.AddComponent<DiaryPanel>();
        var so = new SerializedObject(diary);
        so.FindProperty("openButton").objectReferenceValue = button;
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("backdrop").objectReferenceValue = backdrop;
        so.FindProperty("islandLabel").objectReferenceValue = island;
        so.FindProperty("rowTemplate").objectReferenceValue = row;
        so.FindProperty("rowContainer").objectReferenceValue = list;
        SetEventReference(so.FindProperty("openSound"), DiaryOpenEvent);
        SetEventReference(so.FindProperty("closeSound"), DiaryCloseEvent);
        so.ApplyModifiedPropertiesWithoutUndo();

        // El contenedor no debe tapar clics del juego cuando el diario está cerrado.
        panel.gameObject.SetActive(false);
    }

    static Button CreateTextButton(Transform parent, string name, string glyph, float size)
    {
        var rect = CreateRect(parent, name);
        var hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var text = CreateText(rect, "Glyph", glyph, size, InkColor, bodyBoldFont);
        Stretch(text.rectTransform);
        return button;
    }

    // ---------- Utilidades ----------

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color,
        TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var tmp = CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
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

    static void Stretch(RectTransform rect) => SetAnchors(rect, Vector2.zero, Vector2.one);

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    /// <summary>Brújula con flechas (clic = girar por pasos) y, debajo, la placa con la pista del clic derecho.</summary>
    static void CreateRotateWidget(Transform canvas)
    {
        // Los tamaños de la placa, el ratón, el texto y el contenedor los pone IslandRotateLayout.
        var root = CreateRect(canvas, "IslandRotate");
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = -CornerMargin;

        var compassRect = CreateRect(root, "Compass");
        compassRect.anchorMin = compassRect.anchorMax = compassRect.pivot = new Vector2(0.5f, 1f);
        compassRect.sizeDelta = new Vector2(CompassSize, CompassSize);
        var compassImage = compassRect.gameObject.AddComponent<Image>();
        compassImage.sprite = compass;
        compassImage.raycastTarget = false;

        // Zonas de clic sobre las flechas del sprite (en fracciones de la imagen de 1254 px).
        var left = CreateArrowButton(compassRect, "RotateLeft", new Vector2(0f, 0.2f), new Vector2(0.27f, 0.6f), compassImage);
        var right = CreateArrowButton(compassRect, "RotateRight", new Vector2(0.73f, 0.2f), new Vector2(1f, 0.6f), compassImage);

        var plaqueRect = CreateRect(root, "Plaque");
        plaqueRect.anchorMin = plaqueRect.anchorMax = plaqueRect.pivot = new Vector2(0.5f, 0f);
        var plaqueImage = plaqueRect.gameObject.AddComponent<Image>();
        plaqueImage.sprite = plaque;
        plaqueImage.raycastTarget = false;

        // Franja de pergamino del sprite (sin el borde ni el adorno inferior).
        var content = CreateRect(plaqueRect, "Content");
        content.anchorMin = new Vector2(0.06f, 0.33f);
        content.anchorMax = new Vector2(0.94f, 0.76f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // El sprite del ratón tiene mucho margen: se ve más grande que la franja sin salirse.
        var mouseRect = CreateRect(content, "MouseHint");
        var mouseLayout = mouseRect.gameObject.AddComponent<LayoutElement>();
        var mouseImage = mouseRect.gameObject.AddComponent<Image>();
        mouseImage.sprite = mouseRightClick;
        mouseImage.preserveAspect = true;
        mouseImage.raycastTarget = false;

        var label = CreateRect(content, "Label").gameObject.AddComponent<TextMeshProUGUI>();
        label.font = titleFont;
        label.text = "GIRAR ISLA";
        label.color = InkColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        var widget = root.gameObject.AddComponent<IslandRotateWidget>();
        var so = new SerializedObject(widget);
        so.FindProperty("rotateLeftButton").objectReferenceValue = left;
        so.FindProperty("rotateRightButton").objectReferenceValue = right;
        so.FindProperty("mouseHint").objectReferenceValue = mouseRect;
        so.ApplyModifiedPropertiesWithoutUndo();

        var widgetLayout = root.gameObject.AddComponent<IslandRotateLayout>();
        var layoutSo = new SerializedObject(widgetLayout);
        layoutSo.FindProperty("compass").objectReferenceValue = compassRect;
        layoutSo.FindProperty("plaque").objectReferenceValue = plaqueImage;
        layoutSo.FindProperty("mouseHint").objectReferenceValue = mouseLayout;
        layoutSo.FindProperty("label").objectReferenceValue = label;
        layoutSo.ApplyModifiedPropertiesWithoutUndo();
        widgetLayout.Refresh();
    }

    /// <summary>Área de clic invisible que aclara la brújula al pasar el ratón.</summary>
    static Button CreateArrowButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Graphic tintTarget)
    {
        var rect = CreateRect(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = tintTarget;
        // Son atajos del ratón: el teclado y el mando no necesitan foco aquí.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = IdleTint;
        colors.highlightedColor = Color.white;
        colors.selectedColor = IdleTint;
        colors.pressedColor = new Color32(200, 170, 120, 255);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }
}
