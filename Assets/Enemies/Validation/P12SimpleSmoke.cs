#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    // One short feature smoke under task/TEST-POLICY.md. No balance or statistical trials.
    public sealed class P12SimpleSmoke : P12PlayTest
    {
        IEnumerator Shot(string name){Directory.CreateDirectory("task/p12/screens/final-smoke");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("task/p12/screens/final-smoke/"+name+".png");yield return null;}
        protected override IEnumerator Run()
        {
            Begin();world.Mode(true);world.Look(90,14);
            var player=world.player.GetComponent<PlayerMonsterHealth>();var combat=world.player.GetComponent<PlayerCombat>();
            foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster)
            {
                var e=EnemyPool.Ensure().Spawn(row.archetype,world.origin+Vector3.right*6,EnemyScaling.Default);
                Check(e!=null&&e.Alive&&e.Animation.CurrentState=="Spawn",row.archetype.id+" spawns with animation");
                e.Brain.enabled=false;yield return new WaitForSeconds(1.1f);float hp=e.Vitality.Health;
                combat.ResolveHit(e.Vitality,combat.config.chainPercent[2],true);
                Check(e.Vitality.Health<hp,row.archetype.id+" real sword hit responds");
                if(e.Alive)e.Vitality.ApplyDamage(DamageInfo.Create(e.Vitality.Health/(1-e.Vitality.defense)+1,Element.None,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject));
                yield return new WaitForSeconds(1.8f);Check(!e.Alive,row.archetype.id+" dies and returns to pool");EnemyPool.Instance.ReleaseAll();
            }
            var heavy=LevelCatalog.Instance.Get(10).spawnTable.roster.Find(x=>x.archetype.id=="thiet-giap-nguu").archetype;
            foreach(float side in new[]{1.2f,1.4f})
            {
                world.PlacePlayer(world.origin);player.Revive(1,0);while(player.Invulnerable)yield return null;
                var e=EnemyPool.Instance.Spawn(heavy,world.origin+Vector3.right*6,EnemyScaling.Default);e.Brain.enabled=false;
                var runner=e.GetComponent<EnemyAbilityRunner>();int before=player.DamageCount;runner.Force(e.archetype.abilities[0].ability,world.player.transform);
                yield return new WaitForSeconds(.4f);world.PlacePlayer(world.origin+Vector3.forward*side);world.Look(90,14);
                if(side<1.3f)yield return Shot("charge-warning");yield return new WaitForSeconds(1.3f);
                Check(side<1.3f?player.DamageCount>before:player.DamageCount==before,"charge real hit edge "+side+"m matches warning");EnemyPool.Instance.ReleaseAll();
            }
            world.PlacePlayer(world.origin);player.Revive(1,0);while(player.Invulnerable)yield return null;
            var boss=EnemyPool.Instance.Spawn(LevelCatalog.Instance.Get(7).bosses[0],world.origin+Vector3.right*6,LevelCatalog.Instance.Get(7).Scaling);
            var b=boss.GetComponent<BossController>();int oldHits=player.DamageCount;b.Force(BossAttack.ShadowDash);
            yield return new WaitForSeconds(.15f);world.PlacePlayer(world.origin+Vector3.forward*1.7f);world.Look(90,14);yield return Shot("shadow-warning");
            yield return new WaitForSeconds(.75f);b.Cancel();Check(player.DamageCount>oldHits,"shadow dash real hit at1.7m inside widened warning");EnemyPool.Instance.ReleaseAll();
            world.PlacePlayer(world.origin);player.Revive(1,0);
            var imp=EnemyPool.Instance.Spawn(LevelCatalog.Instance.Get(1).spawnTable.roster[0].archetype,world.origin+Vector3.right*2,EnemyScaling.Default);imp.Brain.enabled=false;imp.Motor.Stop();imp.Vitality.SetMaxHealth(10000,true);
            yield return new WaitForSeconds(1.1f);Vector3 center=world.origin+Vector3.right*6;float distance=Vector3.Distance(imp.transform.position,center);
            var hole=world.player.GetComponent<BlackHoleRuntime>();hole.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();
            Check(hole.CastAt(center),"Black Hole real cast responds");yield return new WaitForSeconds(1.4f);yield return Shot("black-hole");yield return new WaitForSeconds(1.4f);
            Vector3 grouped=imp.transform.position;Measure("Black Hole grouped="+grouped+" center="+center+" initialDistance="+distance+" remaining="+Vector3.Distance(grouped,center));
            Check(Vector3.Distance(grouped,center)<distance*.3f,"Black Hole actually gathers a live model");yield return new WaitForSeconds(.7f);
            Measure("Black Hole release="+imp.transform.position+" moved="+Vector3.Distance(grouped,imp.transform.position)+" navFailures="+hole.NavigationFailures);
            Check(Vector3.Distance(grouped,imp.transform.position)>3.7f&&imp.Motor.enabled&&imp.Motor.OnMesh&&!hole.IsCasting,"Black Hole expels and restores navigation");
        }
    }
}
#endif
