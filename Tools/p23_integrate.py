from pathlib import Path
def edit(path,old,new):
 p=Path(path);s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old);p.write_text(s.replace(old,new),encoding='utf-8')
ui='Assets/CampusRiftUI/Runtime/'
edit(ui+'SettingsManager.cs','public bool ReduceCameraShake;','public bool ReduceCameraShake;\n        public bool Subtitles=true, SlowReading;\n        public int TextSize;\n        public AccessiblePalette AccessibleColors;')
edit(ui+'SettingsManager.cs','s.TouchSensitivity=Mathf.Clamp','s.TextSize=Mathf.Clamp(s.TextSize,0,2);\n            if(!Enum.IsDefined(typeof(AccessiblePalette),s.AccessibleColors))s.AccessibleColors=AccessiblePalette.Default;\n            s.TouchSensitivity=Mathf.Clamp')
edit('Assets/Combat/Runtime/Element.cs','switch (e)\n            {\n                case Element.Kim: return new Color','if (UI.Accessibility.ColorBlind) return UI.Accessibility.Color(e);\n            switch (e)\n            {\n                case Element.Kim: return new Color')
edit(ui+'FireWarningHUD.cs','arrow.gameObject.SetActive(DoorNode>=0);','arrow.gameObject.SetActive(DoorNode>=0&&s!=Shelter.Indoor);')
edit('Assets/Learning/Runtime/LearningUI.cs','Time.unscaledTime+LearningEngine.PracticeMinutes*60f','Time.unscaledTime+LearningEngine.PracticeMinutes*60f*Accessibility.ReadingTime(QuizKindProxy.Study)')
# Lesson/shrine have no deadline in the design; describe that explicitly in Settings. Exam clock unchanged.
edit('Assets/SkyBeast/Runtime/SkyBeastController.cs','LastRoarVariant=next;RoarCount++;','UI.ImportantCaptions.Show(UI.LevelHUD.Vietnamese?definition.nameVN+" [gầm vang]":definition.nameEN+" [gầm vang]",definition.nameEN+" [roars]",3);\n            LastRoarVariant=next;RoarCount++;')
edit('Assets/SkyBeast/Runtime/FireBreathCycle.cs','if(phase==Phase.Warning){TickCount=0;','if(phase==Phase.Warning){UI.ImportantCaptions.Show("[Còi báo Thiên Hỏa] Tìm mái che!","[Fire storm alarm] Find shelter!",4);TickCount=0;')
edit('Assets/SkyBeast/Runtime/FireBreathCycle.cs','void Enter(Phase phase)','void Enter(Phase phase)')
for name in ['P21StoryCinematic.cs','HeavenSwordCinematic.cs']:
 edit('Assets/SkyBeast/Runtime/'+name,'if(c!=overlay&&c.enabled)','if(c!=overlay&&c.GetComponent<ImportantCaptions>()==null&&c.enabled)')
edit('Assets/SkyBeast/Runtime/SkyBeastIdentity.cs','new Color(.40f,.13f,.13f)','new Color(.74f,.40f,.35f)')
edit('Assets/SkyBeast/Runtime/SkyBeastIdentity.cs','new Color(1.2f,.15f,.025f)','new Color(.025f,.0015f,.001f)')
edit('Assets/SkyBeast/Runtime/P21StoryCinematic.cs','cam.fieldOfView=Ending?(p<.65f?60:48):65;','cam.fieldOfView=Ending?(p<.65f?60:48):Mathf.Lerp(65,42,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,p)));\n            if(!Ending&&dragon!=null&&p>.3f){float close=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,p));var focus=dragon.transform.position+Vector3.up*7;cam.transform.position=Vector3.Lerp(cam.transform.position,focus+new Vector3(54,10,-78),close);cam.transform.rotation=Quaternion.LookRotation(Vector3.Lerp(look,focus,close)-cam.transform.position);}')
# Scale accessibility captions without removing the existing story narration/subtitles.
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','title=ComboUIFactory.Text','ImportantCaptions.Show("[Vạn Kiếm hội tụ] Thiên Kiếm giáng xuống cự thú.","[Swords gather] The Heaven Sword descends upon the sky beast.",duration);\n            title=ComboUIFactory.Text')
# The modern HUD deliberately hides BreakthroughProgressUI. Assert real LevelHUD and CultivationService.
edit('Assets/Learning/Validation/LearningPlayTest.cs','var hud = Object.FindAnyObjectByType<BreakthroughProgressUI>();\n            Check(hud != null && hud.Current == 4, "The HUD ring shows four finished tiers");','var hud = Object.FindAnyObjectByType<LevelHUD>();\n            Check(hud != null && cultivation.Tier == 5 && hud.BodyText.Length>0, "Current LevelHUD is present while four tiers are complete");')
edit('Assets/Learning/Validation/LearningPlayTest.cs','cultivation.CanAttemptBreakthrough && hud.Current == 5','cultivation.CanAttemptBreakthrough && hud != null && cultivation.Tier == 5')
edit('Assets/Learning/Validation/LearningPlayTest.cs','A full tier 5 is the bottleneck and lights the whole ring','A full tier 5 is the bottleneck in the current HUD')
# Mode/floor fields retain schema1 compatibility and make future telemetry distinguish P22 modes.
edit('Assets/Progression/Runtime/LocalTelemetry.cs','public string kind, utc, outcome, deathCause;','public string kind, utc, outcome, deathCause, mode;\n            public int towerFloor;')
edit('Assets/Progression/Runtime/LocalTelemetry.cs','kind = "run", level = d.index, utc','kind = "run", level = d.index, mode=d.runMode.ToString(), towerFloor=d.towerFloor, utc')
print('P23 integration applied')
