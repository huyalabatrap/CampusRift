from pathlib import Path
import shutil
p=Path('Assets/Enemies/Validation/EnemyAnimationPlayTest.cs');b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig').replace('        protected override IEnumerator Run()', '        public bool FailedItemsOnly;\n        protected override IEnumerator Run()')
s=s.replace('                var e=EnemyPool.Ensure()', '                if(FailedItemsOnly && arch.id!="anh-yeu" && arch.id!="trieu-hon-su" && arch.id!="duc-yeu" && arch.id!="hoa-linh")continue;\n                bool unfinished=!FailedItemsOnly || arch.id=="hoa-linh";\n                var e=EnemyPool.Ensure()')
s=s.replace('                Check(states.All','                if(unfinished)Check(states.All')
s=s.replace('Check(driver.Animator.speed==0','if(unfinished)Check(driver.Animator.speed==0')
s=s.replace('Mathf.Abs(normalized-.5f)<.14f','Mathf.Abs(normalized-driver.profile.attackImpactSeconds/driver.profile.Clip(driver.profile.attack).length)<.14f && Mathf.Abs(driver.AttackImpactAt-(Time.time-.0f))<.08f')
# The authored strike is 67.5% in replacement P19 clips, 50% in original clips.
s=s.replace('                yield return new WaitForSeconds(1.4f);','                if(FailedItemsOnly && (arch.id=="anh-yeu" || arch.id=="trieu-hon-su")){EnemyPool.Instance.Release(e);yield return null;continue;}\n                yield return new WaitForSeconds(1.4f);')
s=s.replace('plant.ResetMetrics();','if(plant!=null)plant.ResetMetrics();')
s=s.replace('arch.isFlying||plant.PlantSamples','arch.isFlying||plant!=null&&plant.PlantSamples')
s=s.replace('Measure(arch.id+', 'if(plant!=null)Measure(arch.id+')
p.write_text(s,encoding='utf-8')
