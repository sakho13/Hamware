using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>保存済みの試作シーンを開く。シーンやビルド設定を自動生成・上書きしない。</summary>
public static class HamwarePrototypeSetup
{
    public const string ScenePath = "Assets/Scenes/Hamware.unity";

    [MenuItem("Hamware/Open Playable Prototype")]
    public static void OpenScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }
}
