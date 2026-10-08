from pathlib import Path
import shutil,json
root=Path(__file__).resolve().parents[1]
docs=root/'task/models'
license_text=(docs/'LICENSES.md').read_text(encoding='utf-8')
dest=root/'Assets/Enemies/Models/ReplacementP19'
license_text=license_text.replace('(source/manifest.json)','(../../../../task/models/source/manifest.json)').replace('(source/)','(../../../../task/models/source/)').replace('(derived/)','(../../../../task/models/derived/)')
license_text=license_text.replace('(derived-image-audit.json)','(../../../../task/models/derived-image-audit.json)')
(dest/'LICENSES.md').write_text(license_text,encoding='utf-8')
shutil.copy2(docs/'source/manifest.json',dest/'source-manifest.json')
global_doc=root/'Assets/Enemies/LICENSES.md'
s=global_doc.read_text(encoding='utf-8').replace('Không thêm nguồn CC-BY.','Nguồn model Quaternius gốc không thêm CC-BY; xem thay thế MODELS-P19 bên dưới.')
if '## MODELS-P19' not in s:
 s+='\n## MODELS-P19 — model gameplay thay thế\n\nBốn prefab P19 hiện dùng Night Demon (Morgan Strauss/nubux, CC-BY3), Mage (thecubber, rig nguồn Julius, **CC-BY-SA3**), Vampire Bat (rubberduck, CC0), Lava Golem bản clean (gavlig, CC0). [Nguồn, SHA256, attribution và giấy phép dẫn xuất](Models/ReplacementP19/LICENSES.md). Giữ CC-BY-SA3 cho asset Mage chỉnh sửa khi phân phối. Quaternius ở Models/P19 là nguồn rollback, không còn được bốn prefab gameplay tham chiếu. VFX/SFX/gameplay giữ giấy phép P19 ở trên.\n'
global_doc.write_text(s,encoding='utf-8')
# Preserve prior roster evidence before the one requested rerun of the original harness.
prior=docs/'previous-roster'
prior.mkdir(exist_ok=True)
for suffix in ['.json','-DONE.txt']:
 f=root/('Artifacts/P12/tests/EnemyRosterPlayTest'+suffix)
 if f.exists() and not (prior/f.name).exists():shutil.copy2(f,prior/f.name)
print('License and source manifest installed; previous roster evidence preserved.')
