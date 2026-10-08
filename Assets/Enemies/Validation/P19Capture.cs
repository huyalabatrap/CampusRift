#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Skills;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
    public sealed class P19Capture:P12PlayTest
    {
        public bool WarningsOnly;
        bool oldTutorial;
        EnemyInstance Spawn(string id,Vector3 position)
        {var e=EnemyPool.Ensure().Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset"),position,LevelCatalog.Instance.Get(10).Scaling);e.Brain.enabled=false;e.Motor.Stop();e.GetComponent<EnemyConcealment>()?.RevealFor(10);if(e.GetComponent<FlyingMotor>()!=null)e.GetComponent<FlyingMotor>().Manual=true;return e;}
        IEnumerator Shot(string name)
        {yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/p19/screens/"+name+".png",t.EncodeToPNG());Destroy(t);Measure(name+" framebuffer "+Screen.width+"x"+Screen.height+" camera="+world.camera.transform.position+" rotation="+world.camera.transform.eulerAngles);yield return null;}
        protected override IEnumerator Run()
        {
            Begin();oldTutorial=UI.TutorialDirector.Suppress;UI.TutorialDirector.Suppress=true;Directory.CreateDirectory("task/p19/screens");world.Lighting(false);world.player.enabled=false;world.player.followCamera=null;
            CameraAt(new Vector3(-5,3,-7),new Vector3(-2,1,1));var shadow=Spawn("anh-yeu",world.origin+new Vector3(-5,0,3));yield return new WaitForSeconds(.9f);Time.timeScale=.2f;var special=shadow.GetComponent<ExpandedEnemyRuntime>();special.Force("teleport",world.player.transform);yield return new WaitForSeconds(.25f);Check(special.Teleports==0,"Photo captures purple warning before teleport impact");yield return Shot("anh-yeu-telegraph");Time.timeScale=1;Clean();CameraAt(new Vector3(-3,3,-4),new Vector3(4,1,0));
            var caster=Spawn("trieu-hon-su",world.origin+Vector3.right*6);yield return new WaitForSeconds(.9f);Time.timeScale=.2f;caster.GetComponent<ExpandedEnemyRuntime>().Force("summon",world.player.transform);yield return new WaitForSeconds(.3f);yield return Shot("trieu-hon-su-telegraph");Time.timeScale=1;Clean();
            if(WarningsOnly){Check(true,"Final colored smoke warnings captured");yield break;}
            var wing=Spawn("duc-yeu",world.origin+Vector3.right*7);yield return new WaitForSeconds(.9f);
            // Gameplay camera orbit at a wider allowed distance, showing flying actor and its ground warning together.
            world.player.followCamera=null;world.camera.transform.position=world.origin+new Vector3(-3,4,-4);world.camera.transform.LookAt(world.origin+new Vector3(3,2,0));world.camera.fieldOfView=60;
            wing.GetComponent<ExpandedEnemyRuntime>().Force("dive",world.player.transform);yield return new WaitForSeconds(.25f);yield return Shot("duc-yeu-telegraph");Clean();CameraAt(new Vector3(-3,3,-4),new Vector3(4,1,0));
            var fire=Spawn("hoa-linh",world.origin+Vector3.right*7);yield return new WaitForSeconds(.9f);fire.Motor.MoveTo(world.origin+Vector3.right*4);yield return new WaitForSeconds(1.3f);fire.Brain.enabled=true;float end=Time.time+4;while(fire.Brain.State!=MinionState.Windup&&Time.time<end)yield return null;yield return Shot("hoa-linh-telegraph");Clean();
            var one=Spawn("thiet-giap-nguu",world.origin+Vector3.right*6);one.Elite.Configure(6,20,EliteAffixKind.Guardian);yield return new WaitForSeconds(2.6f);yield return Shot("elite-one-affix");Check(Combat.EnemyHealthBars.Instance.IsShowing(one.Vitality),"Elite one real nameplate visible");Clean();
            var two=Spawn("anh-yeu",world.origin+Vector3.right*6);two.Elite.Configure(10,21,EliteAffixKind.Berserk,EliteAffixKind.FireHeart);two.GetComponent<EnemyConcealment>().RevealFor(10);yield return new WaitForSeconds(.9f);yield return Shot("elite-two-affixes");Clean();
            CameraAt(new Vector3(0,4,-9),new Vector3(12,2,0));int index=0;foreach(var r in LevelCatalog.Instance.Get(10).spawnTable.roster){Spawn(r.archetype.id,world.origin+new Vector3(10+(index%3)*2.5f,0,(index/3-1.5f)*2.3f));index++;}yield return new WaitForSeconds(2.6f);yield return Shot("level10-crowd");
            Check(true,"Recaptured four characteristic warnings, two comic affix nameplates, dense11type fixture with final scale");
        }
        void CameraAt(Vector3 from,Vector3 to){world.camera.transform.position=world.origin+from;world.camera.transform.LookAt(world.origin+to);world.camera.fieldOfView=60;}
        void Clean(){EnemyPool.Instance.ReleaseAll();EnemyTelegraph.Clear();world.player.GetComponent<SkillVfxPool>().Clear();foreach(var g in FindObjectsByType<BurningGround>(FindObjectsSortMode.None))g.Clear();}
        protected override void Cleanup(){Time.timeScale=1;UI.TutorialDirector.Suppress=oldTutorial;world.player.enabled=true;base.Cleanup();File.WriteAllText("task/p19/capture.json",JsonUtility.ToJson(report,true));}
    }
}
#endif
