#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CampusRift.Progression;
using CampusRift.SkyBeast;
namespace CampusRift.Levels
{
    public sealed class V2DevCommands : EditorWindow
    {
        int level=8,tier=1;Realm realm=Realm.HoaThan;
        [MenuItem("Campus Rift/V2/DEV - Chon man va canh gioi")]static void Window()=>GetWindow<V2DevCommands>("V2 DEV");
        void OnGUI()
        {
            EditorGUILayout.HelpBox("DEV dùng hồ sơ tạm. Chọn đúng cảnh giới màn; QA không chứng minh cân bằng.",MessageType.Info);
            level=EditorGUILayout.IntSlider("Màn",level,1,10);realm=(Realm)EditorGUILayout.EnumPopup("Cảnh giới",realm);tier=EditorGUILayout.IntSlider("Tầng",tier,1,9);
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if(GUILayout.Button("Chơi màn · cảnh giới đề nghị"))V2DevTools.StartLevel(level);
                if(GUILayout.Button("Đặt cảnh giới / tầng")){V2DevTools.Disposable();ProfileService.Instance.Cultivation.SetState(realm,tier,0);}
                if(GUILayout.Button("Bật/tắt bất tử"))V2DevTools.ToggleGod();
                if(GUILayout.Button("Bật/tắt sát thương QA"))V2DevTools.ToggleDamage();
            }
        }
        static bool Playing()=>Application.isPlaying;
        [MenuItem("Campus Rift/V2/DEV - Cong 10000 Linh Thach")]static void Money(){if(!Playing())return;V2DevTools.Disposable();ProfileService.Instance.Wallet.Earn(10000,"dev");}
        [MenuItem("Campus Rift/V2/DEV - Mo moi man")]static void Unlock(){if(!Playing())return;V2DevTools.Disposable();ProfileService.Instance.Cultivation.SetState(Realm.DoKiep,1,0);for(int i=1;i<=10;i++)ProfileService.Instance.Data.Level(i,true).cleared=true;ProfileService.Instance.MarkDirty();}
        [MenuItem("Campus Rift/V2/DEV - Ha het dot hien tai")]static void Kill(){if(Playing())V2DevTools.KillCurrentWave();}
        [MenuItem("Campus Rift/V2/DEV - Day Kiem Y")]static void Fill(){if(Playing())V2DevTools.FillIntent();}
        [MenuItem("Campus Rift/V2/DEV - Bat tat Thien Hoa")]static void Fire(){if(!Playing())return;var d=LevelDirector.Instance;if(d==null||d.Level.index<8)return;var f=FireBreathCycle.Ensure();if(f.State==FireBreathCycle.Phase.Disabled)f.StartCycle(FireBreathProfile.Load(d.Level.index,SkyBeastScheduler.Instance.Phase));else f.StopCycle();}
        [MenuItem("Campus Rift/V2/DEV - Hoi day mau")]static void Heal(){if(Playing())V2DevTools.Heal();}
        [MenuItem("Campus Rift/V2/DEV - Bat tat bat tu")]static void God(){if(Playing())V2DevTools.ToggleGod();}
        [MenuItem("Campus Rift/V2/DEV - Bat tat sat thuong QA")]static void Damage(){if(Playing())V2DevTools.ToggleDamage();}
        [MenuItem("Campus Rift/V2/DEV - Ket thuc ho so tam")]static void End(){if(Playing())V2DevTools.End();}
        [MenuItem("Campus Rift/V2/DEV - Smoke 1 den 10")]static void Smoke(){if(Playing())new GameObject("P16 smoke 1 to 10").AddComponent<Validation.Level8to10PlayTest>();}
    }
}
#endif
