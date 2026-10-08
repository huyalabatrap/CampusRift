from pathlib import Path
import shutil
p=Path('Assets/CampusRiftUI/Validation/P17Smoke.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        Report report=new Report();','        public bool FailedItemsOnly;\n        Report report=new Report();')
s=s.replace('            yield return null;yield return null;Check(', '''            if(FailedItemsOnly){
                s.ControlMode=Controls.ControlMode.Mobile;SettingsManager.Instance.Apply(s,false);yield return null;yield return null;
                yield return Shot("tutorial-mobile");
                string focusedTelemetry="task/p17/telemetry-smoke-"+Guid.NewGuid().ToString("N");LocalTelemetry.TestFolder=focusedTelemetry;
                LevelEvents.RaiseStarted(LevelCatalog.Instance.Get(1));LocalTelemetry.Skill("giant-hand-seal");LocalTelemetry.Instance.Finish("won",1,4);
                Check(!Directory.Exists(focusedTelemetry),"opt-out writes no file or directory");yield break;
            }
            yield return null;yield return null;Check(''')
s=s.replace('string telemetry="task/p17/telemetry-smoke";', 'string telemetry="task/p17/telemetry-smoke-"+Guid.NewGuid().ToString("N");')
p.write_text(s,encoding='utf-8')
