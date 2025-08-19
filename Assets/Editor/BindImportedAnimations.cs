using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public static class BindImportedAnimations
{
    [MenuItem("Tools/Setup/Bind Imported Animations")] 
    public static void Bind()
    {
        var playerController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player.controller");
        var fpController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/FPHands.controller");
        if (playerController == null && fpController == null)
        {
            Debug.LogError("Animator controllers not found. Run Tools/Setup/Create Player Animator and UI Icons and Create FP Hands Rig first.");
            return;
        }

        var allClipGuids = AssetDatabase.FindAssets("t:AnimationClip");
        var allClips = new List<AnimationClip>();
        foreach (var guid in allClipGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) allClips.Add(clip);
        }

        if (allClips.Count == 0)
        {
            Debug.LogWarning("No AnimationClips found in project.");
        }

        var map = new Dictionary<string, AnimationClip>();
        map["Idle"] = PickClip(allClips, new[]{"idle","stand","ожид","стой","idle_","idla"});
        map["Gather"] = PickClip(allClips, new[]{"gather","pick","bend","scoop","snow","собир","наклон","копа","греб"});
        map["Mold"] = PickClip(allClips, new[]{"mold","roll","knead","form","леп","ката","форм"});
        map["Throw"] = PickClip(allClips, new[]{"throw","cast","кида","брос"});

        if (playerController != null)
        {
            ApplyToController(playerController, map, "Player");
        }
        if (fpController != null)
        {
            ApplyToController(fpController, map, "FPHands");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BindImportedAnimations: done.");
    }

    static AnimationClip PickClip(List<AnimationClip> clips, string[] keywords)
    {
        if (clips == null || clips.Count == 0) return null;
        foreach (var kw in keywords)
        {
            var match = clips.FirstOrDefault(c => c != null && c.name.ToLowerInvariant().Contains(kw));
            if (match != null) return match;
        }
        return clips.FirstOrDefault();
    }

    static void ApplyToController(AnimatorController controller, Dictionary<string, AnimationClip> map, string label)
    {
        if (controller.layers.Length == 0)
        {
            Debug.LogWarning($"{label}: controller has no layers");
            return;
        }
        var sm = controller.layers[0].stateMachine;
        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state == null) continue;
            if (map.TryGetValue(state.name, out var clip) && clip != null)
            {
                state.motion = clip;
                EditorUtility.SetDirty(controller);
                Debug.Log($"{label}: state '{state.name}' -> '{clip.name}'");
            }
        }
    }
}
