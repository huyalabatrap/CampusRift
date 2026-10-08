"""Inspect the built APK without installing it or changing device data."""
import hashlib
import json
import pathlib
import zipfile

root = pathlib.Path(__file__).resolve().parents[1]
folder = root / 'Artifacts' / 'Android'
apk = folder / 'CampusRift.apk'
digest = hashlib.file_digest(apk.open('rb'), 'sha256').hexdigest()
(folder / 'CampusRift.apk.sha256').write_text(f'{digest}  CampusRift.apk\n', encoding='utf-8')
with zipfile.ZipFile(apk) as archive:
    names = archive.namelist()
    metadata_name = next(n for n in names if n.endswith('/global-metadata.dat'))
    metadata = archive.read(metadata_name)
    content_markers = {
        'english_course_packaged': b'Introduction to Algorithms',
        'vietnamese_course_packaged': 'Nhập môn thuật toán'.encode('utf-8'),
        'vietnamese_ui_packaged': 'TRI THỨC = SỨC MẠNH'.encode('utf-8'),
    }
    found_content = dict.fromkeys(content_markers, False)
    for name in names:
        if name.startswith('assets/bin/Data/') and '/Managed/' not in name and not name.endswith('/'):
            data = archive.read(name)
            for key, marker in content_markers.items():
                found_content[key] |= marker in data
            if all(found_content.values()):
                break
    checks = {
        'arm64_il2cpp': 'lib/arm64-v8a/libil2cpp.so' in names,
        'learning_engine_in_player': b'LearningEngine\0' in metadata,
        'localization_in_player': b'LocalizationService\0' in metadata,
        'editor_test_harnesses_excluded': all(n not in metadata for n in [b'LearningPlayTest\0', b'LocalizationPlayTest\0']),
        **found_content,
    }
report = {'apk': str(apk), 'bytes': apk.stat().st_size, 'sha256': digest, 'checks': checks}
(folder / 'package-verification.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps(report, ensure_ascii=True))
raise SystemExit(0 if all(checks.values()) else 1)
