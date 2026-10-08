# P02 — Khung kỹ năng 4 ô và điều khiển

> **Mục tiêu:** kỹ năng được mô tả bằng dữ liệu; người chơi mang đúng 4 kỹ năng; input PC và mobile đi qua 4 ô chung thay vì gắn cứng từng kỹ năng.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P01 · **Tham chiếu:** §6, §7.1, §14.3, §14.5
>
> **Kết quả:** 3 kỹ năng cũ (Đại Thủ Ấn, Hư Không Kết Giới, Ảnh Phân Thân) chạy qua khung mới trên cả PC và mobile.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P02-T01 | `SkillDefinition` và dữ liệu 3 kỹ năng khởi đầu | Dữ liệu | 0,3 | P01 | ✅ |
| P02-T02 | Lớp cha `SkillRuntime` và `SkillState` | Code | 0,4 | T01 | ✅ |
| P02-T03 | `SkillLoadout`: 4 ô trên người chơi | Code | 0,3 | T02 | ✅ |
| P02-T04 | Adapter cho 3 kỹ năng cũ; Void Wall 3 lượt nạp | Code | 0,6 | T03 | ✅ |
| P02-T05 | Viết lại `CampusInput` cho action chung | Code | 0,6 | T03 | ✅ |
| P02-T06 | HUD 4 ô kỹ năng (PC) | UI | 0,4 | T04, T05 | ✅ |
| P02-T07 | HUD mobile sinh nút từ bộ kỹ năng | UI | 0,5 | T04, T05 | ✅ |
| P02-T08 | `SkillUnlockService` thay `LearningSkillGate` | Code | 0,2 | T02 | ✅ |
| P02-T09 | Cập nhật các harness kỹ năng và điều khiển | Test | 0,2 | T04–T07 | ✅ |

---

## P02-T01 — `SkillDefinition` và dữ liệu 3 kỹ năng khởi đầu

- **Loại:** Dữ liệu · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P01
- **File:** (mới) `Assets/Skills/Core/Runtime/SkillDefinition.cs`; `Assets/Skills/Core/Data/dai-thu-an.asset`, `hu-khong-ket-gioi.asset`, `anh-phan-than.asset`
- **Các bước:**
  1. Tạo `SkillDefinition` theo §14.3:
     - `id`, `displayName`, `displayNameVN`, `description`, `descriptionVN`
     - `element`, `role`, `icon`
     - `unlockRealm`, `unlockTier`
     - `cooldown`, `spiritCost`, `castType`
     - `SkillRank[5] ranks`
     - `runtimePrefab`
  2. `enum CastType { Instant, Aimed, Channel, Toggle }` và `enum SkillRole { Burst, Control, Defense, Mobility, Summon, Support, Utility }`.
  3. `SkillRank`: `effectMultiplier`, `cooldownMultiplier`, `upgradeCost`, `requiredRealm`.
  4. Tạo asset cho 3 kỹ năng theo số liệu §7.3:
     - Đại Thủ Ấn: Thổ, 18 giây, 30 Linh Lực.
     - Hư Không Kết Giới: Không Gian, 15 Linh Lực, 3 lượt nạp.
     - Ảnh Phân Thân: Âm, 18 giây, 20 Linh Lực.

     Icon dùng tạm glyph có sẵn (`GiantHandGlyph`, `VoidWallGlyph`, `PhantomGlyph`).
- **Hoàn thành khi:**
  - [x] Có 3 asset với ID kebab-case.
  - [x] Validation báo lỗi nếu có ID trùng hoặc thiếu tên VN.

## P02-T02 — Lớp cha `SkillRuntime` và `SkillState`

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P02-T01
- **File:** (mới) `Assets/Skills/Core/Runtime/SkillRuntime.cs`, `SkillState.cs`
- **Các bước:**
  1. `enum SkillState { Locked, Ready, Cooldown, Aiming, Casting, NoCharge, NoSpirit, Unavailable }`. Enum này thay `MobileSkillState` đang nằm trong `MobileControlsHUD`.
  2. `abstract class SkillRuntime : MonoBehaviour`:
     - dữ liệu: `Definition`, `Rank`;
     - hồi chiêu: `CooldownRemaining`, `CooldownDuration`;
     - điều kiện: `SpiritCost`, `IsUnlocked`, `IsReady`;
     - thao tác: `BeginAim()`, `Confirm()`, `Cancel()`, `QuickCast()`;
     - `GetState()`, event `Changed`.
  3. `IsReady` kiểm tra đủ Linh Lực. Nếu chưa có `SpiritPower` (P03-T02) thì coi như luôn đủ.
- **Hoàn thành khi:**
  - [x] Compile sạch.
  - [x] Có một lớp con giả dùng cho test.

## P02-T03 — `SkillLoadout`: 4 ô trên người chơi

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P02-T02
- **File:** (mới) `Assets/Skills/Core/Runtime/SkillLoadout.cs` · (sửa) `CampusExplorer.prefab`
- **Các bước:**
  1. `SkillLoadout` giữ 4 ô, mỗi ô là một `SkillRuntime` hoặc rỗng.
  2. API: `Equip(slot, skillId)`, `Get(slot)`, `IndexOf(skillId)`, event `LoadoutChanged`.
  3. Kỹ năng **không** nằm trong ô thì không nhận input.
  4. Bộ mặc định cho tới khi có hồ sơ (P06): [Đại Thủ Ấn, Hư Không Kết Giới, Ảnh Phân Thân, rỗng].
- **Hoàn thành khi:**
  - [x] Đổi ô lúc runtime thì HUD và input cập nhật theo.

## P02-T04 — Adapter cho 3 kỹ năng cũ; Void Wall 3 lượt nạp

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P02-T03
- **File:** (mới) `Assets/Skills/Core/Runtime/Adapters/GiantHandRuntime.cs`, `VoidWallRuntime.cs`, `PhantomRuntime.cs` · (sửa) `GiantHandSkill.cs`, `VoidWallSkill.cs`, `PhantomDecoySkill.cs`, `VoidWallConfig.asset`
- **Hiện trạng:**
  - Mỗi kỹ năng tự đọc phím trong `Update()`. Ví dụ `GiantHandSkill` đọc `controls.Pressed(CampusAction.Hand)`.
  - Void Wall có 20 lượt dùng, không hồi.
- **Các bước:**
  1. Chuyển việc đọc phím ra khỏi 3 skill; chỉ `SkillLoadout` gọi các hàm public có sẵn (`BeginPreview`, `Confirm`, `CancelPreview`, `QuickCast`, `CastNearest`, `Cast`).
  2. Mỗi adapter cài `SkillRuntime`: đọc `CooldownRemaining` và trạng thái preview/cast từ skill gốc.
  3. Void Wall:
     - `charges` từ 20 xuống 3;
     - thêm hồi 1 lượt mỗi 12 giây trong `VoidWallSkill`;
     - HUD hiện số lượt còn lại.
  4. Đổi tên hiển thị (EN/VN) trong LocalizationCatalog: Đại Thủ Ấn, Hư Không Kết Giới, Ảnh Phân Thân.
- **Hoàn thành khi:**
  - [x] Cả 3 kỹ năng dùng được từ ô 1–3.
  - [x] Void Wall hồi đúng 12 giây mỗi lượt.
  - [x] Hành vi ngắm và bắn giữ nguyên như cũ.

## P02-T05 — Viết lại `CampusInput` cho action chung

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P02-T03
- **File:** (sửa) `Assets/Controls/Runtime/CampusInput.cs`, `ContextInteraction.cs`, `ControlHintText.cs`
- **Hiện trạng:**
  - `enum CampusAction { Jump, Interact, Wall, Hand, Confirm, Cancel, Respawn, Dash, Grapple, Attack, Phantom }`.
  - `queued = new bool[11]` viết cứng số 11.
  - Phím Q/F/G gắn cứng cho Wall/Hand/Phantom; R là Respawn; E là tương tác.
- **Các bước:**
  1. Thêm action `Skill1..Skill4`, `Ultimate`, `Item1..Item3`, `LockOn`. Giữ `Attack` và `Dash` (đã khai báo nhưng chưa dùng). Có thể bỏ `Wall/Hand/Phantom/Grapple/Respawn` sau khi đã cập nhật harness.
  2. `queued` lấy kích thước bằng `Enum.GetValues(typeof(CampusAction)).Length` để tránh tràn mảng.
  3. Bảng phím PC:

| Phím | Action |
|---|---|
| Q / E / R / F | Skill1–4 |
| Chuột trái | Attack; khi đang ngắm thì là Confirm |
| Chuột phải | Cancel khi đang ngắm |
| Ctrl | Dash |
| V | Ultimate |
| 1 / 2 / 3 | Item1–3 |
| Tab | LockOn |
| G | Interact (đổi từ E) |

  4. `Holding()`, `BeginAim()`, `EndAim()`, `CancelAim()` làm theo ô, qua `SkillLoadout`, không còn gắn tên kỹ năng.
  5. Bỏ phím R Respawn ở gameplay; nếu cần thì chỉ giữ trong Editor.
  6. Cập nhật `ControlHints` và nhãn phím trên HUD.
- **Hoàn thành khi:**
  - [x] Mọi phím trong bảng hoạt động đúng.
  - [x] Không còn chỗ nào dùng hằng số 11.
  - [x] Tương tác thang máy và cửa dùng G.

## P02-T06 — HUD 4 ô kỹ năng (PC)

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P02-T04, P02-T05
- **File:** (sửa) `Assets/CampusRiftUI/Editor/UIFoundationBuilder.Gameplay.cs`, `SkillBarUI.cs` · (mới) `Assets/CampusRiftUI/Runtime/SkillBarBinder.cs`
- **Hiện trạng:** builder tạo 3 ô (`new SkillSlotUI[3]`) và ô thứ 3 là "Ultimate Slot" màu vàng.
- **Các bước:**
  1. Builder tạo **4 ô thường** và **1 ô Thiên Kiếm riêng**. Ô Thiên Kiếm ẩn mặc định, dùng ở P15.
  2. `SkillBarBinder` đọc `SkillLoadout`: cấu hình icon, nhãn phím (lấy từ bảng phím), tooltip, tầng kỹ năng; cập nhật hồi chiêu mỗi khung hình; hiện lớp khóa.
  3. Chạy lại builder trên SampleScene (sao lưu trước).
- **Hoàn thành khi:**
  - [x] 4 ô hiển thị đúng bộ kỹ năng.
  - [x] Hồi chiêu chạy đúng.
  - [x] Ô rỗng hiện khung trống.

## P02-T07 — HUD mobile sinh nút từ bộ kỹ năng

- **Loại:** UI · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P02-T04, P02-T05
- **File:** (sửa) `Assets/Controls/Runtime/MobileControlsHUD.cs`, `MobileTouchZone.cs`
- **Hiện trạng:** `TouchRole { Move, Look, Jump, Sprint, Wall, Hand, Interact, Pause, Cancel, Phantom }`, và 3 nút kỹ năng dựng cứng.
- **Các bước:**
  1. `TouchRole` thêm `Skill1..Skill4`. `Attack`, `Dash`, `Ultimate`, `Item1..3`, `LockOn` sẽ thêm ở P03, P08, P15.
  2. Sinh 4 nút kỹ năng theo `SkillLoadout`, xếp vòng cung quanh vị trí nút đánh (nút đánh thêm ở P03). Icon lấy từ `SkillDefinition`.
  3. Trạng thái nút lấy từ `SkillRuntime.GetState()`; bỏ đoạn tính riêng cho Hand/Wall/Phantom.
  4. Giữ cơ chế giữ–kéo–thả để ngắm cho kỹ năng dạng `Aimed`.
- **Hoàn thành khi:**
  - [x] 4 nút đúng bộ kỹ năng.
  - [x] Ngắm, thả và hủy hoạt động.
  - [x] Bố cục không chồng lấn ở 16:9, 19,5:9 và 4:3.

## P02-T08 — `SkillUnlockService` thay `LearningSkillGate`

- **Loại:** Code · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P02-T02
- **File:** (mới) `Assets/Skills/Core/Runtime/SkillUnlockService.cs` · (sửa) `Assets/Learning/Runtime/LearningSkillGate.cs`
- **Các bước:**
  1. `SkillUnlockService.IsUnlocked(SkillDefinition)`: tạm thời mở 3 kỹ năng khởi đầu. P06 sẽ nối với cảnh giới.
  2. `LearningSkillGate.Allows()` chuyển thành lớp đệm gọi service mới, để code cũ không vỡ.
- **Hoàn thành khi:**
  - [x] Kỹ năng chưa mở không bấm được và hiện khóa trên cả PC và mobile.

## P02-T09 — Cập nhật các harness kỹ năng và điều khiển

- **Loại:** Test · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P02-T04 đến T07
- **File:** (sửa) `GiantHandPlayTest`, `VoidWallPlayTest`, `VoidWallQuickCastPlayTest`, `PhantomDecoyWorldPlayTest`, `MobileControlPlayTest`, `MobileChasePlayTest`
- **Các bước:**
  1. Đổi cách gọi kỹ năng từ phím cũ sang ô 1–3.
  2. Sửa assertion Void Wall theo 3 lượt nạp, hồi 12 giây.
  3. Sửa phím tương tác thành G.
- **Hoàn thành khi:**
  - [x] Toàn bộ harness trên PASS.

---

## Kiểm chứng cuối phase

- [x] Nhóm hồi quy Điều khiển và Kỹ năng PASS.
- [x] Trên PC và mobile: đổi bộ kỹ năng lúc runtime, HUD và input đổi theo.
- [x] Cập nhật trạng thái P02 trong `task/README.md`.

---

## Ghi chú triển khai (2026-09-29)

Những chỗ làm khác mô tả ban đầu, và lý do:

1. **Bộ mặc định là Q Hư Không Kết Giới · E Ảnh Phân Thân · R trống · F Đại Thủ Ấn** (kế hoạch ghi Đại Thủ Ấn, Kết Giới, Phân Thân, trống). Chọn vậy để giữ phím cũ của Void Wall (Q) và Giant Hand (F); chỉ Phân Thân đổi từ G sang E, còn G thành phím Tương tác. Người chơi sẽ tự đổi bộ ở màn Chuẩn Bị (P09).
2. **3 kỹ năng cũ giữ nguyên logic input bên trong** (tap/giữ/bấm dồn của Void Wall…). Chúng vẫn hỏi "action định danh" Wall/Hand/Phantom; `CampusInput.Resolve()` đổi định danh thành phím của ô đang chứa kỹ năng, và kỹ năng không trang bị thì không nhận input. Kỹ năng mới (P10) đọc thẳng ô `Skill1..4`.
3. **Không dựng lại HUD bằng builder** (builder từ chối khi HUD đã tồn tại). `SkillBarBinder` sắp xếp lúc runtime: ô của kỹ năng cũ (có glyph, tinh thể lượt dùng riêng) đi theo kỹ năng; kỹ năng mới và ô trống dùng prefab `SkillSlot` (`SkillBarUI.slotTemplate`). Ô Thiên Kiếm riêng sẽ thêm ở P15.
4. **`SkillUnlockService` vẫn tôn trọng khóa cũ của hệ học tập** (Giant Hand mở sau 5 lần đột phá) cho tới P06, để không phá `LearningPlayTest`. Khi có hệ cảnh giới, luật này thay bằng "kỹ năng khởi đầu mở sẵn".
5. **Giữ enum `MobileSkillState`** (test cũ dùng) cạnh `SkillState` mới; `MobileControlsHUD.ToMobile()` chuyển đổi. `HandState/WallState/PhantomState` vẫn còn, nay tính theo ô.
6. Thêm `VoidWallSkill.RefillCharges()` (cho harness, sau này dùng cho vật phẩm) và pool tường quay vòng (trước đây `pool[poolIndex++]` không quay vòng).
7. File mới: `Assets/Skills/Core/Runtime/` (`SkillDefinition`, `SkillRuntime`, `SkillLoadout`, `SkillUnlockService`, `GiantHandRuntime`, `VoidWallRuntime`, `PhantomRuntime`), `Assets/Skills/Core/Editor/SkillCoreSetup.cs` (menu `Campus Rift/V2/Setup Skill Loadout`, `Validate Skill Definitions`), `Assets/Skills/Core/Validation/SkillLoadoutPlayTest.cs`, `Assets/CampusRiftUI/Runtime/SkillBarBinder.cs`; dữ liệu `Assets/Skills/Core/Data/*.asset`.

**Kết quả kiểm thử:** `SkillLoadoutPlayTest` 20/0; hồi quy đầy đủ `Artifacts/V2/P02/` khớp baseline (GiantHand 75/0, VoidWall 28/0, QuickCast 13/0, MobileControls 41/0, DamagePipeline 23/0, SkyVictory 49/0); Learning 41/0, LearningFollowup 9/0, Localization 27/0. Các FAIL còn lại là lỗi có sẵn trong `Artifacts/V2/Baseline.md`. Ảnh: `Artifacts/UI/P02-skillbar-pc.png`, `Artifacts/UI/P02-mobile-skills.png`.
