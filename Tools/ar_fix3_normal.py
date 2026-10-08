from pathlib import Path
s=Path('Tools/ar_fix2_normal.py').read_text(encoding='utf-8-sig').replace('from ar_fix2_local import *','from ar_fix3_local import *').replace('AR fix2','AR fix3')
s=s.replace('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().Shutdown();','var loader=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(loader!=null)loader.Shutdown();')
exec(compile(s,'ar_fix3_normal_collector','exec'))
