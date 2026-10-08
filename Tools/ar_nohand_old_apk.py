from pathlib import Path
import sys
source=Path('Tools/b1007_verify_apk.py').read_text(encoding='utf-8-sig').replace("out=Path('task/batch-1007/build');out.mkdir(exist_ok=True)","out=Path('task/ar/nohand/old-apk');out.mkdir(parents=True,exist_ok=True)",1)
sys.argv=[__file__,'APK-Test/CampusRift-20261007-batch1007-dev2.apk'];exec(compile(source,__file__,'exec'))
