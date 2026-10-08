#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CampusRift.Enemies;
using CampusRift.Combat;
using CampusRift.Levels;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    public sealed class EnemyAbilitiesPlayTest : P12PlayTest
    {
        public bool SkipStatistics; // TEST-POLICY: P23 runs functional assertions, omits 50-trial balance statistics.
        EnemyInstance Spawn(string id,int level,Vector3 offset){var a=AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");var s=EnemyScaling.Default;s.level=level;return EnemyPool.Ensure().Spawn(a,world.origin+offset,s);}
        protected override IEnumerator Run(){Begin();var p=world.player.GetComponent<Monsters.PlayerMonsterHealth>();
            var t=Spawn("tieu-yeu",3,new Vector3(6,0,0));Check(!t.GetComponent<EnemyAbilityRunner>().Unlocked("leap"),"TieuYeu L3 leap locked");EnemyPool.Instance.Release(t);
            t=Spawn("tieu-yeu",4,new Vector3(6,0,0));t.Brain.enabled=false;var r=t.GetComponent<EnemyAbilityRunner>();Check(r.Unlocked("leap"),"TieuYeu L4 leap unlocked");
            var leap=t.archetype.abilities[0].ability;r.Force(leap,world.player.transform);var from=t.transform.position;yield return new WaitForSeconds(.4f);Check(Vector3.Distance(from,t.transform.position)<.1f,"leap holds through .6s warning");yield return new WaitForSeconds(1.3f);Check(r.Executions==1&&Vector3.Distance(from,t.transform.position)>4,"leap moves up to6m after warning");EnemyPool.Instance.ReleaseAll();
            world.PlacePlayer(world.origin);p.Revive(1,0);while(p.Invulnerable)yield return null;
            var leapWall=GameObject.CreatePrimitive(PrimitiveType.Cube);leapWall.transform.position=world.origin+new Vector3(4,1,0);leapWall.transform.localScale=new Vector3(.4f,2,4);Physics.SyncTransforms();
            t=Spawn("tieu-yeu",4,new Vector3(6,0,0));t.Brain.enabled=false;r=t.GetComponent<EnemyAbilityRunner>();float leapHealth=p.CurrentHealth;from=t.transform.position;r.Force(leap,world.player.transform);yield return new WaitForSeconds(1.8f);
            Check(t.transform.position.x>leapWall.transform.position.x+.3f&&Mathf.Approximately(p.CurrentHealth,leapHealth),"blocked leap lands before wall and cannot damage at intended landing behind it");
            Check(t.Animation.AirborneHeight==0,"blocked leap clears airborne visual offset");Destroy(leapWall);EnemyPool.Instance.ReleaseAll();yield return null;
            var heavy=Spawn("thiet-giap-nguu",5,new Vector3(8,0,0));heavy.Brain.enabled=false;var charge=heavy.archetype.abilities[0].ability;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.position=world.origin+new Vector3(4,1,0);cube.transform.localScale=new Vector3(.4f,2,4);Physics.SyncTransforms();
            r=heavy.GetComponent<EnemyAbilityRunner>();r.Force(charge,world.player.transform);from=heavy.transform.position;yield return new WaitForSeconds(.8f);Check(Vector3.Distance(from,heavy.transform.position)<.1f,"charge holds1s warning");yield return new WaitForSeconds(1.4f);Check(heavy.transform.position.x>cube.transform.position.x+.3f,"charge14m/s stops at solid wall");Destroy(cube);EnemyPool.Instance.ReleaseAll();yield return null;
            var wallPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Skills/VoidWall/VoidWall.prefab");
            foreach(int level in new[]{5,6}){var wall=Instantiate(wallPrefab).GetComponent<VoidWall>();wall.Deploy(world.origin+Vector3.right*4,Quaternion.Euler(0,90,0),4,null,500);heavy=Spawn("thiet-giap-nguu",level,new Vector3(8,0,0));heavy.Brain.enabled=false;r=heavy.GetComponent<EnemyAbilityRunner>();yield return new WaitForSeconds(.2f);r.Force(charge,world.player.transform);yield return new WaitForSeconds(2);Check(level==5?wall.IsSolid:wall.WasBroken,"charge VoidWall "+(level==5?"blocked before6":"breaks instantly at6"));wall.Dissolve(false);Destroy(wall.gameObject);EnemyPool.Instance.ReleaseAll();yield return null;}
            world.PlacePlayer(world.origin);var bomber=Spawn("bao-thi",6,new Vector3(3,0,0));bomber.Brain.enabled=false;var victim=Spawn("tieu-yeu",6,new Vector3(4,0,0));victim.Brain.enabled=false;float initial=victim.Vitality.Health;float playerInitial=p.CurrentHealth;r=bomber.GetComponent<EnemyAbilityRunner>();
            bomber.Vitality.ApplyDamage(DamageInfo.Create(99999,Element.None,DamageSource.Skill,bomber.transform.position,Vector3.right));yield return new WaitForSeconds(.25f);Check(r.Explosions==0,"bomber death warning .4s before explosion");yield return new WaitForSeconds(.3f);
            Check(r.Explosions==1&&Mathf.Abs(initial-victim.Vitality.Health-15)<.01f&&p.CurrentHealth<playerInitial,"bomberR3.5 real30Reaction / half damage to monsters");Measure("Explosion victim damage="+(initial-victim.Vitality.Health)+", player damage="+(playerInitial-p.CurrentHealth)+", player distance="+Vector3.Distance(bomber.transform.position,world.player.transform.position));Check(r.ImpactAt-r.WarningAt>=.39f,"bomber explosion timing");EnemyPool.Instance.ReleaseAll();
            var doc=Spawn("doc-nhan",5,new Vector3(8,0,0));doc.Brain.enabled=false;r=doc.GetComponent<EnemyAbilityRunner>();int before=EnemyProjectilePool.Ensure().TotalFired;r.Force(doc.archetype.abilities[0].ability,world.player.transform);float timeout=Time.time+3;while(r.Executions==0&&Time.time<timeout)yield return null;Check(EnemyProjectilePool.Ensure().TotalFired==before+3,"DocNhan L5 three fan bolts at impact");int created=EnemyProjectilePool.Instance.CreatedCount;EnemyPool.Instance.ReleaseAll();Check(EnemyProjectilePool.Instance.ActiveCount==0&&EnemyProjectilePool.Instance.CreatedCount==created,"level/pool cleanup clears lingering bolts without recreating pool");
            if(SkipStatistics){Measure("50-trial balance statistics omitted per TEST-POLICY; functional assertions above unchanged");yield break;}
            int attempts=0,dodges=0;UnityEngine.Random.InitState(4201);
            for(int i=0;i<50;i++){var e=Spawn("tieu-yeu",3,new Vector3(8,0,10));var scaling=e.scaling;scaling.aiTier=2;e.Configure(e.archetype,scaling);yield return new WaitForSeconds(e.Animation.SpawnSeconds+.05f);
                e.Motor.Stop();int token=DangerZoneRegistry.Register(e.transform.position,.6f,1);yield return new WaitForSeconds(.35f);attempts+=e.Brain.DodgeAttempts;dodges+=e.Brain.Dodges;
                if(e.Brain.Dodges>0){int old=e.Brain.Dodges;DangerZoneRegistry.Remove(token);token=DangerZoneRegistry.Register(e.transform.position,.6f,.25f);yield return new WaitForSeconds(.2f);Check(e.Brain.Dodges==old,"T2 trial "+i+" three-second cooldown");}
                DangerZoneRegistry.Remove(token);EnemyPool.Instance.Release(e);yield return null;}
            Check(attempts==50&&dodges>=15&&dodges<=25,"T2 actual 50 zones: "+dodges+"/"+attempts+" dodges (40%±10%)");Measure("T2 "+dodges+"/"+attempts+" real navigation sidesteps; seed4201; cooldown3s; no synthetic roll-only test");
        }
    }
}
#endif
