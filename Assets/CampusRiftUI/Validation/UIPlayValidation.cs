#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CampusRift;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Explicit, repeatable Play Mode acceptance checks, never included in a player build.
public sealed class UIPlayValidation : MonoBehaviour
{
    public bool SkipCaptureMatrix;
    public bool FailedItemsOnly;
    [Serializable] public sealed class Report {public List<string> passed=new List<string>();public List<string> failed=new List<string>();public List<string> captures=new List<string>();}
    Report report=new Report();Keyboard keyboard;Mouse mouse;GameSettings original;bool oldBackground;
    static readonly Vector2Int[] Sizes={new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(2560,1440),new Vector2Int(1920,1200)};
    UIStateManager State=>UIStateManager.Instance;
    UIManager View=>Object.FindAnyObjectByType<UIManager>();
    [MenuItem("Campus Rift/UI/Run Play Acceptance Tests")]
    public static void Begin()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode from MainMenu first.");
        if(Object.FindAnyObjectByType<UIPlayValidation>()!=null)return;
        Application.runInBackground=true;
        var go=new GameObject("UI Acceptance Runner");DontDestroyOnLoad(go);go.AddComponent<UIPlayValidation>();
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory("Artifacts/UI");
        oldBackground=Application.runInBackground;Application.runInBackground=true;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
        original=SettingsManager.Instance.Current.Copy();
        var test=Run();
        while(true)
        {
            object next=null;bool moved=false;
            try{moved=test.MoveNext();if(moved)next=test.Current;}catch(Exception e){report.failed.Add(e.ToString());break;}
            if(!moved)break;yield return next;
        }
        CampusRift.Progression.ProfileService.Instance?.EndTransient();
        if(SettingsManager.Instance!=null)SettingsManager.Instance.Apply(original);
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
        Application.runInBackground=oldBackground;
        File.WriteAllText("Artifacts/UI/UI_TEST_REPORT.json",JsonUtility.ToJson(report,true));
        Debug.Log($"CAMPUS RIFT UI ACCEPTANCE: {report.passed.Count} passed / {report.failed.Count} failed. Artifacts/UI/UI_TEST_REPORT.json");
        Destroy(gameObject);
    }
    void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);}
    void Click(string path)
    {
        var target=View.transform.Find(path);if(target==null)throw new Exception("Button not found: "+path);
        var button=target.GetComponent<Button>();if(button==null||!button.IsInteractable())throw new Exception("Button unavailable: "+path);
        button.onClick.Invoke();
    }
    IEnumerator Key(Key key)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.3f);
    }
    IEnumerator WaitScene()
    {
        float until=Time.realtimeSinceStartup+30;
        while(GameSceneManager.Instance.IsLoading && Time.realtimeSinceStartup<until)yield return null;
        if(GameSceneManager.Instance.IsLoading)throw new Exception("Scene load timeout");
        yield return new WaitForSecondsRealtime(.1f);
    }
    void Layout(string label)
    {
        try{Check(true,label+" / "+UIValidation.AuditLayout());}catch(Exception e){Check(false,label+" / "+e.Message);}
    }
    IEnumerator Capture(string name)
    {
        if(SkipCaptureMatrix)yield break;
        string path="Artifacts/UI/"+name+".png";ScreenCapture.CaptureScreenshot(path);report.captures.Add(path);yield return new WaitForSecondsRealtime(.15f);
    }
    IEnumerator Responsive(string label)
    {
        if(SkipCaptureMatrix){UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(.3f);Layout(label);yield break;}
        foreach(var size in Sizes)
        {
            UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);
            Layout(label);yield return Capture(label+"-"+size.x+"x"+size.y);
        }
        UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(.15f);
    }
    IEnumerator Run()
    {
        if(SceneManager.GetActiveScene().path!=GameSceneManager.Instance.Config.MainMenuScene){GameSceneManager.Instance.LoadMainMenu();yield return WaitScene();}
        yield return new WaitForSecondsRealtime(1);
        if(State.State==UIState.Hub)State.Back();
        CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());State.OpenHub();State.Back();yield return null;
        if(!FailedItemsOnly)Check(State.State==UIState.Menu && Cursor.visible && Time.timeScale==1,"Launch Main Menu / cursor / time");
        Check(!View.MainMenu.transform.Find("Navigation/CONTINUE").GetComponent<Button>().interactable,"Continue disabled without save provider");
        if(!FailedItemsOnly)yield return Responsive("MainMenu");
        Click("MainMenu/Navigation/COURSES");yield return new WaitForSecondsRealtime(.3f);Check(State.State==UIState.Hub&&HubUI.Instance!=null&&HubUI.Instance.Visible,"Courses button opens real Hub");if(!FailedItemsOnly){Layout("Courses");yield return Capture("Courses");}yield return Key(UnityEngine.InputSystem.Key.Escape);
        Click("MainMenu/Navigation/CREDITS");yield return new WaitForSecondsRealtime(.3f);if(!FailedItemsOnly)Check(State.State==UIState.Credits,"Credits opens");
        var credits=View.Credits.GetComponent<P21CreditsMenu>();credits.Show(0);
        var body=View.Credits.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="Credits content");string firstPage=body.text;
        var page=View.Credits.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="Page");int pages=int.Parse(page.text.Split('/')[1].Trim());
        View.Credits.GetComponentsInChildren<Button>().First(b=>b.name=="Next").onClick.Invoke();
        Check(pages>=2&&body.text!=firstPage&&page.text.StartsWith("2 /"),"Credits paged catalog exposes overflow content via Next");yield return Capture("Credits");yield return Key(UnityEngine.InputSystem.Key.Escape);
        Click("MainMenu/Navigation/SETTINGS");yield return new WaitForSecondsRealtime(.3f);
        var settings=Object.FindAnyObjectByType<SettingsUI>();
        yield return Responsive("Settings-Video");
        settings.SelectAudio();yield return Responsive("Settings-Audio");
        var slider=settings.Volumes[3];var area=(RectTransform)slider.handleRect.parent;
        var clickPoint=area.TransformPoint(new Vector3(area.rect.xMin+area.rect.width*.25f,area.rect.center.y,0));
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=clickPoint};
        ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        Check(Mathf.Abs(slider.value-.25f)<.03f,"Pointer interaction sets slider from track position");
        settings.SelectGameplay();yield return Responsive("Settings-Gameplay");
        settings.SelectVideo();settings.Resolution.Show();yield return new WaitForSecondsRealtime(.3f);
        Check(settings.Resolution.IsExpanded,"Resolution dropdown opens");yield return Capture("Resolution-Dropdown");
        yield return Key(UnityEngine.InputSystem.Key.Escape);Check(State.State==UIState.Settings && !settings.Resolution.IsExpanded,"ESC closes dropdown before Settings");
        settings.Volumes[3].value=.35f;settings.MouseSensitivity.value=1.2f;settings.InvertY.isOn=true;settings.Apply();
        float db;Check(SettingsManager.Instance.Mixer.GetFloat("MonsterVolume",out db) && Mathf.Abs(db-20*Mathf.Log10(.35f))<.02f,"Monster volume isolated mixer control");
        Check(Mathf.Abs(SettingsManager.Instance.ReadSaved().MonsterVolume-.35f)<.001f,"Settings saved and loaded from PlayerPrefs");
        settings.Volumes[3].value=.1f;settings.Cancel();State.OpenSettings();
        Check(Mathf.Abs(settings.Volumes[3].value-.35f)<.001f,"Rapid Cancel/reopen discards un-applied changes");
        yield return Key(UnityEngine.InputSystem.Key.Escape);Check(State.State==UIState.Menu,"ESC returns Settings to Main Menu");
        Click("MainMenu/Navigation/QUIT");Check(Application.isPlaying,"Editor Quit logs without closing editor");
        Click("MainMenu/Navigation/PLAY");bool hub=State.State==UIState.Hub&&HubUI.Instance.Visible;GameSceneManager.Instance.StartSandbox();Check(hub&&State.State==UIState.Loading && !State.GameplayInputEnabled,"PLAY enters Hub, gameplay entry loads and blocks input");
        yield return Capture("Loading");yield return WaitScene();
        Check(SceneManager.GetActiveScene().path==GameSceneManager.Instance.Config.GameplayScene,"PLAY loads audited SampleScene");
        Check(State.State==UIState.Gameplay && Time.timeScale==1,"Gameplay state after scene activation");
        var player=Object.FindAnyObjectByType<CampusExplorer>();var hp=player.GetComponent<PlayerMonsterHealth>();var hud=Object.FindAnyObjectByType<GameplayHUD>();
        Check(Mathf.Abs(player.mouseSensitivity-.144f)<.001f && player.invertY,"Sensitivity and invert persist into Player adapter");SettingsManager.Instance.Apply(original);
        var monster=Object.FindAnyObjectByType<MonsterBrain>();var nav=monster.GetComponent<MonsterNavigation>();var source=monster.GetComponent<AudioSource>();
        Check(nav.Ready,"Monster navigation remains ready");
        Check(source.clip!=null && source.spatialBlend==1 && source.isPlaying && source.outputAudioMixerGroup.name=="Monster","Monster spatial audio playing through separate group");
        var monsterBefore=monster.transform.position;
        var before=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return new WaitForSecondsRealtime(.4f);
        Check(Vector3.Distance(before,player.transform.position)>.1f,"W input moves existing Player");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift));yield return new WaitForSecondsRealtime(.4f);Check(player.IsSprinting,"Shift sprint remains active");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        Check(Vector3.Distance(monsterBefore,monster.transform.position)>.01f || monster.CurrentState==MonsterState.Attack,"Monster AI moves or attacks after UI integration");
        yield return Key(UnityEngine.InputSystem.Key.Escape);
        Check(State.State==UIState.Paused && Time.timeScale==0 && Cursor.visible,"Real ESC pauses and unlocks cursor");
        var pausePos=player.transform.position;var rotation=player.followCamera.transform.rotation;var monsterPause=monster.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.Space));InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(200,200)});yield return new WaitForSecondsRealtime(.2f);
        Check(Vector3.Distance(pausePos,player.transform.position)<.001f && Quaternion.Angle(rotation,player.followCamera.transform.rotation)<.001f,"Paused movement/jump/look blocked");
        Check(Vector3.Distance(monsterPause,monster.transform.position)<.001f,"Monster movement freezes during Pause");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());
        yield return Responsive("Pause");
        Click("PauseMenu/Pause Card/SETTINGS");yield return new WaitForSecondsRealtime(.3f);yield return Key(UnityEngine.InputSystem.Key.Escape);
        Check(State.State==UIState.Paused && Time.timeScale==0,"Real ESC returns Settings to Pause");
        // Render HUD without advancing combat while auditing layout; runtime state is restored next.
        State.Resume();Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);yield return Responsive("HUD");Time.timeScale=1;
        Check(!hud.GetComponent<CanvasGroup>().blocksRaycasts,"HUD display does not intercept input");
        hp.TakeDamage(7);Check(hud.Health.Value.text==$"{Mathf.CeilToInt(hp.CurrentHealth)} / {Mathf.CeilToInt(hp.maxHealth)}","HP event updates value immediately");
        hud.Breakthrough.SetProgress(3,5);Check(hud.Breakthrough.Count.text=="3 / 5","Breakthrough reusable 3/5 API");hud.Breakthrough.SetProgress(5,5);Check(hud.Breakthrough.Available.gameObject.activeSelf,"5/5 shows breakthrough available");hud.Breakthrough.SetProgress(0,5);
        hud.Skills.Slots[0].SetCooldown(5,10);Check(Mathf.Abs(hud.Skills.Slots[0].CooldownOverlay.fillAmount-.5f)<.01f,"Skill cooldown radial API");hud.Skills.Slots[0].SetCooldown(0,10);
        hud.Objective.CompleteObjective();Check(hud.Objective.Status.text.Contains("COMPLETE"),"Objective completion includes text status");hud.Objective.SetObjective("Escape from the monster.");
        hud.Warning.SetWarning(true);Check(hud.Warning.Indicator.gameObject.activeSelf,"Explicit nondirectional monster warning hook");hud.Warning.SetWarning(false);
        // Existing elevator modal tested with the player placed inside the existing cabin.
        var elevator=Object.FindObjectsByType<CampusElevator>().First();var cc=player.GetComponent<CharacterController>();
        var savedPos=player.transform.position;cc.enabled=false;player.transform.position=elevator.cabinSpaces[0].center-Vector3.up*.85f+Vector3.up*elevator.CabinOffset;cc.enabled=true;
        var interaction=player.GetComponent<ElevatorInteraction>();interaction.OpenPanel(elevator);Check(interaction.PanelOpen && State.State==UIState.Modal && !State.GameplayInputEnabled,"Elevator modal takes exclusive gameplay input");
        Check(player.CurrentSpeed==0 && !player.IsSprinting,"Elevator modal clears movement telemetry and sprint animation");
        var cameraBeforeModal=player.followCamera.transform.position;
        // Emulate cabin carriage, without injecting look input or changing the elevator logic.
        cc.enabled=false;player.transform.position+=Vector3.up*.25f;cc.enabled=true;yield return new WaitForSecondsRealtime(.1f);
        Check(Vector3.Distance(cameraBeforeModal,player.followCamera.transform.position)>.01f,"Camera follows cabin displacement while modal blocks look input");
        yield return Key(UnityEngine.InputSystem.Key.Escape);Check(!interaction.PanelOpen && State.State==UIState.Gameplay,"Real ESC closes elevator without also opening Pause");
        player.ReturnToSpawn();
        // Reset damage immunity in the temporary test fixture before issuing the lethal hit.
        typeof(PlayerMonsterHealth).GetField("protectedUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hp,0f);
        hp.TakeDamage(10000);yield return new WaitForSecondsRealtime(.3f);
        Check(hp.CurrentHealth==0 && State.State==UIState.GameOver && Time.timeScale==0,"Health death event opens GameOver without respawn");
        yield return Key(UnityEngine.InputSystem.Key.Escape);Check(State.State==UIState.GameOver,"GameOver overrides ESC Pause");
        yield return Responsive("GameOver");Click("GameOver/Defeat Card/RETRY");yield return WaitScene();
        Check(Time.timeScale==1 && State.GameplayInputEnabled && !Cursor.visible,"Retry restores time and gameplay cursor");
        Check(Object.FindAnyObjectByType<PlayerMonsterHealth>().CurrentHealth==Object.FindAnyObjectByType<PlayerMonsterHealth>().maxHealth,"Retry restores fresh Player health");
        Check(Object.FindObjectsByType<UIServices>().Length==1 && Object.FindObjectsByType<EventSystem>().Length==1,"Retry has one services root and one EventSystem");
        State.Pause();Click("PauseMenu/Pause Card/RESTART");yield return WaitScene();Check(State.State==UIState.Gameplay && Time.timeScale==1,"Pause Restart returns to gameplay");
        State.Pause();Click("PauseMenu/Pause Card/MAIN MENU");yield return WaitScene();Check(State.State==UIState.Hub && HubUI.Instance.Visible && Time.timeScale==1 && Cursor.visible,"Pause Main Menu restores Hub/time/cursor");
        Check(Object.FindObjectsByType<UIServices>().Length==1 && Object.FindObjectsByType<EventSystem>().Length==1,"Round trip has no duplicate managers/EventSystem");
        var clone=Instantiate(Resources.Load<UIServices>("CampusRiftServices"));yield return null;
        Check(Object.FindObjectsByType<UIServices>().Length==1 && State!=null && SettingsManager.Instance!=null,"Duplicate service prefab safely rejected");
        yield return Capture("MainMenu-Final");
    }
}
#endif
