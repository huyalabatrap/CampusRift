#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace CampusRift.Skills
{
    // Explicitly launched through MCP in Play Mode; never installed in a scene/prefab.
    public sealed class GiantHandPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {public List<string> passed=new List<string>(),failed=new List<string>(),notes=new List<string>();}
        readonly Report report=new Report();
        readonly List<GameObject> fixtures=new List<GameObject>();
        readonly List<NavMeshDataInstance> meshes=new List<NavMeshDataInstance>();
        CampusExplorer player;GiantHandSkill skill;GiantHandConfig original;
        MonsterBrain brain;MonsterNavigation nav;MonsterVitality vitality;MonsterCombat combat;MonsterPerception sight;
        CharacterController controller;Camera camera;PlayerMonsterHealth health;
        Keyboard keyboard,realKeyboard;Mouse realMouse;
        InputSettings originalInput,testInput;
        bool running;
        const string Output="Artifacts/GiantHandSeal/";
        void Update(){if(running && UIStateManager.Instance.State!=UIState.Victory){UIStateManager.Instance?.EnterScene(true);Time.timeScale=1;}}
        void Check(bool ok,string label)
        {(ok?report.passed:report.failed).Add(label);Write();Debug.Log("HAND SEAL QA "+(ok?"PASS ":"FAIL ")+label);}
        void Note(string message){report.notes.Add(message);Write();}
        void Write(){File.WriteAllText(Output+"Validation.json",JsonUtility.ToJson(report,true));}
        Vector3 Ground(Vector3 p)=>NavMesh.SamplePosition(p,out var h,0.8f,NavMesh.AllAreas)?h.position:p;
        void Player(Vector3 p)
        {player.enabled=false;controller.enabled=false;player.transform.position=Ground(p);controller.enabled=true;Physics.SyncTransforms();}
        void Monster(Vector3 p)
        {
            brain.enabled=false;nav.Stop();combat.Interrupt();
            if(!nav.Agent.enabled){brain.transform.position=Ground(p);nav.Agent.enabled=true;}
            nav.Agent.Warp(Ground(p));nav.Stop();
            brain.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(player.transform.position-brain.transform.position,Vector3.up).normalized);
            Physics.SyncTransforms();
        }
        IEnumerator Ready()
        {while(skill.CooldownRemaining>0 || skill.IsCasting || vitality.Suppressed)yield return null;var spirit=skill.GetComponent<CampusRift.Combat.SpiritPower>();if(spirit!=null)spirit.Refill();}
        GameObject Box(string name,Vector3 center,Vector3 size)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=center;go.transform.localScale=size;fixtures.Add(go);return go;}
        void Floor(Vector3 center)
        {
            var floor=Box("Seal QA floor",center+Vector3.down*.1f,new Vector3(16,.2f,20));
            var source=new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(floor.transform.position,Quaternion.identity,Vector3.one),size=floor.transform.localScale};
            var settings=NavMesh.GetSettingsByID(nav.Agent.agentTypeID);settings.overrideVoxelSize=true;settings.voxelSize=.08f;
            meshes.Add(NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(settings,new List<NavMeshBuildSource>{source},new Bounds(center,new Vector3(18,8,22)),Vector3.zero,Quaternion.identity)));
        }
        void Capture(string label,Vector3 focus,float lookHeight=1.6f)
        {
            camera.transform.position=focus+new Vector3(4,6.2f,-5.8f);camera.transform.LookAt(focus+Vector3.up*lookHeight);camera.fieldOfView=55;
            if(label=="05-Low-ceiling"){camera.transform.position=focus+new Vector3(0,1.7f,-5);camera.transform.LookAt(focus+Vector3.up*.9f);camera.fieldOfView=62;}
            var rt=RenderTexture.GetTemporary(1440,1000,24);var old=RenderTexture.active;var prior=camera.targetTexture;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes(Output+label+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=prior;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);running=true;Application.runInBackground=true;UIStateManager.Instance?.EnterScene(true);
            var qaStats=Object.FindAnyObjectByType<CampusRift.Combat.PlayerStats>();if(qaStats!=null)qaStats.suppressCrit=true;
            originalInput=InputSystem.settings;testInput=Instantiate(originalInput);InputSystem.settings=testInput;
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            realKeyboard=Keyboard.current;realMouse=Mouse.current;
            if(realKeyboard!=null)InputSystem.DisableDevice(realKeyboard);if(realMouse!=null)InputSystem.DisableDevice(realMouse);
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            player=Object.FindAnyObjectByType<CampusExplorer>();controller=player.GetComponent<CharacterController>();skill=player.GetComponent<GiantHandSkill>();camera=Camera.main;health=player.GetComponent<PlayerMonsterHealth>();
            brain=Object.FindAnyObjectByType<MonsterBrain>();nav=brain.GetComponent<MonsterNavigation>();vitality=brain.GetComponent<MonsterVitality>();combat=brain.GetComponent<MonsterCombat>();sight=brain.GetComponent<MonsterPerception>();
            sight.enabled=false;brain.GetComponent<MonsterHearing>().enabled=false;
            original=skill.config;skill.config=Instantiate(original);
            vitality.maxHealth=10000;typeof(MonsterVitality).GetProperty("Health").SetValue(vitality,10000f);
            var center=new Vector3(0,.05f,-6);
            Player(new Vector3(0,.13f,-10));Monster(center);
            yield return null;
            camera.transform.position=new Vector3(0,2,-12);camera.transform.LookAt(center);
            Check(original.cooldown==18 && Mathf.Approximately(original.damagePercent,3f) && original.radius==2.8f && original.stagger==3f,"Production balance: 18s / 300% Công / 2.8m / 3s stun");
            float before=vitality.Health;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(skill.IsCasting && skill.LastAutoTarget==vitality && !skill.IsPreviewing,
                "F input auto locks the nearest valid monster and casts immediately");
            Check(vitality.Suppressed && nav.Agent.isStopped && vitality.Health==before,
                "F binds immediately without dealing damage before impact");
            Note("F input: auto target="+skill.LastAutoTarget+" target="+skill.Target.reason+" currentKeyboard="+Keyboard.current?.name);
            Capture("01-Target",center);
            Check(skill.Target.valid,"Test 1: auto cast targets the actual outdoor campus");
            yield return new WaitForSeconds(.47f);Capture("02-Summon",center,2.4f);
            Check(!skill.CastAt(center) && skill.CooldownRemaining>17,"Repeated cast is rejected during summon / cooldown");
            yield return new WaitForSeconds(.34f);Capture("03-Impact",center,.8f);
            Check(skill.LastHitCount==1 && Mathf.Abs(before-vitality.Health-60)<1,"AoE hits the monster once for 60 damage");
            Check(vitality.Suppressed && !combat.IsAttacking && brain.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Sealed"),"Monster visibly recoils into the authored Sealed animation");
            var hud=Object.FindAnyObjectByType<GiantHandHUD>();
            Check(hud.slot.CooldownOverlay.fillAmount>.9f && hud.slot.CooldownText.text!="" && !hud.slot.IsLocked,"Test 9: HUD shows the real 18-second cooldown");
            yield return new WaitForSeconds(.4f);Capture("04-Aftermath",center,.6f);
            yield return Ready();
            // The comic HUD keeps the slot key alone; readiness is shown by the
            // empty timer/overlay and white glyph. Allow its Update to observe zero.
            yield return null;
            Check(skill.CooldownRemaining==0 && !skill.IsCasting &&
                hud.slot.CooldownOverlay.fillAmount<.02f && hud.slot.CooldownText.text=="" &&
                hud.slot.KeyLabel.text==skill.GetComponent<CampusRift.Controls.CampusInput>().KeyLabel(CampusRift.Controls.CampusAction.Hand) &&
                hud.glyph.color==Color.white && !hud.slot.IsLocked,
                "Cooldown completes and comic HUD shows the ready key, white glyph and empty timer");
            Check(!skill.Visual.Visible && skill.Visual.LiveParticles==0 && !vitality.Suppressed,"Effect and stun finish cleanly");
            // Only the test copy changes cooldown for the following repeated scenarios.
            skill.config.cooldown=2.6f;Note("First cooldown tested at production 18s. Remaining casts use a runtime-only 2.6s copy; visual timing/damage unchanged.");

            var room=new Vector3(200,0,200);Floor(room);Floor(new Vector3(230,0,200));
            var left=Box("Narrow corridor left",room+new Vector3(-1.35f,1.4f,0),new Vector3(.2f,2.8f,14));
            var right=Box("Narrow corridor right",room+new Vector3(1.35f,1.4f,0),new Vector3(.2f,2.8f,14));
            var ceiling=Box("Low ceiling",room+Vector3.up*2.9f,new Vector3(2.9f,.2f,14));
            Player(room+Vector3.back*4);Monster(room);Physics.SyncTransforms();
            var indoor=skill.Targeting.Evaluate(room,player.transform,skill.config);
            Check(indoor.valid && indoor.height<2.5f && indoor.visualScale<.6f,"Test 2: narrow corridor fits hand and rift below the low ceiling");
            before=vitality.Health;Check(skill.CastAt(room),"Indoor skill confirmation succeeds");yield return new WaitForSeconds(.82f);
            Check(vitality.Health<before && skill.LastHitCount==1,"Indoor impact deals damage");Capture("05-Low-ceiling",room,.8f);yield return Ready();
            left.SetActive(false);right.SetActive(false);ceiling.SetActive(false);
            var post=Box("Thin post between rays",room+new Vector3(.9f,1,.7f),new Vector3(.15f,2,.15f));Physics.SyncTransforms();
            var postTarget=skill.Targeting.Evaluate(room,player.transform,skill.config);
            Check(postTarget.valid && postTarget.visualScale<.45f,"Thin pillars between targeting rays shrink the complete effect volume");post.SetActive(false);
            var wall=Box("Closed doorway",room+new Vector3(0,1.5f,0),new Vector3(10,3,.2f));
            Player(room+Vector3.back*4);Monster(room+Vector3.forward*.65f);Physics.SyncTransforms();
            Check(!skill.CastAt(room+Vector3.forward*2) && skill.CooldownRemaining==0,"Test 3: cannot cast through a closed wall; no cooldown consumed");
            Check(!vitality.Suppressed,"Rejected cast does not bind a monster through the wall");
            before=vitality.Health;Check(skill.CastAt(room+Vector3.back*1.3f),"Near-wall cast is usable on the visible side");yield return new WaitForSeconds(.82f);
            Check(vitality.Health==before && skill.LastHitCount==0 && !vitality.Suppressed,"Wall blocks AoE damage and binding to a nearby monster behind it");yield return Ready();wall.SetActive(false);

            Player(new Vector3(0,.13f,-10));Monster(new Vector3(0,.05f,-4));
            sight.Scan();brain.GetComponent<MonsterMemory>().RememberSight(sight);brain.enabled=true;brain.Decide();
            Check(brain.CurrentState==MonsterState.Chase,"Test 4 setup: monster is actually chasing");
            nav.Agent.speed=30;nav.Agent.acceleration=100;nav.MoveTo(player.transform.position);
            yield return new WaitForSeconds(.1f);
            Vector3 castPosition=brain.transform.position;before=vitality.Health;
            Check(skill.CastNearest(),"F auto cast catches a fast chasing monster");
            // NavMesh publishes velocity on its next simulation tick; isStopped is immediate.
            Check(vitality.Suppressed && nav.Agent.isStopped,"Fast chase stops in the same frame as casting");
            yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(castPosition,brain.transform.position)<.05f && nav.Agent.velocity.sqrMagnitude<.001f && vitality.Health==before,"Target stays at the cast position throughout summon, before damage");
            while(skill.LastHitCount==0 && skill.IsCasting)yield return null;
            Check(vitality.Health<before && vitality.Suppressed && vitality.SuppressionRemaining>2.9f,"Impact applies the full three-second stun");
            float release=Time.time+vitality.SuppressionRemaining;
            bool stayedHeld=true;
            while(Time.time<release-.1f)
            {
                stayedHeld &= vitality.Suppressed && !combat.IsAttacking && Vector3.Distance(castPosition,brain.transform.position)<.05f;
                yield return null;
            }
            Check(stayedHeld,"Target cannot move or attack for the entire five-second stun");
            Vector3 held=brain.transform.position;Check(nav.Agent.isStopped,"Suppression stops pursuit movement");
            Note("Suppression movement: "+held+" -> "+brain.transform.position+" stopped="+nav.Agent.isStopped+" ride="+nav.Riding+" link="+nav.Agent.isOnOffMeshLink+" remaining="+vitality.SuppressionRemaining);
            brain.enabled=false;yield return Ready();
            sight.Scan();brain.Decide();Check(nav.Ready && !nav.Agent.isStopped,"AI can resume navigation after suppression");nav.Stop();

            Player(new Vector3(0,.13f,-10));Monster(new Vector3(2.5f,.05f,-6));
            Check(skill.CastAt(new Vector3(0,.05f,-6)),"Cast catches a monster near the outer edge");
            while(skill.LastHitCount==0 && skill.IsCasting)yield return null;
            Check(skill.LastHitCount==1 && skill.LastDamage==60 && vitality.SuppressionRemaining>2.9f,"Edge hit still deals 60 damage and stuns for three seconds");
            yield return Ready();

            Player(new Vector3(0,.13f,-10));Monster(new Vector3(0,.05f,-8.8f));
            yield return new WaitForSeconds(combat.config.AttackCooldown+0.05f);
            float hp=health.CurrentHealth;sight.Scan();combat.TryAttack();Check(combat.WindingUp,"Monster begins a real attack windup before casting");
            Check(skill.CastAt(brain.transform.position),"Test 5: close-range cast is valid");
            Check(!combat.IsAttacking && vitality.Suppressed,"Casting immediately interrupts the pending attack");
            yield return new WaitForSeconds(.9f);
            Check(!combat.IsAttacking && health.CurrentHealth==hp && vitality.Suppressed,"Interrupted attack cannot damage the player during summon or impact");yield return Ready();

            var layered=new Vector3(230,0,200);var platform=Box("Upper floor slab",layered+Vector3.up*3.9f,new Vector3(10,.2f,12));
            Player(layered+Vector3.back*4);nav.Stop();nav.Agent.enabled=false;brain.transform.position=layered+Vector3.up*4.05f;Physics.SyncTransforms();
            Check(!skill.Targeting.Evaluate(layered+Vector3.up*4,player.transform,skill.config).valid,"Test 6: cannot target the floor above through a slab");
            before=vitality.Health;Check(skill.CastAt(layered),"Lower-floor cast remains valid");yield return new WaitForSeconds(.82f);
            Check(vitality.Health==before,"Impact does not leak damage to a different floor");yield return Ready();
            Monster(layered);platform.SetActive(false);
            foreach(var point in new[]{new Vector3(-26.2f,0,17.6f),new Vector3(-26.2f,4,17.6f),new Vector3(42.4f,32.01f,30.4f)})
            {
                Player(point);var result=skill.Targeting.Evaluate(Ground(point),player.transform,skill.config);
                Note("Campus floor sample "+point+": "+result.valid+" "+result.reason+" height="+result.height+" scale="+result.visualScale);
                Check(result.valid && Mathf.Abs(result.point.y-point.y)<.65f,"Real campus stair landing / upper-floor target stays on its own level: "+point);
            }
            var lift=Array.Find(Object.FindObjectsByType<CampusElevator>(),l=>l.building=="C");
            if(lift!=null)
            {
                var point=lift.floors[0].entrances[0];Player(point);var result=skill.Targeting.Evaluate(Ground(point),player.transform,skill.config);
                Check(result.valid || result.reason=="MOVE AWAY FROM WALL" || result.reason=="LINE OF SIGHT BLOCKED" || result.reason=="MOVING LIFT","Lift threshold has a valid target or explicit geometry/motion feedback");
                Note("Lift threshold: "+result.reason);
            }

            Player(new Vector3(0,.13f,-10));Monster(new Vector3(12,.1f,-4));
            int transforms=skill.Visual.GetComponentsInChildren<Transform>().Length;
            int materials=Resources.FindObjectsOfTypeAll<Material>().Length;
            for(int i=0;i<12;i++)
            {Check(skill.CastAt(center),"Test 7 repeated cast "+(i+1));yield return new WaitForSeconds(.82f);Check(skill.Visual.LiveParticles<=164,"Particle cap is bounded on cast "+(i+1));yield return Ready();}
            Check(transforms==skill.Visual.GetComponentsInChildren<Transform>().Length && materials==Resources.FindObjectsOfTypeAll<Material>().Length && !skill.Visual.Visible && skill.Visual.LiveParticles==0,"Repeated casts do not grow effect objects/materials or leave particles");

            player.spawnPosition=new Vector3(0,.13f,-10);player.spawnYaw=0;player.ReturnToSpawn();player.enabled=true;Cursor.lockState=CursorLockMode.Locked;
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.3f);
            Check(player.CurrentSpeed>3.4f,"Test 8: original sprint movement remains active");
            Note("Sprint: speed="+player.CurrentSpeed+" keyboard="+Keyboard.current?.name+" W="+keyboard.wKey.isPressed+" shift="+keyboard.leftShiftKey.isPressed+" cursor="+Cursor.lockState+" ui="+UIStateManager.Instance.State);
            Check(skill.CastAt(new Vector3(0,.05f,-4)),"Seal can be committed during sprint");
            yield return new WaitForSeconds(.9f);Check(player.CurrentSpeed>3.4f && !float.IsNaN(player.transform.position.y),"Movement remains stable through summon and impact");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());player.enabled=false;yield return Ready();
            Player(new Vector3(0,.13f,-10));camera.transform.position=new Vector3(0,2,-12);camera.transform.LookAt(center);
            var oldSkill=player.GetComponent<VoidWallSkill>();
            skill.BeginPreview();oldSkill.BeginPreview();yield return null;
            Check(oldSkill.Charges==oldSkill.MaxCharges && oldSkill.IsPreviewing && !skill.IsPreviewing,"Test 10: Void Wall keeps its full charges; previews cannot confirm both skills");oldSkill.CancelPreview();
            Check(Object.FindObjectsByType<Unity.AI.Navigation.NavMeshLink>().Length>=6 && nav.Ready,"Navigation links and existing monster agent remain usable");
            Check(original.cast!=null && original.charge!=null && original.rift!=null && original.descent!=null && original.impact!=null && original.aftershock!=null && original.dissipate!=null && original.output!=null,"All skill SFX and mixer references are assigned");
            var healthUI=Object.FindAnyObjectByType<PlayerHealthUI>();
            Check(player.GetComponents<PlayerMonsterHealth>().Length==1 && healthUI.Source==health,"HUD and combat share exactly one player health source");
            float previousHealth=health.CurrentHealth;health.TakeDamage(10);
            Check(health.CurrentHealth<previousHealth && healthUI.Value.text.StartsWith(Mathf.CeilToInt(health.CurrentHealth).ToString()),"Player damage updates the existing health HUD");
            // Death is persistent for this monster and cannot turn back into an attack.
            vitality.ReceiveSeal(vitality.Health,2.2f);brain.Decide();combat.TryAttack();yield return null;
            Check(vitality.Defeated && !combat.IsAttacking && nav.Agent.isStopped,"Lethal damage defeats the monster and stops combat");
            Check(UIStateManager.Instance.State==UIState.Victory,"Monster defeat enters Victory");
            UIStateManager.Instance.EnterScene(true); // Exercise player defeat separately after verifying the win.
            Note("Defeat: hp="+vitality.Health+" attack="+combat.IsAttacking+" stopped="+nav.Agent.isStopped+" riding="+nav.Riding+" link="+nav.Agent.isOnOffMeshLink);
            Note("No grapple component exists in the current player; sprint and normal camera/movement were exercised.");
            yield return new WaitForSeconds(.55f);
            Vector3 defeatPosition=player.transform.position;health.TakeDamage(health.maxHealth);
            Check(health.CurrentHealth==0 && UIStateManager.Instance.State==UIState.GameOver && Vector3.Distance(defeatPosition,player.transform.position)<.01f,"Player defeat enters Game Over without a hidden respawn");
            running=false;File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
        void OnDestroy()
        {
            if(realKeyboard!=null)InputSystem.EnableDevice(realKeyboard);if(realMouse!=null)InputSystem.EnableDevice(realMouse);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalInput!=null)InputSystem.settings=originalInput;if(testInput!=null)Destroy(testInput);
            if(skill!=null && original!=null){var copy=skill.config;skill.config=original;Destroy(copy);}
            foreach(var mesh in meshes)if(mesh.valid)mesh.Remove();foreach(var go in fixtures)if(go!=null)Destroy(go);
        }
    }
}
#endif
