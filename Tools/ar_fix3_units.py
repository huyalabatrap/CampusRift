from ar_fix3_local import *
console(out/'compile-final.json')
result=json.loads((out/'gesture-unit-tests.json').read_text()) if (out/'gesture-unit-tests.json').exists() else code('return CampusRift.AR.ARGestureUnitTests.Run();')
save(out/'gesture-unit-tests.json',result)
print(json.dumps(result,ensure_ascii=True))
assert not result['failed'], result
code('CampusRift.AR.Editor.ARFix3Setup.Install();return true;')
save(out/'installed.json',code('return new { scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };'))
progress('B unit gate\n- Geometry/evidence synthetic tests đạt; xem fix3/gesture-unit-tests.json. Scene installer cập nhật autofocus/pointcloud. Chưa AR harness/audit/HubFlow/build.')
