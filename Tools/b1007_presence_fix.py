from pathlib import Path
import shutil
p=Path('Assets/SkyBeast/Validation/SkyBeastPresencePlayTest.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        protected override IEnumerator Run()', '        public bool PrimaryOnly;\n        protected override IEnumerator Run()')
s=s.replace('            foreach(int level', '''            if(PrimaryOnly){SkyBeastPresence.Begin(10);yield return null;
                var primary=SkyBeastScheduler.Instance.Beasts.FirstOrDefault();
                Check(primary!=null&&primary.definition.id=="023","level 10 correct primary dragon");yield break;
            }
            foreach(int level''')
s=s.replace('var c=FindAnyObjectByType<SkyBeastController>();string expected=', 'var c=SkyBeastScheduler.Instance.Beasts.FirstOrDefault();string expected=')
p.write_text(s,encoding='utf-8')
