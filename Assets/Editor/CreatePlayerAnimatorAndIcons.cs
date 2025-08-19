using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class CreatePlayerAnimatorAndIcons
{
    [MenuItem("Tools/Setup/Create Player Animator and UI Icons")] 
    public static void CreateAll()
    {
        EnsureFolder("Assets/Animations");
        EnsureFolder("Assets/Sprites");

        var controllerPath = "Assets/Animations/Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AddTrigger(controller, "Gather");
        AddTrigger(controller, "Mold");
        AddTrigger(controller, "Throw");

        var idleClip = CreateOrGetClip("Assets/Animations/Idle.anim");
        var gatherClip = CreateOrGetClip("Assets/Animations/Gather.anim");
        var moldClip = CreateOrGetClip("Assets/Animations/Mold.anim");
        var throwClip = CreateOrGetClip("Assets/Animations/Throw.anim");

        var sm = controller.layers[0].stateMachine;
        var idleState = FindOrAddState(sm, "Idle", idleClip);
        sm.defaultState = idleState;
        var gatherState = FindOrAddState(sm, "Gather", gatherClip);
        var moldState = FindOrAddState(sm, "Mold", moldClip);
        var throwState = FindOrAddState(sm, "Throw", throwClip);

        AddAnyToStateTransition(sm, gatherState, "Gather");
        AddAnyToStateTransition(sm, moldState, "Mold");
        AddAnyToStateTransition(sm, throwState, "Throw");

        AddBackToIdle(sm, gatherState, idleState);
        AddBackToIdle(sm, moldState, idleState);
        AddBackToIdle(sm, throwState, idleState);

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
        }

        var empty = CreateColorSprite("Assets/Sprites/EmptyIcon.png", new Color(0.5f,0.5f,0.5f,1f));
        var snow = CreateColorSprite("Assets/Sprites/SnowIcon.png", new Color(0.7f,0.85f,1f,1f));
        var snowball = CreateColorSprite("Assets/Sprites/SnowballIcon.png", Color.white);

        var ui = Object.FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            ui.emptyIcon = empty;
            ui.snowIcon = snow;
            ui.snowballIcon = snowball;
            EditorUtility.SetDirty(ui);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static AnimationClip CreateOrGetClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.frameRate = 30f;
            AssetDatabase.CreateAsset(clip, path);
        }
        return clip;
    }

    static AnimatorState FindOrAddState(AnimatorStateMachine sm, string name, Motion motion)
    {
        foreach (var s in sm.states)
        {
            if (s.state != null && s.state.name == name)
            {
                s.state.motion = motion;
                return s.state;
            }
        }
        var state = sm.AddState(name);
        state.motion = motion;
        return state;
    }

    static void AddTrigger(AnimatorController c, string name)
    {
        foreach (var p in c.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name) return;
        }
        c.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    static void AddAnyToStateTransition(AnimatorStateMachine sm, AnimatorState target, string trigger)
    {
        foreach (var t in sm.anyStateTransitions)
        {
            if (t.destinationState == target) return;
        }
        var tr = sm.AddAnyStateTransition(target);
        tr.hasExitTime = false;
        tr.duration = 0.05f;
        tr.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    static void AddBackToIdle(AnimatorStateMachine sm, AnimatorState from, AnimatorState idle)
    {
        foreach (var t in from.transitions)
        {
            if (t.destinationState == idle) return;
        }
        var tr = from.AddTransition(idle);
        tr.hasExitTime = true;
        tr.exitTime = 0.9f;
        tr.duration = 0.1f;
    }

    static Sprite CreateColorSprite(string path, Color color)
    {
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite;
    }
}
