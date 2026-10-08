from pathlib import Path
import json,hashlib,datetime
p=Path('Assets/SkyBeast/LICENSES.md')
s=p.read_text(encoding='utf-8-sig')+'''

# P15 — Thiên Kiếm / Vạn Kiếm Quy Tông

- Phi kiếm dùng mesh gốc P03 `FlyingSword.BladeMesh`; lưỡi150m, batch2400/1600/960 kiếm, vòng trận/phù văn/shockwave, shader HeavenGold và UI được viết riêng cho Campus Rift. Không thêm model bên thứ ba.
- Chuỗi7âm cast/charge/rift/descent/impact/aftershock/dissipate tái dùng `GiantHandConfig`, nguồn **Kenney**, [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) và [Impact Sounds](https://kenney.nl/assets/impact-sounds), **CC0 1.0**. License gốc trong `Assets/Skills/GiantHandSeal/Audio/`. Kiếm ngân dùng `impactBell_heavy_000.ogg`, cũng Kenney/CC0.
- `Resources/P15/victory.wav`: **celestialghost8**, [Victory](https://opengameart.org/content/victory), **CC0 1.0**. Fanfare nguyên bản 2A03, tải [Victory.wav](https://opengameart.org/sites/default/files/Victory.wav) ngày03/10/2026; SHA256 trong `task/p15/source/manifest.json`. Không gắn licenseCC0 cho model rồng người dùng.
- Các trang nguồn đã được kiểm giấy phép trực tiếp03/10/2026. Model/texture/rig/LOD và mọi giấy phép P12–P14 được giữ nguyên.
'''
p.write_text(s,encoding='utf-8')
Path('task/p15/source').mkdir(exist_ok=True)
f=Path('Assets/SkyBeast/Resources/P15/victory.wav')
Path('task/p15/source/manifest.json').write_text(json.dumps({'date':'2026-10-03','sources':[{'author':'celestialghost8','license':'CC0 1.0','page':'https://opengameart.org/content/victory','url':'https://opengameart.org/sites/default/files/Victory.wav','file':str(f),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()},{'author':'Kenney','license':'CC0 1.0','pages':['https://kenney.nl/assets/impact-sounds','https://kenney.nl/assets/sci-fi-sounds'],'reuse':'Assets/Skills/GiantHandSeal/GiantHandConfig.asset'}]},indent=2),encoding='utf-8')
