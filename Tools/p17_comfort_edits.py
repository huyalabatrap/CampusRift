from pathlib import Path
def edit(p,old,new):
    p=Path(p);s=p.read_text(encoding='utf-8-sig');assert old in s,(p,old);p.write_text(s.replace(old,new),encoding='utf-8')
edit('Assets/Learning/Runtime/LearningUI.cs','float pulse=0.55f+0.45f*Mathf.Sin(t*9f);float fade=', 'bool reduced=SettingsManager.Instance?.Current.ReduceSkillFlashes??false;\n            float pulse=reduced?.30f:0.55f+0.45f*Mathf.Sin(t*9f);float fade=')
edit('Assets/Learning/Runtime/LearningUI.cs','bool flash=UnityEngine.Random.value<0.28f;b.color=', 'bool flash=!reduced&&UnityEngine.Random.value<0.28f;b.color=')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandVisual.cs','Sigil(flashDisk,target.point+Vector3.up*.26f,config.radius,Mathf.Clamp01(1-aftermath/.18f),4,Gold);', 'bool reduced=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false;\n                Sigil(flashDisk,target.point+Vector3.up*.26f,config.radius,Mathf.Clamp01(1-aftermath/.18f)*(reduced?.12f:1),4,Gold);')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandVisual.cs','impactLight.intensity=6*','impactLight.intensity=(reduced?.4f:6)*')
edit('Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs','if(Time.time<flashUntil)','if(Time.time<flashUntil && !(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false))')
edit('Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs','float pulse=.5f+.5f*Mathf.Sin(Time.time*22f);','float pulse=(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false)?.5f:.5f+.5f*Mathf.Sin(Time.time*22f);')
edit('Assets/SkyBeast/Runtime/Attacks/SkyStrikePool.cs','flash.Emit(new ParticleSystem.EmitParams','if(!(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false)) flash.Emit(new ParticleSystem.EmitParams')
edit('Assets/SkyBeast/Runtime/HeavenSwordUltimate.cs','Channeling=false;circle.Fade();cinematic.Play();Changed?.Invoke();','Channeling=false;circle.Fade();Progression.LocalTelemetry.Skill("thien-kiem");cinematic.Play();Changed?.Invoke();')
edit('Assets/Progression/Runtime/LocalTelemetry.cs','void OnApplicationPause(bool pause) { if (pause) Finish("suspended", 0); }', '// Keep a run open across background/resume; one row is committed at outcome or quit.')
edit('Assets/CampusRiftUI/Runtime/TelemetryConsentUI.cs','if (card != null) card.gameObject.SetActive(show);', 'if (card != null) card.gameObject.SetActive(show);\n            if(show&&UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true)Choose(false);')
