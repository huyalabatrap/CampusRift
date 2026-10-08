from pathlib import Path
import shutil,json
p=Path('Assets/CampusRiftUI/Validation/P17RelatedSmoke.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        Report report=new Report();','        public bool ContinueMusicOnly;\n        Report report=new Report();')
s=s.replace('var director=LevelDirector.Ensure();director.Begin','var director=LevelDirector.Ensure();if(!ContinueMusicOnly){director.Begin',1)
s=s.replace('            foreach(int level','            }\n            foreach(int level',1)
s=s.replace('var source=music.GetComponent<AudioSource>();','var source=music.CurrentVoice;')
s=s.replace('source.clip!=null&&(level==1?source.clip.name=="dusk":level==3?source.clip.name=="night":source.clip.name=="BossPhase1")','source!=null&&source.clip!=null&&music.CurrentTrack==(level==1?"P17/Music/dusk":level==3?"P17/Music/night":"P21/Music/giao")')
p.write_text(s,encoding='utf-8')
# The runtime shortName is English before LocalizedText runs.
p=Path('Assets/Controls/Runtime/MobileControlsHUD.cs')
s=p.read_text(encoding='utf-8').replace('if(name.Contains("HƯ KHÔNG BÍCH"))return "HƯ KHÔNG";', 'if(name.Contains("HƯ KHÔNG BÍCH")||name=="VOID WALL")return LevelHUD.Vietnamese?"HƯ KHÔNG":"WALL";')
p.write_text(s,encoding='utf-8')
# Preserve the six checks captured before the uncaught missing-component error.
f=Path('task/batch-1007/regression/runs/P17Related');r=json.loads((f/'row.json').read_text(encoding='utf-8'))
r['status']='FAIL';r['summary']=json.loads((f/'partial-report.json').read_text(encoding='utf-8'))
r['failures']=['Missing AudioSource on Level Director: obsolete pre-crossfade harness; remaining music/language checks not reached']
(f/'row.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
