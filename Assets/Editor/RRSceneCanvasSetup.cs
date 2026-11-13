using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public static class RRSceneCanvasSetup
{
    [MenuItem("Tools/Minigame/Create Saved Canvas")]
    public static void Create()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Charlie scene.unity", OpenSceneMode.Single);
        // Replace the previously generated minigame objects so the saved scene
        // receives fresh TMP components and matching serialized references.
        DestroyGeneratedRoots("Minigame Systems");
        DestroyGeneratedRoots("Minigame Canvas");
        var systems = new GameObject("Minigame Systems");
        var number = systems.AddComponent<RRLuckyNumberManager>();
        var roulette = systems.AddComponent<RRouletteManager>();
        var clues = systems.AddComponent<RRClueManager>();
        clues.Configure(number);
        var game = systems.AddComponent<RRGameManager>();
        game.Configure(number, roulette, clues);
        var screen = systems.AddComponent<RRMinigameScreen>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var canvasGo = new GameObject("Minigame Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
        var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var panel = MakeRect("Panel", canvasGo.transform, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1050,680));
        panel.gameObject.AddComponent<Image>().color = new Color(.055f,.075f,.12f);
        var title = MakeText("NUMBER DUEL", panel, 40, new Color(.95f,.72f,.28f), new Vector2(0,285), new Vector2(950,58));
        var subtitle = MakeText("Adivina el número del manager. La ruleta del enemigo es aleatoria.", panel, 26, Color.white, new Vector2(0,235), new Vector2(950,42));
        var status = MakeText("Preparando partida…", panel, 26, new Color(.55f,.85f,1f), new Vector2(0,180), new Vector2(950,45));
        var turns = MakeText("Turnos: —", panel, 26, Color.white, new Vector2(0,130), new Vector2(950,38));
        var cluesText = MakeText("Pistas: ninguna", panel, 20, new Color(.78f,.84f,.92f), new Vector2(0,65), new Vector2(900,80));
        var chamber = MakeText("Ruleta: lista", panel, 26, Color.white, new Vector2(0,5), new Vector2(950,38));

        var guessMethods = new UnityAction[] {screen.Guess1,screen.Guess2,screen.Guess3,screen.Guess4,screen.Guess5,screen.Guess6};
        var buttons = new System.Collections.Generic.List<Button>();
        for (int i=0;i<6;i++) buttons.Add(MakeButton((i+1).ToString(), panel, new Vector2(-375+i*150,-95), new Vector2(125,62), new Color(.16f,.25f,.39f), guessMethods[i]));
        buttons.Add(MakeButton("Revelar pista", panel, new Vector2(-300,-190), new Vector2(240,62), new Color(.14f,.42f,.38f), screen.RevealClue));
        buttons.Add(MakeButton("Girar ruleta", panel, new Vector2(0,-190), new Vector2(240,62), new Color(.55f,.31f,.13f), screen.Spin));
        buttons.Add(MakeButton("Jalar gatillo", panel, new Vector2(300,-190), new Vector2(240,62), new Color(.58f,.18f,.2f), screen.Shoot));

        var so = new SerializedObject(screen);
        so.FindProperty("numberManager").objectReferenceValue = number;
        so.FindProperty("gameManager").objectReferenceValue = game;
        so.FindProperty("rouletteManager").objectReferenceValue = roulette;
        so.FindProperty("clueManager").objectReferenceValue = clues;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("turnsText").objectReferenceValue = turns;
        so.FindProperty("cluesText").objectReferenceValue = cluesText;
        so.FindProperty("chamberText").objectReferenceValue = chamber;
        var array=so.FindProperty("gameplayButtons"); array.arraySize=buttons.Count;
        for(int i=0;i<buttons.Count;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static void DestroyGeneratedRoots(string generatedName)
    {
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == generatedName || root.name.StartsWith(generatedName + " ("))
                Object.DestroyImmediate(root);
        }
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>(); r.anchorMin=anchor; r.anchorMax=anchor; r.anchoredPosition=pos; r.sizeDelta=size; return r;
    }
    static TextMeshProUGUI MakeText(string value, Transform parent, int size, Color color, Vector2 pos, Vector2 rectSize)
    {
        var r=MakeRect(value,parent,new Vector2(.5f,.5f),pos,rectSize); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=TMP_Settings.defaultFontAsset; t.text=value; t.fontSize=size; t.color=color; t.alignment=TextAlignmentOptions.Center; t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Truncate; return t;
    }
    static Button MakeButton(string label, Transform parent, Vector2 pos, Vector2 size, Color color, UnityAction action)
    {
        var r=MakeRect(label,parent,new Vector2(.5f,.5f),pos,size); var image=r.gameObject.AddComponent<Image>(); image.color=color;
        var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        var text=MakeText(label,r,22,Color.white,Vector2.zero,size); text.raycastTarget=false; return button;
    }
}

