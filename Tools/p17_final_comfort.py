from pathlib import Path
changes={
 'Assets/CampusRiftUI/Runtime/MonsterWarningUI.cs':(
  'Indicator.alpha=.7f+.25f*Mathf.Sin(Time.unscaledTime*3);',
  'Indicator.alpha=SettingsManager.Instance?.Current.ReduceSkillFlashes==true?.85f:.7f+.25f*Mathf.Sin(Time.unscaledTime*3);'),
 'Assets/CampusRiftUI/Runtime/SwordIntentUI.cs':(
  'intent!=null&&intent.Full?Color.Lerp',
  'intent!=null&&intent.Full&&SettingsManager.Instance?.Current.ReduceSkillFlashes!=true?Color.Lerp'),
 'Assets/Controls/Runtime/MobileControlsHUD.cs':(
  'f.edge.color=ready?Color.Lerp',
  'f.edge.color=ready&&SettingsManager.Instance?.Current.ReduceSkillFlashes!=true?Color.Lerp')
}
for name,(old,new) in changes.items():
 p=Path(name);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==1,name;p.write_text(s.replace(old,new),encoding='utf-8');print(name)
