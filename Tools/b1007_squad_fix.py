from pathlib import Path
import shutil
p=Path('Assets/Enemies/Validation/SquadIntegrationSmoke.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        protected override IEnumerator Run()', '        public bool AdaptationOnly;\n        protected override IEnumerator Run()')
s=s.replace('float end=Time.time+8,nextSample=0;', '''float end=Time.time+(AdaptationOnly?30:8),nextSample=0;
            var watched=typeof(SquadTactics).GetField("watchedEscapeDirection",BindingFlags.Instance|BindingFlags.NonPublic);
            var watching=typeof(SquadTactics).GetField("watchingEscape",BindingFlags.Instance|BindingFlags.NonPublic);''')
s=s.replace('while(Time.time<end)', 'while(Time.time<end&&(!AdaptationOnly||d.Squad.AdaptedExitCount==0))')
s=s.replace('cc.Move(Vector3.right*3*Time.deltaTime);', '''Vector3 direction=Vector3.right;
                    if(AdaptationOnly&&(bool)watching.GetValue(d.Squad))direction=(Vector3)watched.GetValue(d.Squad);
                    direction.y=0;direction=direction.normalized;
                    cc.Move(direction*3*Time.deltaTime);
                    if(AdaptationOnly)field.SetValue(world.player,direction*3);''')
s=s.replace('if(Time.time>=nextSample)', 'if(!AdaptationOnly&&Time.time>=nextSample)')
for prefix in ['Check(ranged','Check(d.Squad.MaxPaths','Check(d.MaxMelee','Check(LevelCatalog']:
    s=s.replace(prefix,'if(!AdaptationOnly)'+prefix)
p.write_text(s,encoding='utf-8')
