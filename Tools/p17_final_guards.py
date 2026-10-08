"""Apply after functional QA; preserve the same Editor behavior."""
from pathlib import Path
edits={
 'Assets/SkyBeast/Runtime/FireBreathCycle.cs':[(
  '        public void StartDev(int level=8){StartCycle(FireBreathProfile.Load(level));DevMode=true;}',
  '#if UNITY_EDITOR || DEVELOPMENT_BUILD\n        public void StartDev(int level=8){StartCycle(FireBreathProfile.Load(level));DevMode=true;}\n#endif')],
 'Assets/Skills/Core/Runtime/Set1SkillRuntime.cs':[(
  '        public void ResetCooldownForValidation(){readyAt=0;}',
  '#if UNITY_EDITOR || DEVELOPMENT_BUILD\n        public void ResetCooldownForValidation(){readyAt=0;}\n#endif')],
 'Assets/Scripts/CampusExplorer.cs':[(
  '            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)\n                showControls = !showControls;',
  '#if UNITY_EDITOR || DEVELOPMENT_BUILD\n            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)\n                showControls = !showControls;\n#endif'),(
  '        void OnGUI()\n        {',
  '#if UNITY_EDITOR || DEVELOPMENT_BUILD\n        void OnGUI()\n        {'),(
  '        }\n    }\n}',
  '        }\n#endif\n    }\n}')]
}
for name,pairs in edits.items():
 p=Path(name);s=p.read_text(encoding='utf-8-sig')
 for old,new in pairs:
  assert s.count(old)==1,(name,old,s.count(old))
  s=s.replace(old,new)
 p.write_text(s,encoding='utf-8')
 print(name)
