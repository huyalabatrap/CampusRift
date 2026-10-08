from pathlib import Path
import shutil
p=Path('Assets/CampusRiftUI/Validation/P23GameplaySmoke.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        readonly Report report=new Report();','        public bool LargeMobileOnly;\n        readonly Report report=new Report();')
at='            // Co-dispatched warning + roar must retain the alarm text.'
branch='''            if(LargeMobileOnly){
                var indoor=FindObjectsByType<LearningShrine>().First();var at=indoor.transform.position;
                w.PlacePlayer(at+new Vector3(0,0,-1.4f));w.camera.transform.position=at+new Vector3(0,1.7f,-2.5f);w.camera.transform.LookAt(at+Vector3.up*1.05f);
                Mode(2,true);FindAnyObjectByType<FireWarningHUD>().FindDoor();yield return Audit("size2-gameplay-mobile");yield break;
            }
'''
assert at in s;s=s.replace(at,branch+at)
p.write_text(s,encoding='utf-8')
