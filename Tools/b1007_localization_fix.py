from pathlib import Path
import shutil
p=Path('Assets/Localization/Runtime/LocalizationPlayTest.cs')
b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig')
s=s.replace('readonly Report report=new Report();','public bool CurriculumOnly;\n        readonly Report report=new Report();')
start=s.index('            bool content=true,options=true;')
end=s.index('            State.OpenHub();',start)
block=s[start:end].replace('questions==440','questions==449').replace('P17 curriculum21 lessons/440','Current curriculum21 lessons/449')
block=block.replace('            Check(options,','            if(includeOptions)Check(options,')
s=s[:start]+'            CheckCurriculum(true);\n'+s[end:]
pos=s.index('        IEnumerator Run()')
s=s[:pos]+'        void CheckCurriculum(bool includeOptions)\n        {\n'+block+'        }\n'+s[pos:]
s=s.replace('        IEnumerator Run()\n        {','        IEnumerator Run()\n        {\n            if(CurriculumOnly)\n            {\n                var language=SettingsManager.Instance.Current.Copy();language.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(language,false);\n                yield return Frames();CheckCurriculum(false);yield break;\n            }')
p.write_text(s,encoding='utf-8')
