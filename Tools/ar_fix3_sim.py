from ar_fix3_local import *
assert not (out/'placement-DONE.txt').exists(),'Do not redo completed measurement'
console(out/'compile-pre-sim.json')
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);UIValidation.SetResolution(1600,720);return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');time.sleep(.3);background()
code('new UnityEngine.GameObject("Fix3 placement measurement").AddComponent<CampusRift.AR.ARFix3PlacementSmoke>();return true;')
for i in range(40):
    time.sleep(1)
    if (out/'placement-timing.json').exists():
        data=json.loads((out/'placement-timing.json').read_text())
        if 'failed' in data:raise RuntimeError(str(data))
    if (out/'placement-DONE.txt').exists():break
else:raise TimeoutError('placement measurement')
console(out/'placement-console.json')
progress('A simulation mesuré\n- XR provider thật đã neo world anchor; timing ở fix3/placement-timing.json. Pending đầu do patch nhỏ giữ initial-placement-pending.json. Tiếp drift/screens/harness.')
print((out/'placement-timing.json').read_text())
