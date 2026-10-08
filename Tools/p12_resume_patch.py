from pathlib import Path

def replace(path, old, new):
    p=Path(path); s=p.read_text(encoding='utf-8-sig'); assert old in s, path; p.write_text(s.replace(old,new),encoding='utf-8')
# Full flight framing uses the animated mesh bounds; keep FOV60 and the normal ground camera.
replace('Assets/Enemies/Validation/P12VisualCapture.cs', 'IEnumerator Shot(string path)', '''void TrackDragon(SkyBeastController c){var rs=c.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=rs[0].bounds;foreach(var r in rs)if(r.enabled)bounds.Encapsulate(r.bounds);var delta=bounds.center-world.player.followCamera.transform.position;world.Look(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);}
        IEnumerator Track(SkyBeastController c,float seconds){float until=Time.time+seconds;while(Time.time<until){TrackDragon(c);yield return null;}}
        IEnumerator Shot(string path)''')
replace('Assets/Enemies/Validation/P12VisualCapture.cs', '''var direction=c.transform.position-world.player.transform.position;
                    float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;float pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;world.Look(yaw,pitch);
                    yield return new WaitForSeconds(.4f);''', '''c.SeekFlightForValidation(c.definition.period*.12f);yield return Track(c,.4f);''')
replace('Assets/Enemies/Validation/P12VisualCapture.cs', '''c.Roar();yield return new WaitForSeconds(.5f);yield return Shot("dragons/frames/"+id+"-roar-"+(dark?"dark":"light")+".png");''', '''c.Roar();yield return Track(c,.5f);yield return Shot("dragons/frames/"+id+"-roar-"+(dark?"dark":"light")+".png");c.SeekFlightForValidation(0);yield return Track(c,.5f);yield return Shot("dragons/frames/"+id+"-lowpass-"+(dark?"dark":"light")+".png");''')
replace('Assets/Enemies/Editor/P12DataSetup.cs','data.scale=row[0]=="020"?7:6','data.scale=9')
# Never bless a URP failure just because the CPU loop still has a high FPS.
p=Path('Assets/Enemies/Validation/Benchmark/P12StandaloneBench.cs'); s=p.read_text(encoding='utf-8-sig')
s=s.replace('public int enemies=25,dragons=1,samples,renderedFrames;', 'public int enemies=25,dragons=1,samples,renderedFrames,runtimeErrors,minLiving=25,visibleSkins;public string[] counterNames;')
s=s.replace('IEnumerator Start(){', '''void RuntimeLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.runtimeErrors++;report.error=message;}}
        IEnumerator Start(){''')
s=s.replace('if(Application.isEditor)yield break;', 'if(Application.isEditor)yield break;Application.logMessageReceived+=RuntimeLog;')
s=s.replace('settings.VSync=false;', 'settings.VSync=false;settings.SkyBrightness=1;')
s=s.replace('var roster=LevelCatalog.Instance.Get(10)', 'SettingsManager.Instance.Sky.SetPreset(SkyPreset.Dusk);var roster=LevelCatalog.Instance.Get(10)')
s=s.replace('var scale=LevelCatalog.Instance.Get(10).Scaling;', 'var scale=LevelCatalog.Instance.Get(6).Scaling;')
s=s.replace('var tri=ProfilerRecorder.StartNew', '''var handles=new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);var counters=new List<ProfilerRecorder>();var names=new List<string>();foreach(var h in handles){var desc=Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);if(desc.Category==ProfilerCategory.Render&&desc.Name.EndsWith("Draw Calls Count")){names.Add(desc.Name);counters.Add(ProfilerRecorder.StartNew(desc.Category,desc.Name,1));}}report.counterNames=names.ToArray();
            var tri=ProfilerRecorder.StartNew''')
s=s.replace('report.drawCalls=Math.Max(report.drawCalls,draws.LastValue);', 'long totalDraws=0;foreach(var counter in counters)totalDraws+=counter.LastValue;report.drawCalls=Math.Max(report.drawCalls,totalDraws);report.minLiving=Math.Min(report.minLiving,EnemyDirector.Instance.Active.Count);')
s=s.replace('tri.Dispose();draws.Dispose();', 'foreach(var counter in counters)counter.Dispose();tri.Dispose();draws.Dispose();')
s=s.replace('report.error=render.Error;', 'if(render.Error.Length>0)report.error=render.Error;')
s=s.replace('report.passed=report.samples>120', '''var planes=GeometryUtility.CalculateFrustumPlanes(player.followCamera);foreach(var r in FindObjectsByType<SkinnedMeshRenderer>())if(r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds))report.visibleSkins++;
            report.passed=report.runtimeErrors==0&&report.triangles>100000&&report.drawCalls>0&&report.visibleSkins>=8&&report.minLiving>=25&&report.samples>120''')
s=s.replace('25 live mixed-model enemies level10 AI','25 live mixed-model enemies, level6 AI/stat scaling (no level7 self-fuse), full seven-model roster')
p.write_text(s,encoding='utf-8')
print('Prepared flight captures and native benchmark guards')
