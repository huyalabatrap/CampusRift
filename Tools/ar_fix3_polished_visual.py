from ar_fix3_local import *
def shot(kind,w,h):
    code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/fix3/'+kind+'-'+str(w)+'x'+str(h)+'.png");return true;');time.sleep(.4)
for w,h in [(2400,1080),(1600,720)]:
    code('UIValidation.SetResolution('+str(w)+','+str(h)+');var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.GetComponent<CampusRift.AR.ARBattleHUD>().enabled=false;p.Reposition();p.InputBlocked=true;return true;');time.sleep(.2)
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.GetComponent<CampusRift.AR.ARBattleHUD>().enabled=false;p.InputBlocked=true;return true;');time.sleep(.6)
    code('var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>();typeof(CampusRift.AR.ARBattleHUD).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(h,null);h.GetComponent<CampusRift.AR.RiftPlacementService>().InputBlocked=true;return true;');time.sleep(.05)
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.GetComponent<CampusRift.AR.ARBattleHUD>().enabled=false;p.InputBlocked=true;return true;')
    shot('placement',w,h)
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.GetComponent<CampusRift.AR.ARBattleHUD>().enabled=true;p.InputBlocked=false;p.Confirm();return true;')
    for i in range(20):
        time.sleep(.2)
        if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Shrine!=null;'):break
    code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;');time.sleep(1)
    shot('battle',w,h)
    save(out/('coverage-final-'+str(w)+'.json'),code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>().Coverage();'))
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>().SetMenu(true);return true;');time.sleep(.2);shot('menu',w,h)
    code('var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>();h.SetMenu(false);h.SetHelp(true);return true;');shot('guide',w,h)
    code('var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>();h.SetHelp(false);h.ShowCombo(0);return true;');shot('combo',w,h)
save(out/'focused-text-after-fix.json',code(Path('task/ar/fix3-focused-text.cs').read_text(encoding='utf-8')))
console(out/'polished-visual-console.json')
progress('C sửa text / visual cuối\n- Full ComicTextAudit1lượt phát hiện1issue status; đã hạbaseline, autosize nhãnBàn/Sàn và dịchĐổi vị trí. Kiểm hẹp chỉ3labelđổi trong focused-text-after-fix.json, không chạy lạifullaudit. Đã chụp lại10ảnh saucodeđổi; disableplane manager sauanchor đểvisualkhôngbật lại. TiếpARharness1lượt.')
print((out/'focused-text-after-fix.json').read_text(encoding='utf-8'))
