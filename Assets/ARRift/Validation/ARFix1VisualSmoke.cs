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
    // A single visual capture session and focused follow-up to the two failed combo checks.
    public sealed class ARFix1VisualSmoke:MonoBehaviour
    {
        [Serializable] public class Evidence { public List<string> captures=new List<string>(),reactions=new List<string>();public bool armorShatter,convergence,placementHidden,shrineHit,shrineBroken;public int textIssues; }
        Evidence report=new Evidence(); ARBattlefield field;ARSkillCaster caster;List<EnemyInstance> actors=new List<EnemyInstance>();
        const string Output="task/ar/screens/fix1/";
        IEnumerator Start()
        {
            field=FindAnyObjectByType<ARBattlefield>();caster=field.GetComponent<ARSkillCaster>();
            var director=field.GetComponent<ARMonsterDirector>();var archetype=director.Actors[0].archetype;director.enabled=false;
            FindAnyObjectByType<MockGestureSource>().enabled=false;
            for(int i=0;i<6;i++){var e=EnemyPool.Ensure().SpawnAR(archetype,field.Root,field.Root.position,field);e.Vitality.SetMaxHealth(10000,true);e.GetComponent<ARMinionBrain>().enabled=false;actors.Add(e);}
            field.Shrine.SetProgressionMaxHealth(300);field.Shrine.Revive(1,0);caster.Caster.GetComponent<PlayerStats>().suppressCrit=true;
            ReactionResolver.ReactionTriggered+=Reaction;
            for(int i=0;i<5;i++)
            {
                Arrange();Ready();Cast(i);yield return new WaitForSeconds(i==0?.65f:i==1?1.1f:i==2?.35f:i==3?1.4f:.55f);
                yield return Shot("m5-"+GestureSkillMapper.Ids[i]);yield return new WaitForSeconds(4.5f);
            }
            Arrange();Ready();report.reactions.Clear();Cast(0);yield return new WaitForSeconds(1.0f);Cast(3);yield return new WaitForSeconds(4.4f);report.armorShatter=report.reactions.Contains("ArmorShatter");
            Arrange();Ready();report.reactions.Clear();Cast(1);yield return new WaitForSeconds(.35f);Cast(3);yield return new WaitForSeconds(4.4f);report.convergence=report.reactions.Contains("Convergence");
            caster.Caster.GetComponent<SkillVfxPool>().Clear();foreach(var e in actors)EnemyPool.Instance.Release(e);actors.Clear();
            var panel=field.transform.Find("AR Battle HUD/AR HUD content/Placement guidance");var modes=field.transform.Find("AR Battle HUD/AR HUD content/Placement mode");report.placementHidden=panel!=null&&!panel.gameObject.activeSelf&&modes!=null&&!modes.gameObject.activeSelf;
            yield return Shot("hud-rune-shrine");
            var camera=FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();camera.transform.position=field.Shrine.transform.position+new Vector3(.32f,.42f,.46f);camera.transform.LookAt(field.Shrine.transform.position+Vector3.up*.12f);
            var info=DamageInfo.Create(120,Element.None,DamageSource.Melee,field.Shrine.transform.position,Vector3.forward);info.ignoreInvulnerability=true;report.shrineHit=field.Shrine.ApplyDamage(info);yield return new WaitForSeconds(.1f);yield return Shot("shrine-hit");
            info.amount=1000;field.Shrine.ApplyDamage(info);yield return new WaitForSeconds(.55f);report.shrineBroken=field.Shrine.GetComponent<ARShrineVisual>().Broken;yield return Shot("shrine-broken");
            field.Shrine.Revive(1,0);
            camera.transform.position=field.Root.position+new Vector3(.55f,1,1.15f);camera.transform.LookAt(field.Root.position+Vector3.up*.1f);yield return null;
            var audit=CampusRift.UI.ComicTextAudit.Scan("AR fix1 HUD");CampusRift.UI.ComicTextAudit.Save(audit,"task/ar/fix1/text-audit.json");report.textIssues=audit.issues.Count;
            File.WriteAllText("task/ar/fix1/visual.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/ar/fix1/VISUAL-DONE.txt","Complete");Destroy(this);
        }
        void Reaction(ReactionType type,MonsterVitality target){report.reactions.Add(type.ToString());}
        void Arrange()
        {
            for(int i=0;i<actors.Count;i++){var e=actors[i];e.Status.Clear();e.Vitality.ResetVitality();var a=e.GetComponent<NavMeshAgent>();var p=field.Root.TransformPoint(new Vector3((i%3-1)*1.2f,0,(i/3-.5f)*1.1f));if(a.enabled&&a.isOnNavMesh){a.Warp(p);a.isStopped=true;}else e.transform.position=p;}Physics.SyncTransforms();
        }
        void Ready(){foreach(var r in caster.Caster.GetComponents<SkillRuntime>())r.ReadyOnRestEquip();caster.Caster.GetComponent<SpiritPower>().Refill();caster.Caster.GetComponent<SkillVfxPool>().Clear();}
        void Cast(int i){var runtime=caster.Runtime(GestureSkillMapper.Labels[i]);if(i==0)caster.Caster.GetComponent<GiantHandSkill>().CastAt(field.Root.position);else ((Set1SkillRuntime)runtime).CastAt(field.Root.position);}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+name+".png");report.captures.Add(name);yield return null;}
        void OnDestroy(){ReactionResolver.ReactionTriggered-=Reaction;}
    }
}
#endif
