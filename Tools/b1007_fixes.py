from b1007_runner import *
stop()
backup=ROOT/'Backups/Regression-APK-pre-20261006'
def edit(path,changes):
    p=ROOT/path;b=backup/path
    if not b.exists():b.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,b)
    s=p.read_text(encoding='utf-8-sig')
    for a,z in changes:
        assert a in s,(path,a[:70]);s=s.replace(a,z,1)
    p.write_text(s,encoding='utf-8')
edit('Assets/Skills/Core/Runtime/Set1SkillRuntime.cs',[
 ('if(!aiming)return false;\n            if(aim!=null)', 'if(!aiming)return false;\n            // Instant self/chain skills need no valid ground target on touch release.\n            if(definition!=null&&definition.castType==CastType.Instant)return QuickCast();\n            if(aim!=null)')])
edit('Assets/Controls/Runtime/LookSmoke.cs',[
 ('[NonSerialized] public string ResumeCheckpoint;', '[NonSerialized] public string ResumeCheckpoint;\n        [NonSerialized] public bool GoldenBellOnly;'),
 ('yield return new WaitForSeconds(3);world.player.GetComponent<SpiritPower>().Refill();', 'yield return new WaitForSeconds(3);world.player.GetComponent<SpiritPower>().Refill();'),
 ('var pt=Point(TouchRole.Skill1+i);Touch(10+i', 'world.player.GetComponent<SpiritPower>().Refill();\n                var pt=Point(TouchRole.Skill1+i);Touch(10+i'),
 ('IEnumerator Start()\n        {', '''IEnumerator GoldenBellFailure()
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
        {'''),
 ('stack.Push(string.IsNullOrEmpty(ResumeCheckpoint)?Run():ResumeTail());', 'stack.Push(GoldenBellOnly?GoldenBellFailure():string.IsNullOrEmpty(ResumeCheckpoint)?Run():ResumeTail());')])
edit('Assets/Controls/Runtime/BoostEnergyPlayTest.cs',[
 ('Keyboard keyboard,oldKeyboard;', 'public bool ExhaustionOnly;\n        Keyboard keyboard,oldKeyboard;'),
 ('IEnumerator Start()\n        {', '''bool ExhaustionDisplayed(MobileControlsHUD mobile)
        {
            var boost=mobile.Zones.First(z=>z.role==TouchRole.Sprint);
            var arc=boost.transform.Find("Cooldown arc").GetComponent<MobileArc>();
            var fill=(float)typeof(MobileArc).GetField("fill",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(arc);
            var edge=boost.transform.Find("Rift edge").GetComponent<RiftGraphic>();
            return player.BoostExhausted && hud.Label.text==CampusRift.Localization.LocalizationService.Instance.Translate("RECOVERING") && Mathf.Abs(hud.Fill.fillAmount-player.EnergyFraction)<.01f && arc.isActiveAndEnabled && Mathf.Abs(fill-player.EnergyFraction)<.01f && edge.isActiveAndEnabled && ((Vector4)edge.color-(Vector4)new Color(.4f,.46f,.57f)).sqrMagnitude<.001f;
        }
        IEnumerator ExhaustionFailure()
        {
            Directory.CreateDirectory(Output);running=true;Application.runInBackground=true;UIStateManager.Instance.EnterScene(true);
            originalGame=SettingsManager.Instance.Current.Copy();Mode(ControlMode.Mobile);player=FindAnyObjectByType<CampusExplorer>();input=player.GetComponent<CampusInput>();
            var level=FindAnyObjectByType<CampusRift.Levels.LevelDirector>();if(level!=null)level.enabled=false;foreach(var brain in FindObjectsByType<MonsterBrain>())brain.gameObject.SetActive(false);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(500,-.1f,600);floor.transform.localScale=new Vector3(100,.2f,500);player.spawnPosition=new Vector3(500,.1f,500);player.ReturnToSpawn();yield return new WaitForSeconds(.3f);hud=FindAnyObjectByType<PlayerEnergyUI>();
            input.TouchMove=Vector2.up;input.TouchSprint=true;float until=Time.time+12;while(!player.BoostExhausted&&Time.time<until)yield return null;yield return new WaitForSeconds(.3f);
            Check(ExhaustionDisplayed(FindAnyObjectByType<MobileControlsHUD>()),"HUD and mobile boost button both display exhaustion");input.TouchMove=Vector2.zero;input.TouchSprint=false;running=false;
            File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
        IEnumerator Start()
        {
            if(ExhaustionOnly){yield return ExhaustionFailure();yield break;}'''),
 ('Check(hud.Label.text==CampusRift.Localization.LocalizationService.Instance.Translate("RECOVERING") && Mathf.Abs(hud.Fill.fillAmount-player.EnergyFraction)<.01f &&\n                mobile.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==CampusRift.Localization.LocalizationService.Instance.Translate("RECOVER")),"HUD and mobile boost button both display exhaustion");', 'Check(ExhaustionDisplayed(mobile),"HUD and mobile boost button both display exhaustion");')])
edit('Assets/ARRift/Validation/ARNavigationPlayTest.cs',[
 ('var mode=FindAnyObjectByType<ARModeSession>();mode.Choose(ARModeCatalog.Training);FindAnyObjectByType<ARBattleHUD>().SetHelp(false);', 'var selection=FindAnyObjectByType<ARModeSelectionHUD>();selection.GetComponentsInChildren<Button>(true).First(b=>b.name=="Select training").onClick.Invoke();selection.GetComponentsInChildren<Button>(true).First(b=>b.name=="Start selected").onClick.Invoke();FindAnyObjectByType<ARBattleHUD>().SetHelp(false);'),
 ('placement.enabled=true;yield return Wait(()=>placement.Root!=null);', 'placement.enabled=true;yield return Wait(()=>placement.ReticleValid);placement.Confirm();yield return Wait(()=>placement.Root!=null);yield return Wait(()=>placement.Adjusting);placement.StartBattlefield();')])
edit('Assets/ARRift/Validation/ARRiftPlayTest.cs',[
 ('yield return Hold("Thumb_Up",.7f);', 'yield return Hold("ILoveYou",.7f);'),
 ('void Reset()', 'void Reset()'),
 ('void Reset(){', 'void Reset(){typeof(ARSkillCaster).GetMethod("ResetEnergy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(caster,null);')])
edit('Assets/ARRift/Runtime/ARHandsStudyHUD.cs',[
 ('0,180,1420,170','0,180,1360,132'),(',-478,60,420,32,21',',-450,49,410,27,19'),(',215,60,890,32,18',',215,49,830,27,16'),
 ('0,0,1360,92','0,0,1300,72'),('-505,0,78,78','-470,0,62,62'),('-505,-58,110,24,15','-470,0,110,24,13'),
 ('0,0,66,66','0,0,54,54'),('0,0,42,52','0,0,36,44'),('465,-63,360,30,22','460,-49,320,26,19'),('-35,-63,530,30,18','-25,-49,530,26,16'),
 ('-505+(float)ahead*245','-470+(float)ahead*230')])
edit('Assets/MonsterShaban/Scripts/MonsterDoorInteraction.cs',[
 ('if (Mathf.Abs(transform.position.y - door.doorway.min.y) > 0.9f) continue;', '// A descending capsule can intersect the leaf above its feet.\n                if (transform.position.y + 1.7f < door.doorway.min.y + .15f || transform.position.y + .25f > door.doorway.max.y - .15f) continue;')])
edit('Assets/Scripts/CampusAutomaticDoor.cs',[
 ('if (position.y < doorway.min.y - 0.7f || position.y > doorway.min.y + 1.1f) return;', 'if (position.y + 1.7f < doorway.min.y + .15f || position.y + .25f > doorway.max.y - .15f) return;')])
progress('Mốc sửa/harness: backup thêm shared skill/door; instant touch Confirm dùng cùng quy tắc QuickCast; Boost kiểm ring+tint mới với cùng trạng thái cạn; ARNavigation đi qua UI chọn mode/Confirm polygon; ARRift reset Linh Ấn giữa fixture và dùng ILoveYou cho unmapped; Luyện Ấn giảm1360×132; door nhận capsule trên bậc thay vì chỉ cao độ chân. Các mục fail sẽ chỉ retest1 lần riêng.')
call('refresh_unity',{});time.sleep(2)
print('Changes staged and refresh requested',flush=True)
