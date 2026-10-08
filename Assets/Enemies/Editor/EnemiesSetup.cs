using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

// P04: FBX import settings, materials, animator controllers, prefabs and data for the light monsters.
public static class EnemiesSetup
{
    const string Root = "Assets/Enemies/";
    const string Source = Root + "Models/Source/";
    const int EnemyLayer = 7;

    sealed class Spec
    {
        public string key, prefabName, archetypeId, en, vi; public Element element; public float height, radius, health, damage, speed, range, cooldown;
        public bool ranged; public Color tint = Color.white; public string move; public float min = 8, max = 12; public int weight = 1; public string atlas;
    }

    static readonly Spec[] Specs =
    {
        new Spec { key = "goblin", prefabName = "TieuYeu", archetypeId = "tieu-yeu", en = "Imp", vi = "Tiểu Yêu", element = Element.Tho, height = 1.3f, radius = 0.3f,
            health = 60, damage = 8, speed = 5.5f, range = 1.6f, cooldown = 1.4f, move = "Run", tint = new Color(1.55f, 0.95f, 0.55f), atlas = "goblin_Atlas.png" },
        new Spec { key = "spiky", prefabName = "DocNhan", archetypeId = "doc-nhan", en = "One-Eyed Archer", vi = "Độc Nhãn Xạ Thủ", element = Element.Moc, height = 1.5f, radius = 0.45f,
            health = 45, damage = 10, speed = 4f, range = 12f, cooldown = 2.2f, ranged = true, move = "Walk" },
    };

    [MenuItem("Campus Rift/V2/Setup Enemies")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        Directory.CreateDirectory(Root + "Materials"); Directory.CreateDirectory(Root + "Animation"); Directory.CreateDirectory(Root + "Prefabs");
        Directory.CreateDirectory(Root + "Resources/EnemyVfx");
        AssetDatabase.Refresh();

        var shaban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterShaban/Monster_Shaban.prefab");
        int agentType = shaban != null ? shaban.GetComponent<NavMeshAgent>().agentTypeID : 0;

        var prefabs = new Dictionary<string, GameObject>();
        foreach (var spec in Specs)
        {
            var info = JsonUtility.FromJson<ModelInfo>(File.ReadAllText(Source + spec.key + ".json"));
            var materials = BuildMaterials(spec, File.ReadAllText(Source + spec.key + ".json"));
            ConfigureImporter(spec, materials, info, log);
            var controller = BuildController(spec, info, log);
            prefabs[spec.key] = BuildPrefab(spec, controller, materials, agentType, log);
        }

        BuildVfx(log);
        BuildTiers(log);
        var goblin = Archetype(Specs[0], prefabs["goblin"], 0.5f, 1);
        var archer = Archetype(Specs[1], prefabs["spiky"], 0.5f, 1);
        goblin.attackClipSeconds = ClipSeconds("goblin", "Attack"); archer.attackClipSeconds = ClipSeconds("spiky", "Attack");
        EditorUtility.SetDirty(goblin); EditorUtility.SetDirty(archer);
        var heavy = Simple("thiet-giap-nguu", "Armored Ox", "Thiết Giáp Ngưu", Element.Kim, 220, 18, 4f, 2.2f, 2, 0.2f, pending: true);
        var bomber = Simple("bao-thi", "Bomber Corpse", "Bạo Thi", Element.Hoa, 50, 30, 6f, 1.4f, 1, 0, pending: true);
        var boss = Simple("shaban", "Shaban", "Shaban", Element.Am, 500, 25, 5.5f, 1.6f, 4, 0, pending: false);
        boss.prefab = shaban; boss.isBoss = false; EditorUtility.SetDirty(boss);
        AssetDatabase.SaveAssets();
        log.Add("archetypes: " + string.Join(", ", new[] { goblin.id, archer.id, heavy.id, bomber.id, boss.id }));
        return string.Join("\n", log);
    }

    [Serializable] sealed class ModelInfo { public float height; public float fps; }
    static float ClipSeconds(string key, string clip)
    {
        var text = File.ReadAllText(Source + key + ".json");
        var fps = JsonUtility.FromJson<ModelInfo>(text).fps;
        int at = text.IndexOf("\"" + clip + "\": [", StringComparison.Ordinal);
        if (at < 0) return 0.5f;
        int open = text.IndexOf('[', at), close = text.IndexOf(']', open);
        var numbers = text.Substring(open + 1, close - open - 1).Split(',').Select(t => float.Parse(t.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return (numbers[1] - numbers[0]) / fps;
    }

    // ---- materials ----
    static Dictionary<string, Material> BuildMaterials(Spec spec, string json)
    {
        var result = new Dictionary<string, Material>();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (spec.atlas != null)
        {
            string texturePath = Source + spec.atlas;
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var m = MakeMaterial(Root + "Materials/" + spec.prefabName + "_Atlas.mat", lit, spec.tint, 0.1f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); EditorUtility.SetDirty(m);
            result["Atlas"] = m; return result;
        }
        // Flat colours: read the linear base colours Blender reported and convert to sRGB for URP.
        foreach (var entry in ParseMaterials(json))
        {
            Color c = new Color(entry.Value[0], entry.Value[1], entry.Value[2]).gamma;
            // The glTF greens are nearly black under campus lighting: lift the body to a readable jade (Mộc).
            if (entry.Key == "Green_Main") c = new Color(0.28f, 0.72f, 0.22f);
            var m = MakeMaterial(Root + "Materials/" + spec.prefabName + "_" + entry.Key + ".mat", lit, c, entry.Key.Contains("Eye_White") ? 0.3f : 0.15f);
            result[entry.Key] = m;
        }
        return result;
    }

    static Material MakeMaterial(string path, Shader shader, Color color, float smoothness)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader; m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", 0);
        EditorUtility.SetDirty(m); return m;
    }

    static Dictionary<string, float[]> ParseMaterials(string json)
    {
        var result = new Dictionary<string, float[]>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(json, @"""([^""]+)"":\s*\{\s*""color"":\s*\[([^\]]+)\]"))
            result[m.Groups[1].Value] = m.Groups[2].Value.Split(',').Select(t => float.Parse(t.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return result;
    }

    // ---- model import ----
    static void ConfigureImporter(Spec spec, Dictionary<string, Material> materials, ModelInfo info, List<string> log)
    {
        string path = Source + spec.key + ".fbx";
        AssetDatabase.ImportAsset(path);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true; importer.importCameras = false; importer.importLights = false; importer.importBlendShapes = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.globalScale = 1f; importer.useFileScale = true;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.SaveAndReimport();
        // Clips: keep our names; idle and walk cycles loop, the rest play once.
        var clips = importer.defaultClipAnimations;
        var kept = new List<ModelImporterClipAnimation>();
        foreach (var c in clips)
        {
            string n = c.name.Contains("|") ? c.name.Split('|').Last() : c.name;
            if (n == "Dance" || n == "Yes" || n == "No" || n == "Jump") continue;
            c.name = n; c.loopTime = n == "Idle" || n == "Walk" || n == "Run"; c.loopPose = c.loopTime;
            kept.Add(c);
        }
        importer.clipAnimations = kept.ToArray();
        importer.SaveAndReimport();
        log.Add(spec.key + ": clips " + string.Join(",", kept.Select(k => k.name)));
    }

    // ---- animator ----
    static AnimationClip Clip(string key, string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Source + key + ".fbx").OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__") && (c.name == name || c.name.EndsWith("|" + name)));
    }

    static AnimatorController BuildController(Spec spec, ModelInfo info, List<string> log)
    {
        string path = Root + "Animation/" + spec.prefabName + ".controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);
        controller.parameters = controller.parameters.Select(p => { if (p.name == "AttackSpeed") p.defaultFloat = 1f; return p; }).ToArray();
        foreach (var t in new[] { "Attack", "Hit", "Die" }) controller.AddParameter(t, AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        AnimatorState State(string n, string clipName, Vector3 pos)
        {
            var clip = Clip(spec.key, clipName);
            var s = sm.AddState(n, pos); s.motion = clip; return s;
        }
        var idle = State("Idle", "Idle", new Vector3(250, 0)); var move = State("Move", spec.move, new Vector3(250, 90));
        var attack = State("Attack", "Attack", new Vector3(520, 0)); var hit = State("Hit", "Hit", new Vector3(520, 90)); var death = State("Death", "Death", new Vector3(520, 180));
        attack.speedParameterActive = true; attack.speedParameter = "AttackSpeed";
        sm.defaultState = idle;
        AnimatorStateTransition T(AnimatorState from, AnimatorState to, bool exit, float exitTime, float duration)
        { var t = from.AddTransition(to); t.hasExitTime = exit; t.exitTime = exitTime; t.duration = duration; t.hasFixedDuration = true; return t; }
        var toMove = T(idle, move, false, 0, 0.1f); toMove.AddCondition(AnimatorConditionMode.Greater, 0.3f, "MoveSpeed");
        var toIdle = T(move, idle, false, 0, 0.1f); toIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "MoveSpeed");
        var anyAttack = sm.AddAnyStateTransition(attack); anyAttack.hasExitTime = false; anyAttack.duration = 0.05f; anyAttack.canTransitionToSelf = true; anyAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        var anyHit = sm.AddAnyStateTransition(hit); anyHit.hasExitTime = false; anyHit.duration = 0.03f; anyHit.canTransitionToSelf = true; anyHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        var anyDie = sm.AddAnyStateTransition(death); anyDie.hasExitTime = false; anyDie.duration = 0.05f; anyDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        T(attack, idle, true, 0.95f, 0.1f); T(hit, idle, true, 0.9f, 0.05f);
        EditorUtility.SetDirty(controller);
        log.Add(spec.key + ": animator (" + sm.states.Length + " states)");
        return controller;
    }

    // ---- prefab ----
    static GameObject BuildPrefab(Spec spec, AnimatorController controller, Dictionary<string, Material> materials, int agentType, List<string> log)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Source + spec.key + ".fbx");
        var root = new GameObject(spec.prefabName);
        root.layer = EnemyLayer;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform); model.name = "Model";
        // Scale the model to its design height, feet on the ground.
        var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        Bounds bounds = skins[0].bounds; foreach (var s in skins) bounds.Encapsulate(s.bounds);
        float scale = spec.height / Mathf.Max(0.01f, bounds.size.y);
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = new Vector3(0, -(bounds.min.y - model.transform.position.y) * scale, 0);
        foreach (var s in skins)
        {
            var mats = new Material[s.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                string name = s.sharedMaterials[i] != null ? s.sharedMaterials[i].name : null;
                mats[i] = name != null && materials.TryGetValue(name, out var m) ? m : materials.Values.First();
            }
            s.sharedMaterials = mats; s.updateWhenOffscreen = false;
        }
        var animator = model.GetComponent<Animator>(); if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = EnemyLayer;

        var agent = root.AddComponent<NavMeshAgent>();
        agent.agentTypeID = agentType; agent.radius = spec.radius; agent.height = spec.height;
        agent.speed = spec.speed; agent.acceleration = 16f; agent.angularSpeed = 540f; agent.stoppingDistance = 0.1f;
        agent.autoTraverseOffMeshLink = true; agent.autoBraking = true; agent.avoidancePriority = 50 + UnityEngine.Random.Range(0, 20);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.enabled = false;
        var body = root.AddComponent<CapsuleCollider>();
        body.radius = Mathf.Min(spec.radius, spec.height * 0.4f); body.height = spec.height; body.center = new Vector3(0, spec.height / 2f, 0);
        var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        root.AddComponent<MonsterVitality>(); root.AddComponent<StatusEffectHost>();
        root.AddComponent<EnemyInstance>(); root.AddComponent<MinionMotor>(); root.AddComponent<MinionBrain>();
        string path = Root + "Prefabs/" + spec.prefabName + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        log.Add(spec.prefabName + ": prefab, model scale " + scale.ToString("0.000") + " (source height " + bounds.size.y.ToString("0.00") + ")");
        return prefab;
    }

    // ---- data ----
    static EnemyArchetype Archetype(Spec spec, GameObject prefab, float impact, int weight)
    {
        var a = Load(spec.archetypeId);
        a.id = spec.archetypeId; a.displayName = spec.en; a.displayNameVN = spec.vi; a.element = spec.element; a.prefab = prefab; a.prefabPending = false;
        a.baseHealth = spec.health; a.baseDamage = spec.damage; a.baseSpeed = spec.speed; a.attackRange = spec.range; a.attackCooldown = spec.cooldown;
        a.ranged = spec.ranged; a.preferredMin = 8; a.preferredMax = 12; a.projectileSpeed = 14; a.impactFraction = impact; a.swordIntentWeight = weight;
        EditorUtility.SetDirty(a); return a;
    }

    static EnemyArchetype Simple(string id, string en, string vn, Element element, float hp, float dmg, float speed, float cooldown, int weight, float defense, bool pending)
    {
        var a = Load(id);
        a.id = id; a.displayName = en; a.displayNameVN = vn; a.element = element; a.baseHealth = hp; a.baseDamage = dmg; a.baseSpeed = speed;
        a.attackCooldown = cooldown; a.swordIntentWeight = weight; a.defense = defense; a.prefabPending = pending; a.attackRange = 1.6f;
        EditorUtility.SetDirty(a); return a;
    }

    static EnemyArchetype Load(string id)
    {
        string path = Root + "Data/" + id + ".asset";
        Directory.CreateDirectory(Root + "Data");
        var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
        if (a == null) { a = ScriptableObject.CreateInstance<EnemyArchetype>(); AssetDatabase.CreateAsset(a, path); }
        return a;
    }

    static void BuildTiers(List<string> log)
    {
        string path = Root + "Resources/AITierProfiles.asset";
        var set = AssetDatabase.LoadAssetAtPath<AITierProfile>(path);
        if (set == null) { set = ScriptableObject.CreateInstance<AITierProfile>(); AssetDatabase.CreateAsset(set, path); }
        set.tiers = new[]
        {
            new AITierProfile.Tier { name = "T0", windup = 0.8f, meleeTokens = 2, rangedTokens = 2 },
            new AITierProfile.Tier { name = "T1", windup = 0.6f, meleeTokens = 3, rangedTokens = 2, surround = true },
            new AITierProfile.Tier { name = "T2", windup = 0.5f, meleeTokens = 3, rangedTokens = 3, surround = true, dodgeChance = 0.4f },
            new AITierProfile.Tier { name = "T3", windup = 0.5f, meleeTokens = 4, rangedTokens = 3, surround = true, dodgeChance = 0.4f, ambush = true },
            new AITierProfile.Tier { name = "T4", windup = 0.5f, meleeTokens = 4, rangedTokens = 4, surround = true, dodgeChance = 0.6f, ambush = true, adapt = true },
        };
        EditorUtility.SetDirty(set); log.Add("AI tiers T0–T4");
    }

    static void BuildVfx(List<string> log)
    {
        var shader = Shader.Find("Campus Rift/Speed Force Additive");
        var flare = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/SpeedForce/Textures/Flare.png");
        var streak = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/SpeedForce/Textures/Streak.png");
        Additive(Root + "Resources/EnemyVfx/EnemyProjectile.mat", shader, null, 2.2f, 0.15f);
        Additive(Root + "Resources/EnemyVfx/EnemyTrail.mat", shader, streak, 2.4f, 0.2f);
        Additive(Root + "Resources/EnemyVfx/EnemyBurst.mat", shader, flare, 2.4f, 0.3f);
        log.Add("vfx materials");
    }
    static void Additive(string path, Shader shader, Texture texture, float intensity, float core)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader; m.SetFloat("_Intensity", intensity); m.SetFloat("_CoreBoost", core); if (texture != null) m.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(m);
    }

    [MenuItem("Campus Rift/V2/Validate Enemies")]
    public static string Validate()
    {
        var errors = new List<string>(); var warnings = new List<string>(); var ids = new Dictionary<string,EnemyArchetype>(); int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:EnemyArchetype"))
        {
            var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(AssetDatabase.GUIDToAssetPath(guid)); count++;
            if (string.IsNullOrEmpty(a.id) || !System.Text.RegularExpressions.Regex.IsMatch(a.id, "^[a-z0-9]+(-[a-z0-9]+)*$")) errors.Add(a.name + ": id must be kebab-case");
            else if(ids.TryGetValue(a.id,out var canonical)) {
                // Only the exact P12/P19 runtime Resources aliases may duplicate canonical data.
                string path=AssetDatabase.GUIDToAssetPath(guid), prior=AssetDatabase.GetAssetPath(canonical);
                var aliases=new Dictionary<string,string>{{"tieu-yeu","P12/TieuYeu"},{"anh-yeu","P19/AnhYeu"},{"duc-yeu","P19/DucYeu"},{"hoa-linh","P19/HoaLinh"},{"trieu-hon-su","P19/TrieuHonSu"}};
                bool summonAlias=aliases.TryGetValue(a.id,out var alias) && new[]{path,prior}.Contains(Root+"Resources/"+alias+".asset") && new[]{path,prior}.Contains(Root+"Data/"+a.id+".asset") && a.prefab==canonical.prefab && JsonUtility.ToJson(a)==JsonUtility.ToJson(canonical);
                if(!summonAlias)errors.Add(a.id+": duplicate id");
            } else ids.Add(a.id,a);
            if (string.IsNullOrEmpty(a.displayNameVN)) errors.Add(a.id + ": missing Vietnamese name");
            if (a.baseHealth <= 0 || a.baseDamage <= 0 || a.baseSpeed <= 0) errors.Add(a.id + ": health, damage and speed must be positive");
            if (a.prefab == null) { if (a.prefabPending) warnings.Add(a.id + ": prefab pending"); else errors.Add(a.id + ": prefab is null"); }
            else if (a.id != "shaban")
            {
                bool shaban=a.prefab.GetComponent<ShabanEnemyBridge>()!=null;
                bool motor=shaban?a.prefab.GetComponent<CampusRift.Monsters.MonsterBrain>()!=null && a.prefab.GetComponent<CampusRift.Monsters.MonsterNavigation>()!=null:a.prefab.GetComponent<MinionBrain>()!=null && a.prefab.GetComponent<MinionMotor>()!=null;
                if(a.prefab.GetComponent<EnemyInstance>()==null || !motor)errors.Add(a.id+": prefab lacks its minion or P12 Shaban bridge runtime");
                if (a.prefab.layer != EnemyLayer) errors.Add(a.id + ": prefab must be on the Enemy layer");
                if (a.prefab.GetComponentInChildren<Animator>() == null || a.prefab.GetComponentInChildren<Animator>().runtimeAnimatorController == null) errors.Add(a.id + ": prefab has no animator controller");
            }
        }
        var tiers = Resources.Load<AITierProfile>("AITierProfiles");
        if (tiers == null || tiers.tiers.Length != 5) errors.Add("AITierProfiles must hold 5 tiers");
        else for (int i = 1; i < 5; i++) if (tiers.tiers[i].windup > tiers.tiers[i - 1].windup + 0.001f) errors.Add("Windup must not grow with tier (T" + i + ")");
        string result = "ENEMY DATA " + (errors.Count == 0 ? "PASS" : "FAIL") + ": " + count + " archetypes; " + string.Join("; ", errors) + (warnings.Count > 0 ? " [warnings: " + string.Join("; ", warnings) + "]" : "");
        Debug.Log(result); return result;
    }
}
