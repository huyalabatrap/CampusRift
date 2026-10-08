# P13 — Trong nhà/ngoài trời và Thiên Hỏa

> **03/10/2026: ✅ hoàn tất theo smoke.** FireBreath 42/42; layer 11/11; ShelterAudit 2086/2126 = 98,1185%, 40 lệch có lý do. [Báo cáo](p13/REPORT-P13.md). Không benchmark/full regression/Android thực theo TEST-POLICY.

> **Mục tiêu:** game biết chính xác người chơi đang ở trong nhà, bán che hay ngoài trời; có chu kỳ Thiên Hỏa với 3 mức sát thương, Dư Hỏa, HUD cảnh báo, và ảnh hưởng của lửa lên quái.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P05 · **Tham chiếu:** §4.2–§4.5, §14.4
>
> **Kết quả:** Thiên Hỏa chạy được độc lập (chưa cần cự thú); trong nhà 12%, bán che 45%, ngoài trời 100%.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P13-T01 | Gán layer `Environment` cho công trình campus | Công cụ | 0,3 | P00-T04 | ✅ smoke |
| P13-T02 | `ShelterDetector` và `IndoorVolume` | Code | 0,4 | T01 | ✅ smoke |
| P13-T03 | Kiểm toán phân loại trên toàn campus | Test | 0,4 | T02 | ✅ smoke |
| P13-T04 | `FireBreathCycle`: chu kỳ và sát thương 3 mức | Code | 0,5 | T02 | ✅ smoke |
| P13-T05 | Hình ảnh và âm thanh của Thiên Hỏa | Asset | 0,5 | T04 | ✅ smoke |
| P13-T06 | Dư Hỏa: vệt lửa trên sân | Code | 0,3 | T04 | ✅ smoke |
| P13-T07 | HUD: đếm ngược, biểu tượng trú ẩn, mũi tên vào nhà | UI | 0,4 | T02, T04 | ✅ smoke |
| P13-T08 | Thiên Hỏa tác động lên quái | Code | 0,2 | T04 | ✅ smoke |
| P13-T09 | Luật Huyết Nguyệt ngoài trời (màn 6) | Code | 0,1 | T02, P12-T08 | ✅ smoke |
| P13-T10 | Harness `FireBreathPlayTest` | Test | 0,4 | T04–T08 | ✅ smoke |

---

## P13-T01 — Gán layer `Environment` cho công trình campus

- **Loại:** Công cụ · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P00-T04
- **File:** (mới) `Assets/SkyBeast/Editor/EnvironmentLayerTool.cs` · (sửa) `SampleScene.unity` (sao lưu trước)
- **Các bước:**
  1. Menu `Campus Rift/V2/Assign Environment Layer`: tìm mọi collider công trình dưới model campus `Comic_Vibrant_Elevator_System_T77` (collider do `CampusCollisionSetup` tạo ra), rồi gán layer `Environment`.
  2. Rà soát lại các mask sau khi đổi layer: camera né vật cản (`cameraObstacles`), tầm nhìn Shaban (`ClearLine`), vị trí đặt Void Wall, mục tiêu Giant Hand.
  3. Kiểm tra camera: `far clip plane` hiện là 350 m; cự thú ở P14 phải nằm trong tầm này.
- **Hoàn thành khi:**
  - [x] Smoke layer/camera/traversal: 11 PASS, 0 FAIL, 0 exception. Full regression Quái/Kỹ năng/CampusTraversalValidation không chạy theo TEST-POLICY.

## P13-T02 — `ShelterDetector` và `IndoorVolume`

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P13-T01
- **File:** (mới) `Assets/SkyBeast/Runtime/ShelterDetector.cs`, `IndoorVolume.cs`, `Shelter.cs`
- **Các bước (§14.4):**
  1. `enum Shelter { Outdoor, Partial, Indoor }`.
  2. Bắn 3 tia thẳng lên từ đầu nhân vật (tâm và 2 điểm lệch ±0,6 m), dài 60 m, chỉ trúng layer `Environment`:
     - trúng cả 3 → `Indoor`;
     - trúng 1–2 → `Partial`;
     - không trúng → `Outdoor`.
  3. `IndoorVolume` (BoxCollider trigger) cho phép designer đặt tay để ghi đè ở giếng trời, mái kính.
  4. Tần suất: người chơi 5 lần/giây; quái 2 lần/giây, lệch nhịp nhau. Có property `Current` và event `Changed`.
  5. Hàm static `Evaluate(Vector3 point)` cho các hệ khác gọi (quái, vị trí Dư Hỏa, điều kiện Thiên Kiếm).
- **Hoàn thành khi:**
  - [x] Đứng giữa sân → Outdoor; dưới hiên → Partial; trong phòng → Indoor.

## P13-T03 — Kiểm toán phân loại trên toàn campus

- **Loại:** Test · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P13-T02
- **File:** (mới) `Assets/SkyBeast/Validation/ShelterAuditPlayTest.cs` → `Artifacts/SkyBeast/ShelterAudit.json`
- **Các bước:**
  1. Lấy mẫu mọi nút trong `CampusRoomGraph` (2.126 nút):
     - nút `Outdoor` phải ra Outdoor hoặc Partial;
     - nút `StairLanding` và `Observation` ở tầng ≥ 1 phải ra Indoor;
     - nút `Door` ở tầng trệt được phép ra Partial.
  2. Xuất danh sách nút sai kèm vị trí, BuildingID, FloorID.
  3. Sửa bằng `IndoorVolume` hoặc chỉnh collider, rồi chạy lại.
- **Hoàn thành khi:**
  - [x] Đúng ≥ 98% số nút.
  - [x] Các nút còn lệch được ghi lý do chấp nhận.

## P13-T04 — `FireBreathCycle`: chu kỳ và sát thương 3 mức

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P13-T02
- **File:** (mới) `Assets/SkyBeast/Runtime/FireBreathCycle.cs`, `FireBreathProfile.cs` (ScriptableObject)
- **Các bước:**
  1. Các trạng thái:
     - `Warning`: 6 giây (màn 10 pha 3: 4 giây);
     - `Breath`: 4 giây, 8 nhịp, mỗi 0,5 giây;
     - `Afterfire`: 10 giây;
     - `Rest`: phần còn lại của chu kỳ.
  2. `FireBreathProfile` cho từng màn/pha: chu kỳ (45 / 40→32 / 20→28→25 giây) và tổng sát thương mỗi lần phun theo §4.3:

| Màn | Ngoài trời | Bán che | Trong nhà |
|:-:|:-:|:-:|:-:|
| 8 | 280 | 126 | 34 |
| 9 | 416 | 187 | 50 |
| 10 | 600 | 270 | 72 |

  3. Mỗi nhịp gây tổng ÷ 8, dùng `DamageInfo` nguồn Environment, `ignoreInvulnerability = true`.
  4. Giảm sát thương: `BuffSystem.fireResistance`, Kim Chung Tráo, vùng sau Hư Không Kết Giới tính là Partial. Tổng giảm tối đa 80%.
  5. **Long Nộ** (màn 10): 12 giây liên tục. Mỗi giây mất (tính theo máu đề nghị): ngoài trời 10%, bán che 4%, trong nhà 1%. Có API riêng để P14 gọi.
  6. Event cho hệ khác: `WarningStarted`, `BreathStarted`, `BreathTick`, `BreathEnded`. Property `IsBreathing` và `IsFury` (đang Long Nộ) dùng cho luật Thiên Kiếm ở P15: chỉ chặn khi Long Nộ; lúc đang phun vẫn niệm được nhưng sẽ bị ngắt nếu không có hộ thể.
  7. Có thể chạy độc lập bằng lệnh DEV, không cần cự thú.
- **Hoàn thành khi:**
  - [x] Sát thương đúng bảng (sai số ±1).
  - [x] Bỏ qua miễn thương.
  - [x] Trần 80% hoạt động.

## P13-T05 — Hình ảnh và âm thanh của Thiên Hỏa

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh còi báo, tiếng gầm xa, tiếng lửa; texture/hạt mưa lửa) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P13-T04
- **Các bước:**
  1. Trong lúc Warning: bầu trời đỏ dần (`SkyLightingController`, preset `Inferno`/`RedEclipse`), còi báo, tiếng gầm xa.
  2. Trong lúc Breath:
     - mưa hạt lửa **chỉ sinh trong bán kính khoảng 60 m quanh máy quay** (mẹo hiệu năng, §4.2);
     - lớp phủ màn hình màu cam; rung máy quay nhẹ;
     - khi đang Indoor: âm thanh bị nghẹt (lọc tần số thấp), lửa chỉ thấy qua cửa sổ.
  3. Ngân sách hạt trên mobile giảm 50%.
- **Hoàn thành khi:**
  - [x] Nhìn như cháy cả campus.
  - [ ] FPS giảm không quá 10%: chưa đo; bỏ benchmark theo `task/TEST-POLICY.md`. Không dùng tiêu chí này để kết luận smoke.

## P13-T06 — Dư Hỏa: vệt lửa trên sân

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (texture/hạt vệt lửa cháy trên mặt đất, âm thanh lửa cháy) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P13-T04
- **File:** (mới) `Assets/SkyBeast/Runtime/BurningGround.cs`
- **Các bước:**
  1. Sau mỗi lần phun: tạo 8–12 vệt lửa ngoài trời, gồm vài vệt gần người chơi và vài vệt ngẫu nhiên. Vị trí phải nằm trên NavMesh và được `ShelterDetector.Evaluate` xác nhận là Outdoor.
  2. Mỗi vệt tồn tại 10 giây, bán kính 2,5 m. Đứng trong vệt mất 4%/giây máu đề nghị.
  3. Băng Tâm Phù miễn nhiễm Dư Hỏa.
- **Hoàn thành khi:**
  - [x] Không có vệt lửa nào xuất hiện trong nhà.
  - [x] Sát thương đúng; dùng pool.

## P13-T07 — HUD: đếm ngược, biểu tượng trú ẩn, mũi tên vào nhà

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P13-T02, P13-T04
- **File:** (mới) `Assets/CampusRiftUI/Runtime/FireWarningHUD.cs`
- **Các bước:**
  1. Đếm ngược "THIÊN HỎA 6…5…", dùng lại `MonsterWarningUI` (nhấp nháy).
  2. Biểu tượng trú ẩn luôn hiện ở màn 8–10: 🏠 an toàn / ⚠ bán che / 🔥 ngoài trời, bằng sprite, không dùng emoji.
  3. Mũi tên chỉ tới lối vào nhà gần nhất, chỉ hiện trong lúc Warning. Lấy từ nút `Door` ở tầng trệt trong `RoomGraph`, khoảng cách ước theo đường đi.
- **Hoàn thành khi:**
  - [x] Mũi tên chỉ đúng hướng.
  - [x] Biểu tượng đổi ngay khi bước qua ngưỡng cửa.

## P13-T08 — Thiên Hỏa tác động lên quái

- **Loại:** Code · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P13-T04
- **Các bước (§4.4):**
  1. Quái không phải hệ Hỏa, đang ở ngoài trời: nhận 50% mức sát thương ngoài trời.
  2. Quái hệ Hỏa (Bạo Thi; Hỏa Linh ở P19) và tinh anh có phụ tố Hỏa Tâm: miễn nhiễm, được +20% tốc độ trong 10 giây sau mỗi lần phun.
  3. Quái chết vì lửa **vẫn tính** Kiếm Ý.
- **Hoàn thành khi:**
  - [x] Dụ được quái ra sân để lửa đốt.
  - [x] Quái hệ Hỏa không mất máu.

## P13-T09 — Luật Huyết Nguyệt ngoài trời (màn 6)

- **Loại:** Code · **Ước lượng:** 0,1 ngày · **Phụ thuộc:** P13-T02, P12-T08
- **Các bước:** thay bản tạm "+10% mọi quái" ở P12-T08 bằng luật đúng: quái đang ở ngoài trời nhanh hơn 15%.
- **Hoàn thành khi:**
  - [x] Quái trong nhà không được tăng tốc.

## P13-T10 — Harness `FireBreathPlayTest`

- **Loại:** Test · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P13-T04 đến T08
- **File:** (mới) `Assets/SkyBeast/Validation/FireBreathPlayTest.cs` → `Artifacts/SkyBeast/FireBreath.json`
- **Kiểm tra:**
  - Đưa người chơi tới các điểm đã biết là ngoài trời, bán che, trong nhà; chạy một chu kỳ; so tổng sát thương với bảng.
  - Bỏ qua miễn thương.
  - Tị Hỏa Châu, Kim Chung Tráo, trần 80%.
  - Dư Hỏa, Băng Tâm Phù.
  - Lửa tác động lên quái.
  - Long Nộ.
- **Hoàn thành khi:**
  - [x] PASS.

---

## Kiểm chứng cuối phase

- [x] Lệnh DEV bật Thiên Hỏa trong màn 1: chạy vào nhà thì sống, đứng ngoài thì mất khoảng 56% máu (theo mức màn 8).
- [x] Cập nhật trạng thái P13 trong `task/README.md`.
