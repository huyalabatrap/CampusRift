"""Apply final source corrections only in Edit Mode, before the full remaining pass."""
from pathlib import Path
import shutil

root=Path.cwd();backup=root/'Backups/P23-resume-20261004'
def edit(rel,old,new):
    p=root/rel;dest=backup/rel;dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists():shutil.copy2(p,dest)
    s=p.read_text(encoding='utf-8-sig');assert old in s,(rel,old)
    p.write_text(s.replace(old,new),encoding='utf-8')

edit('Assets/CampusRiftUI/Runtime/SettingsUI.cs',
     'Quiz / bia không giới hạn giờ; Thi Đột Phá giữ giờ gốc. Telemetry mặc định tắt, chỉ lưu máy; không thông tin cá nhân.',
     'Đọc chậm: giờ luyện tập ×1,5; bia không giới hạn giờ. Thi Đột Phá giữ giờ gốc. Telemetry mặc định tắt, chỉ lưu trên máy.')
edit('Assets/CampusRiftUI/Runtime/SettingsUI.cs',
     'Quiz / stele: no deadline. Exam time unchanged. Telemetry: off by default, local only, no personal data.',
     'Slow reading: practice time ×1.5; stele has no deadline. Exam time unchanged. Telemetry: off by default, local only.')

edit('Assets/CampusRiftUI/Validation/P23Performance.cs',
     'readonly Report report=new Report();SkillSet1TestWorld world;',
     'PlayerMonsterHealth qaHealth;\n        bool KeepAlive(DamageInfo hit){qaHealth.Revive(1,0);return true;}\n        readonly Report report=new Report();SkillSet1TestWorld world;')
edit('Assets/CampusRiftUI/Validation/P23Performance.cs',
     'HeavenSwordCinematic.Active?.Cancel();if(d!=null)d.End();',
     'if(qaHealth!=null)qaHealth.BeforeDefeat-=KeepAlive;HeavenSwordCinematic.Active?.Cancel();if(d!=null)d.End();')
edit('Assets/CampusRiftUI/Validation/P23Performance.cs',
     'world.player.GetComponent<PlayerMonsterHealth>().Revive(1,600);',
     'world.player.GetComponent<PlayerMonsterHealth>().Revive(1,600);qaHealth=world.player.GetComponent<PlayerMonsterHealth>();qaHealth.BeforeDefeat+=KeepAlive;')
edit('Assets/CampusRiftUI/Validation/P23Performance.cs',
     'AI active, player100000HP to keep fixture alive.',
     'AI active; player kept alive by a fixture-only BeforeDefeat revival because Begin resets progression health.')

edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs',
     '''                var cue=kit.Button(card,L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓"),390,590,470,38,()=>{scroll.verticalNormalizedPosition=scroll.verticalNormalizedPosition<.05f?1:Mathf.Max(0,scroll.verticalNormalizedPosition-height/Mathf.Max(height,list.rect.height-height));},true,false,18);
                // Button's standard68px minimum would intrude into the footer; this compact cue
                // supplements the large swipe target and remains an actual clickable page-down.
                ((RectTransform)cue.transform).sizeDelta=new Vector2(470,38);
                var cueText=cue.GetComponentInChildren<TMP_Text>();cueText.rectTransform.sizeDelta=new Vector2(434,38);cueText.enableAutoSizing=false;cueText.fontSize=18;''',
     '''                Round(card,"Scroll next","",926,594,68,()=>{scroll.verticalNormalizedPosition=scroll.verticalNormalizedPosition<.05f?1:Mathf.Max(0,scroll.verticalNormalizedPosition-height/Mathf.Max(height,list.rect.height-height));},null,false,"v");
                var cueText=kit.Text(card,L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓"),390,604,520,38,18,null,TextAlignmentOptions.MidlineRight,false);
                cueText.enableAutoSizing=false;cueText.fontSize=18;''')
edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs',
     '),34,630,980,50,20,null,TextAlignmentOptions.TopLeft,false);',
     '),34,mobile?670:630,mobile?800:980,50,20,null,TextAlignmentOptions.TopLeft,false);')

with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n- Soi ảnh Settings phát hiện ghi chú cũ nói quiz không giới hạn giờ, trái với timer300→450s đã audit. Sửa mô tả đúng luật thực tế, không đổi timer. Chuẩn bị fixture hiệu năng native giữ sống qua BeforeDefeat vì Begin reset HP; chỉ trong Editor/dev define, không đổi gameplay/balance. Sẽ audit lại nhãn Settings lớn và chạy mẫu native sau build.\n')
    f.write('- Level8to10 sau audio105/2: tất cả20assertion clip/nearest PASS. Nút cuộn bổ sung ở STABILIZE là hit target38px/chữ nhật: sửa thành nút tròn68px, giữ page-down và viewport hai hàng; kiểm nhánh UI riêng. FAIL Results do fixture chờ2,7s trong khi P21 dawn20s +credits; giữ assertion/raw và dùng Cinematic hiện hành kiểm ending→Results.\n')
print('Final Settings wording and native fixture corrected')
