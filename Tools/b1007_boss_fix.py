from pathlib import Path
import shutil
p=Path('Assets/Enemies/Validation/ShabanBossPlayTest.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        protected override IEnumerator Run()', '        public bool FailedItemsOnly;\n        protected override IEnumerator Run()')
s=s.replace('boss.AddsSpawned==3','boss.AddsSpawned==2').replace('three adds','two Shadow adds')
s=s.replace('eliteSeconds=60,iceLightning=5','eliteSeconds=60,iceLightning=5,summonersSeen=1,summonersKilled=1')
s=s.replace('eliteSeconds=60.1f,iceLightning=4','eliteSeconds=60.1f,iceLightning=4,summonersSeen=1,summonersKilled=0,secondSummon=true')
s=s.replace('            var elite=', '''            if(FailedItemsOnly){
                var e=EnemyPool.Ensure().Spawn(LevelCatalog.Instance.Get(7).bosses[0],world.origin+Vector3.right*5,LevelCatalog.Instance.Get(7).Scaling);
                var boss=e.GetComponent<BossController>();float old=e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed;
                e.Vitality.ApplyDamage(DamageInfo.Create(e.MaxHealth*.51f/(1-e.Vitality.defense),Element.None,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject));yield return null;
                Check(boss.PhaseTwo&&boss.PhaseTransitions==1&&boss.AddsSpawned==2&&Mathf.Abs(e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed-old*1.3f)<.01f,"boss7 one phase transition / speed+30% / two Shadow adds");
                EnemyPool.Instance.ReleaseAll();
                var yes=new StarRun{giantHandKills=3,poisonHits=3,eliteKilled=true,eliteSeconds=60,iceLightning=5,summonersSeen=1,summonersKilled=1};
                var no=new StarRun{giantHandKills=2,poisonHits=4,eliteKilled=true,eliteSeconds=60.1f,iceLightning=4,summonersSeen=1,summonersKilled=0,secondSummon=true,healUsed=true,revived=true};
                Check(StarEvaluator.Evaluate(6,true,60,300,yes)==7&&(StarEvaluator.Evaluate(6,true,800,300,no)&6)==0&&StarEvaluator.Evaluate(6,false,1,300,yes)==0,"stars L6 pass/fail/completion guards");yield break;
            }
            var elite=''')
p.write_text(s,encoding='utf-8')
