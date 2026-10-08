"""Resolve only campus material references after FBX remapping, never geometry."""
from pathlib import Path
import re
import json

ROOT = Path(__file__).resolve().parents[1]
MODEL = ROOT / 'Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx.meta'
GUID = 'fa9be14020bf0a448b036e9edf31e4e0'

def repair():
    names = dict(line.split('\t', 1) for line in
                 (ROOT / 'task/look/original-material-ids.tsv').read_text(encoding='utf-8-sig').splitlines())
    remaps = dict(re.findall(r'      name: (.*?)\n    second: (\{[^\n]+\})',
                            MODEL.read_text(encoding='utf-8-sig')))
    scene = ROOT / 'Assets/Scenes/SampleScene.unity'
    text = scene.read_text(encoding='utf-8-sig')
    changes = {}
    def material(match):
        original = match.group(0)
        name = names.get(match.group(1))
        target = remaps.get(name)
        if not target:
            return original
        changes[name] = changes.get(name, 0) + 1
        return target
    def slots(match):
        return re.sub(r'\{fileID: (-?\d+), guid: ' + GUID + r', type: \d+\}', material, match.group(0))
    updated = re.sub(r'  m_Materials:\n(?:  - .*\n)*', slots, text)
    scene.write_text(updated, encoding='utf-8', newline='\n')
    result = {'repairedSlots': sum(changes.values()), 'bySourceMaterial': changes,
              'scope': 'SampleScene MeshRenderer m_Materials only'}
    (ROOT / 'task/look/material-reference-repair.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))

if __name__ == '__main__':
    repair()
