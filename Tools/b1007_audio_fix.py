from pathlib import Path
import shutil
p=Path('Assets/Enemies/Validation/P12AudioEndingPlayTest.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('  GameObject ambience,combat;', '  public bool CompletionOnly;\n  GameObject ambience,combat;')
s=s.replace('   var mixer=', '''   if(CompletionOnly){
    CampusRift.Progression.ProfileService.Instance.Data.longVuongRevealSeen=true;
    float beganFocused=Time.realtimeSinceStartup;yield return SkyBeastEnding.Play();
    Check(!SkyBeastEnding.Playing&&!SkyBeastEnding.Skipped&&Time.realtimeSinceStartup-beganFocused>=3.4f&&Time.realtimeSinceStartup-beganFocused<3.8f,"unskipped repeated P21 reveal runs3.5 real seconds and restores HUD");yield break;
   }
   var mixer=''')
s=s.replace('>=3.9f','>=3.4f').replace('<4.3f','<3.8f').replace('unskipped ending runs four real seconds','unskipped repeated P21 reveal runs3.5 real seconds')
p.write_text(s,encoding='utf-8')
