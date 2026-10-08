from pathlib import Path
for name,src in [('GestureGeometry','geometry'),('GestureStateMachine','decision')]:
    Path(f'Assets/ARRift/Runtime/{name}.cs').write_text(Path(f'Tools/ar_goia_{src}.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARModeSettings.cs');s=p.read_text(encoding='utf-8-sig').replace('public float modelThreshold=', 'public bool enableRescue=false;public float gestureDwell=.1f,stableSwitch=.2f;\n        public float modelThreshold=').replace('directionMargin=.08f','directionMargin=.15f');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Settings/ARModeSettings.asset');s=p.read_text(encoding='utf-8-sig').replace('directionMargin: 0.08','directionMargin: 0.15');s+='  enableRescue: 0\n  gestureDwell: 0.1\n  stableSwitch: 0.2\n';p.write_text(s,encoding='utf-8')
