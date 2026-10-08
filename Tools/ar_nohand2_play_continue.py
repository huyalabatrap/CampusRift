from ar_nohand2_checks import *
connect()
folder=OUT/'checks/ARRiftPlayTest';folder.mkdir(parents=True,exist_ok=True)
assert not (folder/'invoked.json').exists(),'Already invoked; collect without rerun'
save('checks/ARRiftPlayTest/setup-help.json',code((OUT/'close-fixture-help.cs').read_text(encoding='utf-8')))
wait_for('!UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Paused')
code('var m=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>();m.enabled=false;return true;')
state=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var c=f.GetComponent<CampusRift.AR.ARSkillCaster>();return new {placed=f.Root!=null,f.Paused,f.MenuPaused,inputBlocked=f.placement.InputBlocked,f.placement.Adjusting,c.AimValid,mode=f.Mode.id,hands=c.source.HandCount,dev=CampusRift.Progression.DevMode.Active,transient=CampusRift.Progression.ProfileService.Instance.Transient};')
save('checks/ARRiftPlayTest/setup.json',state)
assert state['placed'] and state['AimValid'] and not any(state[k] for k in ['Paused','MenuPaused','inputBlocked','Adjusting','dev']),state
wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARMonsterDirector>().Actors.Count>0')
paths=['task/ar/goiA/ar-results.json','task/ar/goiA/AR-DONE.txt']+[p.relative_to(ROOT).as_posix() for p in (ROOT/'task/ar/screens/goiA').glob('combat-*.png')]
old=historical(paths,folder);stamp=time.time()
save('checks/ARRiftPlayTest/invoked.json',dict(utc=stamp,component='ARRiftPlayTest',OnlySkill=-1,count=1))
code('new GameObject("NOHAND2 ARRiftPlayTest one run").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
milestone('ARGestureUnitTests đã xong55/0 đúng1lượt. Setup ARRiftPlayTest lần đầu timeout trước invocation vì guide mở sau chọn mode; đã đóng bằng HUD SetHelp(false), không override Paused/input runtime. Tiếp tục fixture đã neo/polygon/anchorTracking; gate thực Paused/MenuPaused/InputBlocked/Adjusting=False, Dev=False/transient=True. ARRiftPlayTest OnlySkill=-1 invoked đúng1lượt; theo dõi output mới, không rerun suite hoặc hồi quy khác. Không chạy mock riêng; trace bắt buộc là C# trực tiếp D1.')
path=ROOT/'task/ar/goiA/AR-DONE.txt';deadline=time.time()+220
while time.time()<deadline:
    if path.exists() and path.stat().st_mtime>=stamp:break
    time.sleep(1)
else:raise TimeoutError('Suite pending; collect output without rerun')
data=json.loads((ROOT/'task/ar/goiA/ar-results.json').read_text(encoding='utf-8-sig'));save('checks/ARRiftPlayTest/result.json',data);shutil.copy2(path,folder/'DONE.txt')
for rel in paths:
    p=ROOT/rel
    if p.exists() and p.suffix=='.png':shutil.copy2(p,folder/p.name)
for rel,b in old.items():(ROOT/rel).write_bytes(b)
save('checks/ARRiftPlayTest/console.json',call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True}))
milestone('ARRiftPlayTest hoàn tất1lượt: '+str(len(data['passed']))+' PASS / '+str(len(data['failed']))+' FAIL / casts='+str(data['casts'])+'. Unit55/0 và trace PASS đã đủ; không chạy lại suite. Historical evidence suite đã phục hồi. Tiếp build APK fix2, verify/install/package check và phục hồi Editor/save/settings.')
print(json.dumps(data,ensure_ascii=True));stop()
