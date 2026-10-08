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
    public sealed class ModelsP19Capture:P12PlayTest
    {
        public bool CorrectionsOnly;
        bool oldTutorial;
        EnemyInstance Spawn(string id,Vector3 offset)
        {
            var e=EnemyPool.Ensure().Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset"),world.origin+offset,LevelCatalog.Instance.Get(10).Scaling);
            e.Brain.enabled=false;e.Motor.Stop();e.GetComponent<EnemyConcealment>()?.RevealFor(30);
            if(e.GetComponent<FlyingMotor>()!=null)e.GetComponent<FlyingMotor>().Manual=true;
            e.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(world.camera.transform.position-e.transform.position,Vector3.up));return e;
        }
        void CameraAt(Vector3 from,Vector3 to)
        {world.camera.transform.position=world.origin+from;world.camera.transform.LookAt(world.origin+to);world.camera.fieldOfView=55;}
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes("task/models/screens/"+name+".png",image.EncodeToPNG());Destroy(image);
            Measure(name+" framebuffer "+Screen.width+"x"+Screen.height+" camera "+world.camera.transform.position+" particles="+world.player.GetComponent<SkillVfxPool>().ParticleCount);
            foreach(var e in EnemyDirector.Instance.Active)Measure(e.archetype.id+" pose="+e.Animation.CurrentState+" animatorTime="+e.Animation.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime+" activeHeight="+e.GetComponentsInChildren<SkinnedMeshRenderer>()[0].bounds.size.y);
            yield return null;
        }
        void Clean()
        {EnemyPool.Instance.ReleaseAll();EnemyTelegraph.Clear();world.player.GetComponent<SkillVfxPool>().Clear();foreach(var g in FindObjectsByType<BurningGround>(FindObjectsSortMode.None))g.Clear();}
        protected override IEnumerator Run()
        {
            Begin();oldTutorial=UI.TutorialDirector.Suppress;UI.TutorialDirector.Suppress=true;Directory.CreateDirectory("task/models/screens");
            world.Lighting(false);world.player.enabled=false;world.player.followCamera=null;
            foreach(string id in new[]{"anh-yeu","trieu-hon-su","duc-yeu","hoa-linh"})
            {
                if(CorrectionsOnly&&id!="duc-yeu")continue;
                bool flying=id=="duc-yeu";CameraAt(new Vector3(6,flying?5:2.6f,-8),new Vector3(6,flying?2.6f:1,0));
                var actor=Spawn(id,new Vector3(6,0,0));Spawn("liem-hon",new Vector3(3.7f,0,0));Spawn("bao-thi",new Vector3(8.3f,0,0));
                yield return new WaitForSeconds(2.6f);actor.Animation.Play(actor.Animation.profile.idle,1,5);yield return new WaitForSeconds(.25f);
                yield return Shot(id+"-comparison");Clean();
            }
            CameraAt(new Vector3(-4,3,-7),new Vector3(-2,1,1));var shadow=Spawn("anh-yeu",new Vector3(-5,0,3));yield return new WaitForSeconds(.9f);
            Time.timeScale=.2f;shadow.GetComponent<ExpandedEnemyRuntime>().Force("teleport",world.player.transform);yield return new WaitForSeconds(.12f);yield return Shot("anh-yeu-special");Time.timeScale=1;Clean();
            CameraAt(new Vector3(6,2.7f,-7),new Vector3(6,1,0));var caster=Spawn("trieu-hon-su",new Vector3(6,0,0));yield return new WaitForSeconds(.9f);
            // Allow the real spawn transition once, then hold the actor for the support-pose photo checks.
            caster.Brain.enabled=true;yield return null;caster.Brain.enabled=false;
            Time.timeScale=.2f;var support=caster.GetComponent<ExpandedEnemyRuntime>();support.Force("summon",world.player.transform);yield return new WaitForSeconds(.18f);yield return Shot("trieu-hon-su-special");Time.timeScale=1;
            yield return new WaitForSeconds(1.2f);var ally=support.Owned.FirstOrDefault();if(ally!=null){ally.Brain.enabled=false;ally.Motor.Stop();ally.Vitality.ApplyDamage(Combat.DamageInfo.Create(ally.Vitality.maxHealth*.3f,Combat.Element.None,Combat.DamageSource.Skill,ally.transform.position,Vector3.forward));support.HealAllies(1);Check(caster.Animation.CurrentState=="Heal_Channel","Actual ally heal selects support channel pose");support.ShieldAllies();Check(caster.Animation.CurrentState=="Shield_Cast","Actual ward grant selects shield cast pose");}Clean();
            CameraAt(new Vector3(4,5,-10),new Vector3(3,2.5f,0));var wing=Spawn("duc-yeu",new Vector3(7,0,0));yield return new WaitForSeconds(.9f);
            var dive=wing.GetComponent<ExpandedEnemyRuntime>();dive.Force("dive",world.player.transform);Time.timeScale=.2f;yield return new WaitForSeconds(.28f);yield return Shot("duc-yeu-special");Time.timeScale=1;yield return new WaitForSeconds(.9f);Check(dive.Dives==1&&dive.ImpactAt-dive.WarningAt>=.59f,"Pitched flight visual retains real dive impact clock");Clean();
            if(!CorrectionsOnly){CameraAt(new Vector3(6,2.7f,-7),new Vector3(6,1,0));var fire=Spawn("hoa-linh",new Vector3(7,0,0));yield return new WaitForSeconds(.9f);
            fire.Motor.MoveTo(world.origin+new Vector3(4,0,0));yield return new WaitForSeconds(1.3f);fire.Animation.BeginAttack(.6f,"Fire_Cast",fire.Animation.profile.Clip("Fire_Cast").length*.5f);yield return new WaitForSeconds(.25f);yield return Shot("hoa-linh-special");Clean();
            }
            CameraAt(new Vector3(10,5,-11),new Vector3(10,2,0));int index=0;
            foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster)
            {Spawn(row.archetype.id,new Vector3(6+(index%4)*2.6f,0,(index/4-1)*2.8f));index++;}
            yield return new WaitForSeconds(2.6f);yield return Shot("level10-crowd");Check(EnemyDirector.Instance.Active.Count==11,"All eleven actual L10 archetypes visible in crowd fixture");
        }
        protected override void Cleanup()
        {Time.timeScale=1;UI.TutorialDirector.Suppress=oldTutorial;world.player.enabled=true;base.Cleanup();File.WriteAllText("task/models/capture.json",JsonUtility.ToJson(report,true));}
    }
}
#endif
