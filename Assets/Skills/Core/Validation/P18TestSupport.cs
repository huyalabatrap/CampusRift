#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public static class P18TestSupport
    {
        public static void SetRank(SkillRuntime skill,int rank)
        {var p=Progression.ProfileService.Instance;var entry=p.Data.skills.ranks.Find(x=>x.key==skill.Id);if(entry==null)p.Data.skills.ranks.Add(new Progression.KeyCount{key=skill.Id,count=rank});else entry.count=rank;p.UseTransient(p.Data);skill.ApplyRank(rank);}
        public static void Prepare(SkillSet1TestWorld w,bool line=false)
        {
            Time.timeScale=1;UI.UIStateManager.Instance.EnterScene(true);foreach(var s in w.player.GetComponents<SkillRuntime>())s.ReadyOnRestEquip();
            w.player.GetComponent<SkillMasteryFields>().Clear();w.player.GetComponent<SoulSummonRuntime>().ClearCorpsesForValidation();w.player.GetComponent<Enemies.PlayerEnemyControl>().Clear();EnemyProjectilePool.Instance?.Clear();
            foreach(var wall in VoidWall.Active.ToArray())wall.Dissolve(false);
            bool respawn=false;foreach(var m in w.victims)if(m==null||m.Defeated||!m.gameObject.activeSelf)respawn=true;
            if(respawn)
            {
                var template=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");foreach(var m in w.victims)if(m!=null)EnemyPool.Instance.Release(m.GetComponent<EnemyInstance>());
                for(int i=0;i<w.victims.Length;i++){var e=EnemyPool.Ensure().Spawn(template,w.origin+Vector3.right*(2+i),EnemyScaling.Default,false);if(e==null)throw new InvalidOperationException("Fixture respawn");e.Brain.enabled=false;var animator=e.Motor.Animator;if(animator!=null)animator.transform.localScale=(Vector3)typeof(MinionBrain).GetField("modelScale",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(e.Brain);e.Motor.Stop();w.victims[i]=e.Vitality;e.Vitality.SetMaxHealth(10000,true);e.Vitality.defense=0;}
            }
            w.Arrange(line);foreach(var m in w.victims){m.GetComponent<EnemyAnimationDriver>()?.Play("Idle_Alert");}w.Look(65,18);w.Lighting(false);w.player.GetComponent<SpiritPower>().Refill();w.player.GetComponent<PlayerMonsterHealth>().Revive(1,0);w.player.RefillEnergy();var pool=w.player.GetComponent<SkillVfxPool>();pool.Clear();pool.ResetMetrics();w.player.GetComponent<GenerationChainTracker>().ResetChain();DamageNumberPool.Instance?.ReactionLabels.Clear();w.player.GetComponent<ReactionFeedback>()?.ClearForValidation();
        }
        public static bool Casting(SkillRuntime s)
        {if(s is Set2SkillRuntime)return ((Set2SkillRuntime)s).IsCasting;if(s is Set1SkillRuntime)return ((Set1SkillRuntime)s).IsCasting;if(s is GiantHandRuntime)return s.GetComponent<GiantHandSkill>().IsCasting;if(s is PhantomRuntime)return s.GetComponent<PhantomDecoySkill>().LiveDecoys>0;return false;}
        public static IEnumerator FinishFast(SkillRuntime s)
        {Time.timeScale=s is LightningFlashRuntime?1:6;float deadline=Time.realtimeSinceStartup+9;while(Casting(s)&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSeconds(.7f);Time.timeScale=1;}
        public static IEnumerator Capture(string name)
        {System.IO.Directory.CreateDirectory("task/p18/screens");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("task/p18/screens/"+name+".png");yield return null;yield return null;}
        public static bool Close(float a,float b)=>Mathf.Abs(a-b)<Mathf.Max(.15f,Mathf.Abs(b)*.005f);
    }
}
#endif
