using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupFirstPersonHands
{
    [MenuItem("Tools/Setup/Create FP Hands Rig")]
    public static void CreateRig()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found");
            return;
        }

        var cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("Player camera not found");
            return;
        }

        var rigRoot = new GameObject("FP_Rig");
        rigRoot.transform.SetParent(cam.transform, false);
        rigRoot.transform.localPosition = new Vector3(0.2f, -0.25f, 0.5f);
        rigRoot.transform.localRotation = Quaternion.identity;

        var hands = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hands.name = "FP_Hands";
        hands.transform.SetParent(rigRoot.transform, false);
        hands.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);
        Object.DestroyImmediate(hands.GetComponent<Collider>());

        var heldSnow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        heldSnow.name = "FP_HeldSnowball";
        heldSnow.transform.SetParent(hands.transform, false);
        heldSnow.transform.localPosition = new Vector3(0.08f, 0.02f, 0.05f);
        heldSnow.transform.localScale = Vector3.one * 0.08f;
        Object.DestroyImmediate(heldSnow.GetComponent<Collider>());

        var controllerPath = "Assets/Animations/FPHands.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Gather", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Mold", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
        }

        var idle = CreateClip("Assets/Animations/FP_Idle.anim");
        var gather = CreateClip("Assets/Animations/FP_Gather.anim");
        var mold = CreateClip("Assets/Animations/FP_Mold.anim");
        var thrw = CreateClip("Assets/Animations/FP_Throw.anim");

        var sm = controller.layers[0].stateMachine;
        var idleState = FindOrAddState(sm, "Idle", idle);
        sm.defaultState = idleState;
        var gatherState = FindOrAddState(sm, "Gather", gather);
        var moldState = FindOrAddState(sm, "Mold", mold);
        var throwState = FindOrAddState(sm, "Throw", thrw);

        AddAny(sm, gatherState, "Gather");
        AddAny(sm, moldState, "Mold");
        AddAny(sm, throwState, "Throw");
        Back(sm, gatherState, idleState);
        Back(sm, moldState, idleState);
        Back(sm, throwState, idleState);

        var anim = hands.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var throwing = player.GetComponent<PlayerThrowing>();
        if (throwing != null)
        {
            throwing.fpAnimator = anim;
            throwing.heldSnowballVisual = heldSnow;
            EditorUtility.SetDirty(throwing);
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    static AnimationClip CreateClip(string path)
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
            if (s.state.name == name)
            {
                s.state.motion = motion;
                return s.state;
            }
        }
        var st = sm.AddState(name);
        st.motion = motion;
        return st;
    }

    static void AddAny(AnimatorStateMachine sm, AnimatorState target, string trigger)
    {
        var tr = sm.AddAnyStateTransition(target);
        tr.hasExitTime = false;
        tr.duration = 0.05f;
        tr.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    static void Back(AnimatorStateMachine sm, AnimatorState from, AnimatorState idle)
    {
        var tr = from.AddTransition(idle);
        tr.hasExitTime = true;
        tr.exitTime = 0.9f;
        tr.duration = 0.1f;
    }
}
