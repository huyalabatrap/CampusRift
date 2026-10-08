#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.SkyBeast;
using CampusRift.UI;
namespace CampusRift.Validation
{
    // One compact run also supplies the requested 1→10 DEV smoke, never a balance bot.
    public sealed class Level8to10PlayTest : P12PlayTest
    {
        const string Root="task/p19/regressions/levels/";
        public bool ResultScreenOnly;
        LevelDirector director;PlayerMonsterHealth hp;readonly Vector3 outdoor=new Vector3(-5,.13f,2);
        readonly List<string> playthrough=new List<string>();
        [Serializable]sealed class EntranceEvidence{public string state;public int available,blocked;public Vector3 free;public List<Vector3> occupied=new List<Vector3>();}
        [Serializable]sealed class Shot{public string image,state;public Vector3 camera,angle;public int width,height;}
        [Serializable]sealed class Capture{public string method="Unity framebuffer in real campus; AI/frame holds, accelerated spawn/DEV lethal damage, disposable profile. No native/device/balance evidence.";public List<Shot> shots=new List<Shot>();}
        readonly Capture capture=new Capture();
        IEnumerator Until(Func<bool> condition,float seconds=20)
        {float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
        void Hold()
        {
            foreach(var e in EnemyDirector.Instance.Active){if(e.Brain!=null)e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();e.Motor?.Stop();}
        }
        void BeginLevel(int level)
        {
            V2DevTools.StartLevel(level);director=LevelDirector.Instance;director.introSeconds=.02f;director.portalLead=0;director.spawnInterval=.005f;director.winDelay=.05f;
            world.player.enabled=false;world.PlacePlayer(outdoor);hp=world.player.GetComponent<PlayerMonsterHealth>();hp.respawnOnDefeat=false;hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);
            var qa=world.player.GetComponent<V2QaPlayer>()??world.player.gameObject.AddComponent<V2QaPlayer>();qa.God=true;
            if(level>=8){var fire=FireBreathCycle.Instance;fire.AutoAdvance=false;fire.GetComponent<FireBreathVisuals>().enabled=false;fire.Ground.Clear();}
        }
        IEnumerator ShotFrame(string name)
        {
            yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);
            var audit=ComicTextAudit.Scan("P16 "+name);File.WriteAllText(Root+"screens/"+name+"-text-audit.json",JsonUtility.ToJson(audit,true));
            if(!ResultScreenOnly)Check(audit.issues.Count==0,name+" ComicTextAudit 0 issues");
            capture.shots.Add(new Shot{image=name,state=director.State.ToString(),camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles,width=Screen.width,height=Screen.height});
            File.WriteAllText(Root+"capture.json",JsonUtility.ToJson(capture,true));
        }
        void FrameDoor(Vector3 door)
        {
            var delta=Vector3.ProjectOnPlane(outdoor-door,Vector3.up).normalized;if(delta.sqrMagnitude<.1f)delta=Vector3.back;
            world.camera.transform.position=door+delta*8+Vector3.up*2.6f;world.camera.transform.LookAt(door+Vector3.up*1.3f);world.camera.fieldOfView=60;
        }
        IEnumerator AiSmoke()
        {
            BeginLevel(8);director.enabled=false;FireBreathCycle.Instance.StopCycle();var ed=EnemyDirector.Instance;var tactics=ed.Tactics;
            tactics.FindEntrances();if(!ResultScreenOnly)Check(tactics.Entrances.Count>=2,"Campus provides reachable shelter entrances within40m");
            var template=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
            var enemies=new List<EnemyInstance>();
            for(int i=0;i<6;i++)
            {var point=tactics.Entrances.Count>0?tactics.Entrances[Mathf.Min(i,tactics.Entrances.Count-1)].position:outdoor+Vector3.right*8;
                enemies.Add(EnemyPool.Ensure().Spawn(template,point, new EnemyScaling{health=6.4f,damage=5.8f,speed=1.25f,aiTier=3,level=8},false));}
            yield return Until(()=>enemies.All(e=>e!=null&&e.Brain.State==MinionState.Chase),3);
            ed.ResetCounters();
            bool first=ed.TryAcquire(enemies[0]),early=ed.TryAcquire(enemies[1]);
            yield return new WaitForSeconds(.31f);bool second=ed.TryAcquire(enemies[1]);
            if(!ResultScreenOnly)Check(first&&!early&&second,"T3 two attacks start at least0.3s apart");
            foreach(var e in enemies)ed.Release(e);
            if(!ResultScreenOnly)Check(ed.MaxMeleeTokensSeen<=ed.TierFor(3).meleeTokens&&ed.MaxRangedTokensSeen<=ed.TierFor(3).rangedTokens,"Coordinated attacks retain token caps");
            tactics.PlanAmbush();if(!ResultScreenOnly)Check(tactics.AmbushCount>0,"T3 selects Door on predicted player route");
            EnemyInstance ambusher=enemies.FirstOrDefault(e=>tactics.TryPosition(e,out var p,out var g)&&!g);
            if(ambusher!=null)
            {tactics.TryPosition(ambusher,out var door,out var guard);yield return Until(()=>ambusher.Brain.WaitingAtDoor,2);FrameDoor(door);yield return ShotFrame("door-ambush");}
            int before=enemies[0].Brain.HeardSounds;
            SoundEventBus.Publish(new SoundEvent(enemies[0].transform.position,1.5f,PlayerSoundType.Sprint,Time.time,world.player.transform));
            if(!ResultScreenOnly)Check(enemies[0].Brain.HeardSounds==before+1&&enemies[0].Brain.HearingPursuit,"T3 hears sprint through SoundEventBus and pursues");
            SoundEventBus.Publish(new SoundEvent(enemies[0].transform.position+Vector3.right*100,5,PlayerSoundType.Combat,Time.time,world.player.transform));
            if(!ResultScreenOnly)Check(enemies[0].Brain.HeardSounds==before+1,"Out-of-range skill sound ignored");
            foreach(var e in enemies){var scale=e.scaling;scale.aiTier=4;e.scaling=scale;}
            var fire=FireBreathCycle.Instance;fire.RestartWarning();tactics.PlanWarning();
            if(!ResultScreenOnly)Check(tactics.BlockedCount>0&&tactics.BlockedCount<=3&&tactics.BlockedCount*2<=tactics.Entrances.Count,"T4 warning assigns at most3 and50% entrances");
            var evidence=new EntranceEvidence{state=fire.State.ToString(),available=tactics.Entrances.Count,blocked=tactics.BlockedCount,free=tactics.FreeEntrance};
            foreach(var e in enemies)if(tactics.TryPosition(e,out var p,out bool guard)&&guard)evidence.occupied.Add(p);
            File.WriteAllText(Root+"entrances.json",JsonUtility.ToJson(evidence,true));
            if(!ResultScreenOnly)Check(evidence.occupied.All(p=>Vector3.Distance(p,evidence.free)>3)&&tactics.Entrances.Count>tactics.BlockedCount,"Nearest reachable entrance reserved and never assigned");
            if(!ResultScreenOnly)Check(EnemyTactics.BlockLimit(0)==0&&EnemyTactics.BlockLimit(1)==0&&EnemyTactics.BlockLimit(2)==1&&EnemyTactics.BlockLimit(7)==3,"Door rule boundaries0/1/2/7 retain escape");
            var blocker=enemies.FirstOrDefault(e=>tactics.TryPosition(e,out var p,out bool g)&&g);
            if(blocker!=null)
            {
                tactics.TryPosition(blocker,out var door,out bool guard);blocker.Motor.Place(door);yield return new WaitForSeconds(.3f);
                if(!ResultScreenOnly)Check(blocker.Brain.GuardingDoor&&blocker.Brain.WaitingAtDoor,"Blocker holds entrance in guard pose");FrameDoor(door);yield return ShotFrame("warning-door-blocker");
                float health=blocker.Vitality.Health;Vector3 point=blocker.transform.position;
                blocker.Motor.Place(outdoor+Vector3.right*7);fire.Advance(fire.Profile.warningSeconds+.5f);
                if(!ResultScreenOnly)Check(blocker.Vitality.Health<health,"Outdoor non-Fire door blocker takes actual breath tick");blocker.Motor.Place(point);
            }
            director.End();yield return null;
        }
        void StarBoundaries()
        {
            if(!ResultScreenOnly)Check((StarEvaluator.Evaluate(8,true,10,720,new StarRun())&4)!=0 && (StarEvaluator.Evaluate(8,true,10,720,new StarRun{outdoorFireHits=1})&4)==0,"L8 star pass0/fail1 outdoor breath hit");
            if(!ResultScreenOnly)Check((StarEvaluator.Evaluate(9,true,10,900,new StarRun{swordReadyCount=2,swordsInTime=2})&4)!=0 && (StarEvaluator.Evaluate(9,true,10,900,new StarRun{swordReadyCount=2,swordsInTime=1})&4)==0,"L9 star requires both timely swords, pass2/fail1");
            if(!ResultScreenOnly)Check((StarEvaluator.Evaluate(10,true,10,1080,new StarRun{chains=3})&4)!=0 && (StarEvaluator.Evaluate(10,true,10,1080,new StarRun{chains=2})&4)==0,"L10 star pass3/fail2 generating chains");
        }
        IEnumerator RestPanel()
        {
            var ui=RestLoadoutUI.Instance;var fire=FireBreathCycle.Instance;var kit=world.player.GetComponent<SkillLoadout>();
            fire.AutoAdvance=true; // Validate pause/resume against a running fire clock, not the wave fixture hold.
            if(!ResultScreenOnly)Check(director.CanChangeSkills&&director.RestRemaining>14,"L9 rest opens15s loadout window");
            world.Mode(false);yield return new WaitForSecondsRealtime(.1f);
            var removed=kit.Find("han-bang-phong-an") as Set1SkillRuntime;
            int index=kit.IndexOf(removed);if(index<0){kit.EquipDuringRest(0,removed.Id);index=0;}
            removed.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();removed.CastAt(outdoor+Vector3.right*3);float oldCooldown=removed.CooldownRemaining;
            var added=kit.Find("phat-no-hoa-lien") as Set1SkillRuntime;added.ResetCooldownForValidation();added.CastAt(outdoor+Vector3.right*4);float incoming=added.CooldownRemaining;
            ui.Open();
            float rest=director.RestRemaining,fireRest=fire.Remaining;
            yield return new WaitForSecondsRealtime(.35f);
            if(!ResultScreenOnly)Check(ui.Visible&&UIStateManager.Instance.State==UIState.Modal&&Mathf.Abs(rest-director.RestRemaining)<.02f&&Mathf.Abs(fireRest-fire.Remaining)<.02f,"Panel pauses level and fire rest timers");
            yield return ShotFrame("loadout-pc");
            bool equipped=kit.EquipDuringRest(index,added.Id);
            if(!ResultScreenOnly)Check(equipped&&incoming>0&&added.CooldownRemaining==0&&oldCooldown>0&&removed.CooldownRemaining>0,"Incoming skill ready, removed skill retains real cooldown");
            world.Mode(true);yield return new WaitForSecondsRealtime(.2f);ui.SelectSlot(index);yield return ShotFrame("loadout-mobile");
            var buttons=ui.GetComponentsInChildren<Button>();
            if(!ResultScreenOnly)Check(buttons.All(b=>b.GetComponent<RiftGraphic>()!=null&&b.GetComponent<RectTransform>().rect.width>=68),"Every mobile panel button round and at least68px");
            var close=buttons.First(b=>b.name=="Close");ExecuteEvents.Execute(close.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);yield return null;
            if(!ResultScreenOnly)Check(!ui.Visible&&!director.RestLoadoutOpen&&UIStateManager.Instance.State==UIState.Gameplay,"Mobile Continue click closes panel and restores input");
            rest=director.RestRemaining;yield return new WaitForSecondsRealtime(.25f);if(!ResultScreenOnly)Check(director.RestRemaining<rest&&fire.Remaining<fireRest,"Rest countdown resumes after close");fire.AutoAdvance=false;world.Mode(false);
        }
        protected override IEnumerator Run()
        {
            Directory.CreateDirectory(Root+"screens");Begin();UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(.5f);
            director=LevelDirector.Ensure();hp=world.player.GetComponent<PlayerMonsterHealth>();
            if(!ResultScreenOnly){yield return AiSmoke();StarBoundaries();}
            for(int level=ResultScreenOnly?10:1;level<=10;level++)
            {
                BeginLevel(level);float until=Time.realtimeSinceStartup+75;var roster=new HashSet<string>();int swords=0;var voiceClips=new HashSet<string>();bool nearestOnly=true;int voiceSamples=0;
                if(level>=8)
                {
                    var data=director.Level;if(!ResultScreenOnly)Check(data.TotalMonsters==(level==8?32:level==9?36:45)&&data.waves.Count==level-7,"L"+level+" authored count and waves");
                    var f=FireBreathProfile.Load(level);if(!ResultScreenOnly)Check(f.outdoorDamage==(level==8?280:level==9?416:600)&&data.restSeconds==15,"L"+level+" fixed Fire damage /15s rest");
                }
                if(level==10)
                {
                    var chain=world.player.GetComponent<GenerationChainTracker>();
                    for(int i=0;i<3;i++){chain.ResetChain();chain.Record(Element.Moc);chain.Record(Element.Hoa);chain.Record(Element.Tho);}
                    if(!ResultScreenOnly)Check(director.GetComponent<StarEvaluator>().Run.chains==3,"Three real ChainCompleted events recorded this run");
                }
                while(director.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup<until)
                {
                    EnemyDirector.Instance.UpdateVoices();
                    var voiced=EnemyDirector.Instance.Active.Where(e=>e!=null&&e.Alive&&e.Voice!=null&&e.VoiceClip!=null).ToArray();
                    if(voiced.Length>0)
                    {
                        voiceSamples++;foreach(var e in voiced)voiceClips.Add(e.VoiceClip.name);
                        var nearest=voiced.OrderBy(e=>(e.transform.position-world.player.transform.position).sqrMagnitude).First();
                        var audible=voiced.Where(e=>e.Voice.isPlaying&&!e.Voice.mute).ToArray();
                        nearestOnly &= audible.Length==1&&audible[0]==nearest;
                    }
                    foreach(var e in director.Alive)if(e!=null)roster.Add(e.archetype.id);Hold();V2DevTools.KillCurrentWave();
                    if(director.AwaitingSkySword)
                    {
                        var scheduler=SkyBeastScheduler.Instance;var fire=FireBreathCycle.Instance;var u=HeavenSwordUltimate.Instance;
                        if(!ResultScreenOnly)Check(u.Intent.Full&&u.Intent.PlannedCount==director.Level.waves[director.WaveIndex].TotalCount,"L"+level+" wave"+(director.WaveIndex+1)+" real deaths fill intent");
                        if(level==10)
                        {
                            string name=scheduler.SwordTarget.GetComponent<SkyBeastController>().definition.id;
                            if(!ResultScreenOnly)Check(name==(swords==0?"023":swords==1?"026":"020"),"L10 target order "+name);
                            if(swords==2)
                            {if(!ResultScreenOnly)Check(scheduler.Fury.Warning&&!scheduler.SwordAllowed,"Long No requested before final sword");scheduler.Fury.Advance(5);if(!ResultScreenOnly)Check(fire.IsFury&&fire.IsBreathing,"Long No actual12s breath starts");fire.Advance(12);if(!ResultScreenOnly)Check(scheduler.Fury.Completed&&scheduler.SwordAllowed,"Long No completion unlocks final sword");}
                        }
                        world.PlacePlayer(outdoor);var accepted=u.TryChannel();if(!ResultScreenOnly)Check(accepted,"L"+level+" sword"+(swords+1)+" real outdoor channel");
                        yield return Until(()=>HeavenSwordCinematic.Active!=null,5);
                        var cine=HeavenSwordCinematic.Active;if(cine!=null&&cine.CanSkip)cine.Skip();
                        yield return Until(()=>!u.Busy,8);swords++;
                        if(!ResultScreenOnly)Check(!u.Intent.Full,"L"+level+" sword consumes intent");
                        if(director.State==LevelDirector.Phase.Rest)
                        {
                            if(!ResultScreenOnly)Check(scheduler.Phase==swords+1,"L"+level+" sword transitions to phase"+scheduler.Phase);
                            if(level==9&&swords==1)yield return RestPanel();
                        }
                    }
                    yield return null;
                }
                if(!ResultScreenOnly)Check(voiceSamples>0&&voiceClips.Count==1&&voiceClips.Contains(CampusRift.Audio.GameSfx.SmallMonsterForLevel(level).name), "L"+level+" every observed ordinary monster uses one stable clip");
                if(!ResultScreenOnly)Check(voiceSamples>0&&nearestOnly,"L"+level+" exactly one ordinary voice is audible at its nearest monster");
                if(!ResultScreenOnly)Check(director.State==LevelDirector.Phase.Won&&director.Kills==director.TotalPlanned&&director.WinsRaised==1,"L"+level+" spawn -> DEV deaths -> one victory, no stuck gate");
                if(level<=8)if(!ResultScreenOnly)Check(!RestLoadoutUI.Instance.ButtonVisible&&!director.CanChangeSkills,"L"+level+" has no between-wave skill button");
                if(level>=8)
                {if(!ResultScreenOnly)Check(new[]{"tieu-yeu","doc-nhan","liem-hon","thiet-giap-nguu","bao-thi","quang-ma","hoa-trung"}.All(id=>roster.Contains(id))&&roster.Count==11,"L"+level+" all seven user-provided and four new enemy types spawn");if(!ResultScreenOnly)Check(swords==level-7&&SkyBeastScheduler.Instance.Completed,"L"+level+" required swords complete scheduler");if(!ResultScreenOnly)Check((director.Result.starMask&4)!=0,"L"+level+" challenge star recorded from live events");}
                playthrough.Add("| "+level+" | "+((Realm)director.Level.requiredRealm)+" "+director.Level.requiredTier+" | "+director.Kills+"/"+director.TotalPlanned+" | "+swords+" | "+director.State+" | "+director.Result.starMask+" |");
                if(level==10)
                {
                    // P21 adds a dawn cinematic and a user-dismissed credits page before Results.
                    yield return Until(()=>P21StoryCinematic.Active!=null&&P21StoryCinematic.Active.CreditsVisible,25);
                    if(P21StoryCinematic.Active!=null)P21StoryCinematic.Active.Skip();
                    yield return Until(()=>UIStateManager.Instance.State==UIState.Victory,6);
                    Check(UIStateManager.Instance.State==UIState.Victory&&director.Result.starMask==7,"Level10 Results three stars");yield return ShotFrame("level10-results-three-stars");
                }
                director.End();yield return null;
            }
            File.WriteAllText(Root+"Playthrough-P19.md","# P19 · một lượt smoke liên tục1→10\n\nSpawn và thắng thật qua DEV lethal damage, đúng cảnh giới đề nghị, HP10000/bất tử/AI giữ, spawn tăng tốc. **Chưa đo thực chiến**, không chứng minh cân bằng/thời lượng. Không full regression.\n\n| Màn | Cảnh giới | Kills | Thiên Kiếm | State | Star mask |\n|---|---|---|---|---|---|\n"+string.Join("\n",playthrough)+"\n\nCác lỗi/giới hạn và raw tại task/p19/PROGRESS.md và Level8to10PlayTest.json.\n");
        }
        protected override void Cleanup()
        {
            RestLoadoutUI.Instance?.Close();director?.End();var qa=world.player.GetComponent<V2QaPlayer>();if(qa!=null)Destroy(qa);
            world.player.enabled=true;base.Cleanup();
            File.WriteAllText(Root+"Level8to10PlayTest.json",JsonUtility.ToJson(report,true));File.WriteAllText(Root+"smoke-DONE.txt",report.passed.Count+" PASS / "+report.failed.Count+" FAIL");
        }
    }
}
#endif
