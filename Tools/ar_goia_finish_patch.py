from pathlib import Path
p=Path('Assets/ARRift/Runtime/ARGestureCheck.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('"Giơ một chiêu; GIỮ NGUYÊN khi báo "+','"Hạ tay khi đổi yêu cầu rồi giơ một chiêu; GIỮ NGUYÊN khi báo "+')
s=s.replace('" · không được tự thử lại"','" · sau 5 giây bỏ từ chối, vẫn giữ tay"')
p.write_text(s,encoding='utf-8')
p=Path('task/ar/PROGRESS.md');s=p.read_text(encoding='utf-8-sig');line='- **Gói A Hub hoàn tất:** HubFlow đúng1lượt42PASS/0FAIL, Console0 trước Stop, evidence `goiA/HubFlow`; Artifacts/UI gốc đã phục hồi. Đang kiểm hẹp metrics/5VFX theo lịch/selector sau bổ sung rolling10s và clock; còn build/verify/adb/restore/report.\n'
s=s.replace('## Trạng thái hiện tại — tiếp tục từ đây\n','## Trạng thái hiện tại — tiếp tục từ đây\n'+line,1);p.write_text(s,encoding='utf-8')
