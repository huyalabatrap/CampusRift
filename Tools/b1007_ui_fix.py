from pathlib import Path
import shutil
p=Path('Assets/CampusRiftUI/Validation/UIPlayValidation.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('    public bool SkipCaptureMatrix;', '    public bool SkipCaptureMatrix;\n    public bool FailedItemsOnly;')
s=s.replace('        if(SettingsManager.Instance!=null)SettingsManager.Instance.Apply(original);', '        CampusRift.Progression.ProfileService.Instance?.EndTransient();\n        if(SettingsManager.Instance!=null)SettingsManager.Instance.Apply(original);',1)
s=s.replace('        Check(State.State==UIState.Menu', '        if(State.State==UIState.Hub)State.Back();\n        CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());State.OpenHub();State.Back();yield return null;\n        if(!FailedItemsOnly)Check(State.State==UIState.Menu',1)
s=s.replace('        yield return Responsive("MainMenu");','        if(!FailedItemsOnly)yield return Responsive("MainMenu");',1)
s=s.replace('Check(State.State==UIState.Course,"Courses button opens placeholder")','Check(State.State==UIState.Hub&&HubUI.Instance!=null&&HubUI.Instance.Visible,"Courses button opens real Hub")')
s=s.replace('Layout("Courses");yield return Capture("Courses");','if(!FailedItemsOnly){Layout("Courses");yield return Capture("Courses");}')
s=s.replace('Check(State.State==UIState.Credits,"Credits opens");','if(!FailedItemsOnly)Check(State.State==UIState.Credits,"Credits opens");')
old='var scroll=View.Credits.GetComponentInChildren<ScrollRect>();Check(scroll!=null && scroll.content.rect.height>scroll.viewport.rect.height,"Credits Scroll View has overflow content");scroll.verticalNormalizedPosition=0;'
new='''var credits=View.Credits.GetComponent<P21CreditsMenu>();credits.Show(0);
        var body=View.Credits.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="Credits content");string firstPage=body.text;
        var page=View.Credits.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="Page");int pages=int.Parse(page.text.Split('/')[1].Trim());
        View.Credits.GetComponentsInChildren<Button>().First(b=>b.name=="Next").onClick.Invoke();
        Check(pages>=2&&body.text!=firstPage&&page.text.StartsWith("2 /"),"Credits paged catalog exposes overflow content via Next");'''
assert old in s;s=s.replace(old,new)
old='Click("MainMenu/Navigation/PLAY");Check(State.State==UIState.Loading && !State.GameplayInputEnabled,"PLAY enters Loading and blocks input");'
new='Click("MainMenu/Navigation/PLAY");bool hub=State.State==UIState.Hub&&HubUI.Instance.Visible;GameSceneManager.Instance.StartSandbox();Check(hub&&State.State==UIState.Loading && !State.GameplayInputEnabled,"PLAY enters Hub, gameplay entry loads and blocks input");'
assert old in s;s=s.replace(old,new)
s=s.replace('Check(State.State==UIState.Menu && Time.timeScale==1 && Cursor.visible,"Pause Main Menu restores menu/time/cursor")','Check(State.State==UIState.Hub && HubUI.Instance.Visible && Time.timeScale==1 && Cursor.visible,"Pause Main Menu restores Hub/time/cursor")')
p.write_text(s,encoding='utf-8')
