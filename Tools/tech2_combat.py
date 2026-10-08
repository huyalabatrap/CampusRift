from pathlib import Path
import shutil
backup=Path('Backups/AR-Tech2-pre-20261006')
def edit(name,old,new):
    p=Path(name)
    if not (backup/name).exists():(backup/name).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup/name)
    s=p.read_text(encoding='utf-8-sig')
    if old not in s:raise RuntimeError('Missing anchor '+name+' '+old[:50])
    p.write_text(s.replace(old,new),encoding='utf-8')
edit('Assets/ARRift/Runtime/ARDepthCollision.cs','==Supported.Supported','==UnityEngine.XR.ARSubsystems.Supported.Supported')
edit('Assets/ARRift/Runtime/GestureStateMachine.cs','public int epoch;public string label;','public int epoch,handId;public string label;')
edit('Assets/ARRift/Runtime/GestureStateMachine.cs','id=++intentId,epoch=f.epoch,','id=++intentId,handId=f.handId,epoch=f.epoch,')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','ARPlayerCombat playerCombat;','public readonly ARTwoHands twoHands=new ARTwoHands();long dualIntentId;float delayedVoiceMultiplier=1;\n        ARPlayerCombat playerCombat;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','source.Result+=Frame;','source.Result+=Frame;source.HandsResult+=Hands;twoHands.Single+=DualSingle;twoHands.Pair+=DualPair;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','source.Result-=Frame;','source.Result-=Frame;source.HandsResult-=Hands;twoHands.Single-=DualSingle;twoHands.Pair-=DualPair;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','void Frame(GestureFrame f)\n        {','void Frame(GestureFrame f)\n        {\n            if(source.HandCount==2)return;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','void Invalidate(){','''bool DualAllowed=>Caster!=null&&!field.Paused&&!Practice&&!field.CheckLoad&&(!field.Mode.loseOnShrine||!field.Shrine.IsDead)&&!field.GetComponent<ARMonsterDirector>().Finished;
        void Hands(GestureHandsFrame batch){twoHands.Process(batch,field.placement.settings,DualAllowed);}
        void DualSingle(GestureIntent intent){if(!DualAllowed)return;intent.id=++dualIntentId;Intent(intent);}
        void DualPair(bool quick,GestureIntent intent)
        {
            if(!DualAllowed||!field.ModeSession.Has(ARModeFeature.Sequences)||(GetComponent<ARKnowledgeSeal>()?.Active??false)||field.Mode.winRule==ARWinRule.Rhythm)return;
            sequences.Cancel();GetComponent<ARSpaceModes>()?.CancelDynamic();intent.id=++dualIntentId;
            if(quick){if(Seal>=100)Ultimate(ARUltimate.SwordConvergence,new GestureSequenceMatcher.Cast{intent=intent,aim=Aim,aimValid=aimValid});else{Feedback=CampusRift.UI.LevelHUD.Vietnamese?"Cần Linh Ấn đầy":"Full seals required";Outcome?.Invoke(intent,CastOutcome.Unavailable);}return;}
            bool ok=aimValid&&Caster.GetComponent<GiantHandSkill>().CastARTwin(Aim);Feedback=CampusRift.UI.LevelHUD.Vietnamese?(ok?"THIÊN THỦ ĐÔI":"Thiên Thủ chưa sẵn sàng · hạ hai tay"):(ok?"TWIN HANDS":"Hand skill unavailable · lower both hands");Outcome?.Invoke(intent,ok?CastOutcome.Success:CastOutcome.Unavailable);CastAttempted?.Invoke("Open_Palm",ok);if(ok){Fired++;ARHaptics.Skill("Open_Palm");}
        }
        void Invalidate(){twoHands.Reset();''')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','void ResetEnergy(){Seal=0;','void ResetEnergy(){twoHands.Reset();Seal=0;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','void Clear(){Seal=0;','void Clear(){twoHands.Reset();Seal=0;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','sequences.Tick(Time.unscaledTime);','if(source.HandCount==2){if(DualAllowed)twoHands.Flush(GestureRecognizerBridge.Now);else twoHands.Reset();}\n            sequences.Tick(Time.unscaledTime);')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','CastARUltimate(field.Root.position,3);','CastARUltimate(field.Root.position,3*delayedVoiceMultiplier);')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','{bool ok=false;if(!field.Paused&&Seal>=100&&Caster!=null){switch(kind)', '{var voice=GetComponent<ARVoiceCommands>();float voicePower=voice?.Multiplier(kind)??1;bool ok=false;if(!field.Paused&&Seal>=100&&Caster!=null){switch(kind)')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','((SwordRainRuntime)Runtime("Victory")).CastARUltimate(field.Root.position);','((SwordRainRuntime)Runtime("Victory")).CastARUltimate(field.Root.position,3*voicePower);')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','delayedUltimate=kind;lightningAt=field.Clock+.8f;','delayedUltimate=kind;delayedVoiceMultiplier=voicePower;lightningAt=field.Clock+.8f;')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','CastARAbsorption();','CastARAbsorption(voicePower);')
edit('Assets/ARRift/Runtime/ARSkillCaster.cs','if(ok){GetComponent<ARSpaceModes>()?.Boss.AcceptUltimate();','if(ok){voice?.Consume();GetComponent<ARSpaceModes>()?.Boss.AcceptUltimate();')
# Snapshot voice multiplier at cast start; it cannot boost unrelated/single later hits.
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','bool impacted,arAbsorption;','GiantHandVisual twinVisual;bool arTwin;float absorptionPower=1;\n        bool impacted,arAbsorption;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','public bool CastARAbsorption()','public bool CastARAbsorption(float multiplier=1)')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','gathered=0;arAbsorption=true;','gathered=0;arTwin=false;absorptionPower=multiplier;arAbsorption=true;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','HandSealTarget committed;','''public bool CastARTwin(Vector3 point)
        {
            if(ar==null||!CastAt(point))return false;arTwin=true;
            if(twinVisual==null){var go=new GameObject("AR twin hand reused");go.transform.SetParent(ar.battlefield.Root,true);twinVisual=go.AddComponent<GiantHandVisual>();twinVisual.Initialize(config,transform);}
            var second=committed;second.point=ar.ConstrainVisual(point+ar.battlefield.Root.right*ar.battlefield.placement.Radius*.23f);twinVisual.Begin(second,Quaternion.LookRotation(ar.battlefield.Root.forward));
            var first=committed;first.point=ar.ConstrainVisual(point-ar.battlefield.Root.right*ar.battlefield.placement.Radius*.23f);Visual.Begin(first,Quaternion.LookRotation(ar.battlefield.Root.forward));return true;
        }
        HandSealTarget committed;''')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','Visual.Animate(elapsed,transform.position);','Visual.Animate(elapsed,transform.position);if(arTwin&&twinVisual!=null)twinVisual.Animate(elapsed,transform.position);')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','Visual.Finish();','Visual.Finish();if(twinVisual!=null)twinVisual.Finish();arTwin=false;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','arAbsorption=false;committed=evaluated;','arAbsorption=false;arTwin=false;absorptionPower=1;committed=evaluated;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','Visual.Impact();','Visual.Impact();if(arTwin&&twinVisual!=null)twinVisual.Impact();')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','info.amount*=1+.3f*gathered;','info.amount*=(1+.3f*gathered)*absorptionPower;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','info.isArea=true;','info.isArea=true;if(arTwin)info.amount*=2;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','void OnDestroy(){if(Visual!=null)Destroy(Visual.gameObject);}','void OnDestroy(){if(Visual!=null)Destroy(Visual.gameObject);if(twinVisual!=null)Destroy(twinVisual.gameObject);}')
# Block attacks aimed at monsters hidden by depth, without altering non-AR combat.
edit('Assets/Skills/Core/Runtime/Set1SkillRuntime.cs','if(victim==null||!victim.isActiveAndEnabled||victim.Defeated)return false;', 'if(victim==null||!victim.isActiveAndEnabled||victim.Defeated||(ARContext?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false))return false;')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandSkill.cs','if(victim==null || !victim.isActiveAndEnabled || victim.Defeated)continue;','if(victim==null || !victim.isActiveAndEnabled || victim.Defeated||(ar?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false))continue;')
edit('Assets/ARRift/Runtime/ARHandInteractions.cs','if(actor==null||!actor.Alive||actor.Status==null||','if(actor==null||!actor.Alive||(GetComponent<ARDepthCollision>()?.Hidden(actor.Vitality)??false)||actor.Status==null||')
edit('Assets/ARRift/Runtime/ARHandInteractions.cs','if(enemy==null||!enemy.Alive)continue;','if(enemy==null||!enemy.Alive||(GetComponent<ARDepthCollision>()?.Hidden(enemy.Vitality)??false))continue;')
edit('Assets/ARRift/Runtime/ARThrownMonster.cs','var delta=transform.position-prior;', 'var delta=transform.position-prior;var depth=field.GetComponent<ARDepthCollision>();if(depth!=null&&depth.Sweep(prior,transform.position,out var contact)){transform.position=contact;Restore();return;}')
edit('Assets/ARRift/Runtime/ARPlayerCombat.cs','shot.visual.transform.position=Vector3.Lerp(shot.start,shot.target,Mathf.Clamp01((field.Clock-shot.launch)/WarningSeconds));','''var destination=Vector3.Lerp(shot.start,shot.target,Mathf.Clamp01((field.Clock-shot.launch)/WarningSeconds));var depth=GetComponent<ARDepthCollision>();
                if(depth!=null&&depth.Sweep(shot.visual.transform.position,destination,out var contact)){caster.Caster.GetComponent<SkillVfxPool>().Burst(contact,CampusRift.UI.ComicTheme.Gold,.35f/field.Scale,null,false);Release(shot);continue;}
                shot.visual.transform.position=destination;''')
edit('Assets/Combat/Runtime/FlyingSword.cs','transform.position=Vector3.Lerp(rainStart,rainPoint,Mathf.Clamp01(rainAge/.24f));','''var next=Vector3.Lerp(rainStart,rainPoint,Mathf.Clamp01(rainAge/.24f));var depth=rainOwner!=null&&rainOwner.ARContext!=null?rainOwner.ARContext.battlefield.GetComponent<CampusRift.AR.ARDepthCollision>():null;
                if(depth!=null&&depth.Sweep(transform.position,next,out var contact)){rainPoint=contact;rainAge=.24f;next=contact;}
                transform.position=next;''')
edit('Assets/ARRift/Runtime/ARTechHUD.cs','✋ + ✋: Thiên Thủ đôi','MỞ + MỞ: Thiên Thủ đôi')
edit('Assets/Plugins/Android/VoiceBridge.kt','băng lôi ngục','băng thiên lôi ngục')
edit('Assets/Plugins/Android/VoiceBridge.kt','hấp tinh đại pháp','thiên thủ hấp tinh')
with Path('task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## Mốc 2: contract / runtime / native\n- Thêm ARTechSettings/ARTwoHands/ARDepthCollision/ARVoiceCommands/ARClipRecorder/ARTechHUD và VoiceBridge/ClipBridge Android. Native GestureBridge numHands1/2, batch2hand; association bằng wrist+handedness, D1 riêng, batch epoch/frameid.\n- HaiPalm dùng TwinHandvisual reuse/cùng1cost-CD/damage2; Fist+Victory cùng180ms gọi SwordConvergence chỉ khi100Seal. Một tay giữ pipeline cũ.\n- Depth chỉproviderhỗtrợ/tierkhôngreduced/budget10rayframe, sweepkiếm/energyball/quáiném vàcachehideđểchặn hit/nhấc. Khôngdepthplacement.\n- VoskmodelVN đãtải33,656,337B/unpack53,290,365B Apache2; từkhóalocal/cửasổKếtẤn, voicepower1.3snapshot. Clipexplicitwarning+systemconsent/FGservice/gallery/silent90s.\n- Source đangcompile; cần hoànthiệnmanifest/nativecompilation,ảnh, tài liệuPhase3 vàphụchồi. Khôngtest/harness.\n')
print('Combat/depth integration and milestone saved')
