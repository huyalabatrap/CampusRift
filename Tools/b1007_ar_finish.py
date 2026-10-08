from b1007_runner import *
backup=ROOT/'Backups/Regression-APK-pre-20261006'
def change(path,a,b):
    p=ROOT/path;s=p.read_text(encoding='utf-8-sig');assert a in s,path
    q=backup/path
    if not q.exists():q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
    p.write_text(s.replace(a,b,1),encoding='utf-8')
change('Assets/ARRift/Runtime/ARCombatHUD.cs','sealText.margin=Vector4.zero;caster.UltimateFired+=Ultimate;caster.CastAttempted+=Cast;','sealText.margin=Vector4.zero;caster.UltimateFired+=Ultimate;caster.CastAttempted+=Cast;field.GetComponent<ARMonsterDirector>().BattleStarted+=NewBattle;')
change('Assets/ARRift/Runtime/ARCombatHUD.cs','public void ToggleActive()','void NewBattle(){introUntil=Time.unscaledTime+5;ultimateUntil=0;}\n        public void ToggleActive()')
change('Assets/ARRift/Runtime/ARCombatHUD.cs','void OnDestroy(){if(caster!=null)','void OnDestroy(){if(field!=null)field.GetComponent<ARMonsterDirector>().BattleStarted-=NewBattle;if(caster!=null)')
change('Assets/ARRift/Runtime/ARHandsStudyHUD.cs','canvas=ARUI.Canvas(transform,"AR hands study HUD");','director.BattleStarted+=NewBattle;canvas=ARUI.Canvas(transform,"AR hands study HUD");')
change('Assets/ARRift/Runtime/ARHandsStudyHUD.cs','void Update()','void NewBattle(){introUntil=Time.unscaledTime+5;}\n        void Update()')
change('Assets/ARRift/Runtime/ARHandsStudyHUD.cs','void OnDestroy(){if(canvas!=null)','void OnDestroy(){if(director!=null)director.BattleStarted-=NewBattle;if(canvas!=null)')
change('Assets/ARRift/Runtime/ARHandsStudyHUD.cs','foreach(var rect in new[]{rhythmPanel,lane,answerPanel}){var image=rect.GetComponent<Image>();var color=image.color;color.a=.42f;image.color=color;}','foreach(var rect in new[]{rhythmPanel,lane,answerPanel})foreach(var image in rect.GetComponentsInChildren<Image>(true)){var color=image.color;color.a*=.42f;image.color=color;}')
change('Assets/ARRift/Runtime/ARSessionBootstrap.cs','Hình ảnh và điểm bàn tay được xử lý ngay trên máy, không lưu, không gửi đi. Chỉ mở camera sau khi bạn đồng ý.','Hình ảnh và điểm bàn tay được xử lý trên máy, không gửi đi. Không lưu hình ảnh khi nhận cử chỉ; chỉ lưu clip cục bộ nếu bạn bật Quay clip và đồng ý trong hộp thoại Android. Điểm tay chỉ giữ tạm trong RAM. Chỉ mở camera sau khi bạn đồng ý.')
change('Assets/ARRift/Runtime/ARSessionBootstrap.cs','Images and hand landmarks are processed on this device, never saved or uploaded. The camera opens only after your permission.','Images and hand landmarks are processed on this device, never uploaded. Gesture recognition saves no images; a local clip is saved only if you enable Record Clip and approve the Android dialog. Hand landmarks stay briefly in RAM. The camera opens only after your permission.')
call('refresh_unity',{'mode':'force','scope':'all','compile':'request','wait_for_ready':True});time.sleep(2)
progress('Mốc AR trước lượt suite: intro5s reset cảReplay; texturepanel Luyện Ấn/đápán cũng bántrongsuốt; consentnói rõ ngoạilệclip opt-in được Android chophép. Bốn suitefail game thường đã retest đúng cácmục1lần vàPASS; rawlượtđầu vẫngiữ.')
print('AR polish refreshed')
