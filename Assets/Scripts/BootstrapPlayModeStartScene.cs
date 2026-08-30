#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class BootstrapPlayModeStartScene
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    static BootstrapPlayModeStartScene()
    {
        EditorApplication.delayCall += ConfigurePlayModeStartScene;
    }

    private static void ConfigurePlayModeStartScene()
    {
        var bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        if (bootstrapScene == null)
        {
            Debug.LogWarning($"Bootstrap scene not found at '{BootstrapScenePath}'.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != bootstrapScene)
        {
            EditorSceneManager.playModeStartScene = bootstrapScene;
        }
    }
}
#endif
