# Task — Campus Rift V2 "Học Để Thắng"

> Nguồn: [`../KE_HOACH_V2_HOC_DE_THANG.md`](../KE_HOACH_V2_HOC_DE_THANG.md), đã duyệt ngày 2026-09-28.
>
> Kế hoạch được chia thành **24 phase nhỏ**, mỗi phase là một file. Mỗi task đủ nhỏ (0,1–1,5 ngày công) để làm và kiểm chứng riêng.
>
> Ký hiệu "§" (ví dụ §7.3) là số mục trong file kế hoạch.

---

## 1. Cách dùng

1. Làm theo thứ tự ở [Bản đồ phase](#2-bản-đồ-phase). Nếu có 2 người, chia theo [hai luồng song song](#4-hai-luồng-song-song-2-người).
2. Trước khi bắt đầu một task, xem cột **Phụ thuộc**. Mọi task phụ thuộc phải ở trạng thái ✅.
3. Làm xong task:
   - tick các mục trong "Hoàn thành khi";
   - chạy phần "Kiểm thử" của task;
   - cập nhật cột Trạng thái trong file phase và trong bảng dưới đây.
4. Có việc mới phát sinh: thêm task theo [mẫu](#10-mẫu-task-mới), lấy ID kế tiếp trong phase đó.

**Trạng thái:** ⬜ Chưa làm · 🔄 Đang làm · ✅ Xong · ⛔ Bị chặn (ghi lý do) · ⏭ Bỏ qua (ghi lý do)

---

## 2. Bản đồ phase

### Phần A — Bản rút gọn (MVP): 10 màn chơi được từ đầu đến cuối

| Phase | Tên | Ngày công | Phụ thuộc | Mốc | Trạng thái |
|---|---|:-:|---|---|:-:|
| [P00](P00-chuan-bi.md) | Chuẩn bị dự án | 1 | — | | ✅ |
| [P01](P01-khung-sat-thuong.md) | Khung sát thương, nguyên tố, trạng thái | 2,5 | P00 | | ✅ |
| [P02](P02-khung-ky-nang-4-o.md) | Khung kỹ năng 4 ô và điều khiển | 3,5 | P01 | | ✅ |
| [P03](P03-chien-dau-co-ban.md) | Chiến đấu cơ bản của người chơi | 3,5 | P02 | | ✅ |
| [P04](P04-quai-thuong-ai-nhe.md) | Quái thường và AI nhẹ | 4 | P01, P03 | | ✅ |
| [AI-SQUAD / fix1](ai/PROMPT-AI-SQUAD-fix1.md) | AI quái phối hợp, vây, lùa, chặn đầu | — | P12, P16, P19 | [Báo cáo fix1](ai/REPORT-AI-SQUAD-fix1.md): squad33/0; phủ thật sân248°/Warning249°, chặn2/case, escape100%; Roster95/0, Levels87/0, Minion31/0, Shaban34/0; cân bằng chưa đo thực chiến | ✅ triển khai + smoke |
| [P05](P05-man-choi-dot-quai.md) | Màn chơi, đợt quái, khe nứt | 3,5 | P04 | **Mốc 1:** chơi được màn 1–2 | ✅ |
| [P06](P06-ho-so-tu-luyen.md) | Hồ sơ v2 và Tu luyện | 3 | P03-T01 | | ✅ |
| [P07](P07-thi-dot-pha-noi-dung.md) | Thi Đột Phá, dạng câu hỏi, nhập nội dung | 3,5 | P06 | | ✅ |
| [P08](P08-linh-thach-cua-hang.md) | Linh Thạch, cửa hàng, vật phẩm | 3 | P06 | | ✅ |
| [P09](P09-sanh-tu-luyen-ui.md) | Sảnh Tu Luyện, bản đồ màn, chuẩn bị, kết quả | 3,5 | P05, P07, P08 | **Mốc 2:** học → lên cấp → mua đồ → chọn 4 kỹ năng → vào màn | ✅ |
| [P10](P10-ky-nang-dot-1.md) | Kỹ năng đợt 1 (7 kỹ năng mới) | 5 | P03, P06 | | ✅ |
| [P11](P11-phan-ung-combo.md) | Phản ứng và combo | 1,5 | P10 | | ✅ |
| [P12](P12-quai-moi-shaban-man-3-7.md) | Quái mới, Shaban, màn 3–7 | 5 | P05, P11 | **Mốc 3:** DEV1→7 đạt; triển khai/smoke xong theo chính sách21:30; cân bằng/full regression chưa xác nhận. [Báo cáo](p12/REPORT-P12.md) | 🔄 |
| [P13](P13-trong-nha-ngoai-troi-thien-hoa.md) | Trong nhà/ngoài trời và Thiên Hỏa | 3,5 | P05 | [Báo cáo](p13/REPORT-P13.md) · [Fix1 hình ảnh](p13/REPORT-P13-fix1.md) · audit 98,12% | ✅ smoke |
| [P14](P14-cu-thu-bau-troi.md) | Cự thú bầu trời | 4 | P13 | [Báo cáo](p14/REPORT-P14.md) · SkyBeast58/58 · FireBreath42/42 · DEV8–10 | ✅ smoke |
| [P15](P15-kiem-y-thien-kiem.md) | Kiếm Ý và Thiên Kiếm | 3,5 | P14 | [Báo cáo](p15/REPORT-P15.md) · HeavenSword41/41 · SkyBeast58/58 · FireBreath42/42 · DEV8–9 | ✅ smoke |
| [P16](P16-man-8-10-ai-cao.md) | Màn 8–10 và AI T3–T4 | 3 | P12, P15 | **Mốc 4 ✅ smoke:** DEV1→10 không kẹt · Level8to10 87/0 · HeavenSword 41/0 · SkyBeast 58/0 · [Báo cáo](p16/REPORT-P16.md) | ✅ smoke |
| [P17](P17-hoan-thien-mvp.md) | Hoàn thiện và phát hành thử MVP | 5 | P00–P16 | **AI hoàn tất; Mốc 5 ⏳:** 4 build + Windows smoke; 17 FAIL lịch sử đã xử lý ở [STABILIZE](stabilize/REPORT-STABILIZE.md), giữ Shelter40điểm baselineP13; chờ 3–5 sinh viên chơi thử, Android thật và giảng viên duyệt. [Báo cáo](p17/REPORT-P17.md) · [Tiến độ](p17/PROGRESS.md) | ✅ AI / ⏳ người |

### Phần B — Nâng lên bản đầy đủ

| Phase | Tên | Ngày công | Phụ thuộc | Mốc | Trạng thái |
|---|---|:-:|---|---|:-:|
| [P18](P18-ky-nang-dot-2-3.md) | Kỹ năng đợt 2–3 và hiệu ứng Viên Mãn | 7,3 | P17 | [21 kỹ năng / 9 phản ứng](p18/REPORT-P18.md) | ✅ |
| [P19](P19-quai-mo-rong-tinh-anh.md) | Quái mở rộng và tinh anh | 6,7 | P17 | **✅ smoke:** 4quái mới · 8phụ tố · roster11L8–10 giữ7gốc · P19 88/0 · Roster95/0 · Heaven41/0 · Levels87/0 · [Báo cáo](p19/REPORT-P19.md) | ✅ smoke |
| [STABILIZE](stabilize/PROMPT-STABILIZE.md) | Ổn định hồi quy +8mục polish sau P19 | — | P16–P19, MODELS | 16suite PASS +Shelter baseline giữ40/0mới; Phantom PASS; Models30/0; polish13/0; Windows dev build0error. [Báo cáo](stabilize/REPORT-STABILIZE.md) · [Trước/sau](../Artifacts/V2/Regression-Stabilize.md) | ✅ smoke |
| [P20](P20-hoc-tap-mo-rong.md) | Học tập mở rộng | 4,5 | P17 | 6 dạng/449 câu ·93 thẻ ·sổ sai/ngày/bia10 màn ·16 vật phẩm/5 pháp bảo; smoke467/0, giữ legacyLearning19/2. [Báo cáo](p20/REPORT-P20.md) · [Tiến độ](p20/PROGRESS.md) | ✅ smoke / ⏳ giảng viên |
| [P21](P21-cu-thu-dien-anh.md) | Cự thú và điện ảnh đầy đủ | 4,5 | P17 | 020/023/026 accents · reveal7s ·3Timeline · dawn20s/credits13trang · nhạc riêng; Cinematic40/0, Heaven41/0, Sky58/0. [Báo cáo](p21/REPORT-P21.md) · [Tiến độ](p21/PROGRESS.md) | ✅ smoke |
| [P22](P22-hau-ket.md) | Hậu kết (Tháp Thí Luyện, Ác Mộng, thành tựu) | 4,5 | P18, P19 | Tháp/Ác Mộng · trần chung100/ngàyUTC ·20thành tựu ·kỷ lục local ·4biến thể trang phục; Endgame112/0, visual9/0,9ảnh audit0. [Báo cáo](p22/REPORT-P22.md) · [Tiến độ](p22/PROGRESS.md) | ✅ smoke |
| [P23](P23-phat-hanh-day-du.md) | Phát hành bản đầy đủ | 3,5 | P18–P22 | **Mốc 6:** bản đầy đủ | ⬜ |

### Tổng công sức

| | Số task | Ngày công | 1 người | 2 người (chia 2 luồng) |
|---|:-:|:-:|:-:|:-:|
| MVP (P00–P17) | 146 | ≈ 61,5 | ≈ 12 tuần | ≈ 6–7 tuần |
| Bản đầy đủ (P00–P23) | 195 | ≈ 92,5 | ≈ 19 tuần | ≈ 10 tuần |

Ước lượng ở đây chi tiết hơn và **thay cho** con số sơ bộ trong §15 của kế hoạch. So với kế hoạch: bản MVP chỉ về đích trong 6–7 tuần nếu có **2 người** làm song song.

**Hình ảnh cần bạn cung cấp:** xem [HINH-ANH-CAN-CUNG-CAP.md](HINH-ANH-CAN-CUNG-CAP.md) (thư mục nhận ảnh: `Content/Images/`).

**Task đang bị chặn:** không. Nội dung Tư tưởng Hồ Chí Minh đã nhập (P07-T10); còn chờ giảng viên duyệt (P17-T06).

---

## 3. Sơ đồ phụ thuộc

```
P00 ─► P01 ─► P02 ─► P03 ─┬─► P04 ─► P05 ─┬──────────────► P12 ─┐
                          │                │                      │
                          │                └─► P13 ─► P14 ─► P15 ─┴─► P16 ─► P17 ─► Phần B
                          │                                        ▲
                          ├─► P10 ─► P11 ─► (P12) ─────────────────┘
                          │
                          └─(P03-T01)─► P06 ─┬─► P07 ─┐
                                             └─► P08 ─┴─► P09 ─► (P17)
```

---

## 4. Hai luồng song song (2 người)

| Luồng | Phase | Ghi chú |
|---|---|---|
| **A — Gameplay** | P01 → P02 → P03 → P04 → P05 → P10 → P11 → P12 → P13 → P14 → P15 → P16 | Chiến đấu, quái, màn, cự thú |
| **B — Học tập và UI** | P06 → P07 → P08 → P09, sau đó hỗ trợ P17 (nội dung, UI) và P20 | Chỉ cần `PlayerStats` (P03-T01) từ luồng A |
| **Chung** | P00, P17, Phần B chia lại theo tình hình | |

---

## 5. Quyết định đã chốt (theo §19 kế hoạch)

| # | Quyết định | Chốt |
|:-:|---|---|
| 1 | Thiên Kiếm là nút riêng, không chiếm ô kỹ năng | **Có** |
| 2 | Phải đứng ngoài trời mới triệu hồi Thiên Kiếm | **Có** |
| 3 | Đổi kỹ năng giữa màn | Không; riêng màn 9–10 được đổi lúc nghỉ giữa đợt |
| 4 | Sát thương Thiên Hỏa | Ngoài trời 100% · bán che 45% · trong nhà 12% |
| 5 | Nội dung học | Khung 6 chương giáo trình 2021; **chờ nội dung thật** của bạn |
| 6 | Hạn hoàn thành | **Chưa chốt.** Làm MVP (Phần A) trước; Phần B tùy thời gian |
| 7 | Nền tảng | PC và Android |
| 8 | Không khí | Hành động tu tiên; màn 3–7 vẫn u tối |
| 9 | Hậu kết | ✅ smoke P22: Tháp, Ác Mộng, thành tựu/kỷ lục/trang phục; chưa đo cân bằng thực chiến |

---

## 6. Định nghĩa "Xong" (áp dụng cho mọi task)

- [ ] Console không có error/warning mới sau khi compile.
- [ ] Đạt mọi tiêu chí "Hoàn thành khi" của task.
- [ ] Harness hoặc validation liên quan PASS; report nằm trong `Artifacts/<Tính năng>/`.
- [ ] Bộ hồi quy liên quan ([mục 8](#8-kiểm-thử)) không có FAIL mới so với baseline (P00-T02).
- [ ] Chuỗi hiển thị mới có đủ EN và VN trong `Assets/Localization/Resources/LocalizationCatalog.asset`.
- [ ] Nếu task có input hoặc UI: chạy được cả PC và mobile (`ControlMode.Mobile`).
- [ ] Đã sao lưu scene/prefab vào `Backups/V2-<yyyyMMdd>/` trước khi sửa.
- [ ] Đã cập nhật trạng thái trong file phase và trong bảng ở mục 2.

---

## 7. Quy ước kỹ thuật

- Dự án **không dùng asmdef**. Code mới vẫn nằm trong Assembly-CSharp / Assembly-CSharp-Editor như hiện tại.
- Thư mục và namespace:

| Thư mục | Namespace | Nội dung |
|---|---|---|
| `Assets/Combat/` | `CampusRift.Combat` | Sát thương, nguyên tố, trạng thái, đánh thường, né |
| `Assets/Skills/Core/`, `Assets/Skills/<TênKỹNăng>/` | `CampusRift.Skills` | Khung kỹ năng và từng kỹ năng |
| `Assets/Enemies/` | `CampusRift.Enemies` | Quái, AI nhẹ, tinh anh |
| `Assets/Levels/` | `CampusRift.Levels` | 10 màn, đợt quái, sao |
| `Assets/SkyBeast/` | `CampusRift.SkyBeast` | Cự thú, Thiên Hỏa, Thiên Kiếm |
| `Assets/Progression/` | `CampusRift.Progression` | Hồ sơ, Tu Vi, Linh Thạch, vật phẩm |
| `Assets/CampusRiftUI/` | `CampusRift.UI` | Giao diện (đã có) |
| `Assets/Learning/` | `CampusRift.Learning` | Học tập (đã có) |

- Mỗi module có các thư mục con `Runtime/`, `Data/` (asset), `Editor/` (setup, validation, menu), `Validation/` (harness Play Mode).
- Số liệu cân bằng để trong **ScriptableObject** (menu `Create → Campus Rift → …`), không viết cứng trong code.
- Menu editor mới đặt dưới `Campus Rift/V2/…`. Lệnh DEV chỉ biên dịch trong Editor (`#if UNITY_EDITOR`).
- Giao diện dựng bằng builder editor theo mẫu `UIFoundationBuilder` và `CampusRiftUITheme`.
- **Không đổi `Time.timeScale`** ngoài `UIStateManager`, vì pause đang dùng timeScale. Hit-stop và slow-mo phải làm bằng tốc độ animator hoặc thời gian cục bộ.
- ID dữ liệu (kỹ năng, vật phẩm, màn, bài, câu hỏi) là chuỗi kebab-case cố định; **không đổi** sau khi phát hành.
- Nội dung môn học tuân thủ §11.5: tách lớp hư cấu, mỗi câu ghi trang nguồn, giảng viên duyệt.

---

## 8. Kiểm thử

- **Harness Play Mode:** MonoBehaviour tên `<TínhNăng>PlayTest`, bọc trong `#if UNITY_EDITOR`. Ghi kết quả vào `Artifacts/<TínhNăng>/Validation.json` (danh sách `passed[]` và `failed[]`) và log `"<TÊN> QA PASS/FAIL <mô tả>"`. Làm theo mẫu `Assets/Controls/Runtime/BoostEnergyPlayTest.cs`.
- **Logic thuần** (công thức, bảng số): static self-check trong Editor, theo mẫu `ShabanHunterValidation`, gọi từ menu `Campus Rift/V2/Validate …`.
- **Chạy qua Unity MCP:** script đặt trong `Tools/` theo mẫu có sẵn. Trước khi chạy, đặt `Application.runInBackground = true` và gọi `UIStateManager.Instance.EnterScene(true)`.
- **Bộ hồi quy hiện có.** Chạy lại nhóm tương ứng mỗi khi task chạm vào hệ thống đó:

| Nhóm | Harness |
|---|---|
| Điều khiển | `BoostEnergyPlayTest`, `MobileControlPlayTest`, `MobileChasePlayTest` |
| Kỹ năng | `GiantHandPlayTest`, `VoidWallPlayTest`, `VoidWallQuickCastPlayTest`, `PhantomDecoyWorldPlayTest` |
| Quái | `ShabanBehaviorPlayTest`, `ShabanCombatPlayTest`, `ShabanHuntScenarioTest`, `ShabanNavigationPlayTest`, `ShabanPressurePlayTest`, `ShabanTraversalPlayTest`, `ShabanHunterValidation` (Editor) |
| Học tập | `LearningPlayTest`, `LearningFollowupPlayTest`, `LearningRegressionRunner`, `LearningContentValidation` |
| Giao diện | `UIValidation`, `UIPlayValidation`, `SkyVictoryPlayTest`, `LocalizationPlayTest` |
| Map | `CampusTraversalValidation` |

- Harness mới của V2 được liệt kê trong từng phase.
- **Chạy hồi quy tự động:** `CampusRift.Levels.V2RegressionRunner.Begin("Artifacts/V2/<tên lượt>/", "<danh sách suite, để trống = tất cả>")` khi đang Play từ MainMenu. Mỗi harness chạy trong màn chơi mới nạp; bộ chạy khôi phục settings và bật lại thiết bị input trước mỗi harness. Kết quả ghi vào `Summary.json`, xong khi có `DONE.txt`. Các nhóm chạy riêng (Học tập, UI, ShabanHunterValidation, CampusTraversalValidation) và các lỗi có sẵn: xem `Artifacts/V2/Baseline.md`.
- Sau khi chạy test Học tập và UI: khôi phục save và settings từ `Backups/V2-20260928/` (hướng dẫn trong README của thư mục đó).

---

## 9. Tài nguyên bên ngoài (model, âm thanh)

- 🔎 Task có dòng **"Tự tìm tài nguyên"**: AI làm task đó tự tìm, tải và gắn asset miễn phí phù hợp (model, animation, âm thanh, nhạc, texture/VFX), không cần chờ bạn cung cấp. Riêng ảnh 2D (icon, chân dung, hình nền) thì theo [HINH-ANH-CAN-CUNG-CAP.md](HINH-ANH-CAN-CUNG-CAP.md).
- Ưu tiên nguồn CC0 (Quaternius, Kenney, Poly Pizza bản CC0). Nguồn CC-BY thì phải ghi công.
- Mỗi thư mục model có file `LICENSES.md` ghi: tên, tác giả, link, giấy phép.
- File GLB chuyển sang FBX bằng Blender chạy headless (máy đã cài sẵn).
- Nhân vật hiện chỉ có animation Idle và chạy. MVP chọn thiết kế **không cần animation mới cho người chơi**: phi kiếm tự bay, né bằng cách lướt kèm bóng ảo.

**Trước khi bắt đầu làm task:** mở Unity Editor để Unity MCP kết nối được. Lần kiểm tra gần nhất, MCP đang mất kết nối.

---

## 10. Mẫu task mới

```markdown
## Pxx-Tyy — Tên task
- **Loại:** Code / Dữ liệu / Asset / UI / Test / Nội dung / Công cụ · **Phạm vi:** MVP / Đầy đủ · **Ước lượng:** x ngày · **Phụ thuộc:** …
- **Mục tiêu:** …
- **File:** (mới) … · (sửa) …
- **Các bước:**
  1. …
- **Hoàn thành khi:**
  - [ ] …
- **Kiểm thử:** …
```

P10: vòng sửa VFX fix2 đã hoàn tất02/10/2026; [báo cáo và bằng chứng](p10/REPORT-P10-fix2.md). Trạng thái P10✅ giữ nguyên sau Main/Pool/Edge/UI, HubFlow/HubLayout và benchmark PC mới.

P11: hoàn tất 02/10/2026; [báo cáo và bằng chứng](p11/REPORT-P11.md). ReactionPlayTest 108/0, ComicTextAudit 73/0, không FAIL hồi quy mới; native PC giảm 4,85%, pool ổn định.

P12: đã chốt công việc theo [TEST-POLICY](TEST-POLICY.md) ngày02/10/2026;
7model/3rồng, elite/boss/sao đã triển khai, smoke27/0 và nút pause3/3.
Unity Android/Edit Mode/0Consoleerror. Giữ 🔄 cho xác nhận toàn bộ tiêu chí
cũ: thời lượng chưa đo thực chiến, Edges có3FAIL lịch sử, full Quái/HubLayout
chưa chạy đủ. Không còn queue bot/benchmark; chi tiết trong báo cáo P12.
Fix1 đã chốt năm mục review hình ảnh, smoke21/0, giữ giới hạn kiểm thử:
[REPORT-P12-fix1](p12/REPORT-P12-fix1.md), [ảnh mới](p12/screens/fix1/).

P16: hoàn tất triển khai và smoke ngày03/10/2026 theo [TEST-POLICY](TEST-POLICY.md).
AI T3/T4, panel nghỉ PC/mobile, sao8–10 và DEV1→10 đạt; polish12/0, TextAudit ảnh0.
**Mốc4 ✅ smoke**; cân bằng12/15/18 phút chưa đo thực chiến, không chạy full regression hoặc native Android.
