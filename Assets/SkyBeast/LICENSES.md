# P12 — Model và âm thanh

- Model 020, 023, 026, texture, rig, 40/40/46 clip, socket: **user-provided, D:\model-3d\Unity_Export**. Bản quyền thuộc người dùng; không khẳng định là CC0. LOD sinh từ model đó bằng Tools/p12_lod.py.
- Tiếng mèo gầm 1881/1882: Joseph SARDIN, BigSoundBank, CC0 1.0. https://bigsoundbank.com/cat-roar-1-s1881.html và https://bigsoundbank.com/cat-roar-2-s1882.html. Đây là bản ghi thú thật, không mô tả là sư tử/hổ.
- Monster Sound Effects Pack: Ogrebane, OpenGameArt, CC0 1.0. https://opengameart.org/content/monster-sound-effects-pack ; https://opengameart.org/sites/default/files/monster_sfx_pack.zip.
- Final Stand: Centurion_of_war, CC0 1.0. https://opengameart.org/content/final-stand-0 ; phase 1.3 / phase 2.2 heavy drums làm nhạc boss.
- Bốn roar mỗi rồng: Tools/p12_audio_design.py xử lý/layer bản ghi trên bằng resampling pitch, EQ, saturation, delay/reverb, fade; peak 0,84. SHA256 nguồn ở Audio/Source/sources.json; đầu ra Audio/Designed/design.json.
- Rumble/wind tự tạo bằng noise lọc và sóng 48 Hz, không gọi là bản ghi động vật.

P14: socket Mouth/Rider/TailTip, Roared/BreathRequested, RequestDive đã có; hiện chỉ presence, không sát thương hay thanh HP cự thú.

# P13 — Thiên Hỏa và Dư Hỏa

- Còi `Resources/P13/alarm.ogg`: **EZduzziteh**, [Alarm](https://opengameart.org/content/alarm-1), **CC0 1.0**. Tệp nguồn alarm_2.ogg được giữ nguyên âm thanh, Unity nén Vorbis mono.
- Lửa `Resources/P13/fire.wav`: **PagDev**, [Fireplace Sound loop](https://opengameart.org/content/fireplace-sound-loop), **CC0 1.0**. Unity import mono/Vorbis; volume và low-pass xử lý runtime.
- `flame.png`, `smoke.png`, `spark.png`: **Kenney**, [Particle Pack](https://kenney.nl/assets/particle-pack), **CC0 1.0**. Trích từ fire_01 / smoke_01 / spark_01 trong PNG Transparent của gói gốc.
- Sprite indoor/partial/outdoor/arrow và scorch: hình polygon/raster tự viết cho project bằng Tools/p13_assets.py; không chứa emoji hay asset thương mại. Viền mực/màu giấy dùng ngôn ngữ ComicTheme hiện có. Shader FireSprite và vật liệu được viết riêng.
- Gầm xa dùng lại roar P12 được thiết kế từ nguồn CC0 liệt kê ở trên; không đổi giấy phép model rồng người dùng.
- URL tải, tên thành viên archive và SHA256: `task/p13/source/manifest.json`. Trang nguồn và zip lưu trong cùng thư mục; đã kiểm tra giấy phép trực tiếp 2026-10-03. Một trang tag tải nhầm nằm ở rejected-download.html, không import vào Assets.

P13 fix1: RollingFire và MouthFlameCone là shader riêng của project, phát triển theo hot-core/dark-rim và lửa cuộn P10FireBillow. Dùng lại smoke CC0 Kenney; không sửa shader/tài nguyên P10. Cone mesh, vignette gradient và marker cửa được tạo bằng code; không thêm asset tải ngoài hay giấy phép mới.

PERF-FIRE: MeteorBatch shader và FireMeteorBatch mesh/billboard được viết riêng cho project, giữ lõi HDR/đuôi cam/viền tối và đầu tròn của P13. Không thêm texture, model hay âm thanh tải ngoài; nguồn và giấy phép P12/P13 giữ nguyên.

# P14 — Cự thú chiến đấu

- Giữ model/rig/LOD 023 Azure Serpent = Xích Hỏa Giao, 026 Lava Wing = Tà Hóa Chu Tước, 020 Silver Cloud = Cửu U Hỏa Long Vương, do người dùng cung cấp. Không gắn nhãn CC0 cho model; không thay mesh nguồn.
- `Resources/P14/wing-flap.ogg`: **AntumDeluge**, [Large Wings Flap](https://opengameart.org/content/large-wings-flap), dựa trên âm chop của dave.des; **CC0 1.0**, nguyên file OGG từ wings_flap_large.zip.
- `Resources/P14/meteor-impact.ogg` và `feather-fall.ogg`: **Kenney**, [Impact Sounds](https://kenney.nl/assets/impact-sounds), **CC0 1.0**; chọn impactMining_000 và impactSoft_medium_000. Âm gầm/lửa vẫn dùng nguồn P12/P13 ở trên.
- Trang nguồn, archive gốc, thành viên và SHA256 đầu ra: `task/p14/source/manifest.json`. Đã kiểm trang nguồn 03/10/2026. Không có nguồn CC-BY mới.
- Feather/rock mesh, shader SkyStrike, telegraph hai lớp, HUD và combat code được viết riêng cho project. Smoke/sparks/flame/scorch dùng tài nguyên CC0 Kenney hoặc tài nguyên tự viết P13. Không có asset hình ảnh bên ngoài mới.


# P15 — Thiên Kiếm / Vạn Kiếm Quy Tông

- Phi kiếm dùng mesh gốc P03 `FlyingSword.BladeMesh`; lưỡi150m, batch2400/1600/960 kiếm, vòng trận/phù văn/shockwave, shader HeavenGold và UI được viết riêng cho Campus Rift. Không thêm model bên thứ ba.
- Chuỗi7âm cast/charge/rift/descent/impact/aftershock/dissipate tái dùng `GiantHandConfig`, nguồn **Kenney**, [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) và [Impact Sounds](https://kenney.nl/assets/impact-sounds), **CC0 1.0**. License gốc trong `Assets/Skills/GiantHandSeal/Audio/`. Kiếm ngân dùng `impactBell_heavy_000.ogg`, cũng Kenney/CC0.
- `Resources/P15/victory.wav`: **celestialghost8**, [Victory](https://opengameart.org/content/victory), **CC0 1.0**. Fanfare nguyên bản 2A03, tải [Victory.wav](https://opengameart.org/sites/default/files/Victory.wav) ngày03/10/2026; SHA256 trong `task/p15/source/manifest.json`. Không gắn licenseCC0 cho model rồng người dùng.
- Các trang nguồn đã được kiểm giấy phép trực tiếp03/10/2026. Model/texture/rig/LOD và mọi giấy phép P12–P14 được giữ nguyên.

# P21 — Identity / cinematic / ending

- Model020/023/026, rig/animation/LOD giữ tài nguyên do người dùng cung cấp, không gắn nhãnCC0. Chín sừng/bờm, phiến cánh/đuôi và vảy: mesh trọng số cứng tạo bằng code SkyBeastIdentity, shader IdentityGlow viết cho project; 1 mesh/renderer mỗi con, dùng chung các LOD.
- Rift ring, Timeline shot clips, camera/subtitle/credits UI viết cho project. Không có model/texture thương mại mới.
- Reveal dùng lại tiếng gầm P12, rumble/wind P12; dawn fanfare dùng Emma_MA/CC0 ở Audio/Resources/P21/Music/LICENSES.md. Bình minh thêm Morning Birds in a Quiet Urban Garden của WhisperingEarth, Freesound/CC0; dùng public HQ MP3 preview, qua mixer SFX. License tại Audio/Resources/P21/LICENSES.md.
- Nhạc riêng và manifest nguồn/SHA256: Audio/Resources/P21/Music/LICENSES.md; task/p21/source/manifest.json.
