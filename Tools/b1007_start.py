from pathlib import Path
import json
from b1007_runner import call,value,code,progress,OUT
p=Path('Assets/ARRift/Validation/ARGestureUnitTests.cs');s=p.read_text(encoding='utf-8-sig').replace('cases[c]=="unmapped"?"Thumb_Up"','cases[c]=="unmapped"?"ILoveYou"');p.write_text(s,encoding='utf-8')
print(call('refresh_unity',{}))
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True})
(OUT.parent/'7-compile-hud.json').write_text(json.dumps(console,indent=2),encoding='utf-8');print(console)
progress('Mốc 2: HUD đã thu gọn: nút tròn dùng vùng chữ nội tiếp; Linh Ấn mảnh/chỉ hiện chuỗi tiền tố; hints đầu trận5s; dragon toast3s theo trạng thái/vòng Kiếm Ý cạnh rail; quiz fallback chỉ khi rune cắt chữ; Luyện Ấn dải170px/nền bán trong suốt, hướng dẫn5s. HarnessAR cập nhật ThumbUp thành khiên và bước chọn mode trước placement; không làm yếu D1. Inventory91suite game thường từ runnerP23, bỏ alias lặp. Bắt đầu một lượt sau compile sạch.')
