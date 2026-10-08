from pathlib import Path
import shutil
def edit(p,a,b):
 p=Path(p);s=p.read_text(encoding='utf-8');assert a in s,(p,a);p.write_text(s.replace(a,b),encoding='utf-8')
shutil.copy2('Artifacts/SkyVictory/Settings-before.json','task/stabilize/original-settings.json')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','readonly Report report=new Report();LearningEngine original,engine;GameSettings settingsBefore;',
 'readonly Report report=new Report();LearningEngine original,engine;GameSettings settingsBefore;string originalPrefs;bool hadPrefs;')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','Directory.CreateDirectory(Output);Application.runInBackground=true;',
 'Directory.CreateDirectory(Output);Application.runInBackground=true;hadPrefs=PlayerPrefs.HasKey("CampusRift.Settings.v1");originalPrefs=PlayerPrefs.GetString("CampusRift.Settings.v1","");')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','SettingsManager.Instance.Apply(settingsBefore);',
 'SettingsManager.Instance.Apply(settingsBefore,false);if(hadPrefs)PlayerPrefs.SetString("CampusRift.Settings.v1",originalPrefs);else PlayerPrefs.DeleteKey("CampusRift.Settings.v1");PlayerPrefs.Save();')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','File.WriteAllText(Output+"content-details.txt",',
 '''foreach(var c in engine.Catalog.courses)if(c.examBank!=null)foreach(var q in c.examBank.questions)
            {questions++;content &= map(q.prompt,q.promptVN,q.id+" exam prompt") && map(q.explanation,q.explanationVN,q.id+" exam explanation");foreach(var o in q.options)options &= Loc.Translate(o.text??"")==(o.textVN??"");}
            File.WriteAllText(Output+"content-details.txt",''')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','pool=world.player.GetComponent<SkillVfxPool>();yield return null;',
 'pool=world.player.GetComponent<SkillVfxPool>();yield return new WaitForSeconds(1); // P12 spawn pose must finish before checking the normal model bounds.')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','world.player.cameraHeight=world.victims[0].GetComponentInChildren<SkinnedMeshRenderer>().bounds.center.y-world.player.transform.position.y;',
 'world.player.cameraHeight=Mathf.Max(.5f,world.victims[0].GetComponentInChildren<SkinnedMeshRenderer>().bounds.center.y-world.player.transform.position.y);')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','cameraDetails.AppendLine("camera="+cameraPos+"; player="+world.player.transform.position+"; victim="+rigs[0][0].bounds);',
 'if(cameraDetails.Length<2500)cameraDetails.AppendLine("camera="+cameraPos+"; player="+world.player.transform.position+"; victim="+rigs[0][0].bounds);')
p=Path('Tools/StabilizeModelPolish.cs.txt');s=p.read_text(encoding='utf-8').replace('visual.localScale*=.8f;', 'if(!SessionState.GetBool("CampusRift.Stabilize.batScaled",false)){visual.localScale*=.8f;SessionState.SetBool("CampusRift.Stabilize.batScaled",true);}')
s=s.replace('var bones=root.GetComponentsInChildren<Transform>()', 'foreach(var child in visual.Cast<Transform>().Where(t=>new[]{"Torn shadow cloak","Shadow hood","Crimson face scarf","Shadow bone dagger"}.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);\n            var bones=root.GetComponentsInChildren<Transform>()')
s=s.replace('cloakSkin.localBounds=body.localBounds;', 'cloakSkin.localBounds=cloak.bounds;')
Path('Assets/Editor/StabilizeModelPolish.cs').write_text(s,encoding='utf-8')
shutil.copy2('Tools/EliteFireHeartVisual.cs.txt','Assets/Enemies/Runtime/EliteFireHeartVisual.cs')
