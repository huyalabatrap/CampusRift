"""Related warm-frame ambient/music branch; no full legacy ending rerun."""
from pathlib import Path
import json,time
import unity_mcp as m
root=Path.cwd();out=root/'task/p23/audio-duck-probe';out.mkdir(parents=True,exist_ok=True);m.initialize()
def call(name,args):
    for _ in range(35):
        raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
        if v.get('success') is not False:return v
        transient=v.get('data',{}).get('reason') in ('no_unity_session','unity_session_not_ready')
        if not transient and not any(s in str(v.get('message','')).lower() for s in ('retry','not ready','compiling','domain reload','busy')):raise RuntimeError(v)
        time.sleep(2)
    raise RuntimeError(v)
def code(s):return call('execute_code',{'action':'execute','code':s}).get('data',{}).get('result')
call('manage_editor',{'action':'stop'});code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "opened";')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(3)
code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.UI.UIStateManager.Instance.EnterScene(true);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());return "warm";')
snippet='''
var mixer=CampusRift.UI.SettingsManager.Instance.Mixer;
var a=new GameObject("ambient P23 related").AddComponent<AudioSource>();a.volume=.8f;
var b=new GameObject("combat P23 related").AddComponent<AudioSource>();b.volume=.7f;
var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
System.Collections.IEnumerator Probe(){
yield return new WaitForSecondsRealtime(.5f);
float original=0,sfx=0;mixer.GetFloat("MusicVolume",out original);bool hasSfx=mixer.GetFloat("SFXVolume",out sfx);
float started=Time.realtimeSinceStartup;CampusRift.SkyBeast.SkyBeastAudioDuck.Ensure().Duck(.4f);
yield return new WaitForSecondsRealtime(.25f);
float low=0,liveSfx=0;mixer.GetFloat("MusicVolume",out low);mixer.GetFloat("SFXVolume",out liveSfx);
float ambient=a.volume,combat=b.volume,sampled=Time.realtimeSinceStartup-started;
yield return new WaitForSecondsRealtime(1.1f);float restored=0;mixer.GetFloat("MusicVolume",out restored);
string json="{\\"originalDb\\":"+original.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"duckedDb\\":"+low.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"ambientDuring\\":"+ambient.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"combatDuring\\":"+combat.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"sampledSeconds\\":"+sampled.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"duckPass\\":"+(low<original-8&&ambient<.4f).ToString().ToLowerInvariant()+",\\"sfxPass\\":"+(Mathf.Approximately(combat,.7f)&&(!hasSfx||Mathf.Approximately(sfx,liveSfx))).ToString().ToLowerInvariant()+",\\"restorePass\\":"+(Mathf.Abs(restored-original)<.1f&&Mathf.Approximately(a.volume,.8f)).ToString().ToLowerInvariant()+"}";
System.IO.File.WriteAllText("task/p23/audio-duck-probe/result.json",json);UnityEngine.Object.Destroy(a.gameObject);UnityEngine.Object.Destroy(b.gameObject);
}
player.StartCoroutine(Probe());return "scheduled related branch";
'''
code(snippet)
for _ in range(30):
    if (out/'result.json').exists():break
    time.sleep(1)
result=json.loads((out/'result.json').read_text());console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'format':'detailed','include_stacktrace':True});(out/'console.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'});print(result,flush=True);assert all(result[k] for k in ('duckPass','sfxPass','restorePass'))
