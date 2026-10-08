#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.UI;
using CampusRift.Skills;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace CampusRift.Controls
{
    public sealed class LookSmoke : MonoBehaviour
    {
        [Serializable] public class Layout {public string name;public float effectiveRightScale;public List<string> issues=new List<string>();}
        [Serializable] public class Report {public List<string> passed=new List<string>(),failed=new List<string>();public List<ComicTextAudit.ScreenReport> audits=new List<ComicTextAudit.ScreenReport>();public List<Layout> layouts=new List<Layout>();public string continuation;}
        [NonSerialized] public string ResumeCheckpoint;
        [NonSerialized] public bool GoldenBellOnly;
        readonly Report report=new Report();SkillSet1TestWorld world;MobileControlsHUD hud;Touchscreen touch;
        InputSettings oldInputSettings,testInputSettings;Mouse oldMouse;GameSettings original;
        void Check(bool ok,string text){(ok?report.passed:report.failed).Add(text);Write();}
        void Write(){File.WriteAllText("task/look/smoke.json",JsonUtility.ToJson(report,true));}
        IEnumerator Frames(int n=5){for(int i=0;i<n;i++)yield return null;}
        MobileTouchZone Zone(TouchRole r)=>hud.Zones.First(z=>z.role==r);
        Vector2 Point(TouchRole role)=>RectTransformUtility.WorldToScreenPoint(null,Zone(role).transform.position);
        void Touch(int id,TouchPhase phase,Vector2 point){InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=point,pressure=phase==TouchPhase.Ended?0:1});}
        static Rect ScreenBounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            Vector2 min=RectTransformUtility.WorldToScreenPoint(null,corners[0]),max=min;
            foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(null,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        static Rect Union(Rect a,Rect b)=>Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));
        static Rect TextBounds(TMPro.TMP_Text text)
        {
            text.ForceMeshUpdate();var bounds=ScreenBounds(text.rectTransform);
            if(string.IsNullOrWhiteSpace(text.text))return bounds;
            var ink=text.textBounds;
            var min=RectTransformUtility.WorldToScreenPoint(null,text.rectTransform.TransformPoint(ink.min));
            var max=RectTransformUtility.WorldToScreenPoint(null,text.rectTransform.TransformPoint(ink.max));
            return Union(bounds,Rect.MinMaxRect(min.x,min.y,max.x,max.y));
        }
        void ControlLayout(Rect safe,string label)
        {
            hud.ApplySafeArea(safe);Canvas.ForceUpdateCanvases();
            // Include hidden Cancel/Ultimate and empty status rows, reserving future states.
            var controls=hud.Zones.Where(z=>z.role!=TouchRole.Look).ToArray();
            var issues=new List<string>();
            for(int i=0;i<controls.Length;i++)
            {
                var bounds=hud.ControlScreenBounds(controls[i]);
                if(bounds.xMin<safe.xMin-.5f||bounds.xMax>safe.xMax+.5f||bounds.yMin<safe.yMin-.5f||bounds.yMax>safe.yMax+.5f)issues.Add(controls[i].role+" outside safe area");
                for(int j=i+1;j<controls.Length;j++)
                    if(bounds.Overlaps(hud.ControlScreenBounds(controls[j])))issues.Add(controls[i].role+" overlaps "+controls[j].role);
            }
            Check(issues.Count==0,label+" all button + name + state rectangles safe and separate"+(issues.Count==0?"":": "+string.Join(", ",issues)));
            var forbidden=new Rect(Screen.width*.32f,Screen.height*.20f,Screen.width*.32f,Screen.height*.65f);
            var region=new Layout{name=label,effectiveRightScale=hud.RightControlScale};
            var hint=TextBounds(hud.SafeRoot.Find("Gesture hint").GetComponent<TMPro.TMP_Text>());
            foreach(var z in controls)
            {
                var bounds=hud.ControlScreenBounds(z);
                foreach(var t in z.GetComponentsInChildren<TMPro.TMP_Text>(true))bounds=Union(bounds,TextBounds(t));
                if(bounds.Overlaps(forbidden))region.issues.Add(z.role+" enters center forbidden region");
                if(MobileControlsHUD.IsRightControl(z.role)?bounds.xMin<Screen.width*.64f-.5f:bounds.xMax>Screen.width*.30f+.5f)
                    region.issues.Add(z.role+" crosses cluster boundary");
                if(bounds.Overlaps(hint))region.issues.Add(z.role+" touches bottom hint");
            }
            var items=FindAnyObjectByType<ItemBarUI>();
            if(items!=null)foreach(var item in items.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var bounds=ScreenBounds((RectTransform)item.transform);
                foreach(var graphic in item.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))bounds=Union(bounds,ScreenBounds(graphic.rectTransform));
                if(bounds.xMax>Screen.width*.30f+.5f||bounds.Overlaps(forbidden))region.issues.Add(item.name+" item crosses left boundary");
                if(bounds.Overlaps(hint))region.issues.Add(item.name+" item touches hint");
            }
            report.layouts.Add(region);
            Check(region.issues.Count==0,label+" center clear, right >=64%, left/items <=30%, all captions clear of hint"+(region.issues.Count==0?"":": "+string.Join(", ",region.issues)));
            var vitals=(RectTransform)hud.transform.Find("Vitals");
            var panelBounds=ScreenBounds(vitals);
            foreach(var graphic in vitals.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {var b=ScreenBounds(graphic.rectTransform);panelBounds=Rect.MinMaxRect(Mathf.Min(panelBounds.xMin,b.xMin),Mathf.Min(panelBounds.yMin,b.yMin),Mathf.Max(panelBounds.xMax,b.xMax),Mathf.Max(panelBounds.yMax,b.yMax));}
            Check(!hud.ControlScreenBounds(Zone(TouchRole.Pause)).Overlaps(panelBounds),label+" Pause captions clear of entire Vitals panel");
            bool reachable=true;
            foreach(var z in controls.Where(z=>z.gameObject.activeInHierarchy))
            {
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Point(z.role)},hits);
                reachable &= hits.Count>0&&hits[0].gameObject.GetComponentInParent<MobileTouchZone>()==z;
            }
            Check(reachable,label+" all visible control centers raycast reachable");
        }
        IEnumerator MobileScreens()
        {
            Directory.CreateDirectory("task/polish2/screens");
            foreach(var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)})
            {
                UIValidation.SetResolution(size.x,size.y);yield return Frames(8);
                foreach(float scale in new[]{.85f,1.2f})
                {
                    var settings=SettingsManager.Instance.Current.Copy();settings.MobileControlScale=scale;SettingsManager.Instance.Apply(settings,false);yield return Frames();
                    var full=new Rect(0,0,Screen.width,Screen.height);var inset=new Rect(80,30,Screen.width-160,Screen.height-60);
                    ControlLayout(full,size+" scale "+scale+" full safe area");ControlLayout(inset,size+" scale "+scale+" inset safe area");hud.ApplySafeArea(Screen.safeArea);
                    if((size.x==2400&&scale==1.2f)||(size.x==1600&&scale==.85f))
                    {yield return Frames();LookCapture.Image("task/polish2/screens/fix2-"+size.x+"x"+size.y+".png");}
                }
            }
            var restore=SettingsManager.Instance.Current.Copy();restore.MobileControlScale=1;SettingsManager.Instance.Apply(restore,false);UIValidation.SetResolution(1920,1080);yield return Frames();
        }
        IEnumerator Audit(string state,string screenshot=null)
        {
            yield return Frames();var scan=ComicTextAudit.Scan(state);report.audits.Add(scan);Check(scan.issues.Count==0,state+" ComicTextAudit");
            if(screenshot!=null){LookCapture.Image("task/look/screens/after/"+screenshot+".png");yield return Frames();}
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory("task/look/screens/after");original=SettingsManager.Instance.Current.Copy();
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.Mode(true);world.Lighting(false);UIValidation.SetResolution(1920,1080);
            hud=FindAnyObjectByType<MobileControlsHUD>();yield return new WaitForSecondsRealtime(1.3f);
            // Set the level look after delayed scene startup; retain the known unobstructed cast area.
            world.Arrange();world.Lighting(false);
            oldInputSettings=InputSystem.settings;testInputSettings=Instantiate(oldInputSettings);testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInputSettings;
            oldMouse=Mouse.current;if(oldMouse!=null)InputSystem.DisableDevice(oldMouse);touch=InputSystem.AddDevice<Touchscreen>();
            var input=world.player.GetComponent<CampusInput>();var loadout=world.player.GetComponent<SkillLoadout>();
            string[] ids={"phat-no-hoa-lien","than-kiem-ngu-loi","han-bang-phong-an","kim-chung-trao"};
            for(int i=0;i<4;i++)loadout.Equip(i,ids[i]);world.player.GetComponent<SpiritPower>().Refill();yield return Frames();
            Check(hud.Zones.Where(z=>z.role!=TouchRole.Look).All(z=>z.GetComponent<RiftGraphic>()!=null&&z.GetComponent<RiftGraphic>().Form==RiftGraphic.Shape.Disc),"All mobile button/joystick backgrounds are discs");
            bool circles=true;foreach(var z in hud.Zones.Where(z=>z.role!=TouchRole.Look))
            {
                var r=(RectTransform)z.transform;var center=RectTransformUtility.WorldToScreenPoint(null,r.position);
                var edge=r.TransformPoint(r.rect.center+r.rect.size*.46f);circles&=z.IsRaycastLocationValid(center,null)&&!z.IsRaycastLocationValid(RectTransformUtility.WorldToScreenPoint(null,edge),null);
            }
            Check(circles,"Circular raycast accepts centers and rejects square corners");
            var attack=Zone(TouchRole.Attack);var ar=(RectTransform)attack.transform;
            var corner=RectTransformUtility.WorldToScreenPoint(null,ar.TransformPoint(ar.rect.center+ar.rect.size*.46f));
            Touch(1,TouchPhase.Began,corner);yield return Frames();Check(!attack.Held,"Real touchscreen corner does not acquire attack");Touch(1,TouchPhase.Ended,corner);yield return Frames();
            var centerAttack=Point(TouchRole.Attack);Touch(2,TouchPhase.Began,centerAttack);yield return Frames();Check(attack.Held,"Real touchscreen center acquires attack");Touch(2,TouchPhase.Ended,centerAttack);yield return Frames();Check(!attack.Held,"Attack releases ownership");
            var move=Point(TouchRole.Move);Touch(3,TouchPhase.Began,move);yield return Frames();Touch(3,TouchPhase.Moved,move+Vector2.up*70*hud.PixelScale);yield return Frames();Check(input.TouchMove.y>.3f,"Real joystick drag moves player");Touch(3,TouchPhase.Ended,move);yield return Frames();Check(input.TouchMove==Vector2.zero,"Joystick releases movement");world.Arrange();
            var boost=Point(TouchRole.Sprint);
            Touch(31,TouchPhase.Began,move);yield return Frames();Touch(31,TouchPhase.Moved,move+Vector2.up*110*hud.PixelScale);yield return Frames();
            Check(input.TouchMove.magnitude>.9f&&!input.Sprint,"Full real touchscreen joystick drag does not auto boost by default");
            Touch(32,TouchPhase.Began,boost);yield return Frames();
            Check(Zone(TouchRole.Move).Owner!=Zone(TouchRole.Sprint).Owner&&Zone(TouchRole.Move).Held&&Zone(TouchRole.Sprint).Held&&input.Sprint,"Two real touch contacts own joystick and BOOST independently");
            Touch(32,TouchPhase.Ended,boost);yield return Frames();Check(Zone(TouchRole.Move).Held&&input.TouchMove.magnitude>.9f&&!input.Sprint,"Releasing BOOST preserves joystick and stops sprint");
            Touch(31,TouchPhase.Ended,move);yield return Frames();world.Arrange();
            var jump=Point(TouchRole.Jump);Touch(4,TouchPhase.Began,jump);yield return Frames(7);Check(!world.player.IsGrounded,"Round jump button responds");Touch(4,TouchPhase.Ended,jump);yield return new WaitForSeconds(.7f);
            world.Arrange();world.Lighting(false);yield return Audit("mobile-ready-1920");yield return MobileScreens();
            var start=Point(TouchRole.Skill1);var runtime=loadout.Get(0);Touch(5,TouchPhase.Began,start);yield return Frames();Check(runtime.IsAiming,"Touch begins Lotus aim");
            var outside=start+new Vector2(150,130)*hud.PixelScale;Touch(5,TouchPhase.Moved,outside);yield return Frames();Check(input.SkillDrag.sqrMagnitude>.15f&&Zone(TouchRole.Skill1).Held,"SkillDrag updates outside round button while touch remains owned");yield return Audit("mobile-aiming");
            Touch(5,TouchPhase.Ended,outside);yield return Frames();Check(runtime.CooldownRemaining>0,"Release confirms Lotus cast and cooldown");
            // Casting animation ends before cooldown. Capture the actual cooldown immediately.
            float captureUntil=Time.realtimeSinceStartup+8;
            while(runtime.GetState()!=SkillState.Cooldown&&Time.realtimeSinceStartup<captureUntil)yield return null;
            yield return Audit("mobile-cooldown");
            yield return Tail();
        }
        IEnumerator ResumeTail()
        {
            // Continue an interrupted run at its saved checkpoint; never rerun its matrix or completed input checks.
            JsonUtility.FromJsonOverwrite(File.ReadAllText(ResumeCheckpoint),report);
            if(!report.passed.Contains("mobile-cooldown ComicTextAudit")||report.failed.Count!=0)throw new InvalidOperationException("Not a valid cooldown checkpoint");
            report.continuation="Continued after Unity assembly reload at mobile-cooldown checkpoint; earlier checks were not rerun.";
            original=SettingsManager.Instance.Current.Copy();world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.Mode(true);world.Lighting(false);UIValidation.SetResolution(1920,1080);
            hud=FindAnyObjectByType<MobileControlsHUD>();yield return new WaitForSecondsRealtime(1.3f);world.Arrange();world.Lighting(false);
            oldInputSettings=InputSystem.settings;testInputSettings=Instantiate(oldInputSettings);testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInputSettings;
            oldMouse=Mouse.current;if(oldMouse!=null)InputSystem.DisableDevice(oldMouse);touch=InputSystem.AddDevice<Touchscreen>();
            var loadout=world.player.GetComponent<SkillLoadout>();string[] ids={"phat-no-hoa-lien","than-kiem-ngu-loi","han-bang-phong-an","kim-chung-trao"};
            for(int i=0;i<4;i++)loadout.Equip(i,ids[i]);world.player.GetComponent<SpiritPower>().Refill();yield return Frames();yield return Tail();
        }
        IEnumerator Tail()
        {
            var loadout=world.player.GetComponent<SkillLoadout>();string[] ids={"phat-no-hoa-lien","than-kiem-ngu-loi","han-bang-phong-an","kim-chung-trao"};
            yield return new WaitForSeconds(3);world.player.GetComponent<SpiritPower>().Refill();
            for(int i=1;i<4;i++)
            {
                world.player.GetComponent<SpiritPower>().Refill();
                var pt=Point(TouchRole.Skill1+i);Touch(10+i,TouchPhase.Began,pt);yield return Frames();Check(loadout.Get(i).IsAiming,"Touch aims "+ids[i]);Touch(10+i,TouchPhase.Ended,pt);yield return Frames();Check(loadout.Get(i).CooldownRemaining>0,"Touch casts "+ids[i]);yield return new WaitForSeconds(1.3f);
            }
            var kill=world.victims[0];kill.SetMaxHealth(10,true);kill.ApplyDamage(DamageInfo.Create(100,Element.None,DamageSource.Melee,kill.transform.position,Vector3.right,world.player.gameObject));yield return Frames();Check(kill.IsDead,"Enemy receives hit and dies on new campus materials");
            var pause=Point(TouchRole.Pause);Touch(25,TouchPhase.Began,pause);yield return Frames();Check(UIStateManager.Instance.State==UIState.Paused&&Time.timeScale==0,"Round mobile pause freezes game");Touch(25,TouchPhase.Ended,pause);yield return Frames();UIStateManager.Instance.Resume();yield return Frames();Check(Time.timeScale>0,"Resume restores gameplay");
            Check(Shader.GetGlobalFloat("_CampusLowQuality")>.5f,"Mobile disables normal/detail/notice layer");
            Check(!ComicRendering.Enabled,"Mobile ink disabled");
        }
        IEnumerator GoldenBellFailure()
        {
            Directory.CreateDirectory("task/look");original=SettingsManager.Instance.Current.Copy();
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.Mode(true);world.Lighting(false);UIValidation.SetResolution(1920,1080);
            hud=FindAnyObjectByType<MobileControlsHUD>();yield return new WaitForSecondsRealtime(1.3f);world.Arrange();world.Lighting(false);
            oldInputSettings=InputSystem.settings;testInputSettings=Instantiate(oldInputSettings);testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInputSettings;
            oldMouse=Mouse.current;if(oldMouse!=null)InputSystem.DisableDevice(oldMouse);touch=InputSystem.AddDevice<Touchscreen>();
            var loadout=world.player.GetComponent<SkillLoadout>();loadout.Equip(3,"kim-chung-trao");world.player.GetComponent<SpiritPower>().Refill();yield return Frames();
            var pt=Point(TouchRole.Skill4);Touch(13,TouchPhase.Began,pt);yield return Frames();Check(loadout.Get(3).IsAiming,"Touch aims kim-chung-trao");Touch(13,TouchPhase.Ended,pt);yield return Frames();Check(loadout.Get(3).CooldownRemaining>0,"Touch casts kim-chung-trao");
        }
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(GoldenBellOnly?GoldenBellFailure():string.IsNullOrEmpty(ResumeCheckpoint)?Run():ResumeTail());while(stack.Count>0)
            {
                var run=stack.Peek();bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check(false,e.ToString());break;}
                if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
            }
            if(touch!=null)InputSystem.RemoveDevice(touch);if(oldMouse!=null)InputSystem.EnableDevice(oldMouse);if(oldInputSettings!=null)InputSystem.settings=oldInputSettings;if(testInputSettings!=null)Destroy(testInputSettings);
            Time.timeScale=1;if(world!=null)world.End();UIValidation.SetResolution(1920,1080);if(original!=null)SettingsManager.Instance.Apply(original,false);
            Write();File.WriteAllText("task/look/smoke-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
    }
}
#endif
