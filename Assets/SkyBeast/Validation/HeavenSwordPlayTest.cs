#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class HeavenSwordPlayTest:MonoBehaviour
    {
        [Serializable]sealed class Check{public string name;public bool pass;}
        [Serializable]sealed class Report{public int passed,failed;public string method="One compact smoke. Real level8 and level9 authored waves, actual death feed/channel/ticks/cinematics/results; accelerated spawn/DEV damage and held AI. Not balance playtest.";public List<Check> checks=new List<Check>();}
        readonly Report report=new Report();SkillSet1TestWorld world;LevelDirector director;HeavenSwordUltimate u;PlayerMonsterHealth hp;BuffSystem buffs;FireBreathCycle fire;
        readonly Vector3 outdoor=new Vector3(-5,.13f,2);Vector3 indoor;
        void CheckResult(bool pass,string name){report.checks.Add(new Check{name=name,pass=pass});if(pass)report.passed++;else report.failed++;Write();}
        void Write(){Directory.CreateDirectory("Artifacts/SkyBeast");File.WriteAllText("Artifacts/SkyBeast/HeavenSword.json",JsonUtility.ToJson(report,true));}
        void Begin(int level,Realm realm)
        {
            UIStateManager.Instance.EnterScene(true);ProfileService.Instance.Cultivation.SetState(realm,1,0);LevelSession.Select(level);director.Begin(LevelCatalog.Instance.Get(level));director.enabled=true;
            fire=FireBreathCycle.Instance;fire.AutoAdvance=false;fire.GetComponent<FireBreathVisuals>().enabled=false;fire.Ground.Clear();world.PlacePlayer(outdoor);world.player.enabled=false;
            hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);u=HeavenSwordUltimate.Instance;
        }
        void HoldEnemies(){foreach(var e in EnemyDirector.Instance.Active){if(e.Brain!=null)e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();if(e.Motor!=null)e.Motor.Stop();}}
        public static void Kill(EnemyInstance e){var hit=DamageInfo.Create(1000000,Element.Kim,DamageSource.Skill,e.transform.position,Vector3.down);hit.skillId="p15-smoke";e.Vitality.ApplyDamage(hit);}
        IEnumerator Clear(bool leaveLast=false)
        {
            float until=Time.realtimeSinceStartup+20;
            while(!director.AwaitingSkySword&&Time.realtimeSinceStartup<until)
            {
                HoldEnemies();int remaining=director.TotalPlanned-director.Kills;var enemies=new List<EnemyInstance>(director.Alive);
                foreach(var e in enemies)if(e!=null&&e.Alive&&e.countsForSwordIntent&&(!leaveLast||remaining>1)){Kill(e);remaining--;}
                if(leaveLast&&remaining==1&&director.AliveCount>0)yield break;
                yield return null;
            }
            CheckResult(director.AwaitingSkySword,"Real wave clears and waits for sword, level "+director.Level.index);
        }
        IEnumerator WaitUntil(Func<bool> condition,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
        IEnumerator Start()
        {
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();yield return null;foreach(var v in world.victims)v.gameObject.SetActive(false);
            director=LevelDirector.Ensure();director.introSeconds=.01f;director.spawnInterval=.005f;director.portalLead=0;hp=world.player.GetComponent<PlayerMonsterHealth>();hp.respawnOnDefeat=false;buffs=world.player.GetComponent<BuffSystem>();
            foreach(var n in ShelterGraphReference.Graph.Nodes)if(n.WorldPosition.y<1&&ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor){indoor=n.WorldPosition;break;}
            Begin(8,Realm.NguyenAnh);CheckResult(!u.Visible,"Hidden below Hoa Than");ProfileService.Instance.Cultivation.SetState(Realm.HoaThan,1,0);CheckResult(u.Visible,"Unlocked only level8-10 at Hoa Than");
            CheckResult(u.DurationForRealm(Realm.HoaThan)==2.5f&&u.DurationForRealm(Realm.LuyenHu)==2&&u.DurationForRealm(Realm.DoKiep)==1.5f,"Realm channel times 2.5 / 2 / 1.5");
            CheckResult(Resources.Load<EnemyArchetype>("P12/ShabanElite").swordIntentWeight==4&&UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/thiet-giap-nguu.asset").swordIntentWeight==2,"Authored elite4 / Thiet Giap2 weights");
            yield return Clear(true);CheckResult(!u.Intent.Full&&u.Intent.Fraction<1&&u.Intent.DefeatedCount==u.Intent.PlannedCount-1,"Last remaining enemy keeps meter below100 including queued spawns");
            var pill=ItemCatalog.Instance.Item("kiem-tam-dan");buffs.Apply(pill);CheckResult(!u.Intent.Full,"Item cannot fill meter early");buffs.ClearAll();
            var summon=EnemyPool.Ensure().Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset"),outdoor+Vector3.right*4,EnemyScaling.Default,false);director.TrackSummoned(summon);
            int total=u.Intent.TotalWeight;yield return Clear();CheckResult(u.Intent.Full&&u.Intent.Fraction==1&&u.Intent.TotalWeight==total,"Last counted death fills exact100; summons excluded");yield return WaitUntil(()=>!summon.gameObject.activeSelf,2);CheckResult(!summon.gameObject.activeSelf,"Uncounted summon dissolves without damage/explosion");
            world.PlacePlayer(indoor);CheckResult(u.State==HeavenSwordUltimate.ButtonState.NeedOutdoor&&!u.TryChannel(),"Indoor refuses channel / NeedOutdoor");world.PlacePlayer(outdoor);
            fire.Advance(fire.Profile.warningSeconds);CheckResult(u.State==HeavenSwordUltimate.ButtonState.Ready&&u.FireWarning,"Breath remains Ready with interruption warning");
            CheckResult(u.TryChannel(),"Outdoor begins channel during ordinary breath");FireBreathCycle.DamagePlayer(hp,1,"thien-hoa");CheckResult(!u.Channeling&&u.Intent.Full&&!world.player.GetComponent<CampusInput>().UltimateLocked,"Accepted fire tick interrupts and preserves intent / restores input");
            buffs.Apply(pill);u.TryChannel();CheckResult(Mathf.Abs(u.ChannelDuration-1.5f)<.01f,"Sword pill shortens channel40%");FireBreathCycle.DamagePlayer(hp,1,"thien-hoa");CheckResult(u.Channeling&&buffs.UninterruptedCharges==0,"Pill prevents precisely one interruption");FireBreathCycle.DamagePlayer(hp,1,"thien-hoa");CheckResult(!u.Channeling&&u.Intent.Full,"Next tick interrupts after pill charge used");buffs.ClearAll();
            var bell=(GoldenBellRuntime)world.player.GetComponent<SkillLoadout>().Find("kim-chung-trao");world.player.GetComponent<SkillLoadout>().Equip(3,"kim-chung-trao");bell.enabled=true;bell.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();bool bellCast=bell.CastAt(outdoor);u.TryChannel();FireBreathCycle.DamagePlayer(hp,1,"thien-hoa");CheckResult(bellCast&&bell.ShieldActive&&u.Channeling,"Real Golden Bell blocks channel interruption");u.CancelChannel();bell.enabled=false;bell.enabled=true;buffs.ClearAll();fire.StopCycle();fire.StartCycle(FireBreathProfile.Load(8));
            world.Mode(false);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.V));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            CheckResult(u.Channeling,"PC V action reaches ultimate");yield return WaitUntil(()=>HeavenSwordCinematic.Active!=null);
            var cine=HeavenSwordCinematic.Active;CheckResult(cine!=null&&cine.Duration==6&&!cine.CanSkip,"First cinematic6s and unskippable");
            if(cine!=null){float remaining=fire.Remaining;yield return new WaitForSecondsRealtime(.5f);CheckResult(fire.CinematicPaused&&director.CinematicPaused&&Mathf.Abs(fire.Remaining-remaining)<.01f&&Time.timeScale==1,"Cycle and director pause locally; no global timeScale change");yield return WaitUntil(()=>!cine.Playing,8);}
            CheckResult(SkyBeastScheduler.Instance.Completed&&director.State==LevelDirector.Phase.Won&&director.WinsRaised==1,"Level8 one sword -> one win");CheckResult(SettingsManager.Instance.Sky.DawnProgress==1&&SettingsManager.Instance.Sky.Preset==SkyPreset.Default,"Final sword restores dawn");CheckResult(ProfileService.Instance.Data.heavenSwordSeen,"Seen flag belongs to profile");CheckResult(JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(ProfileService.Instance.Data)).heavenSwordSeen,"Seen flag survives profile JSON roundtrip");
            yield return new WaitForSecondsRealtime(2.5f);CheckResult(UIStateManager.Instance.State==UIState.Victory,"Real Results screen after level8 win");director.End();
            Begin(9,Realm.LuyenHu);yield return Clear();world.Mode(true);yield return new WaitForSecondsRealtime(.2f);
            var mobile=FindAnyObjectByType<MobileControlsHUD>();var zone=mobile.Zones.Find(z=>z.role==TouchRole.Ultimate);CheckResult(zone!=null&&zone.gameObject.activeInHierarchy,"Mobile round ultimate button visible");
            var rect=(RectTransform)zone.transform;Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,rect.position);var pointer=new PointerEventData(EventSystem.current){pointerId=1515,position=screen};zone.OnPointerDown(pointer);zone.OnPointerUp(pointer);yield return null;
            CheckResult(u.Channeling,"Mobile pointer press reaches Ultimate action");yield return WaitUntil(()=>HeavenSwordCinematic.Active!=null);cine=HeavenSwordCinematic.Active;CheckResult(cine!=null&&cine.CanSkip&&cine.Duration==3&&cine.Swords.SwordCount==960,"Repeat3s / skip / Mobile40% sword budget");if(cine!=null)cine.Skip();yield return null;
            CheckResult(SkyBeastScheduler.Instance.Phase==2&&SkyBeastScheduler.Instance.SwordTarget.RemainingSegments==1,"Level9 first sword removes exactly one of two segments");
            CheckResult(director.State==LevelDirector.Phase.Rest&&director.RestRemaining>14.5f&&fire.State==FireBreathCycle.Phase.Rest&&fire.Remaining>14.5f&&fire.Profile.cycleSeconds==32,"Phase2 new profile /15s wave and fire rest");CheckResult(!u.Intent.Full&&u.Intent.Fraction==0,"Successful sword consumes intent");CheckResult(world.player.GetComponent<PlayerStats>().FireResistance>=.5f,"Luyen Hu grants50% fire protection");
            yield return WaitUntil(()=>director.State==LevelDirector.Phase.Wave,17);yield return Clear();CheckResult(u.Intent.Full&&director.WaveIndex==1,"Level9 second authored wave reaches full");world.Mode(false);CheckResult(u.TryChannel(),"Second wave can channel again");yield return WaitUntil(()=>HeavenSwordCinematic.Active!=null);cine=HeavenSwordCinematic.Active;if(cine!=null)cine.Skip();yield return null;CheckResult(SkyBeastScheduler.Instance.Completed&&director.WinsRaised==1,"Level9 two swords complete / one win");
            director.End();Begin(10,Realm.DoKiep);director.enabled=false;var scheduler=SkyBeastScheduler.Instance;scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();
            CheckResult(u.State==HeavenSwordUltimate.ButtonState.Blocked,"Level10 final sword blocked before Fury");scheduler.Fury.Request();CheckResult(u.State==HeavenSwordUltimate.ButtonState.Blocked,"Fury warning blocks channel");scheduler.Fury.Advance(5);CheckResult(u.State==HeavenSwordUltimate.ButtonState.Blocked&&!u.TryChannel(),"Active Fury blocks channel");fire.Advance(12);CheckResult(scheduler.Fury.Completed,"Fury completion keeps P14 gate");
            director.End();world.player.enabled=true;world.End();Write();File.WriteAllText("Artifacts/SkyBeast/HeavenSword-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");Debug.Log("[P15] "+report.passed+" PASS / "+report.failed+" FAIL");Destroy(gameObject);
        }
    }
}
#endif
