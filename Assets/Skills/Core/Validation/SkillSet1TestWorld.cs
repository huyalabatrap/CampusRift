#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.Skills
{
    // Fixtures are existing enemy prefabs in the real campus scene, with deterministic AI holds.
    public sealed class SkillSet1TestWorld
    {
        public CampusExplorer player;
        public Camera camera;
        Camera oldFollow;
        Vector3 oldPosition;Quaternion oldRotation;float oldFov;bool oldCrit;Levels.LevelDirector director;bool oldDirector;
        Levels.LevelDefinition oldLevel;string[] oldLoadout;SkillRuntime[] oldSlots;
        public bool GameplayCamera;
        public static EnemyArchetype RuntimeEnemyTemplate;
        readonly System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        public readonly MonsterVitality[] victims=new MonsterVitality[7];
        public Vector3 origin=new Vector3(-5,.13f,2);
        GameSettings settings;
        MonsterVitality[] previous;
        public void Begin()
        {
            Application.runInBackground=true;Time.timeScale=1;UIStateManager.Instance.EnterScene(true);
            settings=SettingsManager.Instance.Current.Copy();Mode(false);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            ProfileService.Instance.UseTransient(new ProfileData{cultivation=new CultivationData{realm=6,tier=5}});
            player=Object.FindAnyObjectByType<CampusExplorer>();camera=player.followCamera;oldFollow=camera;if(!GameplayCamera)player.followCamera=null;
            oldPosition=player.transform.position;oldRotation=player.transform.rotation;oldFov=camera.fieldOfView;oldCrit=player.GetComponent<PlayerStats>().suppressCrit;
            oldLevel=Levels.LevelSession.Current;oldLoadout=Levels.LevelSession.Loadout;oldSlots=new SkillRuntime[4];for(int i=0;i<4;i++)oldSlots[i]=player.GetComponent<SkillLoadout>().Get(i);
            player.GetComponent<PlayerMonsterHealth>().Revive(1,0);
            player.GetComponent<PlayerStats>().suppressCrit=true;
            director=Object.FindAnyObjectByType<Levels.LevelDirector>();if(director!=null){oldDirector=director.enabled;director.enabled=false;}
            previous=MonsterVitality.Active.ToArray();for(int i=0;i<previous.Length;i++)previous[i].gameObject.SetActive(false);
            EnemyArchetype template=RuntimeEnemyTemplate;
#if UNITY_EDITOR
            template=AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
            if(template==null)foreach(string guid in AssetDatabase.FindAssets("t:EnemyArchetype"))
            {var a=AssetDatabase.LoadAssetAtPath<EnemyArchetype>(AssetDatabase.GUIDToAssetPath(guid));if(a!=null&&a.prefab!=null&&!a.isBoss&&!a.isFlying){template=a;break;}}
#endif
            if(template==null)throw new System.InvalidOperationException("No enemy prefab available.");
            PlacePlayer(origin);
            for(int i=0;i<victims.Length;i++)
            {
                var e=EnemyPool.Ensure().Spawn(template,origin+Vector3.right*(2+i*1.1f),EnemyScaling.Default,false);
                if(e==null)throw new System.InvalidOperationException("Campus NavMesh fixture placement failed.");
                if(e.Brain!=null)
                {
                    var animator=e.GetComponent<MinionMotor>().Animator;
                    if(animator!=null)animator.transform.localScale=(Vector3)typeof(MinionBrain).GetField("modelScale",flags).GetValue(e.Brain);
                    e.Brain.enabled=false;
                }
                var combat=e.GetComponent<MonsterCombat>();if(combat!=null)combat.enabled=false;
                e.GetComponent<MinionMotor>().Stop();
                victims[i]=e.GetComponent<MonsterVitality>();victims[i].SetMaxHealth(10000,true);victims[i].defense=0;victims[i].Element=Element.None;
            }
            FrameCamera();
        }
        public void FrameCamera(){if(GameplayCamera){Look(90,14);return;}camera.transform.position=origin+new Vector3(5,8,-9);camera.transform.LookAt(origin+new Vector3(5,.5f,0));camera.fieldOfView=58;}
        public void Look(float yaw,float pitch){player.SetValidationCameraOrbit(yaw,pitch);}
        public void Lighting(bool dark)
        {Levels.LevelSession.Select(dark?3:1);SettingsManager.Instance.Sky.SetPreset(Levels.LevelCatalog.Instance.Get(dark?3:1).sky);SettingsManager.Instance.Sky.SetBrightness(dark?.2f:1);}
        public void PlacePlayer(Vector3 point)
        {var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=point;player.transform.rotation=Quaternion.LookRotation(Vector3.right);c.enabled=true;Physics.SyncTransforms();player.CancelDash();typeof(CampusExplorer).GetField("cameraInitialized",flags).SetValue(player,false);}
        public void Arrange(bool line=true)
        {
            PlacePlayer(origin);
            for(int i=0;i<victims.Length;i++)
            {
                var m=victims[i];m.ResetVitality();m.resistHardControl=false;m.Element=Element.None;m.GetComponent<StatusEffectHost>().Clear();
                Vector3 p=origin+(line?Vector3.right*(2+i*1.1f):new Vector3(4+(i%3)*1.4f,0,(i/3-1)*1.5f));
                var motor=m.GetComponent<MinionMotor>();motor.Place(p);motor.Stop();
            }
            Physics.SyncTransforms();FrameCamera();
        }
        public void Mode(bool mobile)
        {var s=SettingsManager.Instance.Current.Copy();s.ControlMode=mobile?Controls.ControlMode.Mobile:Controls.ControlMode.PC;s.VSync=false;s.Quality=Mathf.Min(1,QualitySettings.names.Length-1);SettingsManager.Instance.Apply(s,false);SkillVfxPool.ForceMobileQuality=mobile;}
        public void ChainLayout()
        {
            for(int i=0;i<victims.Length;i++){float a=(-65+i*130f/6)*Mathf.Deg2Rad;victims[i].GetComponent<MinionMotor>().Place(origin+new Vector3(Mathf.Cos(a)*5.4f,0,Mathf.Sin(a)*5.4f));victims[i].GetComponent<MinionMotor>().Stop();}Physics.SyncTransforms();
        }
        public void End()
        {
            player.followCamera=oldFollow;SkillVfxPool.ForceMobileQuality=false;
            for(int i=0;i<victims.Length;i++)if(victims[i]!=null)EnemyPool.Instance.Release(victims[i].GetComponent<EnemyInstance>());
            for(int i=0;i<previous.Length;i++)if(previous[i]!=null)previous[i].gameObject.SetActive(true);
            ProfileService.Instance.EndTransient();SettingsManager.Instance.Apply(settings,false);Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            PlacePlayer(oldPosition);player.transform.rotation=oldRotation;camera.fieldOfView=oldFov;player.GetComponent<PlayerStats>().suppressCrit=oldCrit;
            if(oldLevel!=null)Levels.LevelSession.Select(oldLevel.index);else Levels.LevelSession.Clear();Levels.LevelSession.Loadout=oldLoadout;
            if(director!=null)director.enabled=oldDirector;for(int i=0;i<4;i++)player.GetComponent<SkillLoadout>().Equip(i,oldSlots[i]!=null?oldSlots[i].Id:"");
        }
    }
}
#endif
