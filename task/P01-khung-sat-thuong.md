# P01 — Khung sát thương, nguyên tố, trạng thái

> **Mục tiêu:** một đường sát thương chung cho người chơi, quái và môi trường, có hệ ngũ hành, hiệu ứng trạng thái, số sát thương và thanh máu quái.
>
> **Phạm vi:** MVP · **Ước lượng:** 2,5 ngày công · **Phụ thuộc:** P00 · **Tham chiếu:** §4.3, §7.2, §14.5
>
> **Kết quả:** mọi đòn (đánh thường, kỹ năng, quái, Thiên Hỏa) đi qua `DamageInfo`; miễn thương tính đúng cho từng loại nguồn.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P01-T01 | `Element` và bảng khắc chế ngũ hành | Code | 0,25 | P00 | ✅ |
| P01-T02 | `DamageInfo`, `IDamageable`, `DamageCalculator` | Code | 0,25 | T01 | ✅ |
| P01-T03 | Sửa `PlayerMonsterHealth` theo `DamageInfo` | Code | 0,4 | T02 | ✅ |
| P01-T04 | Sửa `MonsterVitality` dùng chung cho mọi quái | Code | 0,4 | T02 | ✅ |
| P01-T05 | `StatusEffectHost`: hiệu ứng trạng thái | Code | 0,5 | T04 | ✅ |
| P01-T06 | Số sát thương bay lên và thanh máu quái | UI | 0,4 | T04 | ✅ |
| P01-T07 | Harness `DamagePipelinePlayTest` | Test | 0,3 | T03–T06 | ✅ |

---

## P01-T01 — `Element` và bảng khắc chế ngũ hành

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,25 ngày · **Phụ thuộc:** P00
- **Mục tiêu:** một nguồn duy nhất cho hệ số khắc chế (§7.2).
- **File:** (mới) `Assets/Combat/Runtime/Element.cs`, `Assets/Combat/Editor/ElementChartValidation.cs`
- **Các bước:**
  1. Tạo `enum Element { None, Kim, Moc, Thuy, Hoa, Tho, Loi, Am, KhongGian }`.
  2. Tạo `static class ElementChart`:
     - `float Multiplier(Element attack, Element target)`:
       - khắc: 1,5 (Kim ▶ Mộc ▶ Thổ ▶ Thủy ▶ Hỏa ▶ Kim);
       - bị khắc: 0,75;
       - Lôi đánh Âm: 1,5; Kim đánh Âm: 0,8;
       - còn lại (gồm None, Không Gian): 1.
     - `bool Generates(Element from, Element to)` theo vòng tương sinh Mộc → Hỏa → Thổ → Kim → Thủy → Mộc.
     - `Color ColorOf(Element)` theo bảng màu §7.2.
  3. Menu `Campus Rift/V2/Validate Element Chart`: kiểm tra đủ ma trận 9×9 so với bảng kỳ vọng.
- **Hoàn thành khi:**
  - [x] Validation log `ELEMENT QA PASS` cho cả 81 cặp.
  - [x] Có đủ 5 cặp tương sinh.
- **Kiểm thử:** chạy menu validation.

## P01-T02 — `DamageInfo`, `IDamageable`, `DamageCalculator`

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,25 ngày · **Phụ thuộc:** P01-T01
- **File:** (mới) `Assets/Combat/Runtime/DamageInfo.cs`, `Assets/Combat/Runtime/DamageCalculator.cs`
- **Các bước:**
  1. `enum DamageSource { Melee, Projectile, Skill, Environment, Reaction }`.
  2. `struct DamageInfo`, gồm các trường:
     - `amount`, `element`, `source`
     - `point`, `direction`
     - `ignoreInvulnerability`, `critical`
     - `attacker` (GameObject), `skillId` (string, có thể rỗng)
  3. `interface IDamageable`, gồm:
     - `bool ApplyDamage(in DamageInfo info)`
     - `Element Element { get; }`
     - `bool IsDead { get; }`
     - `Transform Anchor { get; }` (điểm hiện số sát thương)
  4. `DamageCalculator.Compute(attackPower, percent, element, target, critChance, critDamage, rng)`:
     - công thức: `sát thương = Công × % × ElementChart × (chí mạng ? critDamage : 1)`;
     - trả về `DamageInfo`.

     Phòng thủ **không** trừ ở đây mà trừ ở phía bị đánh (T03, T04).
- **Hoàn thành khi:**
  - [x] Validation mở rộng từ T01 kiểm tra 5 trường hợp công thức, dùng rng cố định.

## P01-T03 — Sửa `PlayerMonsterHealth` theo `DamageInfo`

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P01-T02
- **File:** (sửa) `Assets/MonsterShaban/Scripts/PlayerMonsterHealth.cs`
- **Hiện trạng:** mỗi đòn trúng đặt `protectedUntil = Time.time + 0.5f`, nên các nhịp Thiên Hỏa và đòn của bầy quái bị bỏ qua (§14.5).
- **Các bước:**
  1. Cài `IDamageable`, với `Element = None`.
  2. `ApplyDamage(DamageInfo)`:
     - Melee và Projectile: miễn thương sau đòn **0,25 giây**.
     - Environment và Reaction: bỏ qua miễn thương và không tạo miễn thương mới.
     - Trừ phòng thủ qua property `DamageReduction` (0–0,8; P03-T01 sẽ cấp giá trị).
  3. Giữ `TakeDamage(float)` và `TryTakeDamage(amount, point, direction)` để tương thích ngược: gói thành `DamageInfo` với nguồn Melee.
  4. Thêm `GrantInvulnerability(float seconds)`, dùng cho né và kỹ năng.
  5. Thêm event `BeforeDefeat` (handler trả `true` là hủy cái chết), dùng cho Hộ Mệnh Phù ở P08. Giữ logic `respawnOnDefeat`.
  6. Chạy `ShabanCombatPlayTest` và `ShabanPressurePlayTest`. Nếu harness đang giả định miễn thương 0,5 giây, cập nhật assertion theo luật mới và ghi rõ lý do.
- **Hoàn thành khi:**
  - [x] Các nhịp sát thương môi trường cách nhau 0,5 giây đều trừ máu.
  - [x] Hai đòn cận chiến cách nhau 0,2 giây thì chỉ đòn đầu trúng.
  - [x] Harness Shaban PASS.

## P01-T04 — Sửa `MonsterVitality` dùng chung cho mọi quái

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P01-T02
- **File:** (sửa) `Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs`, `Assets/MonsterShaban/Scripts/MonsterNavigation.cs` · (mới) `Assets/Combat/Runtime/IMotionHold.cs`
- **Hiện trạng:** class có `[RequireComponent(typeof(MonsterBrain))]`, máu cố định 500, chỉ nhận đòn qua `ReceiveSeal`, và gọi thẳng `navigation.HoldForSeal()`.
- **Các bước:**
  1. Bỏ `RequireComponent(MonsterBrain)`; null-check `navigation`, `combat`, `animator`.
  2. Tạo `interface IMotionHold { void HoldForSeal(); }`. `MonsterNavigation` cài interface này. `MinionMotor` (P04) cũng sẽ cài.
  3. Cài `IDamageable`:
     - thêm `[SerializeField] Element element` và `[SerializeField] float defense` (% giảm);
     - `ApplyDamage` trừ máu, nháy sáng, gọi event mới `Damaged(DamageInfo)`, giữ nguyên `DefeatedOnce`.
  4. Thêm `SetMaxHealth(float value, bool refill)` để nhân máu theo màn.
  5. `ReceiveSeal(damage, stagger)` chuyển thành `ApplyDamage` (nguồn Skill, hệ Thổ) cộng `Suppress(stagger)`, để Giant Hand vẫn chạy.
- **Hoàn thành khi:**
  - [x] `GiantHandPlayTest` và `ShabanCombatPlayTest` PASS.
  - [x] Một GameObject chỉ có `MonsterVitality` (không có Brain) vẫn nhận sát thương và chết đúng.

## P01-T05 — `StatusEffectHost`: hiệu ứng trạng thái

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P01-T04
- **File:** (mới) `Assets/Combat/Runtime/StatusEffectHost.cs`, `Assets/Combat/Runtime/StatusType.cs`
- **Các bước:**
  1. `enum StatusType { Burn, Freeze, Chill, Shock, Stun, ArmorBreak, Wet, Pulled }`.
  2. API:
     - `Apply(type, duration, magnitude, source)`: cùng loại thì làm mới thời gian, lấy magnitude lớn hơn.
     - `Has(type)`, `Consume(type)`, `Remaining(type)`.
     - event `Changed`.
  3. Tác động của từng trạng thái:
     - **Burn:** tick mỗi 0,5 giây (Reaction, bỏ qua miễn thương).
     - **Freeze:** giữ tại chỗ qua `IMotionHold`, ngắt đòn đang vung.
     - **Chill:** `SpeedMultiplier` 0,6.
     - **Shock:** ngắt đòn đang vung, 0,5 giây.
     - **Stun:** gọi `MonsterVitality.Suppress`.
     - **ArmorBreak:** −30% phòng thủ.
     - **Wet:** chỉ là cờ, dùng cho phản ứng.
  4. `float SpeedMultiplier`: `MonsterNavigation.ScaleSpeed` và `MinionMotor` nhân thêm hệ số này.
  5. Hiệu ứng nhìn: tint màu bằng `MaterialPropertyBlock` (giống cách nháy sáng trong `MonsterVitality`). Freeze có lớp băng mờ.
  6. Boss được đánh dấu `resistHardControl`: Freeze đổi thành Chill 50%, Stun tối đa 1 giây.
- **Hoàn thành khi:**
  - [x] Mỗi trạng thái áp đúng, hết hạn đúng, không rò trạng thái khi quái được lấy lại từ pool.

## P01-T06 — Số sát thương bay lên và thanh máu quái

- **Loại:** UI · **Phạm vi:** MVP · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P01-T04
- **File:** (mới) `Assets/Combat/Runtime/DamageNumberPool.cs`, `Assets/Combat/Runtime/EnemyHealthBar.cs`
- **Các bước:**
  1. `DamageNumberPool`:
     - TMP world-space, pool khoảng 64 phần tử;
     - màu theo hệ (`ElementChart.ColorOf`); chí mạng to hơn 1,4 lần;
     - hiện được cả nhãn phản ứng (chữ, dùng ở P11);
     - bay lên rồi mờ dần trong 0,8 giây;
     - font Be Vietnam Pro.
  2. `EnemyHealthBar`:
     - billboard trên đầu quái;
     - chỉ hiện khi quái vừa trúng đòn (4 giây) hoặc đang bị khóa mục tiêu;
     - tinh anh hiện thêm tên;
     - dùng pool.
  3. Gắn vào event `MonsterVitality.Damaged`.
  4. Mobile: cỡ chữ tối thiểu tương đương 28 px ở 1080p.
- **Hoàn thành khi:**
  - [x] 200 lần trúng đòn liên tục không tăng số object (pool hoạt động).
  - [x] Số hiện đúng màu theo hệ.

## P01-T07 — Harness `DamagePipelinePlayTest`

- **Loại:** Test · **Phạm vi:** MVP · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P01-T03 đến T06
- **File:** (mới) `Assets/Combat/Validation/DamagePipelinePlayTest.cs` → `Artifacts/Combat/Validation.json`
- **Kiểm tra:**
  - Hệ số hệ trên một mục tiêu giả gồm `MonsterVitality` và `StatusEffectHost`.
  - Luật miễn thương của người chơi: cận chiến 0,25 giây; môi trường bỏ qua.
  - Thời lượng của từng trạng thái.
  - Pool số sát thương được tái sử dụng.
  - Giant Hand vẫn gây sát thương cho Shaban qua đường `ReceiveSeal`.
- **Hoàn thành khi:**
  - [x] Mọi assertion PASS.
  - [x] Nhóm hồi quy Quái và Kỹ năng không có FAIL mới.

---

## Kiểm chứng cuối phase

- [x] `DamagePipelinePlayTest` PASS.
- [x] Nhóm hồi quy Quái và Kỹ năng không có FAIL mới so với baseline.
- [x] Cập nhật trạng thái P01 trong `task/README.md`.
