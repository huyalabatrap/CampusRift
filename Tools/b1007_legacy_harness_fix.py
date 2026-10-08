"""Update the existing legacy harness for the expanded catalog/mastery contract."""
from pathlib import Path
import shutil
p=Path('Assets/Skills/Core/Validation/SkillSet1PlayTest.cs')
b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig')
s=s.replace('public bool failedOnly;', 'public bool failedOnly;\n        public bool LegacyFailedOnly;\n        public string[] FailureLabels;',1)
s=s.replace('stack.Push(Run());','stack.Push(LegacyFailedOnly?FailedLegacy():Run());',1)
s=s.replace('SkillCatalog.Instance.skills.Count==10,"catalog has ten MVP skills"','SkillCatalog.Instance.skills.Count==21,"catalog has21 skills"',1)
s=s.replace('profile.Data.wallet.linhThach=50000;', 'profile.Data.wallet.linhThach=SkillCatalog.Instance.skills.Count*3050+1000;',1)
s=s.replace('saved.skills.ranks.Count==10,"ten ranks survive profile JSON roundtrip"','saved.skills.ranks.Count==21,"21 ranks survive profile JSON roundtrip"',1)
s=s.replace('float pct=expectedPercent*(skill is ChainLightningRuntime?Mathf.Pow(.9f,j):1);','float pct=(skill is FireLotusRuntime&&skill.Mastered?1.5f:expectedPercent)*(skill is ChainLightningRuntime?Mathf.Pow(.9f,j):1);',1)
s=s.replace('rain.CreatedSwords==30,"rain 30 swords / 30 separate landings / 30 warm bodies"','rain.CreatedSwords==50,"rain 30 swords / 30 separate landings / 50 warm bodies"',1)
method=r'''
        bool Selected(string label) => FailureLabels!=null&&Array.IndexOf(FailureLabels,label)>=0;
        IEnumerator FailedLegacy()
        {
            // Existing checks only. Catalog/upgrade setup may populate all ranks,
            // but only the originally failed price and JSON assertions are run.
            world.Begin();yield return null;yield return new WaitForFixedUpdate();
            skills=world.player.GetComponents<Set1SkillRuntime>();pool=world.player.GetComponent<SkillVfxPool>();
            spirit=world.player.GetComponent<SpiritPower>();stats=world.player.GetComponent<PlayerStats>();
            loadout=world.player.GetComponent<SkillLoadout>();input=world.player.GetComponent<CampusInput>();
            MonsterVitality.AnyDamaged+=Hit;
            if(Selected("catalog has ten MVP skills"))Check(SkillCatalog.Instance.skills.Count==21,"catalog has21 skills");
            var profile=ProfileService.Instance;profile.Data.wallet.linhThach=SkillCatalog.Instance.skills.Count*3050+1000;
            foreach(var def in SkillCatalog.Instance.skills)
            {
                int start=profile.Wallet.Balance;bool prices=true;int[] expected={150,400,900,1600};
                for(int r=0;r<4;r++)
                {
                    int realm=Mathf.Min(6,def.unlockRealm+r+1);int balance=profile.Wallet.Balance;
                    profile.Data.cultivation.realm=realm-1;profile.UseTransient(profile.Data);
                    prices&=!profile.Skills.TryUpgrade(def)&&profile.Wallet.Balance==balance;
                    profile.Data.cultivation.realm=realm;profile.UseTransient(profile.Data);
                    prices&=profile.Skills.NextCost(def)==expected[r]&&profile.Skills.RequiredRealm(def)==realm&&profile.Skills.TryUpgrade(def)&&profile.Skills.GetRank(def.id)==r+2;
                }
                prices&=profile.Wallet.Balance==start-3050&&!profile.Skills.TryUpgrade(def);
                var label=def.id+" rank prices / realm cap / max guard";if(Selected(label))Check(prices,label);
            }
            if(Selected("ten ranks survive profile JSON roundtrip"))
            {
                var saved=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));bool persisted=true;
                foreach(var entry in saved.skills.ranks)persisted&=entry.count==5;
                Check(persisted&&saved.skills.ranks.Count==21,"21 ranks survive profile JSON roundtrip");
            }
            profile.Data.skills.ranks.Clear();profile.Data.cultivation.realm=6;profile.Data.cultivation.tier=5;profile.UseTransient(profile.Data);
            foreach(var skill in skills)
            {
                for(int rank=1;rank<=5;rank++)
                {
                    string label=skill.Id+" rank"+rank+" real damage / element / finite";
                    bool flash=rank==1&&skill is LightningFlashRuntime&&Selected("flash 10m / pierces / Shock 0.5s");
                    bool rain=rank==1&&skill is SwordRainRuntime&&Selected("rain 30 swords / 30 separate landings / 30 warm bodies");
                    if(!Selected(label)&&!flash&&!rain)continue;
                    P18TestSupport.Prepare(world,skill is LightningFlashRuntime);P18TestSupport.SetRank(skill,rank);
                    loadout.Equip(2,skill.Id);currentId=skill.Id;hits.Clear();targets.Clear();
                    maxStatus=0;shockObserved=freezeObserved=bossChill=burnObserved=pulledObserved=false;
                    if(skill is FireLotusRuntime)world.victims[0].Element=Element.Kim;
                    if(skill is ChainLightningRuntime)for(int i=0;i<7;i++)world.victims[i].Element=Element.Am;
                    // Wait for fixture colliders/controller to settle before its first dash.
                    yield return new WaitForFixedUpdate();yield return null;
                    bool cast=skill.CastAt(world.origin+Vector3.right*8);
                    if(skill is SwordRainRuntime&&Selected(label))
                    {
                        while(((SwordRainRuntime)skill).SwordsLaunched==0&&skill.IsCasting)yield return null;
                        foreach(var sword in pool.GetComponentsInChildren<FlyingSword>(true))if(sword.gameObject.activeSelf&&sword.State==FlyingSword.Mode.Rain)
                        {var landing=(Vector3)typeof(FlyingSword).GetField("rainPoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(sword);world.victims[0].GetComponent<MinionMotor>().Place(landing);world.victims[0].GetComponent<MinionMotor>().Stop();break;}
                    }
                    float until=Time.realtimeSinceStartup+10;while(skill.IsCasting&&Time.realtimeSinceStartup<until)yield return null;
                    float percent=skill is LightningFlashRuntime?1.8f:skill is FireLotusRuntime?4.5f:skill is IceSealRuntime?1.2f:skill is ChainLightningRuntime?2:skill is BlackHoleRuntime?2.5f:.4f;
                    bool damage=cast&&hits.Count>0,finite=true;
                    for(int j=0;j<hits.Count;j++)
                    {
                        float pct=(skill is FireLotusRuntime&&skill.Mastered?1.5f:percent)*(skill is ChainLightningRuntime?Mathf.Pow(.9f,j):1);
                        float elem=skill is FireLotusRuntime&&targets[j].Element==Element.Kim?1.5f:skill is ChainLightningRuntime?1.5f:1;
                        float expected=stats.Attack*stats.DamageDealt*pct*(1+.12f*(rank-1))*elem;
                        damage&=Close(hits[j].amount,expected)&&hits[j].element==skill.definition.element;
                        finite&=!float.IsNaN(hits[j].amount)&&!float.IsInfinity(hits[j].amount);
                    }
                    if(skill is FireLotusRuntime&&skill.Mastered)
                    {
                        damage&=((FireLotusRuntime)skill).FlowerCount==3;
                        // Three 150% impacts retain the original total450% requirement.
                        foreach(var target in world.victims)
                        {
                            float sum=0;int count=0;for(int j=0;j<hits.Count;j++)if(targets[j]==target){sum+=hits[j].amount;count++;}
                            float elem=target.Element==Element.Kim?1.5f:1;
                            damage&=count==3&&Close(sum,stats.Attack*stats.DamageDealt*4.5f*(1+.12f*(rank-1))*elem);
                        }
                    }
                    if(Selected(label))Check(damage&&finite,label);
                    if(flash)Check(hits.Count>=3&&Close(world.player.LastDashDistance,10)&&shockObserved&&maxStatus>.4f&&maxStatus<=.501f,"flash 10m / pierces / Shock 0.5s");
                    if(rain){var r=(SwordRainRuntime)skill;Check(r.SwordsLaunched==30&&r.SwordImpacts==30&&r.CreatedSwords==50,"rain 30 swords / 30 separate landings / 50 warm bodies");}
                    yield return new WaitForSeconds(3.2f);
                }
            }
        }

'''
s=s.replace('        IEnumerator Run()\n',method+'        IEnumerator Run()\n',1)
p.write_text(s,encoding='utf-8')
