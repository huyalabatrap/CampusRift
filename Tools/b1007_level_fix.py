from b1007_runner import *
p=ROOT/'Assets/Levels/Validation/Level8to10PlayTest.cs';b=ROOT/'Backups/Regression-APK-pre-20261006'/p.relative_to(ROOT);b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig')
s=s.replace('LevelDirector director;','public bool ResultScreenOnly;\n        LevelDirector director;',1)
s=s.replace('yield return AiSmoke();StarBoundaries();','if(!ResultScreenOnly){yield return AiSmoke();StarBoundaries();}',1)
s=s.replace('for(int level=1;level<=10;level++)','for(int level=ResultScreenOnly?10:1;level<=10;level++)',1)
# Preserve TryChannel's product action even when skipping earlier assertions.
s=s.replace('world.PlacePlayer(outdoor);Check(u.TryChannel(),','world.PlacePlayer(outdoor);var accepted=u.TryChannel();Check(accepted,',1)
s=s.replace('Check(','if(!ResultScreenOnly)Check(')
old='if(level==10){yield return new WaitForSecondsRealtime(2.7f);if(!ResultScreenOnly)Check(UIStateManager.Instance.State==UIState.Victory&&director.Result.starMask==7,"Level10 Results three stars");yield return ShotFrame("level10-results-three-stars");}'
new='''if(level==10)
                {
                    // P21 adds a dawn cinematic and a user-dismissed credits page before Results.
                    yield return Until(()=>P21StoryCinematic.Active!=null&&P21StoryCinematic.Active.CreditsVisible,25);
                    if(P21StoryCinematic.Active!=null)P21StoryCinematic.Active.Skip();
                    yield return Until(()=>UIStateManager.Instance.State==UIState.Victory,6);
                    Check(UIStateManager.Instance.State==UIState.Victory&&director.Result.starMask==7,"Level10 Results three stars");yield return ShotFrame("level10-results-three-stars");
                }'''
assert old in s;s=s.replace(old,new,1);p.write_text(s,encoding='utf-8')
progress('Mốc harness cinematic: Level8to10 có ResultScreenOnly; setupchỉmàn10, cácassertđãPASS không chạy lại; duy nhấtcheck Results3sao sau dawn/credits P21 đã đóng. Giữ nguyên starMask7 và UIStateVictory, không thay hành vi cinematic.')
call('refresh_unity',{});time.sleep(2)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(OUT.parent/'7-compile-fixes.json',console);print(console)
