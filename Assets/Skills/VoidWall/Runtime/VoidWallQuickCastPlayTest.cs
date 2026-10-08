#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace CampusRift.Skills
{
    // MCP-launched harness for the Free Fire style quick cast. Writes Artifacts/VoidWall/QuickCast.json + QuickCast-DONE.txt.
    public sealed class VoidWallQuickCastPlayTest:MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed=new List<string>();public List<string> failed=new List<string>();public List<string> notes=new List<string>(); }
        readonly Report report=new Report();
        readonly List<GameObject> fixtures=new List<GameObject>();
        CampusExplorer player;VoidWallSkill skill;MonsterBrain brain;Keyboard keyboard;Mouse mouse;
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
        void OnDestroy(){RestoreInput();}
        
        void Update(){if(running){UIStateManager.Instance?.EnterScene(true);Time.timeScale=1;}}
        void Check(bool value,string label){(value?report.passed:report.failed).Add(label);Write();Debug.Log("Void Wall quick cast "+(value?"PASS ":"FAIL ")+label);}
        void Note(string text){report.notes.Add(text);Write();}
        void Write(){Directory.CreateDirectory("Artifacts/VoidWall");File.WriteAllText("Artifacts/VoidWall/QuickCast.json",JsonUtility.ToJson(report,true));}
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        void RightMouse(bool down)=>InputSystem.QueueStateEvent(mouse,down?new MouseState().WithButton(MouseButton.Right):new MouseState());
        IEnumerator Frames(int n=2){for(int i=0;i<n;i++)yield return null;}
        GameObject Cube(Vector3 p,Vector3 s){var c=GameObject.CreatePrimitive(PrimitiveType.Cube);c.transform.position=p;c.transform.localScale=s;fixtures.Add(c);Physics.SyncTransforms();return c;}
        IEnumerator Reset()
        {
            Keys();RightMouse(false);yield return Frames();
            foreach(var w in VoidWall.Active.ToArray())w.gameObject.SetActive(false);
            yield return new WaitForSeconds(skill.config.deployCooldown+.05f);
        }
        void Start(){StartCoroutine(Run());}
        IEnumerator Run()
        {
            running=true;Application.runInBackground=true;FocusFreeInput();
            player=FindAnyObjectByType<CampusExplorer>();skill=player.GetComponent<VoidWallSkill>();brain=FindAnyObjectByType<MonsterBrain>();
            if(brain!=null)brain.gameObject.SetActive(false);
            // Same approach as VoidWallPlayTest: drive the current devices from inside the player loop.
            keyboard=Keyboard.current;mouse=Mouse.current;
            player.ReturnToSpawn();yield return Frames(3);
            // 1. Tap: press + release deploys with no confirm step.
            int charges=skill.Charges;float start=Time.unscaledTime;
            Keys(Key.Q);yield return null;
            var input=player.GetComponent<Controls.CampusInput>();
            Note("After Q press: kb="+Keyboard.current.name+" q="+Keyboard.current.qKey.isPressed+" enabled="+Keyboard.current.enabled+" holding="+input.Holding(Controls.CampusAction.Wall)+" allowed="+input.Allowed+" mobile="+Controls.CampusInput.Mobile+" previewing="+skill.IsPreviewing+" charges="+skill.Charges+" cooldown="+skill.CooldownRemaining.ToString("0.00")+" hp="+player.GetComponent<PlayerMonsterHealth>().CurrentHealth+" skillOn="+skill.isActiveAndEnabled+" state="+UIStateManager.Instance.State);
            yield return null;Keys();yield return Frames();
            Check(skill.Charges==charges-1 && !skill.IsPreviewing && VoidWall.Active.Count==1,"Tap Q deploys a wall immediately (no confirm step)");
            Note("Tap to wall latency: "+((Time.unscaledTime-start)*1000).ToString("0")+" ms including 4 test frames");
            var tapped=skill.LastDeployed;
            Check(tapped!=null && Vector3.Angle(tapped.transform.forward,Vector3.ProjectOnPlane(player.followCamera.transform.forward,Vector3.up))<2,"Quick wall faces where the camera looks");
            yield return Reset();
            // 2. Hold: ghost appears after the hold threshold, release deploys at the aimed spot.
            charges=skill.Charges;Keys(Key.Q);yield return Frames();
            bool hiddenOnPress=skill.IsPreviewing && !GameObject.Find("Void Wall Placement");
            yield return new WaitForSeconds(.35f);
            var ghost=GameObject.Find("Void Wall Placement");
            Check(hiddenOnPress && skill.IsPreviewing && ghost!=null && ghost.activeSelf,"Holding Q reveals the aim preview only after the hold threshold");
            Keys();yield return Frames();
            Check(skill.Charges==charges-1 && !skill.IsPreviewing,"Releasing a held Q deploys");
            yield return Reset();
            // 3. Hold + right click cancels without spending a charge.
            charges=skill.Charges;Keys(Key.Q);yield return new WaitForSeconds(.3f);
            RightMouse(true);yield return Frames();RightMouse(false);Keys();yield return Frames(3);
            Check(skill.Charges==charges && !skill.IsPreviewing,"Hold Q + right click cancels without spending a charge");
            yield return Reset();
            // 4. Mashing during the cooldown is buffered, not lost (needs two charges: V2 has three with recharge).
            skill.RefillCharges();charges=skill.Charges;
            Keys(Key.Q);yield return Frames();Keys();yield return Frames();Keys(Key.Q);yield return Frames();Keys();
            Check(skill.CastQueued,"Second press during cooldown is queued");
            yield return new WaitForSeconds(skill.config.deployCooldown+.1f);
            Check(skill.Charges==charges-2,"Queued press deploys the second wall as soon as the cooldown ends");
            yield return Reset();
            // 5. Placement fitting on synthetic geometry beside the spawn.
            var o=player.transform.position;var forward=Vector3.forward;
            var blocker=Cube(o+new Vector3(0,1.2f,1.4f),new Vector3(4,2.4f,.2f));
            var p=skill.Evaluate(o,forward);
            Check(p.feet.z<blocker.transform.position.z-.1f-skill.config.thickness*.5f+.01f,"Wall stops in front of solid geometry");
            Destroy(blocker);fixtures.Remove(blocker);yield return null;Physics.SyncTransforms();
            var left=Cube(o+new Vector3(-.6f,1.2f,2.7f),new Vector3(.2f,2.4f,3));var right=Cube(o+new Vector3(1.3f,1.2f,2.7f),new Vector3(.2f,2.4f,3));
            p=skill.Evaluate(o,forward);
            Check(Mathf.Abs(p.width-1.76f)<.08f && Mathf.Abs(p.feet.x-o.x-.35f)<.05f,"Wall narrows and centres itself in a 1.7 m corridor");
            Destroy(right);fixtures.Remove(right);yield return null;Physics.SyncTransforms();
            p=skill.Evaluate(o,forward);
            Check(Mathf.Abs(p.width-skill.config.width)<.01f && Mathf.Abs(p.feet.x-p.width*.5f-(o.x-.5f))<.06f,"One side wall shifts the full-width wall flush against it");
            Destroy(left);fixtures.Remove(left);yield return null;Physics.SyncTransforms();
            var step=Cube(o+new Vector3(.9f,.15f,2.7f),new Vector3(1.2f,.3f,1));
            p=skill.Evaluate(o,forward);
            Check(p.feet.y<o.y+.12f,"On a step the wall sits on the lower surface, leaving no gap underneath");
            Destroy(step);fixtures.Remove(step);yield return null;Physics.SyncTransforms();
            // 6. Threat assist.
            if(brain!=null)
            {
                // Frozen stand-in: the monster body is present, its AI and agent are off.
                var agent=brain.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;brain.enabled=false;brain.gameObject.SetActive(true);
                brain.transform.position=o+new Vector3(3.5f,0,3.5f);Physics.SyncTransforms();
                yield return new WaitForSeconds(.6f);   // threat list rescan is throttled
                p=skill.Evaluate(o,forward);
                Check(p.assisted && Vector3.Angle(p.rotation*Vector3.forward,new Vector3(1,0,1))<3,"A monster ahead turns the wall square across its approach");
                brain.transform.position=o-forward*5;Physics.SyncTransforms();
                p=skill.Evaluate(o,forward);
                Check(!p.assisted && Vector3.Angle(p.rotation*Vector3.forward,forward)<1,"A monster behind does not hijack the aim");
                brain.gameObject.SetActive(false);
            }
            else Note("No monster in scene: threat assist skipped");
            foreach(var f in fixtures)if(f!=null)Destroy(f);
            Keys();RightMouse(false);yield return Frames();
            running=false;RestoreInput();
            File.WriteAllText("Artifacts/VoidWall/QuickCast-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
    }
}
#endif
