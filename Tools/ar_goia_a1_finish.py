from pathlib import Path
p=Path('Assets/ARRift/Runtime/FrameSampler.cs');s=p.read_text(encoding='utf-8').replace('(long)(Time.realtimeSinceStartupAsDouble*1000)','(long)(image.timestamp*1000)');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARBattlefield.cs');s=p.read_text(encoding='utf-8-sig').replace('public bool UserPaused;', 'public bool PracticeActive;public bool UserPaused;')
s=s.replace('if(!Paused)Clock+=Time.deltaTime;', 'var sampler=GetComponent<FrameSampler>();if(sampler!=null)sampler.SetSamplingActive(!applicationPaused&&(PracticeActive||!Paused));\n            if(!Paused)Clock+=Time.deltaTime;')
p.write_text(s,encoding='utf-8')
Path('Assets/ARRift/Runtime/ARAdaptiveQuality.cs').write_text(Path('Tools/ar_goia_adaptive.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
