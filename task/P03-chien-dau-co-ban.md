# P03 — Chiến đấu cơ bản của người chơi

> **Mục tiêu:** người chơi có chỉ số, Linh Lực, đánh thường bằng phi kiếm, né, khóa mục tiêu và cảm giác đánh tốt.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P02 · **Tham chiếu:** §6, §8.4, §16
>
> **Kết quả:** hạ được quái bằng đánh thường và kỹ năng. Không cần animation mới cho nhân vật (phi kiếm tự bay; né bằng lướt kèm bóng ảo).

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P03-T01 | `PlayerStats`: chỉ số và modifier | Code | 0,4 | P02 | ✅ |
| P03-T02 | `SpiritPower` (Linh Lực) và thanh HUD | Code | 0,3 | T01 | ✅ |
| P03-T03 | Đánh thường Ngự Kiếm Thuật | Code | 0,9 | T01 | ✅ |
| P03-T04 | Né (Dash) có miễn thương | Code | 0,5 | T01 | ✅ |
| P03-T05 | Khóa mục tiêu | Code | 0,4 | T03 | ✅ |
| P03-T06 | Phản hồi đòn đánh | Code | 0,3 | T03 | ✅ |
| P03-T07 | Đổi sát thương kỹ năng cũ sang % Công | Code | 0,3 | T01 | ✅ |
| P03-T08 | Harness `PlayerCombatPlayTest` | Test | 0,4 | T02–T07 | ✅ |

---

## P03-T01 — `PlayerStats`: chỉ số và modifier

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P02
- **File:** (mới) `Assets/Combat/Runtime/PlayerStats.cs`, `StatModifier.cs` · (sửa) `CampusExplorer.prefab`
- **Các bước:**
  1. Chỉ số gốc (Luyện Khí tầng 1, §8.4): Máu 100, Công 20, Linh Lực 100, Phòng thủ 0%, Chí mạng 5%, Sát thương chí mạng 150%, Tốc chạy +0%.
  2. Danh sách modifier có nguồn (`Cultivation`, `Artifact`, `Buff`) gồm phần cộng phẳng và phần %. Có hàm `Recalculate()` và event `Changed`.
  3. Áp chỉ số ra ngoài:
     - Máu tối đa → `PlayerMonsterHealth.SetProgressionMaxHealth` (giữ tỷ lệ máu hiện tại).
     - Tốc chạy → `CampusExplorer.walkSpeed` và `runSpeed`, tính từ giá trị gốc (cách làm như `LearningPlayerBridge`).
     - Phòng thủ → `PlayerMonsterHealth.DamageReduction`.
  4. Trần: phòng thủ ≤ 80%, tốc chạy ≤ +10%.
- **Hoàn thành khi:**
  - [x] Thêm hoặc bỏ modifier thì chỉ số tính lại đúng.
  - [x] Nạp lại scene không cộng dồn modifier hai lần.

## P03-T02 — `SpiritPower` (Linh Lực) và thanh HUD

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P03-T01
- **File:** (mới) `Assets/Combat/Runtime/SpiritPower.cs`, `Assets/CampusRiftUI/Runtime/PlayerSpiritUI.cs`
- **Các bước:**
  1. Linh Lực tối đa lấy từ `PlayerStats`. Hồi 4/giây; mỗi đòn đánh thường trúng +5.
  2. API: `TrySpend(cost)`, `Restore(amount)`, event `Changed`.
  3. `SkillRuntime.IsReady` kiểm tra Linh Lực; khi thiếu, ô kỹ năng hiện trạng thái `NoSpirit`.
  4. Thanh HUD nằm dưới thanh máu, làm theo kiểu `PlayerEnergyUI` (nhóm "Vitals"), màu xanh ngọc.
- **Hoàn thành khi:**
  - [x] Dùng kỹ năng thì trừ Linh Lực đúng.
  - [x] Thiếu Linh Lực thì không dùng được và HUD báo rõ.

## P03-T03 — Đánh thường Ngự Kiếm Thuật

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (mesh phi kiếm, texture vệt sáng, âm thanh vút và trúng đòn) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,9 ngày · **Phụ thuộc:** P03-T01
- **File:** (mới) `Assets/Combat/Runtime/PlayerCombat.cs`, `FlyingSword.cs`, `FlyingSwordPool.cs`; `Assets/Combat/Data/NguKiemConfig.asset` (ScriptableObject)
- **Thiết kế (§6):**
  - Phi kiếm hệ **Mộc**, cảm hứng Thanh Trúc Phong Vân Kiếm.
  - 3 thanh kiếm lơ lửng sau vai người chơi.
  - Tự tìm mục tiêu trong nón 60°, tầm 12 m; ưu tiên mục tiêu đang khóa.
- **Các bước:**
  1. Chuỗi 3 nhát: 100% / 100% / 160% Công. Nhấn tiếp trong 0,9 giây thì sang nhát kế, quá thì quay về nhát đầu.
  2. Giữ nút 0,8 giây ra **Kiếm Xuyên**: 250% Công, xuyên thẳng 15 m, trúng nhiều quái.
  3. Phi kiếm là projectile có pool, bay 30 m/s, đuôi sáng màu xanh ngọc. Kiểm tra trúng bằng `SphereCast` (layer `PlayerAttack`). Khi trúng tạo `DamageInfo` qua `DamageCalculator` rồi cộng Linh Lực (T02).
  4. Mesh kiếm: sinh bằng code (theo cách `GiantHandVisual` sinh mesh) hoặc dùng một mesh đơn giản.
  5. Âm thanh: tiếng vút và tiếng trúng (tạm dùng clip có sẵn nếu chưa có clip mới).
  6. Input: `CampusAction.Attack`. Không bắn khi đang ngắm kỹ năng, đang đi thang máy, hoặc UI không ở trạng thái Gameplay.
- **Hoàn thành khi:**
  - [x] Hạ được mục tiêu giả bằng chuỗi 3 nhát.
  - [x] Kiếm Xuyên trúng nhiều mục tiêu trên một đường.
  - [x] Sát thương đúng công thức, có tính hệ.

## P03-T04 — Né (Dash) có miễn thương

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh lướt/né) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P03-T01
- **File:** (mới) `Assets/Combat/Runtime/DodgeAbility.cs` · (sửa) `Assets/Scripts/CampusExplorer.cs`
- **Các bước:**
  1. `CampusExplorer`:
     - thêm `TrySpendEnergy(float)`, vì setter của `Energy` đang private;
     - thêm `Dash(Vector3 direction, float distance, float duration)` di chuyển qua `CharacterController` và dừng khi gặp tường, để không giành quyền điều khiển với controller.
  2. `DodgeAbility`:
     - lướt 5 m theo hướng đang đi (không đi thì lướt lùi);
     - miễn thương 0,3 giây qua `PlayerMonsterHealth.GrantInvulnerability`;
     - tốn 25 Thể Lực, hồi chiêu 0,6 giây.
  3. Bóng ảo khi lướt: dùng `CharacterAfterimageTrail` có sẵn.
  4. Khóa né khi: đang đi thang máy (`RidingElevator`), UI không ở Gameplay, hoặc thiếu Thể Lực.
  5. Mobile: thêm `TouchRole.Dash`, nút né đặt cạnh nút đánh.
- **Hoàn thành khi:**
  - [x] Né đúng khi Shaban đang vung đòn thì không mất máu.
  - [x] Không xuyên tường.
  - [x] Trừ Thể Lực đúng.

## P03-T05 — Khóa mục tiêu

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P03-T03
- **File:** (mới) `Assets/Combat/Runtime/TargetLock.cs`, `Assets/CampusRiftUI/Runtime/TargetReticleUI.cs`
- **Các bước:**
  1. Phím Tab hoặc nút mobile: chọn quái gần tâm màn hình nhất, trong 25 m và nhìn thấy được. Nhấn lại để đổi sang mục tiêu kế; giữ để bỏ khóa.
  2. Mục tiêu chết thì tự chuyển sang mục tiêu gần nhất.
  3. Dấu ngắm là UI trên màn hình, màu theo hệ của quái.
  4. Nối vào `PlayerCombat` và các kỹ năng dạng `Aimed`. `GiantHandTargeting` đã có `LockedMonster`, ưu tiên dùng mục tiêu đang khóa.
- **Hoàn thành khi:**
  - [x] Khóa, đổi mục tiêu, tự chuyển khi mục tiêu chết đều hoạt động trên PC và mobile.

## P03-T06 — Phản hồi đòn đánh

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P03-T03
- **File:** (mới) `Assets/Combat/Runtime/HitFeedback.cs`
- **Các bước:**
  1. Khựng hình: chỉ dừng animator của quái bị trúng trong 40 ms. **Không** chạm vào `Time.timeScale` (xem README, mục 7).
  2. Rung máy quay nhẹ khi trúng đòn mạnh hoặc chí mạng, dùng lại `GiantHandCameraImpulse`.
  3. Viền đỏ trên quái khi nó chuẩn bị ra đòn: API `TelegraphOutline.Show(duration)`, dùng từ P04.
- **Hoàn thành khi:**
  - [x] Đánh có cảm giác "chạm".
  - [x] Pause game giữa lúc khựng hình không làm hỏng timeScale.

## P03-T07 — Đổi sát thương kỹ năng cũ sang % Công

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P03-T01
- **File:** (sửa) `GiantHandSkill.cs`, `GiantHandConfig.cs` + asset, `VoidWallSkill.cs` / `VoidWall.cs`, `PhantomDecoySkill.cs`
- **Các bước:**
  1. Đại Thủ Ấn:
     - bỏ `damage = 60` cố định, thay bằng 300% Công, hệ Thổ;
     - choáng 3 giây, boss tối đa 1 giây;
     - hồi chiêu 24 xuống 18 giây.
  2. Hư Không Kết Giới: máu tường = 40% máu tối đa người chơi (đang cố định 75).
  3. Ảnh Phân Thân: giữ nguyên hành vi. Hiệu ứng nổ khi tan để dành cho tầng 3 (P10-T08).
- **Hoàn thành khi:**
  - [x] Số liệu khớp §7.3.
  - [x] `GiantHandPlayTest` được cập nhật và PASS.

## P03-T08 — Harness `PlayerCombatPlayTest`

- **Loại:** Test · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P03-T02 đến T07
- **File:** (mới) `Assets/Combat/Validation/PlayerCombatPlayTest.cs` → `Artifacts/Combat/PlayerCombat.json`
- **Kiểm tra:**
  - Nhịp chuỗi 3 nhát và cửa sổ 0,9 giây.
  - Giữ nút ra Kiếm Xuyên.
  - Sát thương = Công × % × hệ số hệ.
  - Né có miễn thương và không xuyên tường.
  - Linh Lực tự hồi, trừ khi dùng kỹ năng, cộng khi đánh trúng.
  - Khóa mục tiêu.
  - Nút mobile: đánh, né, khóa.
- **Hoàn thành khi:**
  - [x] PASS toàn bộ.
  - [x] Nhóm hồi quy Điều khiển và Kỹ năng không có FAIL mới.

---

## Kiểm chứng cuối phase

- [x] Trên SampleScene: hạ Shaban bằng đánh thường kết hợp kỹ năng, trên cả PC và mobile.
- [x] Cập nhật trạng thái P03 trong `task/README.md`.

---

## Ghi chú triển khai (2026-09-30)

Khác với mô tả ban đầu, và lý do:

1. **`PlayerStats` là nguồn duy nhất của máu tối đa, tốc chạy, năng lượng và phòng thủ.** `LearningPlayerBridge` (hệ thưởng cũ) nay đẩy modifier vào `PlayerStats` thay vì tự sửa người chơi; P06 sẽ thay hẳn bằng cầu nối cảnh giới. Trần tốc độ: nguồn vĩnh viễn (cảnh giới, pháp bảo) tối đa +10%, buff tạm được cộng thêm, tổng ≤ +60% (kế hoạch chỉ ghi +10%, nhưng Thần Hành Phù +30% và Côn Bằng +40% cần chỗ cộng). Có cờ `suppressCrit` cho harness.
2. **Chi phí Linh Lực đã thu thật cho 3 kỹ năng cũ** (Đại Thủ Ấn 30, Hư Không Kết Giới 15, Ảnh Phân Thân 20) qua `SkillSpirit`. Thiếu Linh Lực thì hủy ngắm và báo "KHÔNG ĐỦ LINH LỰC"; nút mobile hiện trạng thái `NoSpirit`. Các harness cũ phải hồi đầy Linh Lực giữa các lần cast.
3. **Kiếm Xuyên bắn khi giữ đủ 0,8 giây sau một lần bấm** (nhát thường vẫn bắn ngay khi bấm, không phải chờ nhả nút). Nhờ vậy đòn đánh không bị trễ.
4. **Khựng hình 40 ms chỉ áp cho đòn đánh thường/Kiếm Xuyên**, không áp cho kỹ năng: Đại Thủ Ấn đã có animation phản ứng "Sealed" riêng, đóng băng thêm sẽ nuốt mất nó.
5. **Đại Thủ Ấn: 300% Công, hệ Thổ, choáng 3 giây, hồi 18 giây** (bỏ `damage/edgeDamage`, không còn giảm sát thương ở rìa). Hư Không Kết Giới: máu tường = 40% máu tối đa người chơi.
6. **Bố cục nút mobile được sắp lại** quanh nút Tấn Công (210 px): thêm Tấn Công, Né, Khóa; dịch Nhảy, 4 nút kỹ năng, Tương tác, Hủy. Nút là hình vuông nên khoảng cách tính theo hình vuông; harness kiểm tra không chồng nhau ở 1920×1080, 2340×1080, 1600×1200.
7. **Phím PC:** LMB đánh, Ctrl né, Tab khóa (giữ 0,5 giây để nhả), V và 1/2/3 đã nhận diện sẵn cho P08/P15. `MonsterVitality.Active` là danh sách quái đang hoạt động, dùng cho nhắm mục tiêu thay cho tìm khắp scene. `GiantHandTargeting` ưu tiên mục tiêu đang khóa và coi mọi `MonsterVitality` là "diễn viên" (không chặn tia).
8. **Không cần animation mới:** phi kiếm là mesh sinh bằng code (3 thanh, quỹ đạo sau vai), dùng shader `Campus Rift/Speed Force Additive` có sẵn; né dùng `CharacterAfterimageTrail.TriggerDash`.
9. File mới: `Assets/Combat/Runtime/` (`PlayerStats`, `SpiritPower`, `PlayerCombat`, `FlyingSword`, `NguKiemConfig`, `DodgeAbility`, `TargetLock`, `HitFeedback`, `TelegraphOutline`, `CombatLine`), `Assets/Combat/Editor/CombatSetup.cs` (menu `Campus Rift/V2/Setup Player Combat`), `Assets/Combat/Validation/PlayerCombatPlayTest.cs`, `Assets/CampusRiftUI/Runtime/PlayerSpiritUI.cs`, `Assets/Skills/Core/Runtime/SkillSpirit.cs`.

**Kết quả kiểm thử:** `PlayerCombatPlayTest` 63/0; hồi quy đầy đủ `Artifacts/V2/P03/` và `P03-fix/`: GiantHand 75/0, VoidWall 28/0, QuickCast 13/0, MobileControls 41/0, ShabanPressure 32/0, SkyVictory 49/0, DamagePipeline 23/0, SkillLoadout 20/0; Learning 41/0, Followup 9/0, Localization 27/0. Các FAIL còn lại là lỗi có sẵn trong `Artifacts/V2/Baseline.md`. Ảnh: `Artifacts/UI/P03-pc.png`, `Artifacts/UI/P03-mobile.png`.
