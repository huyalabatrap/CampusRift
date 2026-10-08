#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Skills;
namespace CampusRift.AR
{
    public sealed class ARRiftPlayTest:MonoBehaviour
    {
        [Serializable]public sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();public List<string> reactions=new List<string>();public int casts;}
        public int OnlySkill=-1;
        readonly Report report=new Report();readonly List<EnemyInstance> actors=new List<EnemyInstance>();ARSkillCaster caster;ARBattlefield field;MockGestureSource mock;string profileBefore;string current;int damageHits;int originalFps;bool originalCapture;
        void Check(bool ok,string message){(ok?report.passed:report.failed).Add(message);Debug.Log("AR "+(ok?"PASS ":"FAIL ")+message);Save();}
        void Save(){Directory.CreateDirectory("task/ar/goiA");File.WriteAllText("task/ar/goiA/ar-results.json",JsonUtility.ToJson(report,true));}
        void Hit(MonsterVitality m,DamageInfo hit){if(hit.skillId==current&&hit.source==DamageSource.Skill&&m.GetComponent<ARCombatContext>()!=null)damageHits++;}
        void React(ReactionType type,MonsterVitality target){report.reactions.Add(type.ToString());}
        IEnumerator Start()
        {
            caster=FindAnyObjectByType<ARSkillCaster>();field=caster.field;mock=FindAnyObjectByType<MockGestureSource>();mock.enabled=false;
            profileBefore=JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data);
            MonsterVitality.AnyDamaged+=Hit;ReactionResolver.ReactionTriggered+=React;
            var director=field.GetComponent<ARMonsterDirector>();while(director.Actors.Count==0)yield return null;var archetype=director.Actors[0].archetype;director.enabled=false;
            for(int i=0;i<6;i++){var e=EnemyPool.Ensure().SpawnAR(archetype,field.Root,field.Root.position,field);e.Vitality.SetMaxHealth(10000,true);e.GetComponent<ARMinionBrain>().enabled=false;actors.Add(e);}
            field.Shrine.SetProgressionMaxHealth(10000);field.Shrine.Revive(1,0);caster.Caster.GetComponent<PlayerStats>().suppressCrit=true;
            if(OnlySkill<0)Check(caster.Caster.GetComponents<SkillRuntime>().Length==5,"Five original runtimes on independent caster");
            for(int index=0;index<5;index++)
            {
                if(OnlySkill>=0&&index!=OnlySkill)continue;
                Arrange();Reset();current=GestureSkillMapper.Ids[index];damageHits=0;int fired=caster.Fired;
                yield return Release();yield return Hold(GestureSkillMapper.Labels[index],.45f);
                Check(caster.Fired==fired+1&&caster.LastSkill==current,"Gesture routes "+current+" ("+caster.Feedback+")");
                yield return new WaitForSeconds(index==0?.5f:index==1?1.1f:.4f);
                ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/combat-"+current+".png");
                yield return Hold(GestureSkillMapper.Labels[index],4.3f);
                Check(damageHits>0,current+" damages AR actors");Check(caster.Fired==fired+1,current+" held does not repeat");
            }
            if(OnlySkill>=0){report.casts=caster.Fired;Save();File.WriteAllText("task/ar/goiA/AR-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);yield break;}
            int unchanged=caster.Fired;
            yield return Release();yield return Hold("None",.7f);yield return Hold("Open_Palm",.7f,new Vector2(.02f,.5f));yield return Hold("Victory",.7f,new Vector2(.5f,.5f),.02f);yield return Hold("ILoveYou",.7f);
            Check(caster.Fired==unchanged,"None / edge / tiny hand / unmapped never fire");
            Arrange();Reset();yield return Release();yield return Hold("Thumb_Down",.45f);yield return new WaitForSeconds(.2f);yield return Release();yield return Hold("Pointing_Up",.45f);yield return new WaitForSeconds(.8f);
            Check(report.reactions.Contains("IceLightning"),"Freeze + lightning = IceLightning");
            Arrange();Reset();yield return Release();yield return Hold("Open_Palm",.45f);yield return new WaitForSeconds(.8f);yield return Release();yield return Hold("Victory",.45f);yield return new WaitForSeconds(3.7f);
            Check(report.reactions.Contains("ArmorShatter"),"Stun + metal = ArmorShatter");
            Arrange();Reset();yield return Release();yield return Hold("Closed_Fist",.45f);yield return Release();yield return Hold("Victory",.45f);yield return new WaitForSeconds(3.8f);
            Check(report.reactions.Contains("Convergence"),"Pulled + area = Convergence");
            Arrange();Reset();var bolt=caster.Caster.GetComponent<ChainLightningRuntime>();
            bool accepted=bolt.CastAt(field.Root.position);bolt.enabled=false;
            // One 0.7s update exercises catch-up rather than six normal frames.
            typeof(Set1SkillRuntime).GetField("elapsed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(bolt,.7f);
            typeof(ChainLightningRuntime).GetMethod("TickCast",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(bolt,new object[]{.7f});
            Check(accepted&&bolt.BounceCount==6,"Low FPS: one 0.7 second tick catches all six scheduled bounces");bolt.enabled=true;
            field.UserPaused=true;yield return null;float clock=field.Clock,cooldown=bolt.CooldownRemaining;yield return new WaitForSeconds(.3f);
            Check(Mathf.Approximately(clock,field.Clock)&&Mathf.Approximately(cooldown,bolt.CooldownRemaining)&&Time.timeScale==1,"AR pause freezes skill clock without global timeScale");field.UserPaused=false;
            Check(profileBefore==JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data),"Profile and normal loadout unchanged during AR casts/reactions");
            report.casts=caster.Fired;caster.enabled=false;yield return null;
            Check(caster.Caster==null,"Caster context disposed (no global unlock override)");
            Check(profileBefore==JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data),"Profile/loadout unchanged after AR disposal");
            Save();File.WriteAllText("task/ar/goiA/AR-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(this);
        }
        void Arrange()
        {
            for(int i=0;i<actors.Count;i++){var e=actors[i];e.Status.Clear();e.Vitality.ResetVitality();var agent=e.GetComponent<NavMeshAgent>();Vector3 p=field.Root.TransformPoint(new Vector3((i%3-1)*1.2f,0,(i/3-.5f)*1.1f));if(agent.enabled&&agent.isOnNavMesh){agent.Warp(p);agent.isStopped=true;}else e.transform.position=p;}
            Physics.SyncTransforms();
        }
        void Reset(){typeof(ARSkillCaster).GetMethod("ResetEnergy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(caster,null);foreach(var runtime in caster.Caster.GetComponents<SkillRuntime>())runtime.ReadyOnRestEquip();caster.Caster.GetComponent<SpiritPower>().Refill();caster.Caster.GetComponent<SkillVfxPool>().Clear();}
        IEnumerator Release()
        {
            // XR startup can exceed D1's 150ms continuity limit. Wait for a real fresh
            // released baseline before measuring the gesture, without changing D1.
            float start=Time.unscaledTime,deadline=start+8;
            do{mock.Emit("None",new Vector2(.5f,.5f),1,.18f);yield return new WaitForSeconds(.07f);}while(Time.unscaledTime<start+.8f||!caster.gestures.ReleaseReady&&Time.unscaledTime<deadline);
        }
        IEnumerator Hold(string label,float seconds,Vector2? pos=null,float size=.18f)
        {float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until){mock.Emit(label,pos??new Vector2(.5f,.5f),1,size);yield return new WaitForSeconds(.07f);}}
        void OnDestroy(){MonsterVitality.AnyDamaged-=Hit;ReactionResolver.ReactionTriggered-=React;}
    }
}
#endif
