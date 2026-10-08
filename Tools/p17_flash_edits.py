from pathlib import Path
p=Path('Assets/Skills/Core/Runtime/SkillVfxPool.cs');s=p.read_text(encoding='utf-8-sig')
old='void LateUpdate()\n        {\n            if(nodes==null)return;'
assert old in s;s=s.replace(old,'void LateUpdate()\n        {\n            if(nodes==null)return;\n            bool reduced=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false;')
s=s.replace('n.Color*(n.kind==SkillVfxKind.Lotus?1+4*n.Progress:1.6f)','n.Color*(n.kind==SkillVfxKind.Lotus?1+4*n.Progress:1.6f)*(reduced?.25f:1)')
s=s.replace('n.surface.enabled=n.age<.09f;','n.surface.enabled=!reduced&&n.age<.09f;')
s=s.replace('n.surface.enabled=n.age<.066f;','n.surface.enabled=!reduced&&n.age<.066f;')
p.write_text(s,encoding='utf-8');print('Reduced flashes now suppress victim-local white Burst/SwordImpact and lower pool emission.')
