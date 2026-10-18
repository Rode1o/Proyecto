using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds WebGL de prueba: el menú principal y una escena de juego concreta. El botón Jugar se
/// redirige a esa escena solo dentro de la build (no se modifica MainMenu.unity).
/// Se puede lanzar desde el menú o en batch:
/// Unity.exe -batchmode -quit -projectPath . -executeMethod WebBuild.BuildPrototype
/// </summary>
static class WebBuild
{
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    const string PrototypeScene = "Assets/Scenes/IslandsPrototipo.unity";
    const string PrototypeOutput = "Builds/WebGL-Prototipo";

    // Escena a la que lleva Jugar mientras dura la build; null fuera de ella.
    static string gameSceneOverride;

    [MenuItem("Tools/Build/WebGL (IslandsPrototipo)")]
    public static void BuildPrototype() => Build(PrototypeScene, PrototypeOutput);

    static void Build(string gameScene, string output)
    {
        gameSceneOverride = Path.GetFileNameWithoutExtension(gameScene);
        // Con fallback la build abre en cualquier servidor, no solo en los que sirven Brotli con cabeceras.
        var previousFallback = PlayerSettings.WebGL.decompressionFallback;
        PlayerSettings.WebGL.decompressionFallback = true;

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainMenuScene, gameScene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
        }
        finally
        {
            gameSceneOverride = null;
            PlayerSettings.WebGL.decompressionFallback = previousFallback;
        }

        var summary = report.summary;
        Debug.Log($"Build WebGL: {summary.result} en {summary.outputPath} " +
                  $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0} s, " +
                  $"{summary.totalErrors} errores, {summary.totalWarnings} avisos)");

        if (Application.isBatchMode)
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>Cambia la escena de Jugar en la copia del menú que entra en la build.</summary>
    class RedirectPlayButton : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || gameSceneOverride == null)
                return;

            foreach (var root in scene.GetRootGameObjects())
            foreach (var menu in root.GetComponentsInChildren<MainMenu>(true))
            {
                var so = new SerializedObject(menu);
                so.FindProperty("gameSceneName").stringValue = gameSceneOverride;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"Build: Jugar abre '{gameSceneOverride}'.");
            }
        }
    }
}
