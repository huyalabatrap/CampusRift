using System;
using System.Collections.Generic;
using System.Linq;
using CampusRift.Monsters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ShabanCombatSetup
{
    const string Root = "Assets/MonsterShaban/Combat";
    const string MonsterPath = "Assets/MonsterShaban/Monster_Shaban.prefab";
    const string PlayerPath = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";

    [MenuItem("Campus Rift/Shaban/Build Attack and Blood Effects")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before building combat assets.");
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/MonsterShaban", "Combat");
        BuildAttack();
        var blood = BuildBlood();
        var prefab = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            ConfigurePlayer(prefab, blood);
            PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (var health in Object.FindObjectsByType<PlayerMonsterHealth>())
        {
            ConfigurePlayer(health.gameObject, blood);
            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Shaban combat ready: Attack clip (impact 0.35s / recovery 0.9s), blood prefab and player feedback.");
    }

    static void ConfigurePlayer(GameObject player, ParticleSystem blood)
    {
        var feedback = player.GetComponent<PlayerBloodFeedback>();
        if (feedback == null) feedback = player.AddComponent<PlayerBloodFeedback>();
        feedback.bloodPrefab = blood;
        EditorUtility.SetDirty(feedback);
        if (PrefabUtility.IsPartOfPrefabInstance(feedback)) PrefabUtility.RecordPrefabInstancePropertyModifications(feedback);
    }

    static void BuildAttack()
    {
        var prefab = PrefabUtility.LoadPrefabContents(MonsterPath);
        try
        {
            var animator = prefab.GetComponentInChildren<Animator>();
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            var original = controller.animationClips.First(c => c.name == "CINEMA_4D_Main");
            original.SampleAnimation(animator.gameObject, 0);
            var model = animator.transform;
            var bones = model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("mixamorig_") || t.name == "zombie_Walk").ToArray();
            var rotations = bones.Select(t => t.localRotation).ToArray();
            var positions = bones.Select(t => t.localPosition).ToArray();
            var times = new[] {0f, 0.16f, 0.26f, MonsterCombat.ClipImpactTime, 0.45f, 0.64f, MonsterCombat.ClipDuration};
            var recordedRotations = new Quaternion[bones.Length, times.Length];
            var recordedPositions = new Vector3[bones.Length, times.Length];
            for (int frame = 0; frame < times.Length; frame++)
            {
                for (int b = 0; b < bones.Length; b++) { bones[b].localRotation = rotations[b]; bones[b].localPosition = positions[b]; }
                Pose(model, bones, frame);
                for (int b = 0; b < bones.Length; b++) { recordedRotations[b, frame] = bones[b].localRotation; recordedPositions[b, frame] = bones[b].localPosition; }
            }
            var clip = new AnimationClip { name = "Shaban_ClawAttack", frameRate = 60 };
            for (int b = 0; b < bones.Length; b++)
            {
                string path = AnimationUtility.CalculateTransformPath(bones[b], model);
                for (int axis = 0; axis < 4; axis++)
                {
                    var curve = new AnimationCurve();
                    for (int f = 0; f < times.Length; f++) curve.AddKey(new Keyframe(times[f], recordedRotations[b, f][axis]));
                    clip.SetCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis], curve);
                }
                for (int axis = 0; axis < 3; axis++)
                {
                    var curve = new AnimationCurve();
                    for (int f = 0; f < times.Length; f++) curve.AddKey(new Keyframe(times[f], recordedPositions[b, f][axis]));
                    clip.SetCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[axis], curve);
                }
            }
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Shaban_ClawAttack.anim");
            if (saved == null) { AssetDatabase.CreateAsset(clip, Root + "/Shaban_ClawAttack.anim"); saved = clip; }
            else { EditorUtility.CopySerialized(clip, saved); Object.DestroyImmediate(clip); }
            if (!controller.parameters.Any(p => p.name == "AttackSpeed")) controller.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach (var p in parameters) if (p.name == "AttackSpeed") p.defaultFloat = 1;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;
            var attack = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Attack") ?? machine.AddState("Attack", new Vector3(450, 100));
            attack.motion = saved; attack.speedParameter = "AttackSpeed"; attack.speedParameterActive = true; attack.writeDefaultValues = true;
            foreach (var t in attack.transitions) attack.RemoveTransition(t);
            var exit = attack.AddTransition(machine.defaultState); exit.hasExitTime = true; exit.exitTime = 1; exit.hasFixedDuration = true; exit.duration = 0.1f;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(saved);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    static void Pose(Transform model, Transform[] bones, int frame)
    {
        if (frame == 0 || frame == 6) return;
        Transform Bone(string n) => bones.First(t => t.name == n);
        var torso = Bone("mixamorig_Spine");
        var root = Bone("zombie_Walk");
        float[] lunge = {0, -0.045f, -0.02f, 0.38f, 0.32f, 0.1f, 0};
        float[] lean = {0, -9, -12, 18, 22, 8, 0};
        float[] twist = {0, 18, 26, -20, -32, -8, 0};
        root.localPosition += new Vector3(0, 0, lunge[frame]);
        torso.localRotation *= Quaternion.Euler(lean[frame], twist[frame], 0);
        Bone("mixamorig_Head").localRotation *= Quaternion.Euler(-lean[frame] * 0.4f, -twist[frame] * 0.5f, 0);
        Vector3 upper, lower;
        if (frame <= 2) { upper = new Vector3(0.85f, 0.5f, -0.3f); lower = new Vector3(-0.2f, 0.9f, 0.5f); }
        else if (frame == 3) { upper = new Vector3(0.15f, -0.1f, 1); lower = new Vector3(-0.5f, -0.3f, 1); }
        else if (frame == 4) { upper = new Vector3(-0.65f, -0.35f, 0.8f); lower = new Vector3(-0.8f, -0.45f, 0.1f); }
        else { upper = new Vector3(0.3f, -0.85f, 0.35f); lower = new Vector3(-0.15f, -0.7f, 0.6f); }
        Aim(Bone("mixamorig_RightArm"), Bone("mixamorig_RightForeArm"), model.TransformDirection(upper));
        Aim(Bone("mixamorig_RightForeArm"), Bone("mixamorig_RightHand"), model.TransformDirection(lower));
        Aim(Bone("mixamorig_LeftArm"), Bone("mixamorig_LeftForeArm"), model.TransformDirection(new Vector3(-0.6f, -0.5f, 0.45f)));
        Aim(Bone("mixamorig_LeftForeArm"), Bone("mixamorig_LeftHand"), model.TransformDirection(new Vector3(0.25f, -0.1f, 1)));
    }

    static void Aim(Transform bone, Transform child, Vector3 direction)
    { bone.rotation = Quaternion.FromToRotation(child.position - bone.position, direction) * bone.rotation; }

    static ParticleSystem BuildBlood()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/BloodDroplet.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Campus Rift/Blood Droplet"));
            AssetDatabase.CreateAsset(material, Root + "/BloodDroplet.mat");
        }
        var go = new GameObject("BloodImpact");
        try
        {
            var droplets = go.AddComponent<ParticleSystem>();
            ConfigureParticles(droplets, material, false);
            var mist = new GameObject("Fine spray"); mist.transform.SetParent(go.transform, false);
            ConfigureParticles(mist.AddComponent<ParticleSystem>(), material, true);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, Root + "/BloodImpact.prefab");
            return saved.GetComponent<ParticleSystem>();
        }
        finally { Object.DestroyImmediate(go); }
    }

    static void ConfigureParticles(ParticleSystem ps, Material material, bool mist)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false; main.duration = 0.85f; main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(mist ? 0.16f : 0.3f, mist ? 0.32f : 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(mist ? 0.8f : 1.6f, mist ? 2.6f : 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(mist ? 0.1f : 0.035f, mist ? 0.24f : 0.09f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.22f,0.004f,0.008f,mist ? 0.32f : 0.95f),new Color(0.65f,0.015f,0.025f,mist ? 0.5f : 1));
        main.gravityModifier = mist ? 0.12f : 0.8f;
        var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] {new ParticleSystem.Burst(0, (short)(mist ? 13 : 28))});
        var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = mist ? 48 : 30; shape.radius = 0.07f;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(new Color(0.55f,0.3f,0.3f),1)},new[] {new GradientAlphaKey(1,0),new GradientAlphaKey(0.85f,0.4f),new GradientAlphaKey(0,1)}); color.color = gradient;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,1,1,mist ? 1.8f : 0.35f));
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.renderMode = mist ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.045f; renderer.lengthScale = 1.5f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
    }
}
