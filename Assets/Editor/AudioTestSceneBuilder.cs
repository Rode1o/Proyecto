using System.IO;
using FMODUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Genera la escena AudioTest: tres franjas de suelo (Grass, Dirt, Stone) y un jugador placeholder
/// con pasos de FMOD. Se puede volver a ejecutar: reemplaza la escena.
/// </summary>
static class AudioTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/AudioTest.unity";
    const string MaterialFolder = "Assets/Art/Materials/AudioTest";
    const string FootstepEventPath = "event:/Foley/Footsteps";
    // GUID del evento en el proyecto de FMOD Studio (Metadata/Event/{...}.xml). No depende de la caché de FMOD.
    const string FootstepEventGuid = "{62d89f5e-be91-4c70-89c6-49628dae0d99}";

    const float StripWidth = 6f;
    const float StripLength = 14f;

    static readonly Color GrassColor = new Color32(86, 140, 62, 255);
    static readonly Color DirtColor = new Color32(120, 84, 52, 255);
    static readonly Color StoneColor = new Color32(128, 128, 134, 255);
    static readonly Color PlayerColor = new Color32(232, 184, 100, 255);

    [MenuItem("Tools/Audio/Generar escena AudioTest")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateStrip("Ground_Grass", SurfaceType.Grass, -StripWidth, GetMaterial("Grass", GrassColor));
        CreateStrip("Ground_Dirt", SurfaceType.Dirt, 0f, GetMaterial("Dirt", DirtColor));
        CreateStrip("Ground_Stone", SurfaceType.Stone, StripWidth, GetMaterial("Stone", StoneColor));
        CreatePlayer(GetMaterial("Player", PlayerColor));

        RenderSettings.ambientMode = AmbientMode.Trilight;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Escena generada en {ScenePath}");
    }

    static void CreateLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateStrip(string name, SurfaceType surface, float x, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = new Vector3(x, -0.1f, 0f);
        go.transform.localScale = new Vector3(StripWidth, 0.2f, StripLength);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<FootstepSurface>().Surface = surface;
    }

    static void CreatePlayer(Material material)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.05f, -4f);

        var controller = player.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1f, 0f);
        controller.height = 2f;
        controller.radius = 0.4f;

        // Modelo placeholder: cápsula con una "nariz" que indica hacia dónde mira.
        var visual = new GameObject("Visual").transform;
        visual.SetParent(player.transform, false);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(visual, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.GetComponent<MeshRenderer>().sharedMaterial = material;

        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(visual, false);
        nose.transform.localPosition = new Vector3(0f, 1.5f, 0.45f);
        nose.transform.localScale = new Vector3(0.25f, 0.15f, 0.25f);

        // Cámara hija del jugador: lo sigue sin rotar, porque solo gira el modelo.
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(player.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 7f, -7f);
        cameraGo.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);
        var camera = cameraGo.AddComponent<Camera>();
        cameraGo.AddComponent<AudioListener>();
        // El oyente de FMOD va en la cámara, pero la atenuación se mide desde el jugador.
        cameraGo.AddComponent<StudioListener>().AttenuationObject = player;

        var movement = player.AddComponent<PlaceholderPlayerMovement>();
        var movementSo = new SerializedObject(movement);
        movementSo.FindProperty("visual").objectReferenceValue = visual;
        movementSo.FindProperty("viewCamera").objectReferenceValue = camera;
        movementSo.ApplyModifiedPropertiesWithoutUndo();

        var footsteps = player.AddComponent<PlayerFootsteps>();
        var footstepsSo = new SerializedObject(footsteps);
        var eventReference = new EventReference
        {
            Guid = FMOD.GUID.Parse(FootstepEventGuid),
            Path = FootstepEventPath
        };
        footstepsSo.FindProperty("footstepEvent").boxedValue = eventReference;
        footstepsSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static Material GetMaterial(string name, Color color)
    {
        Directory.CreateDirectory(MaterialFolder);
        var path = $"{MaterialFolder}/AudioTest_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }
}
