#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
    public sealed class StabilizeModelsSmoke:P12PlayTest
    {
        EnemyInstance Spawn(string id,Vector3? at=null)
        {
            var a=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");
            var e=EnemyPool.Ensure().Spawn(a,at??world.origin+Vector3.right*7,LevelCatalog.Instance.Get(10).Scaling);
            e.Brain.enabled=false;e.Motor.Stop();e.GetComponent<EnemyConcealment>()?.RevealFor(60);var fly=e.GetComponent<FlyingMotor>();if(fly!=null)fly.Manual=true;return e;
        }
        void Clean(){EnemyPool.Instance.ReleaseAll();EnemyTelegraph.Clear();world.PlacePlayer(world.origin);}
        static void Kill(EnemyInstance e){e.Vitality.ApplyDamage(DamageInfo.Create(1e7f,Element.None,DamageSource.Skill,e.transform.position,Vector3.forward));}
        protected override IEnumerator Run()
        {
            Begin();world.Mode(false);world.Lighting(false);world.player.enabled=false;
            var hp=world.player.GetComponent<PlayerMonsterHealth>();hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);hp.respawnOnDefeat=false;
            foreach(string id in new[]{"duc-yeu","anh-yeu"})
            {
                var e=Spawn(id);yield return new WaitForSeconds(.95f);e.Animation.Play(e.Animation.profile.idle,1,5);yield return null;
                var skins=e.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name!="Elite outline").ToArray();var bounds=skins[0].bounds;
                foreach(var r in skins)bounds.Encapsulate(r.bounds);
                Measure(id+" bounds="+bounds+" capsuleRadius="+e.GetComponent<CapsuleCollider>().radius);
                Check(e.Alive&&skins.All(r=>r.sharedMesh!=null&&r.sharedMaterials.All(m=>m!=null&&m.shader.name!="Hidden/InternalErrorShader")),id+" actual idle / material");
                var group=e.GetComponent<LODGroup>();Check(group.lodCount==3,id+"3LOD preserved");
                for(int i=0;i<3;i++){group.ForceLOD(i);yield return null;Check(group.GetLODs()[i].renderers.All(r=>r!=null&&r.bounds.size.sqrMagnitude>.0001f),id+"LOD"+i+" renders weighted mesh");}group.ForceLOD(-1);
                if(id=="duc-yeu")
                {
                    float wingspan=Mathf.Max(bounds.size.x,bounds.size.z);Check(wingspan>=2.5f&&wingspan<=3.5f,"Bat live wingspan2.5–3.5m (world horizontal major axis)");
                    Check(Mathf.Abs(e.GetComponent<CapsuleCollider>().radius-.35f)<.001f&&e.GetComponent<FlyingMotor>().Height>=3,"Bat body hitbox and3–6m flight height preserved");
                    var special=e.GetComponent<ExpandedEnemyRuntime>();int hits=hp.DamageCount;special.Force("dive",world.player.transform);yield return new WaitForSeconds(.25f);
                    Check(FindObjectsByType<EnemyTelegraph>().Any(t=>t.Live&&Mathf.Abs(t.Radius-2)<.001f),"Bat live dive warning R2 remains matched to landing attack");world.PlacePlayer(world.origin+Vector3.forward*6);yield return new WaitForSeconds(1.1f);
                    Check(special.Dives==1&&special.ImpactAt-special.WarningAt>=.59f&&hp.DamageCount==hits,"Rescaled Bat real0.6s dive can be dodged");
                }
                else
                {
                    Check(new[]{"Torn shadow cloak","Shadow hood","Crimson face scarf","Shadow bone dagger"}.All(n=>skins.Any(s=>s.name==n)),"Shadow all4 weighted accessories present");
                    Check(skins.All(s=>s.sharedMesh.boneWeights.All(w=>w.weight0+w.weight1+w.weight2+w.weight3>.99f)),"Shadow accessories have no unweighted vertices");
                    var bones=e.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                    foreach(string side in new[]{"L","R"})Check(Vector3.Dot((bones["LowerArm."+side].position-bones["UpperArm."+side].position).normalized,Vector3.down)>.75f,"Shadow idle arm"+side+" lowered out of A pose");
                    var special=e.GetComponent<ExpandedEnemyRuntime>();special.Force("teleport",world.player.transform);yield return new WaitForSeconds(.2f);Vector3 committed=special.WarnedPoint;world.PlacePlayer(world.origin+Vector3.forward*6);yield return new WaitForSeconds(.8f);
                    Check(special.Teleports==1&&Vector3.Distance(e.transform.position,committed)<.25f&&special.ImpactAt-special.WarningAt>=.49f,"Shadow new skins preserve0.5s committed teleport");
                    var conceal=e.GetComponent<EnemyConcealment>();conceal.ResetLife();world.PlacePlayer(world.origin+Vector3.forward*20);yield return null;yield return null;
                    Check(conceal.Hidden&&skins.All(s=>s.forceRenderingOff),"Shadow cloak/hood/scarf/knife conceal with body");conceal.RevealFor(3);yield return null;Check(!conceal.Hidden&&skins.All(s=>!s.forceRenderingOff),"Reveal restores all new accessories");
                }
                float before=e.Vitality.Health;world.player.GetComponent<PlayerCombat>().ResolveHit(e.Vitality,1,false);Check(e.Vitality.Health<before,id+" actual hit accepted");Kill(e);yield return new WaitForSeconds(1.6f);Check(!e.gameObject.activeSelf,id+" death/dissolve/pool");Clean();
            }
            var elite=Spawn("anh-yeu");elite.Elite.Configure(10,21,EliteAffixKind.Berserk,EliteAffixKind.FireHeart);yield return new WaitForSeconds(1);
            world.camera.transform.position=world.origin+new Vector3(7,2.7f,-8);world.camera.transform.LookAt(elite.transform.position+Vector3.up);yield return null;
            Check(elite.GetComponent<FireEnemyState>().Immune,"FireHeart immunity retained after cosmetic change");
            var visual=elite.GetComponent<EliteFireHeartVisual>();var flame=visual.Flame;var particles=new ParticleSystem.Particle[24];int count=flame.GetParticles(particles);
            var bodyBounds=elite.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name!="Elite outline").Select(s=>s.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});bodyBounds.Expand(bodyBounds.size*.2f);
            bool bounded=count>0;for(int i=0;i<count;i++)bounded&=bodyBounds.Contains(flame.transform.TransformPoint(particles[i].position));
            Check(bounded&&flame.GetComponent<ParticleSystemRenderer>().maxParticleSize<=.025f,"Live FireHeart particles stay within1.2× body and screen-size cap");Measure("FireHeart live particles="+count+" footRadius="+visual.FootRadius);
            var label=EnemyHealthBars.Instance.GetComponentsInChildren<TextMeshPro>().First(t=>t.text.Contains("Ảnh Yêu"));label.ForceMeshUpdate();
            var glyph=label.textInfo.characterInfo.First(c=>char.IsLetter(c.character));float pixels=Mathf.Abs(world.camera.WorldToScreenPoint(label.transform.TransformPoint(glyph.topRight)).y-world.camera.WorldToScreenPoint(label.transform.TransformPoint(glyph.bottomLeft)).y);Measure("Elite projected letter height="+pixels+"px; lines="+label.textInfo.lineCount);
            Check(label.isActiveAndEnabled&&label.textInfo.lineCount==2&&!label.isTextTruncated&&pixels>=18,"Elite close nameplate two readable lines / letter>=18px");
            EnemyHealthBars.Instance.SetLocked(null);world.camera.transform.position=world.origin+new Vector3(7,3,-35);yield return null;yield return null;Check(!label.gameObject.activeInHierarchy,"Far elite hides full nameplate");EnemyHealthBars.Instance.SetLocked(elite.Vitality);yield return null;Check(label.gameObject.activeInHierarchy,"Lock shows full nameplate at range");
            Clean();world.player.enabled=true;
        }
        protected override void Cleanup(){world.player.enabled=true;base.Cleanup();}
    }
}
#endif
