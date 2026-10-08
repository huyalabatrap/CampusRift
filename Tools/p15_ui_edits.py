from pathlib import Path
p=Path('Assets/Controls/Runtime/MobileTouchZone.cs');s=p.read_text(encoding='utf-8-sig').replace('Attack, Dash, LockOn }','Attack, Dash, LockOn, Ultimate }').replace('else if(role==TouchRole.Dash)hud.Input.Press(CampusAction.Dash);','else if(role==TouchRole.Dash)hud.Input.Press(CampusAction.Dash);\n            else if(role==TouchRole.Ultimate)hud.Input.Press(CampusAction.Ultimate);');p.write_text(s,encoding='utf-8')
p=Path('Assets/Controls/Runtime/MobileControlsHUD.cs');s=p.read_text(encoding='utf-8-sig').replace('RefreshSkillFaces();\n            Button(TouchRole.Interact', 'var ultimate=Button(TouchRole.Ultimate,Vector2.right,new Vector2(-440,930),185,"THIÊN KIẾM",Gold);\n            foreach(var g in ultimate.glyph.GetComponents<Graphic>())Destroy(g);\n            Text(ultimate.glyph,"Sword glyph","V",52,new Vector2(70,80),Vector2.zero);\n            RefreshSkillFaces();\n            Button(TouchRole.Interact')
s=s.replace('static void SetText(TMP_Text target,string value)', '''void UpdateUltimateFace()
        {
            if(!faces.TryGetValue(TouchRole.Ultimate,out var f))return;
            var u=SkyBeast.HeavenSwordUltimate.Instance;bool show=u!=null&&u.Visible;
            f.rect.gameObject.SetActive(show);if(!show)return;
            bool vi=LevelHUD.Vietnamese;SetText(f.label,vi?"THIÊN KIẾM":"HEAVEN SWORD");SetText(f.state,UI.SwordIntentUI.StateLabel(u.State,vi));
            f.arc.SetFill(u.Channeling?u.ChannelProgress:u.Intent!=null?u.Intent.Fraction:0);
            bool ready=u.State==SkyBeast.HeavenSwordUltimate.ButtonState.Ready;f.edge.color=ready?Color.Lerp(Gold,Color.white,(Mathf.Sin(Time.unscaledTime*5)+1)*.35f):u.State==SkyBeast.HeavenSwordUltimate.ButtonState.Blocked?ComicTheme.Red:Gold;
        }
        static void SetText(TMP_Text target,string value)''')
s=s.replace('SetText(hint,text);','SetText(hint,text);UpdateUltimateFace();');p.write_text(s,encoding='utf-8')
p=Path('Assets/CampusRiftUI/Runtime/SkyLightingController.cs');s=p.read_text(encoding='utf-8-sig').replace('public SkyPreset Preset=>preset;', '''public float DawnProgress{get;private set;}
        bool dawn;
        public void SetDawnProgress(float progress)
        {
            if(!dawn){SetPreset(SkyPreset.Default);dawn=true;}
            DawnProgress=Mathf.Clamp01(progress);SetFireStorm(1-DawnProgress);SetBrightness(Brightness);
            if(sun!=null){sun.color=Color.Lerp(new Color(1,.52f,.18f),new Color(1,.88f,.68f),DawnProgress);var rotation=sunRotation.eulerAngles;sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(8,18,DawnProgress),rotation.y,rotation.z);}
        }
        public SkyPreset Preset=>preset;''').replace('preset=value;','dawn=false;DawnProgress=0;\n            preset=value;');p.write_text(s,encoding='utf-8')
