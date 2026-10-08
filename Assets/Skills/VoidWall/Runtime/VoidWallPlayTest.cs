#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace CampusRift.Skills
{
    // Explicit MCP-launched integration harness. Never attached to a shipping scene or prefab.
    public sealed class VoidWallPlayTest:MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed=new List<string>();public List<string> failed=new List<string>();public List<string> notes=new List<string>(); }
        readonly Report report=new Report();
        readonly List<GameObject> fixtures=new List<GameObject>();
        readonly List<NavMeshDataInstance> navFixtures=new List<NavMeshDataInstance>();
        readonly List<VoidWall> walls=new List<VoidWall>();
        MonsterBrain brain;MonsterNavigation nav;MonsterPerception sight;MonsterMemory memory;MonsterCombat combat;MonsterBarrierTactics tactics;
        CampusExplorer player;VoidWallSkill skill;CharacterController cc;Camera camera;
        bool running;
        InputSettings originalInput,testInput;
        // Queued key events must reach the game even while the editor/Game View is unfocused (as BoostEnergyPlayTest does).
        void FocusFreeInput()
        {
            originalInput=InputSystem.settings;testInput=Instantiate(originalInput);InputSystem.settings=testInput;
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }
        void RestoreInput(){if(originalInput!=null)InputSystem.settings=originalInput;if(testInput!=null)Destroy(testInput);}
        
        MonsterAIConfig originalConfig;RoomGraph originalGraph;
        void Update(){if(running){UIStateManager.Instance?.EnterScene(true);Time.timeScale=1;}}
        void Check(bool value,string label)
        {(value?report.passed:report.failed).Add(label);Write();Debug.Log("Void Wall QA "+(value?"PASS ":"FAIL ")+label);}
        void Note(string text){report.notes.Add(text);Write();}
        void Write(){File.WriteAllText("Artifacts/VoidWall/Validation.json",JsonUtility.ToJson(report,true));}
        Vector3 Ground(Vector3 p){return NavMesh.SamplePosition(p,out var h,1,NavMesh.AllAreas)?h.position:p;}
        void PlacePlayer(Vector3 p)
        {cc.enabled=false;player.transform.position=Ground(p);cc.enabled=true;Physics.SyncTransforms();}
        void PlaceMonster(Vector3 p,Vector3 target)
        {nav.Stop();nav.Agent.Warp(Ground(p));brain.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(target-p,Vector3.up));Physics.SyncTransforms();}
        void Observe(Vector3 goal)
        {PlacePlayer(goal);memory.Forget();sight.Scan();memory.RememberSight(sight);}
        VoidWall Spawn(Vector3 p,Quaternion rotation,float width,float lifetime=12)
        {
            var w=Instantiate(skill.wallPrefab);var c=Instantiate(skill.config);c.lifetime=lifetime;w.config=c;
            w.Deploy(Ground(p)+Vector3.up*0.025f,rotation,width,new Collider[]{cc});walls.Add(w);return w;
        }
        void ClearWalls()
        {foreach(var w in walls)if(w!=null){w.Dissolve(false);Destroy(w.gameObject);}walls.Clear();}
        void Fixture(Vector3 center,float width,float length)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Void Wall QA floor";floor.transform.position=center+Vector3.down*0.1f;floor.transform.localScale=new Vector3(width,0.2f,length);fixtures.Add(floor);
            var sources=new List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(floor.transform.position,Quaternion.identity,Vector3.one),size=floor.transform.localScale,area=0}};
            var settings=NavMesh.GetSettingsByID(nav.Agent.agentTypeID);settings.overrideVoxelSize=true;settings.voxelSize=0.08f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(center,new Vector3(width+4,8,length+4)),Vector3.zero,Quaternion.identity);
            navFixtures.Add(NavMesh.AddNavMeshData(data));Physics.SyncTransforms();
        }
        IEnumerator RunAI(Vector3 target,float seconds)
        {
            float end=Time.time+seconds,next=0;int crossings=0;Vector3 prev=brain.transform.position;
            while(Time.time<end && Vector3.Distance(brain.transform.position,target)>1.3f)
            {
                if(Time.time>=next){brain.Decide();next=Time.time+0.15f;}
                yield return null;
                foreach(var w in VoidWall.Active)
                {
                    var a=w.transform.InverseTransformPoint(prev);var b=w.transform.InverseTransformPoint(brain.transform.position);
                    if(a.z*b.z<0 && Mathf.Abs(b.x)<w.Width/2 && Mathf.Abs(b.y)<1)crossings++;
                }
                prev=brain.transform.position;
            }
            Check(crossings==0,"Monster movement never crosses a solid barrier plane");
        }
        void Capture(string name,Vector3 focus)
        {
            camera.transform.position=focus+new Vector3(4,2.2f,5.4f);camera.transform.LookAt(focus+Vector3.up*1.05f);camera.fieldOfView=43;
            var rt=RenderTexture.GetTemporary(1280,900,24);var old=RenderTexture.active;var target=camera.targetTexture;var image=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();File.WriteAllBytes("Artifacts/VoidWall/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Artifacts/VoidWall");running=true;Application.runInBackground=true;FocusFreeInput();
            UIStateManager.Instance?.EnterScene(true);
            brain=Object.FindAnyObjectByType<MonsterBrain>();nav=brain.GetComponent<MonsterNavigation>();sight=brain.GetComponent<MonsterPerception>();memory=brain.GetComponent<MonsterMemory>();combat=brain.GetComponent<MonsterCombat>();tactics=brain.GetComponent<MonsterBarrierTactics>();
            brain.enabled=false;sight.enabled=false;brain.GetComponent<MonsterHearing>().enabled=false;
            player=Object.FindAnyObjectByType<CampusExplorer>();player.enabled=false;cc=player.GetComponent<CharacterController>();skill=player.GetComponent<VoidWallSkill>();camera=Camera.main;
            originalConfig=nav.config;originalGraph=nav.roomGraph;
            PlaceMonster(new Vector3(30,0,-4),Vector3.zero);PlacePlayer(new Vector3(8,0,-4));camera.transform.rotation=Quaternion.identity;
            yield return null;
            int max=skill.MaxCharges;
            Check(skill.Charges==3 && max==3 && skill.config.rechargeSeconds>0,"Starts with 3 charges that recharge over time (V2)");
            Check(skill.BeginPreview() && skill.Placement.valid,"Open campus ground accepts placement preview");
            var near=skill.Evaluate(player.transform.position,Vector3.forward,skill.config.minimumPlacementDistance);
            var far=skill.Evaluate(player.transform.position,Vector3.forward,skill.config.maximumPlacementDistance);
            Check(near.valid && far.valid &&
                Mathf.Abs(Vector3.Distance(player.transform.position,near.feet)-skill.config.minimumPlacementDistance)<.2f &&
                Mathf.Abs(Vector3.Distance(player.transform.position,far.feet)-skill.config.maximumPlacementDistance)<.2f,
                "Void Wall supports placement pulled close and pushed farther away");
            Capture("01-Valid-preview",skill.Placement.feet);
            camera.transform.rotation=Quaternion.identity;
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=player.transform.position+new Vector3(0,1.2f,1.2f);blocker.transform.localScale=new Vector3(4,2.4f,0.2f);Physics.SyncTransforms();
            skill.RefreshPreview();Check(skill.Placement.valid && skill.Charges==max && skill.Placement.feet.z<blocker.transform.position.z-.1f,"Placement stops in front of a solid wall instead of passing through it");
            Capture("02-Obstructed-preview",skill.Placement.feet);Destroy(blocker);yield return null;
            camera.transform.rotation=Quaternion.identity;skill.RefreshPreview();
            var air=skill.Evaluate(new Vector3(1000,50,1000),Vector3.forward);Check(air.valid && Mathf.Abs(air.feet.y-50)<0.1f,"Unsupported and floating placement is allowed");
            PlaceMonster(player.transform.position+Vector3.forward*2.7f,player.transform.position);Physics.SyncTransforms();
            Check(skill.Evaluate(player.transform.position,Vector3.forward).valid,"Placement beside a monster is allowed");
            PlaceMonster(new Vector3(30,0,-4),Vector3.zero);
            Check(skill.Confirm() && skill.Charges==max-1,"Confirm deploys immediately and consumes one charge");
            var first=skill.LastDeployed;yield return new WaitForSeconds(.25f);
            Check(first.IsSolid && first.obstacle.enabled && Physics.GetIgnoreCollision(first.solid,cc),"Collider + carved obstacle active; owner collision ignored");
            Capture("03-Deployed",first.transform.position);
            var before=player.transform.position;cc.Move(Vector3.forward*4);Check(player.transform.position.z-before.z>3.5f,"Owner can escape through their own wall");
            first.Damage(15,first.Center);yield return new WaitForSeconds(.08f);Capture("04-Hit-ripple",first.transform.position);
            first.Damage(15,first.Center);yield return new WaitForSeconds(.3f);Capture("05-Damaged",first.transform.position);
            PlacePlayer(new Vector3(12,0,-4));camera.transform.rotation=Quaternion.identity;
            skill.BeginPreview();Check(skill.Confirm() && skill.Charges==max-2,"Second deployment consumes second charge");
            yield return new WaitForSeconds(.4f);
            player.spawnPosition=new Vector3(16,.13f,-4);player.spawnYaw=0;player.ReturnToSpawn();player.enabled=true;Cursor.lockState=CursorLockMode.Locked;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.2f);
            float sprintSpeed=player.CurrentSpeed;
            // Quick cast: a single tap of Q (press + release) deploys with no separate confirm.
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W,Key.LeftShift,Key.Q));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W,Key.LeftShift));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());player.enabled=false;
            Note("Sprint tap diagnostics: measuredSpeed="+sprintSpeed+" charges="+skill.Charges+" expected="+(max-3)+" keyboardEnabled="+Keyboard.current.enabled+" currentFrameMs="+(Time.unscaledDeltaTime*1000));
            Check(skill.Charges==max-3 && sprintSpeed>3.4f,"A single Q tap deploys instantly while sprinting");
            for(int i=3;i<max;i++)
            {
                yield return new WaitForSeconds(skill.config.deployCooldown+0.02f);
                PlacePlayer(new Vector3(1000+i*4,50,1000));
                skill.BeginPreview();skill.Confirm();
            }
            Check(VoidWall.Active.Count==max && skill.Charges==0,"Simultaneous barriers use every charge");
            Check(!skill.BeginPreview() && skill.Feedback.Contains("NO CHARGES"),"Empty skill gives feedback and cannot spawn an extra barrier");
            yield return new WaitForSeconds(skill.config.rechargeSeconds+0.2f);
            Check(skill.Charges==1,"One charge returns after the recharge time");
            foreach(var wall in VoidWall.Active.ToArray())wall.Dissolve(false);
            yield return new WaitForSeconds(.3f);

            // Isolated deterministic navigation fixtures complement tests on the actual campus.
            nav.config=Instantiate(originalConfig);nav.config.RideLifts=false;nav.roomGraph=null;
            Fixture(new Vector3(200,0,200),12,18);Fixture(new Vector3(230,0,200),2.35f,18);
            yield return new WaitForSeconds(.3f);
            var goal=new Vector3(200,0,205);PlaceMonster(new Vector3(200,0,195),goal);Observe(goal);
            var open=Spawn(new Vector3(200,0,200),Quaternion.identity,2.8f);int detours=tactics.Detours;
            yield return new WaitForSeconds(.3f);
            yield return RunAI(goal,9);
            Check(tactics.Detours>detours && open.HitCount==0 && brain.transform.position.z>202,"Test 3: nearby detour preferred; wall left intact");
            ClearWalls();yield return new WaitForSeconds(.3f);
            goal=new Vector3(230,0,205);PlaceMonster(new Vector3(230,0,195),goal);Observe(goal);
            var sealedWall=Spawn(new Vector3(230,0,200),Quaternion.identity,2.3f);int rev=VoidWall.Revision;
            yield return new WaitForSeconds(.3f);
            yield return RunAI(goal,10);
            Check(sealedWall.WasBroken && sealedWall.HitCount==3,"Test 1/4: narrow corridor without detour is breached with 3 animated hits");
            Check(brain.transform.position.z>201 && nav.Ready && !sealedWall.obstacle.enabled && VoidWall.Revision>rev,"Test 7: broken wall releases the route and pursuit continues");
            ClearWalls();yield return new WaitForSeconds(.3f);
            PlaceMonster(new Vector3(230,0,195),goal);
            var expire=Spawn(new Vector3(230,0,200),Quaternion.identity,2.3f,1.2f);
            yield return new WaitForSeconds(.3f);var path=new NavMeshPath();nav.Agent.CalculatePath(goal,path);
            Check(path.status!=NavMeshPathStatus.PathComplete,"Narrow obstacle really invalidates the NavMesh route");
            yield return new WaitForSeconds(1.1f);
            nav.Agent.CalculatePath(goal,path);
            Check(!expire.IsSolid && !expire.WasBroken && expire.Health==expire.config.health && path.status==NavMeshPathStatus.PathComplete,"Test 6: natural expiration restores route without a break event");
            Capture("06-Expiration",expire.transform.position);
            ClearWalls();yield return new WaitForSeconds(.3f);

            // Real campus front door, with its automatic door and the original room graph.
            Destroy(nav.config);nav.config=originalConfig;nav.roomGraph=originalGraph;
            var door=Array.Find(Object.FindObjectsByType<CampusAutomaticDoor>(),d=>d.name=="Entrance E");
            goal=new Vector3(0,0,-19);PlaceMonster(new Vector3(0,0,-12),goal);Observe(goal);
            if(door!=null)door.RequestOpenFrom(brain.transform.position);
            yield return new WaitForSeconds(1.4f);sight.Scan();memory.RememberSight(sight);
            var placement=skill.Evaluate(new Vector3(0,.1f,-12.8f),Vector3.back);
            Note("Entrance E placement: "+placement.valid+" "+placement.reason+" "+placement.feet+" width="+placement.width);
            Check(placement.valid,"Actual campus entrance accepts placement before the door");
            var doorway=Spawn(placement.valid?placement.feet:new Vector3(0,0,-15.5f),Quaternion.identity,placement.valid?placement.width:2.8f);
            yield return new WaitForSeconds(.3f);
            yield return RunAI(goal,11);
            Check(nav.Ready && (doorway.WasBroken || Vector3.Distance(brain.transform.position,goal)<2),"Test 2: real Entrance E door / AI escapes blockage");
            Capture("07-Entrance-E",doorway.transform.position);ClearWalls();yield return new WaitForSeconds(.3f);

            var lift=Array.Find(Object.FindObjectsByType<CampusElevator>(),e=>e.building=="C");
            if(lift!=null)
            {
                var entrance=lift.floors[0].entrances[0];var forward=-lift.outward;
                var reserved=skill.Evaluate(entrance-forward*skill.config.placementDistance,forward);
                Check(reserved.valid,"Lift threshold/cabin accepts placement");
            }
            else Check(false,"Campus C lift exists");
            Check(Object.FindObjectsByType<Unity.AI.Navigation.NavMeshLink>().Length>=6,"Existing multi-floor stair links preserved");
            Check(skill.wallPrefab.surface.sharedMaterial==skill.config.wallMaterial && skill.wallPrefab.motes.main.maxParticles==100,"Shared materials and a bounded 100 particles per barrier");
            Check(skill.config.deploy!=null && skill.config.impact!=null && skill.config.broken!=null && skill.config.hum!=null && skill.config.output!=null,"Licensed SFX references connected to the SFX mixer");
            nav.Stop();running=false;RestoreInput();
            File.WriteAllText("Artifacts/VoidWall/DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
        void OnDestroy()
        {RestoreInput();foreach(var instance in navFixtures)if(instance.valid)instance.Remove();foreach(var go in fixtures)if(go!=null)Destroy(go);ClearWalls();}
    }
}
#endif
