var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (active.isDirty) return "Scene has unsaved changes: " + active.path;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
var brain = UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();
var animator = brain.GetComponentInChildren<UnityEngine.Animator>();
var bones = new System.Collections.Generic.List<object>();
foreach (var t in animator.GetComponentsInChildren<UnityEngine.Transform>())
    if (!t.name.Contains("Finger") && !t.name.Contains("Thumb")) bones.Add(new {t.name, path=UnityEditor.AnimationUtility.CalculateTransformPath(t,animator.transform), p=t.localPosition.ToString(), r=t.localEulerAngles.ToString(), world=t.position.ToString()});
var clips = new System.Collections.Generic.List<object>();
foreach (var clip in animator.runtimeAnimatorController.animationClips) clips.Add(new {clip.name,clip.length,clip.humanMotion,curves=UnityEditor.AnimationUtility.GetCurveBindings(clip).Length});
return new {brain=brain.transform.position.ToString(), animator=animator.name, human=animator.isHuman, bones, clips, player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>().transform.position.ToString()};
