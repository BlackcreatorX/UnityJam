using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DialogueCanvasDefaultPrefab
{
    public const string PrefabPath = "Assets/Prefabs/DialogueCanvasDefault.prefab";
    private const string SourceScenePath = "Assets/Scenes/MancillasScene/RuletaGame.unity";
    private const string AutoSaveSessionKey = "DialogueCanvasDefaultPrefab.AutoSaved";

    static DialogueCanvasDefaultPrefab()
    {
        EditorApplication.delayCall += TryAutoSaveFromOpenScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem("Tools/Dialogue/Save Current Dialogue Canvas as Default")]
    public static void SaveCurrentAsDefault()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != SourceScenePath)
        {
            EditorUtility.DisplayDialog(
                "Dialogue Canvas Default",
                "Open the RuletaGame scene before saving the Dialogue Canvas default.",
                "OK");
            return;
        }

        GameObject canvas = GameObject.Find("Dialogue Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Dialogue Canvas Default",
                "The active scene does not contain a Dialogue Canvas.",
                "OK");
            return;
        }

        SavePrefab(canvas);
        EditorUtility.DisplayDialog(
            "Dialogue Canvas Default",
            "Dialogue Canvas saved as the default prefab.",
            "OK");
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == SourceScenePath)
            EditorApplication.delayCall += TryAutoSaveFromOpenScene;
    }

    private static void TryAutoSaveFromOpenScene()
    {
        if (SessionState.GetBool(AutoSaveSessionKey, false))
            return;

        SessionState.SetBool(AutoSaveSessionKey, true);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        Scene sourceScene = default;
        bool openedForCopy = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.path == SourceScenePath)
            {
                sourceScene = loadedScene;
                break;
            }
        }

        if (!sourceScene.IsValid())
        {
            sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            openedForCopy = sourceScene.IsValid();
        }

        if (sourceScene.IsValid())
        {
            GameObject canvas = null;
            foreach (GameObject root in sourceScene.GetRootGameObjects())
            {
                if (root.name == "Dialogue Canvas")
                {
                    canvas = root;
                    break;
                }
            }

            if (canvas != null)
                SavePrefab(canvas);

            if (openedForCopy)
                EditorSceneManager.CloseScene(sourceScene, false);
        }
    }

    private static void SavePrefab(GameObject canvas)
    {
        string directory = Path.GetDirectoryName(PrefabPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath, out bool saved);
        if (!saved)
            Debug.LogError($"Failed to save the Dialogue Canvas prefab at '{PrefabPath}'.", canvas);
        else
            Debug.Log($"Dialogue Canvas default saved to '{PrefabPath}'.", canvas);
    }
}
