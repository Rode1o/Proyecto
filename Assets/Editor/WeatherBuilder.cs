using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera el prefab Weather (WeatherController + lluvia) y sus materiales. Se puede volver a ejecutar:
/// reemplaza el prefab; los materiales solo se crean si no existen, para no pisar ajustes hechos a mano.
/// Para usarlo, arrastra el prefab a la escena (necesita una SheepEnemy para cambiar solo).
/// </summary>
static class WeatherBuilder
{
    const string PrefabPath = "Assets/Prefabs/Weather.prefab";
    const string SkyMaterialPath = "Assets/Art/Shaders/WeatherSky.mat";
    const string RainMaterialPath = "Assets/Art/Materials/Rain.mat";
    const string SkyShaderName = "Skybox/Weather Gradient";
    // Material de partículas de URP (transparente, textura redonda por defecto).
    const string UrpParticlesUnlitGuid = "e823cd5b5d27c0f4b8256e7c12ee3e6d";

    // Lluvia ligera para WebGL: pocas gotas, sin colisiones ni sombras.
    const int MaxRaindrops = 700;
    static readonly Color RainColor = new(0.86f, 0.84f, 0.96f, 0.65f);

    [MenuItem("Tools/Clima/Generar prefab Weather")]
    public static void Build()
    {
        var skyMaterial = GetOrCreateSkyMaterial();
        var rainMaterial = GetOrCreateRainMaterial();

        // Se monta en una escena de previsualización para no tocar la escena abierta.
        var preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Weather");
        SceneManager.MoveGameObjectToScene(root, preview);

        var rain = CreateRain(root.transform, rainMaterial);

        var weather = root.AddComponent<WeatherController>();
        var so = new SerializedObject(weather);
        so.FindProperty("skybox").objectReferenceValue = skyMaterial;
        so.FindProperty("rain").objectReferenceValue = rain;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        EditorSceneManager.ClosePreviewScene(preview);
        Debug.Log($"Prefab de clima generado en {PrefabPath}");
    }

    static ParticleSystem CreateRain(Transform parent, Material material)
    {
        var go = new GameObject("Rain");
        go.transform.SetParent(parent, false);
        var rain = go.AddComponent<ParticleSystem>();
        // AddComponent arranca el sistema: se para para poder cambiar la duración.
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = rain.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 20f;
        main.startSize = 0.07f;
        main.startColor = RainColor;
        main.maxParticles = MaxRaindrops;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // WeatherController sube la emisión cuando llueve.
        var emission = rain.emission;
        emission.rateOverTime = 0f;

        // Caja horizontal sobre la vista; girada para que las gotas salgan hacia abajo.
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(36f, 36f, 0.1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Un poco de viento para que no caigan perfectamente rectas.
        var velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 2f;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return rain;
    }

    static Material GetOrCreateSkyMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (material != null)
            return material;

        AssetDatabase.CreateAsset(new Material(Shader.Find(SkyShaderName)), SkyMaterialPath);
        return AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
    }

    static Material GetOrCreateRainMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(RainMaterialPath);
        if (material != null)
            return material;

        AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(UrpParticlesUnlitGuid), RainMaterialPath);
        material = AssetDatabase.LoadAssetAtPath<Material>(RainMaterialPath);
        material.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
