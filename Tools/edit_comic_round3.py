from pathlib import Path
def edit(file,changes):
    p=Path(file);s=p.read_text(encoding='utf-8-sig')
    for old,new in changes:
        if old not in s:raise ValueError(file+' missing '+old[:80])
        s=s.replace(old,new)
    p.write_text(s,encoding='utf-8')
edit('Assets/CampusRiftUI/Runtime/LoadoutUI.cs',[
('118, 158, 194','118, 158, 242'),
('"Icon", 38, 46, 84, 80','"Icon", 24, 50, 110, 110'),
('22, 132, 114, 50, 16','18, 174, 122, 50, 18'),
('6, 70, 148, 30','18, 96, 122, 36'),
(', 326,',', 376,'),
('float rowH = 96; var list = kit.Scroll(body, "Skill List", 744, 414, 680, 410, Mathf.Max(410, skills.Count * rowH));','float rowH = 112; var list = kit.Scroll(body, "Skill List", 744, 462, 680, 336, Mathf.Max(336, skills.Count * rowH));'),
('"Icon", 8, 6, 66, 66','"Icon", 12, 12, 82, 82'),
('90, 10, 370, 38','110, 16, 350, 38'),
('90, 50, 468, 30','110, 62, 448, 30'),
('            // sets','            kit.Text(body,L("Select a slot, then a skill to equip.","Chọn ô, rồi chọn kỹ năng để trang bị."),748,802,660,28,14,UiKit.Muted,TextAlignmentOptions.TopLeft,false);\n            // sets'),
('            float y = 118;','            var itemList=kit.Scroll(body,"Owned Items",1476,118,388,392,Mathf.Max(392,owned.Count*148));\n            float y = 0;'),
('kit.Card(body, "Item " + item.id, 1476, y, 388, 92','kit.Card(itemList, "Item " + item.id, 0, y, 376, 140'),
('94, 10, 270, 34','100, 12, 256, 34'),
('94, 48, 270, 30','100, 52, 256, 30'),
('                y += 100; if (y > 880) break;','                kit.Text(r,item.Description(Vn),18,90,334,36,17,carry>0?ComicTheme.Ink:UiKit.Muted,TextAlignmentOptions.TopLeft,true);\n                y += 148;'),
('            kit.Button(body, L("TAKE NOTHING"','            kit.Text(body,L("EQUIPMENT FOR THIS LEVEL","TRANG BỊ MANG VÀO MÀN"),1486,530,366,38,20,UiKit.Gold,TextAlignmentOptions.TopLeft,false);\n            var packed=inv.Carry.ToList();\n            for(int i=0;i<inv.SlotCount&&i<3;i++)\n            {\n                var slot=kit.Panel(body,"Carried Slot "+i,1476,580+i*102,388,92);\n                if(i<packed.Count)\n                {\n                    var entry=packed[i];var item=catalog.items.FirstOrDefault(d=>d.id==entry.key);\n                    if(item!=null){kit.Image(slot,"Icon",16,12,64,64,Color.white,ItemIcons.Get(item));kit.Text(slot,item.Name(Vn)+" ×"+entry.count,98,20,268,44,20,null,TextAlignmentOptions.TopLeft,true);}\n                }\n                else kit.Text(slot,L("EMPTY EQUIPMENT SLOT","Ô TRANG BỊ TRỐNG"),22,24,336,40,18,UiKit.Muted,TextAlignmentOptions.Center,false);\n            }\n            kit.Button(body, L("TAKE NOTHING"')])
for name in ['ComicReviewPlayTest','ComicOutcomePlayTest','ComicPerformancePlayTest']:
    p=Path('Assets/CampusRiftUI/Validation')/(name+'.cs')
    s=p.read_text(encoding='utf-8-sig').replace('round2','round3').replace('round 2','round 3');p.write_text(s,encoding='utf-8')
edit('Assets/CampusRiftUI/Runtime/SkyLightingController.cs',[
('ambTint=new Color(1f,.87f,.91f);','ambTint=new Color(1f,.92f,.85f);'),
('sunTint=new Color(1f,.72f,.76f);','sunTint=new Color(1f,.78f,.61f);'),
('expS=1.9f','expS=1.2f'),
('ambTint=new Color(.98f,.88f,1f);','ambTint=new Color(1f,.95f,.82f);'),
('sunTint=new Color(1f,.75f,.83f);','sunTint=new Color(1f,.86f,.56f);'),
('expS=1.55f','expS=1.2f')])
root=Path('task/ui-comic')
for file in ['start-review','start-outcome','start-performance','start-tests']:
    p=root/(file+'-round2.cs');(root/(file+'-round3.cs')).write_text(p.read_text(encoding='utf-8-sig').replace('round2','round3'),encoding='utf-8')
print('Round 3 layout and QA paths updated')
