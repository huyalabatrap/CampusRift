from pathlib import Path
import shutil

def edit(path, old, new):
    p=Path(path)
    backup=Path('Backups/P10-fix3-resume-20261002')/p
    if not backup.exists():
        backup.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(p,backup)
    s=p.read_text(encoding='utf-8-sig')
    assert s.count(old)==1,(path,old,s.count(old))
    p.write_text(s.replace(old,new),encoding='utf-8-sig')

edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     'slots[0].rect.anchoredPosition.y != SlotY', 'bar.anchoredPosition.y != SlotY')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '            ComicTheme.Text(t, name == "Key");',
     '''            ComicTheme.Text(t, name == "Key");
            t.margin=Vector4.zero;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=12;
            r.sizeDelta=new Vector2(68,38);''')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '"", 26, new Vector2(1, 0), new Vector2(-8, 4)',
     '"", 22, new Vector2(1, 0), new Vector2(-18, 18)')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '22, new Vector2(0, 1), new Vector2(8, -4)',
     '18, new Vector2(0, 1), new Vector2(16, -14)')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '15, new Vector2(1, 1), new Vector2(-6, -6)',
     '13, new Vector2(1, 1), new Vector2(-18, -14)')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '            if (builtSlots < 0) Rebuild();',
     '            if (builtSlots < 0 || mobile != CampusInput.Mobile) Rebuild();')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     's.count.text = "×" + data.remaining;',
     r's.count.text = "\u00D7" + data.remaining;')
edit('Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
     '                s.cooldown.fillAmount = data.item.IsPassive ? 0 : items.CooldownFraction;',
     '''                s.key.text=mobile?"":(i+1).ToString();
                s.cooldown.fillAmount = data.item.IsPassive ? 0 : items.CooldownFraction;''')
edit('Assets/CampusRiftUI/Comic/ComicInk.shader',
     'smoothstep(.91,.99,min(color.r,color.g))*smoothstep(.3,.7,color.b)',
     'smoothstep(.75,.88,min(color.r,color.g))*smoothstep(.4,.65,color.b)')
print('Item count/key layout and UTF-8 fixed; HDR core mask tuned for post-tonemap colours.')
