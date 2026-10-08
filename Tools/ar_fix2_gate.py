from ar_fix2_local import *
import shutil
assert not (out/'DONE.txt').exists(),'Do not repeat completed harness'
for width,height in [(2400,1080),(1600,720)]:
    code('UIValidation.SetResolution('+str(width)+','+str(height)+');return true;');time.sleep(1)
    save(out/('layout-'+str(width)+'.json'),code('UnityEngine.Canvas.ForceUpdateCanvases();var canvas=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>().transform.Find("AR Battle HUD").GetComponent<UnityEngine.Canvas>();var content=canvas.transform.Find("AR safe area");var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)content).GetWorldCorners(corners);return new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,safe=UnityEngine.Screen.safeArea,canvas=((UnityEngine.RectTransform)canvas.transform).rect,contentCorners=corners,pixelRect=UnityEngine.Camera.main.pixelRect,renderScale=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().SessionPipeline.renderScale};'))
    code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/fix2/hud-'+str(width)+'x'+str(height)+'.png");return true;');time.sleep(.5)
save(out/'text-audit.json',code('var r=CampusRift.UI.ComicTextAudit.Scan("AR fix2 1600x720");CampusRift.UI.ComicTextAudit.Save(r,"task/ar/fix2/text-audit-raw.json");return new {r.visibleTexts,issues=r.issues.Count};'))
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Toggle();return true;');time.sleep(2)
save(out/'diagnostics.json',code('var d=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>();UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/fix2/diagnostics-1600x720.png");return new {d.Visible,text=d.GetComponentInChildren<TMPro.TMP_Text>().text};'))
time.sleep(.5);code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Toggle();return true;')
console(out/'visual-console.json')
prior=out/'prior-m5';shutil.copytree('task/ar/m5',prior)
screens=list(Path('task/ar/screens').glob('m5-*.png'))
for p in screens:shutil.copy2(p,out/p.name)
done=Path('task/ar/m5/DONE.txt');done.unlink()
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — ảnh/audit; harness đang chạy\n- Đã chụp2400×1080 và1600×720 vào screens/fix2, layout JSON + diagnostics overlay. ComicTextAudit chạy đúng1lượt tại1600×720.\n- Bắt đầu ARRiftPlayTest đúng1lượt; output tạm m5, sẽ copy sangfix2 rồi phục hồi evidence M5 gốc. Nếu tiếp quản khi chưafix2/DONE, xem m5/DONE timestamp; không chạy lại harness.\n')
code('new UnityEngine.GameObject("AR fix2 existing harness").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
for i in range(180):
    if done.exists():break
    time.sleep(1)
else:raise RuntimeError('harness timeout')
for name in ['results.json','DONE.txt']:shutil.copy2(Path('task/ar/m5')/name,out/name)
for p in screens:shutil.copy2(out/p.name,p)
shutil.copytree(prior,'task/ar/m5',dirs_exist_ok=True)
console(out/'harness-console.json')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — AR gate hoàn tất\n- ARRiftPlayTest1lượt: '+(out/'DONE.txt').read_text()+'. Raw fix2/results.json; evidence M5 gốc phục hồi. ComicTextAudit/ảnh/diagnostics đã có. Tiếp HubFlow1lượt rồi APK build.\n')
print((out/'DONE.txt').read_text(),flush=True)
