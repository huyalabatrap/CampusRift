from ar_fix3_local import *
before=code('var d=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARMonsterDirector>();return new {actors=d.Actors.Count,root=d.field.Root!=null,pool=CampusRift.Enemies.EnemyPool.Instance!=null};')
save(out/'exit-before.json',before)
assert before['actors']>0 and before['root'] and before['pool'],before
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().Shutdown();UnityEditor.EditorApplication.isPlaying=false;return true;')
time.sleep(.5)
errors=console(out/'exit-console.json');assert errors['data']==[],errors
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
errors=console(out/'cleanup-console.json');assert errors['data']==[],errors
progress('Cleanup AR cuối đạt\n- Stop/đóng scene khi có quái còn sống không exception. Sửa ARMonsterDirector/ARBattlefield dùng Unity null check cho pool đã bị hủy (null-conditional bỏ qua fake-null trước đó). Console cleanup0; lỗi ban đầu giữ teardown-console-initial.json. Ảnh pha đặt đã khóa auto chỉ trong collector để chụp reticle/nút ĐẶT TRẬN, không ảnh nhầm pha chỉnh. Chuẩn bị build2.')
print(before,'Exit console 0',flush=True)
