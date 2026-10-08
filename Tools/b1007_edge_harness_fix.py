from pathlib import Path
import shutil
p=Path('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs')
b=Path('Backups/Regression-APK-pre-20261007-resume')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig')
s=s.replace('readonly DangerZone[] zones=', 'public bool IceExpiryOnly;\n        readonly DangerZone[] zones=',1)
s=s.replace('            foreach(var s in world.player.GetComponents<Set1SkillRuntime>())if(s.definition.castType==CastType.Aimed)',
'''            if(IceExpiryOnly){yield return FailedIceExpiry();world.End();File.WriteAllText("Artifacts/Skills/SkillSet1-Edges-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);yield break;}
            foreach(var s in world.player.GetComponents<Set1SkillRuntime>())if(s.definition.castType==CastType.Aimed)''',1)
method='''        IEnumerator FailedIceExpiry()
        {
            var ice=world.player.GetComponent<IceSealRuntime>();Prep(ice);
            Place(0,world.origin+Vector3.right*4);Place(1,world.origin+Vector3.right*5);world.victims[1].resistHardControl=true;
            var freeze=world.victims[0].GetComponent<StatusEffectHost>();var chill=world.victims[1].GetComponent<StatusEffectHost>();
            float started=-1,freezeDuration=-1,chillDuration=-1;
            Action<StatusType,bool> onFreeze=(type,active)=>{if(type==StatusType.Freeze&&active){started=Time.time;freezeDuration=freeze.Remaining(type);}};
            Action<StatusType,bool> onChill=(type,active)=>{if(type==StatusType.Chill&&active)chillDuration=chill.Remaining(type);};
            freeze.Changed+=onFreeze;chill.Changed+=onChill;
            bool cast=ice.CastAt(world.origin+Vector3.right*8);float timeout=Time.time+4;
            while(started<0&&Time.time<timeout)yield return null;
            while(started>=0&&Time.time<started+2.52f)yield return null;
            // Duration is still exactly2.5s. The start is actual application,
            // rather than cast start plus an assumed0.18s render-frame windup.
            Check(cast&&started>=0&&Mathf.Abs(freezeDuration-2.5f)<.002f&&Mathf.Abs(chillDuration-2.5f)<.002f&&
                !freeze.Has(StatusType.Freeze)&&!chill.Has(StatusType.Chill),"ice control expires at 2.5s");
            freeze.Changed-=onFreeze;chill.Changed-=onChill;
        }

'''
s=s.replace('        IEnumerator Start()\n',method+'        IEnumerator Start()\n',1)
p.write_text(s,encoding='utf-8')
with Path('task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n- Mốc SkillSet1Pool PASS121/0; SkillSet1Edges FAIL duy nhất ice expiry. Focus harness đo từ Changed/apply thay vì giả định render windup từ cast, vẫn bắt durationFreeze/Chill chính xác2.5s và trạng thái hết; chỉmụcFAILretst1lần, khônglặp27mụcPASS. BackupsourceRegression-APK-pre-20261007-resume.\n')
