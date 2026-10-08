from pathlib import Path
p=Path('Assets/Controls/Runtime/MobileControlsHUD.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('new Vector2(-360,890),185','new Vector2(-540,890),185').replace('new Vector2(-590,675),140','new Vector2(-590,560),140')
old='SafeRoot.offsetMin=SafeRoot.offsetMax=Vector2.zero;'
assert old in s
s=s.replace(old,old+'''
            // Keep circular hit regions inside the device safe area at maximum user scale.
            Canvas.ForceUpdateCanvases();
            foreach(var zone in Zones)
            {
                if(zone.role==TouchRole.Look||zone.role==TouchRole.Cancel)continue;
                var r=(RectTransform)zone.transform;var half=r.rect.size*controlScale*.5f;
                var size=SafeRoot.rect.size;var anchor=Vector2.Scale(r.anchorMin,size);
                var center=anchor+basePositions[zone]*controlScale;
                center.x=Mathf.Clamp(center.x,half.x,size.x-half.x);
                center.y=Mathf.Clamp(center.y,half.y,size.y-half.y);
                r.anchoredPosition=center-anchor;
            }''')
p.write_text(s,encoding='utf-8')
print('Safe area layout patched; original harness unchanged.')
