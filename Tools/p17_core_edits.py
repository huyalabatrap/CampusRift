from pathlib import Path

def edit(path, old, new):
    p=Path(path); s=p.read_text(encoding='utf-8-sig'); assert old in s, (path,old[:80]); p.write_text(s.replace(old,new),encoding='utf-8')

edit('Assets/Progression/Runtime/ProfileData.cs','public bool heavenSwordSeen;', 'public CampusRift.UI.TutorialProgress tutorial = new CampusRift.UI.TutorialProgress();\n        public bool heavenSwordSeen;')
edit('Assets/CampusRiftUI/Runtime/SettingsManager.cs','public bool ReduceSkillFlashes;', 'public bool ReduceSkillFlashes;\n        public bool ReduceCameraShake;\n        public bool LocalTelemetryEnabled, TelemetryConsentAsked;')
edit('Assets/Skills/Core/Runtime/SkillRuntime.cs','public void CommitCast()\n        {','public void CommitCast()\n        {\n            Progression.LocalTelemetry.Skill(Id);\n            UI.TutorialDirector.SkillUsed(Id);')
edit('Assets/Progression/Runtime/PlayerItems.cs','ItemUsed?.Invoke(item, result); return result;', 'if (result == ItemUseResult.Used && item != null) { LocalTelemetry.Item(item.id); UI.TutorialDirector.ItemUsed(); } ItemUsed?.Invoke(item, result); return result;')
edit('Assets/Progression/Runtime/PlayerItems.cs','Changed?.Invoke(); Revived?.Invoke(item); ItemUsed?.Invoke(item, ItemUseResult.Used);','Changed?.Invoke(); Revived?.Invoke(item); LocalTelemetry.Item(item.id); ItemUsed?.Invoke(item, ItemUseResult.Used);')
edit('Assets/Learning/Runtime/LearningEngine.cs','void LogCorrect(QuizResult result)\n        {','void LogCorrect(QuizResult result)\n        {\n            CampusRift.Progression.LocalTelemetry.Quiz(result);')
edit('Assets/Skills/GiantHandSeal/Runtime/GiantHandCameraImpulse.cs','float accessibility=UI.SettingsManager.Instance!=null&&UI.SettingsManager.Instance.Current.ReduceSkillFlashes?.2f:1;', 'float accessibility=UI.SettingsManager.Instance!=null&&(UI.SettingsManager.Instance.Current.ReduceCameraShake||UI.SettingsManager.Instance.Current.ReduceSkillFlashes)?.2f:1;')
edit('Assets/Scripts/CampusExplorer.cs','if (controls.Pressed(Controls.CampusAction.Respawn)) ReturnToSpawn();', '#if UNITY_EDITOR || DEVELOPMENT_BUILD\n                if (controls.Pressed(Controls.CampusAction.Respawn)) ReturnToSpawn();\n#endif')
edit('Assets/MonsterShaban/Scripts/PlayerMonsterHealth.cs','public bool showLegacyHUD = true;', 'public bool showLegacyHUD = false;')
edit('Assets/MonsterShaban/Scripts/PlayerMonsterHealth.cs','if (!showLegacyHUD) return;', '#if !UNITY_EDITOR && !DEVELOPMENT_BUILD\n            return;\n#else\n            if (!showLegacyHUD) return;')
edit('Assets/MonsterShaban/Scripts/PlayerMonsterHealth.cs','GUI.color = old;', 'GUI.color = old;\n#endif')
# Existing AI distance cadence is already 5 Hz near / 2 Hz beyond 40m. Keep combat timing unchanged.
edit('Assets/CampusRiftUI/Runtime/TutorialDirector.cs','if (b.GetComponent<Controls.CircularRaycastFilter>() == null) { /* Button area remains large enough for touch. */ }', 'if (b.GetComponent<TutorialCircleHit>() == null) b.gameObject.AddComponent<TutorialCircleHit>();')

# Shared shader property for cosmetic flash suppression. No effect on damage or warning timing.
edit('Assets/SkyBeast/MeteorBatch.shader','float time=_Time.y;', 'float time=_Time.y; float _comfort=1-saturate(_CampusReduceFlashes);')
edit('Assets/SkyBeast/MeteorBatch.shader','half flicker=.92+.08*sin(time*17+v.seed);','half flicker=.92+.08*sin(time*17+v.seed)*_comfort;')
edit('Assets/SkyBeast/MeteorBatch.shader','struct A{','float _CampusReduceFlashes;\n            struct A{')
edit('Assets/SkyBeast/Runtime/FireBreathVisuals.cs','flash.Emit(new ParticleSystem.EmitParams', 'if(!(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes ?? false)) flash.Emit(new ParticleSystem.EmitParams')
