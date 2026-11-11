using DialogueSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Creates a reusable dialogue UI in the currently open scene.</summary>
public static class DialogueCanvasSetup
{
    [MenuItem("Tools/Dialogue/Create Dialogue Canvas")]
    public static void Create()
    {
        var existingUI = Object.FindFirstObjectByType<DialogueUI>();
        if (existingUI != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Dialogue UI already exists",
                "This scene already contains a DialogueUI. Create another dialogue canvas anyway?",
                "Create Another",
                "Cancel");
            if (!replace) return;
        }

        EnsureEventSystem();

        var canvasObject = new GameObject("Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Dialogue Canvas");

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // VN-style character art sits in the main screen area, independent of the dialogue box.
        // Keep it behind the panel in the hierarchy so the dialogue box remains readable.
        var portraitRect = MakeRect("Character Portrait", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), Vector2.zero);
        portraitRect.anchorMin = new Vector2(0.5f, 0f);
        portraitRect.anchorMax = new Vector2(1f, 1f);
        portraitRect.offsetMin = new Vector2(0f, 245f);
        portraitRect.offsetMax = new Vector2(-24f, -24f);
        var portrait = portraitRect.gameObject.AddComponent<Image>();
        portrait.color = Color.white;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        var panel = MakeRect("Dialogue Panel", canvasObject.transform, new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = new Vector2(1f, 0f);
        panel.offsetMin = new Vector2(36f, 24f);
        panel.offsetMax = new Vector2(-36f, 244f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.075f, 0.94f);

        var name = MakeText("Speaker Name", panel, 32, new Color(1f, 0.78f, 0.36f), new Vector2(0f, 76f), new Vector2(1500f, 48f));
        name.alignment = TextAlignmentOptions.Left;
        name.rectTransform.anchorMin = new Vector2(0f, 1f);
        name.rectTransform.anchorMax = new Vector2(1f, 1f);
        name.rectTransform.offsetMin = new Vector2(48f, -76f);
        name.rectTransform.offsetMax = new Vector2(-48f, -28f);
        var body = MakeText("Dialogue Text", panel, 28, Color.white, Vector2.zero, Vector2.zero);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.rectTransform.anchorMin = Vector2.zero;
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(48f, 48f);
        body.rectTransform.offsetMax = new Vector2(-48f, -82f);
        body.text = "Dialogue text will appear here.";

        var continueText = MakeText("Continue Indicator", panel, 20, new Color(0.72f, 0.82f, 1f), Vector2.zero, new Vector2(420f, 32f));
        continueText.alignment = TextAlignmentOptions.Right;
        continueText.rectTransform.anchorMin = new Vector2(1f, 0f);
        continueText.rectTransform.anchorMax = new Vector2(1f, 0f);
        continueText.rectTransform.pivot = new Vector2(1f, 0.5f);
        continueText.rectTransform.anchoredPosition = new Vector2(-48f, 25f);
        continueText.text = "Click / Space to continue";

        var spawnPointObject = new GameObject("Particle Spawn Point");
        Undo.RegisterCreatedObjectUndo(spawnPointObject, "Create Particle Spawn Point");
        spawnPointObject.transform.position = Vector3.zero;

        var systemObject = new GameObject("Dialogue System");
        Undo.RegisterCreatedObjectUndo(systemObject, "Create Dialogue System");
        var manager = systemObject.AddComponent<DialogueManager>();
        var dialogueUI = systemObject.AddComponent<DialogueUI>();

        var serializedUI = new SerializedObject(dialogueUI);
        serializedUI.FindProperty("dialogueManager").objectReferenceValue = manager;
        serializedUI.FindProperty("dialoguePanel").objectReferenceValue = panel.gameObject;
        serializedUI.FindProperty("speakerName").objectReferenceValue = name;
        serializedUI.FindProperty("dialogueText").objectReferenceValue = body;
        serializedUI.FindProperty("characterPortrait").objectReferenceValue = portrait;
        serializedUI.FindProperty("particleSpawnPoint").objectReferenceValue = spawnPointObject.transform;
        serializedUI.FindProperty("continueIndicator").objectReferenceValue = continueText.gameObject;
        serializedUI.ApplyModifiedProperties();

        Selection.activeGameObject = systemObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Dialogue UI Element");
        gameObject.transform.SetParent(parent, false);

        var rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, int fontSize, Color color, Vector2 position, Vector2 size)
    {
        var rect = MakeRect(name, parent, new Vector2(0.5f, 0.5f), position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.text = name;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }
}
