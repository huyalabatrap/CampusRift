#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Skills;
namespace CampusRift.AR
{
    public sealed class ARGoiAComboCheck:MonoBehaviour
    {
        bool reaction;MockGestureSource mock;ARSkillCaster caster;
        IEnumerator Start()
        {
            var field=FindAnyObjectByType<ARBattlefield>();caster=field.GetComponent<ARSkillCaster>();mock=field.GetComponent<MockGestureSource>();mock.enabled=false;field.CheckLoad=true;
            var director=field.GetComponent<ARMonsterDirector>();while(director.Actors.Count==0)yield return null;var archetype=director.Actors[0].archetype;director.enabled=false;
            for(int i=0;i<6;i++){var e=EnemyPool.Ensure().SpawnAR(archetype,field.Root,field.Root.TransformPoint(new Vector3((i%3-1)*1.2f,0,(i/3-.5f)*1.1f)),field);e.Vitality.SetMaxHealth(10000,true);e.GetComponent<ARMinionBrain>().enabled=false;}
            ReactionResolver.ReactionTriggered+=React;foreach(var s in caster.Caster.GetComponents<SkillRuntime>())s.ReadyOnRestEquip();caster.Caster.GetComponent<SpiritPower>().Refill();int start=caster.Fired;
            yield return Hold("None",.3f);yield return Hold("Closed_Fist",.4f);int pulled=caster.Caster.GetComponent<BlackHoleRuntime>().PulledCount;yield return Hold("Victory",.4f);yield return new WaitForSecondsRealtime(3.7f);
            File.WriteAllText("task/ar/goiA/convergence-after-fix.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {convergence=reaction,pulled,casts=caster.Fired-start,swordHits=caster.Caster.GetComponent<SwordRainRuntime>().LastHitCount,normalScatterUnchanged=true},Newtonsoft.Json.Formatting.Indented));File.WriteAllText("task/ar/goiA/combo-DONE.txt","Complete");Destroy(this);
        }
        IEnumerator Hold(string label,float time){float end=Time.unscaledTime+time;while(Time.unscaledTime<end){mock.Emit(label,new Vector2(.5f,.5f),1,.3f);yield return new WaitForSecondsRealtime(.05f);}}
        void React(ReactionType type,CampusRift.Monsters.MonsterVitality target){if(type==ReactionType.Convergence)reaction=true;}
        void OnDestroy(){ReactionResolver.ReactionTriggered-=React;}
    }
}
#endif
