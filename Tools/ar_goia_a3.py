from pathlib import Path
p=Path('Assets/ARRift/Runtime/ARSkillCaster.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public event Action<string,bool> CastAttempted;', 'public event Action<string,bool> CastAttempted;\n        public event Action<GestureIntent,CastOutcome> Outcome;public event Action<GestureIntent,double> FirstVfx;\n        public bool DevPalmAim,Practice;public CastOutcome? InjectedRejection;public bool AimValid=>aimValid;GestureIntent pendingIntent;bool awaitingVfx;')
s=s.replace('gestures.GestureFired+=Fire;', 'gestures.Intent+=Fire;source.Invalidated+=Invalidate;').replace('gestures.GestureFired-=Fire;', 'gestures.Intent-=Fire;source.Invalidated-=Invalidate;')
s=s.replace('context.allUnlocked=field.placement.settings.allUnlocked;', 'context.allUnlocked=field.placement.settings.allUnlocked;context.FirstVfx+=Vfx;')
s=s.replace('filter.Reset();gestures.Suspend(true);', 'filter.Reset();')
start=s.index('            if(allowed){smoothed=')
end=s.index('            gestures.Process',start)
s=s[:start]+'''            if(DevPalmAim&&allowed){smoothed=filter.Filter(GestureCoordinates.Palm(f)*new Vector2(Screen.width,Screen.height),f.timestampMs*.001);aimValid=FindAim(smoothed,out var aim);Aim=aim;}
'''+s[end:]
start=s.index('        bool FindAim(');s=s[:start]+'''        void Invalidate(){gestures.Suspend();aimValid=false;}
        void Update()
        {
            if(field.Root==null||field.placement.view==null){aimValid=false;return;}
            if(!DevPalmAim){aimValid=FindAim(new Vector2(Screen.width*.5f,Screen.height*.5f),out var point);Aim=point;}
            if(indicator!=null)indicator.SetExternal(Aim,aimValid);
        }
        public static bool DiscAim(Ray ray,Vector3 center,Vector3 normal,float radius,out Vector3 point)
        {
            point=center;if(!new Plane(normal,center).Raycast(ray,out float distance)||distance<=0)return false;
            point=ray.GetPoint(distance);return Vector3.ProjectOnPlane(point-center,normal).sqrMagnitude<=radius*radius;
        }
        bool FindAim(Vector2 screen,out Vector3 point)=>DiscAim(field.placement.view.ScreenPointToRay(screen),field.Root.position,field.Root.up,field.placement.Radius,out point);
        void Vfx(double now){if(!awaitingVfx)return;awaitingVfx=false;FirstVfx?.Invoke(pendingIntent,now);}
        void Fire(GestureIntent intent)
        {
            string label=intent.label;var runtime=Runtime(label);bool ok=false;CastOutcome outcome;
            pendingIntent=intent;awaitingVfx=false;
            if(Practice&&runtime!=null){runtime.ReadyOnRestEquip();Caster.GetComponent<SpiritPower>().Refill();}
            if(InjectedRejection.HasValue)outcome=InjectedRejection.Value;
            else if(field.Paused||runtime==null)outcome=CastOutcome.Paused;
            else if(!aimValid)outcome=CastOutcome.Aim;
            else if(runtime.CooldownRemaining>0)outcome=CastOutcome.Cooldown;
            else if(!runtime.HasSpirit)outcome=CastOutcome.Spirit;
            else
            {
                awaitingVfx=true;var hand=runtime as GiantHandRuntime;
                ok=hand!=null?Caster.GetComponent<GiantHandSkill>().CastAt(Aim):((Set1SkillRuntime)runtime).CastAt(Aim);
                outcome=ok?CastOutcome.Success:CastOutcome.Unavailable;if(!ok)awaitingVfx=false;
            }
            Feedback=ok?runtime.ShortName:outcome==CastOutcome.Cooldown?"Đang hồi chiêu · hạ tay rồi giơ lại":outcome==CastOutcome.Spirit?"Thiếu Linh Lực · hạ tay rồi giơ lại":outcome==CastOutcome.Aim?"Ngắm vào vòng trận · hạ tay rồi giơ lại":outcome==CastOutcome.Paused?"Đã tạm dừng · hạ tay rồi giơ lại":"Chiêu chưa sẵn sàng · hạ tay rồi giơ lại";
            if(ok){Fired++;LastSkill=runtime.Id;ARHaptics.Pulse();}else gestures.RequireRelease();
            Outcome?.Invoke(intent,outcome);CastAttempted?.Invoke(label,ok);
        }
    }
    public enum CastOutcome { Success,Cooldown,Spirit,Aim,Paused,Unavailable }
}
'''
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARCombatContext.cs');s=p.read_text(encoding='utf-8-sig').replace('public ARBattlefield battlefield;', 'public event System.Action<double> FirstVfx;public void VfxStarted(){FirstVfx?.Invoke(GestureRecognizerBridge.Now);}\n        public ARBattlefield battlefield;');p.write_text(s,encoding='utf-8')
p=Path('Assets/Skills/Core/Runtime/SkillVfxPool.cs');s=p.read_text(encoding='utf-8-sig').replace('ActiveCount++;return n;', 'ActiveCount++;if(ar!=null)ar.VfxStarted();return n;');p.write_text(s,encoding='utf-8')
p=Path('Assets/Skills/GiantHandSeal/Runtime/GiantHandVisual.cs');s=p.read_text(encoding='utf-8-sig').replace('sparks.Clear();dust.Clear();sparks.Play();dust.Play();', 'sparks.Clear();dust.Clear();sparks.Play();dust.Play();if(owner!=null){var ar=owner.GetComponent<CampusRift.AR.ARCombatContext>();if(ar!=null)ar.VfxStarted();}');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARSessionBootstrap.cs');s=p.read_text(encoding='utf-8-sig').replace('cách máy 30–80 cm. Giữ lối đi thông thoáng.', 'cách máy 25–55 cm (bắt đầu 40 cm), lòng hoặc mu tay đều được. Đưa ngón tay cầm máy khỏi ống kính. Ngắm bằng tâm màn hình; xoay máy nhẹ để chọn mục tiêu.').replace('30–80 cm in front of the rear camera. Keep the area clear.', '25–55 cm from the rear camera (start at 40 cm), palm or back of hand. Keep holding fingers out of the lens. Aim with screen center.');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARBattleHUD.cs');s=p.read_text(encoding='utf-8-sig').replace('Image hp,spirit,handDot;', 'Image hp,spirit,handDot,centerReticle;float handUntil;')
s=s.replace('handDot=ARUI.Rect(', 'centerReticle=ARUI.Rect(content,"Center aim",0,0,22,22).gameObject.AddComponent<Image>();centerReticle.sprite=ComicTheme.Sprite("round-mask");centerReticle.raycastTarget=false;\n            handDot=ARUI.Rect(')
s=s.replace('void Received(GestureFrame f){landmarks.Set(f);}', 'void Received(GestureFrame f){landmarks.Set(f);if(f.handPresent||f.landmarks!=null&&f.landmarks.Length==63)handUntil=Time.unscaledTime+.25f;else handUntil=0;}')
s=s.replace('bool hand=caster.source.Latest.landmarks!=null&&caster.source.Latest.landmarks.Length==63;', 'centerReticle.gameObject.SetActive(fighting);centerReticle.color=caster.AimValid?ComicTheme.Green:ComicTheme.Muted;bool hand=Time.unscaledTime<handUntil&&caster.source.SamplingActive&&!caster.source.Recovering;')
s=s.replace('caster.gestures.Evidence[i]/field.placement.settings.evidenceThreshold','caster.gestures.Evidence[i]')
s=s.replace('caster.Feedback;', 'caster.Feedback;charge[index].Amount=1;')
s=s.replace('caster.source.Result+=Received;', 'caster.source.Result+=Received;caster.source.Invalidated+=ClearHand;')
s=s.replace('void Received(GestureFrame f)', 'void ClearHand(){handUntil=0;}\n        void Received(GestureFrame f)')
s=s.replace('if(caster.source!=null)caster.source.Result-=Received;', 'if(caster.source!=null){caster.source.Result-=Received;caster.source.Invalidated-=ClearHand;}')
p.write_text(s,encoding='utf-8')
