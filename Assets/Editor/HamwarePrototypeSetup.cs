using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HamwarePrototypeSetup
{
    public const string ScenePath = "Assets/Scenes/Hamware.unity";

    // 初回のシーン生成用。既存シーンを上書きしない。
    public static void CreateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Hamware Prototype");
        root.AddComponent<HamwareUI>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Hamware/Open Playable Prototype")]
    public static void OpenScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }
}
