#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P11_BENCH)
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Enemies;
using CampusRift.Monsters;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CampusRift.Combat
{
    public sealed class ReactionTestWorld
    {
        public readonly SkillSet1TestWorld world=new SkillSet1TestWorld{GameplayCamera=true};
        public readonly MonsterVitality[] crowd=new MonsterVitality[12];
        readonly List<FlyingSword> hidden=new List<FlyingSword>();
        public bool hidePassive=true;
        public Vector3 Center=>world.origin+new Vector3(9,0,2);
        public void Begin()
        {
            world.Begin();for(int i=0;i<7;i++)crowd[i]=world.victims[i];
            var template=SkillSet1TestWorld.RuntimeEnemyTemplate;
#if UNITY_EDITOR
            template=AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
#endif
            for(int i=7;i<crowd.Length;i++)
            {
                var e=EnemyPool.Ensure().Spawn(template,Center,EnemyScaling.Default,false);
                if(e==null)throw new System.InvalidOperationException("P11 crowd NavMesh fixture failed");
                if(e.Brain!=null){var motor=e.GetComponent<MinionMotor>();var animator=motor.Animator;if(animator!=null)animator.transform.localScale=(Vector3)typeof(MinionBrain).GetField("modelScale",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(e.Brain);e.Brain.enabled=false;}
                var combat=e.GetComponent<MonsterCombat>();if(combat!=null)combat.enabled=false;e.GetComponent<MinionMotor>().Stop();
                crowd[i]=e.GetComponent<MonsterVitality>();crowd[i].SetMaxHealth(100000,true);crowd[i].defense=0;crowd[i].Element=Element.None;
            }
            if(hidePassive)foreach(var sword in world.player.GetComponent<PlayerCombat>().Swords)if(sword!=null&&sword.gameObject.activeSelf){hidden.Add(sword);sword.gameObject.SetActive(false);}
            Arrange();
        }
        public void Arrange()
        {
            world.PlacePlayer(world.origin);world.Look(90,14);
            for(int i=0;i<crowd.Length;i++)
            {
                var m=crowd[i];m.GetComponent<StatusEffectHost>().Clear();m.ResetVitality();m.defense=0;m.Element=Element.None;m.resistHardControl=false;
                var motor=m.GetComponent<MinionMotor>();motor.Place(Center+new Vector3((i/4-1)*1.6f,0,(i%4-1.5f)*1.65f));motor.Stop();
            }
            Physics.SyncTransforms();world.player.GetComponent<GenerationChainTracker>().ResetChain();
        }
        public void Triple()
        {
            // Three simultaneous groups; gameplay resolver still evaluates all 12 targets.
            for(int i=0;i<crowd.Length;i++)
            {
                var m=crowd[i];var status=m.GetComponent<StatusEffectHost>();m.ResetVitality();status.Clear();
                int kind=i%3;status.Apply(kind==0?StatusType.Freeze:kind==1?StatusType.Burn:StatusType.Pulled,3);
                var info=DamageInfo.Create(100,kind==0?Element.Loi:Element.Hoa,DamageSource.Skill,m.transform.position+Vector3.up,Vector3.right,world.player.gameObject);info.attackPower=100;info.isArea=kind==2;info.skillId="triple-reaction-qa";m.ApplyDamage(info);
            }
        }
        public void End()
        {for(int i=7;i<crowd.Length;i++)if(crowd[i]!=null)EnemyPool.Instance.Release(crowd[i].GetComponent<EnemyInstance>());world.End();foreach(var sword in hidden)if(sword!=null)sword.gameObject.SetActive(true);}
    }
}
#endif
