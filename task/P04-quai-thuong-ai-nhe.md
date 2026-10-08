# P04 — Quái thường và AI nhẹ

> **Mục tiêu:** có quái dạng dữ liệu, AI nhẹ để chạy được nhiều con cùng lúc, bộ điều phối tấn công, quái đánh xa, pool.
>
> **Phạm vi:** MVP · **Ước lượng:** 4 ngày công · **Phụ thuộc:** P01, P03 · **Tham chiếu:** §3.2, §3.3, §3.6
>
> **Kết quả:** Tiểu Yêu và Độc Nhãn Xạ Thủ đánh người chơi đúng luật. Có 14 con hoạt động cùng lúc mà vẫn mượt.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P04-T01 | `EnemyArchetype` và dữ liệu 5 loại quái MVP | Dữ liệu | 0,4 | P01 | ✅ |
| P04-T02 | Model: Tiểu Yêu, Độc Nhãn (+ model tạm) | Asset | 0,6 | — | ✅ |
| P04-T03 | `MinionMotor` và `MinionBrain` | Code | 1 | T01 | ✅ |
| P04-T04 | `EnemyDirector`: vé tấn công, vị trí vây | Code | 0,5 | T03 | ✅ |
| P04-T05 | Quái đánh xa: đạn độc | Code | 0,5 | T03 | ✅ |
| P04-T06 | Chết, tan biến, pool, event `EnemyDied` | Code | 0,4 | T03 | ✅ |
| P04-T07 | Harness `MinionCombatPlayTest` | Test | 0,6 | T04–T06 | ✅ |

---

## P04-T01 — `EnemyArchetype` và dữ liệu 5 loại quái MVP

- **Loại:** Dữ liệu · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P01
- **File:** (mới) `Assets/Enemies/Runtime/EnemyArchetype.cs`, `EnemyAbility.cs`; `Assets/Enemies/Data/tieu-yeu.asset`, `doc-nhan.asset`, `thiet-giap-nguu.asset`, `bao-thi.asset`, `shaban.asset`
- **Các bước:**
  1. `EnemyArchetype` gồm:
     - nhận diện: `id`, `displayName`, `displayNameVN`, `element`, `prefab`;
     - chỉ số gốc: `baseHealth`, `baseDamage`, `baseSpeed`;
     - tấn công: `attackRange`, `windup`, `attackCooldown`;
     - Kiếm Ý: `swordIntentWeight` (1 / 2 / 4);
     - cờ: `isFlying`, `isBoss`, `resistHardControl`;
     - kỹ năng mở theo màn: `List<AbilityUnlock { int minLevel; EnemyAbility ability; }>`.
  2. `EnemyAbility` là ScriptableObject lớp cha (ví dụ vồ, húc, bắn quạt). Lớp con viết ở P12.
  3. Điền số liệu theo bảng §3.2 cho 5 loại MVP. Ảnh Yêu, Triệu Hồn Sư, Dực Yêu, Hỏa Linh để P19.
- **Hoàn thành khi:**
  - [ ] Có 5 asset.
  - [ ] Validation `Campus Rift/V2/Validate Enemies` kiểm tra ID duy nhất, số liệu > 0, prefab không null.

## P04-T02 — Model: Tiểu Yêu, Độc Nhãn (+ model tạm)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model quái có animation idle/run/attack/hit/death) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** —
- **File:** (mới) `Assets/Enemies/Models/<tên>/…`, `Assets/Enemies/Models/LICENSES.md`, prefab `Assets/Enemies/Prefabs/TieuYeu.prefab`, `DocNhan.prefab`
- **Các bước:**
  1. Tìm model quái **có animation** (idle, run, attack, hit, death), giấy phép CC0 hoặc CC-BY. Nguồn ưu tiên: bộ quái của Quaternius (thường CC0 — kiểm tra lại giấy phép lúc tải), Poly Pizza.
  2. GLB → Blender headless → FBX; chuẩn hóa tỉ lệ (Tiểu Yêu cao khoảng 1,2 m) và gốc tọa độ. Dựng lại material URP (chuyển màu glTF từ linear sang gamma).
  3. Animator controller chung cho quái: tham số `MoveSpeed`; trigger `Attack`, `Hit`, `Die`.
  4. **Model tạm dự phòng** để không chặn code: capsule, sừng và mắt phát sáng màu theo hệ.
  5. Ghi `LICENSES.md`: tên, tác giả, link, giấy phép.
- **Hoàn thành khi:**
  - [ ] 2 prefab dùng được, gồm Animator, `NavMeshAgent`, collider trên layer `Enemy`.
  - [ ] Có LICENSES đầy đủ.

## P04-T03 — `MinionMotor` và `MinionBrain`

- **Loại:** Code · **Ước lượng:** 1 ngày · **Phụ thuộc:** P04-T01
- **File:** (mới) `Assets/Enemies/Runtime/MinionMotor.cs`, `MinionBrain.cs`, `EnemyInstance.cs`
- **Các bước:**
  1. `EnemyInstance`: gắn archetype, hệ số của màn (máu, sát thương, tốc độ), cờ `countsForSwordIntent`, cấp AI.
  2. `MinionMotor`:
     - bọc `NavMeshAgent` trên `CampusNavMesh` hiện có, cùng agent type với Shaban;
     - tốc độ = gốc × hệ số màn × `StatusEffectHost.SpeedMultiplier`;
     - cài `IMotionHold` (P01-T04);
     - tự đi qua NavMeshLink (cầu thang); **không** đi thang máy.
  3. `MinionBrain`, máy trạng thái:

     `Spawn` (trồi lên từ khe nứt, 1 giây) → `Chase` → `Windup` (viền đỏ, `TelegraphOutline`) → `Strike` → `Recover` → `Stagger`/`Frozen`/`Stunned` → `Dead`

  4. Khi vung đòn, kiểm tra lại tầm và tầm nhìn đúng lúc chạm, giống `MonsterCombat`. Sát thương đi qua `DamageInfo` (Melee).
  5. Thời gian báo trước theo cấp AI: T0 = 0,8 giây, T1 = 0,6 giây, T2 trở lên = 0,5 giây.
  6. Giảm tần suất tính toán:
     - quái gần (< 40 m) hoặc đang hiện trên màn hình: quyết định 5 lần/giây;
     - quái xa: 2 lần/giây.
- **Hoàn thành khi:**
  - [ ] Tiểu Yêu tìm tới người chơi qua cầu thang, báo trước rồi mới đánh, chết khi hết máu.
  - [ ] Bị đóng băng thì đứng yên.

## P04-T04 — `EnemyDirector`: vé tấn công, vị trí vây

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P04-T03
- **File:** (mới) `Assets/Enemies/Runtime/EnemyDirector.cs`, `AITierProfile.cs` (ScriptableObject)
- **Các bước:**
  1. Danh sách quái đang hoạt động; event `EnemySpawned` và `EnemyDied`.
  2. **Vé tấn công:** mỗi lúc chỉ tối đa N quái cận chiến được vung đòn cùng lúc (T0 = 2, T1 = 3). Quái chưa có vé thì đứng vòng ngoài.
  3. **Vị trí vây** (từ T1): 6 điểm quanh người chơi, bán kính theo vai trò. Quái đánh xa giữ khoảng cách 8–12 m.
  4. `AITierProfile` (T0–T4) chứa: thời gian báo trước, số vé, tỉ lệ né (P12), có vây hay không, có phục kích hay không. Tạo 5 asset.
- **Hoàn thành khi:**
  - [ ] Với 10 Tiểu Yêu, số con vung đòn cùng lúc không vượt quá số vé.
  - [ ] Từ T1, quái tản thành vòng vây.

## P04-T05 — Quái đánh xa: đạn độc

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng đạn độc) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P04-T03
- **File:** (mới) `Assets/Enemies/Runtime/EnemyProjectile.cs`, `EnemyProjectilePool.cs`, `RangedBehaviour.cs`
- **Các bước:**
  1. Đạn độc hệ Mộc, bay 14 m/s, người chơi né được, layer `EnemyAttack`. Trúng thì gây `DamageInfo` (Projectile).
  2. Bị Hư Không Kết Giới chặn: va chạm với collider của `VoidWall`, tường mất máu.
  3. Độc Nhãn giữ khoảng cách 8–12 m, ngắm và bắn khi nhìn thấy người chơi. Bắn 3 tia hình quạt khi màn ≥ 5 (ability, gắn ở P12).
- **Hoàn thành khi:**
  - [ ] Né được đạn.
  - [ ] Kết Giới chặn được đạn.
  - [ ] Đạn được lấy lại từ pool.

## P04-T06 — Chết, tan biến, pool, event `EnemyDied`

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (hiệu ứng tan biến, âm thanh quái chết) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P04-T03
- **File:** (mới) `Assets/Enemies/Runtime/EnemyPool.cs`, `DissolveEffect.cs`
- **Các bước:**
  1. Mỗi archetype có một pool. Khi lấy lại quái: xóa trạng thái, hồi máu, trả `NavMeshAgent` về đúng vị trí.
  2. Khi chết: animation `Die` (nếu có), sau đó tan biến 0,8 giây (thu nhỏ kèm hạt màu theo hệ), rồi trả về pool.
  3. `EnemyDied(EnemyInstance)` phát cho `EnemyDirector`, `LevelDirector` và `SwordIntent`, kèm trọng số và cờ `countsForSwordIntent`.
- **Hoàn thành khi:**
  - [ ] 100 lần sinh ra và chết không tăng số object.
  - [ ] Không có trạng thái cũ (bỏng, băng) sót lại khi quái được dùng lại.

## P04-T07 — Harness `MinionCombatPlayTest`

- **Loại:** Test · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P04-T04 đến T06
- **File:** (mới) `Assets/Enemies/Validation/MinionCombatPlayTest.cs` → `Artifacts/Enemies/MinionCombat.json`
- **Kiểm tra:**
  - Sinh 10 Tiểu Yêu và 3 Độc Nhãn quanh người chơi; tất cả tới được chỗ người chơi.
  - Số con vung đòn không vượt số vé.
  - Có báo trước rồi mới gây sát thương.
  - Chết khi máu về 0; pool được dùng lại.
  - Đạn bị chặn bởi Kết Giới.
  - Hiệu năng: ghi thời gian AI mỗi khung với 14 quái (mục tiêu < 2 ms trên PC dev).
- **Hoàn thành khi:**
  - [ ] PASS.
  - [ ] Có số liệu hiệu năng trong report.

---

## Kiểm chứng cuối phase

- [ ] Có 14 quái hoạt động cùng lúc trong SampleScene, FPS ổn định, không lỗi NavMesh.
- [ ] Cập nhật trạng thái P04 trong `task/README.md`.

---

## Ghi chú triển khai (hoàn tất)

- **Model:** Tiểu Yêu = Goblin, Độc Nhãn = Green Spiky Blob (Quaternius, CC0, Poly Pizza; xem `Assets/Enemies/Models/LICENSES.md`). Chuyển GLB→FBX bằng `Tools/monsters_convert.py`.
- **Cấp AI:** một asset `Resources/AITierProfiles.asset` chứa 5 hàng (T0–T4) thay vì 5 asset riêng.
- **Thiết Giáp Ngưu, Bạo Thi:** dữ liệu đã có, `prefabPending` — chờ model (làm ở phase quái tinh anh).
- **`ReachDistance`:** khi lệch độ cao ≥ 1,8 m thì cộng |dy|×3, để quái không đứng dưới sàn tầng trên mà tưởng đã chạm.
- **NavMeshAgent:** `autoTraverseOffMeshLink = true` để leo cầu thang qua NavMeshLink; agent tắt khi pool giải phóng.
- **Vòng vây (tier ≥ 1):** góc xoay vòng theo trung bình vòng của độ lệch, nên quái ít phải băng qua vòng.
- **Tệp chính:** `Assets/Enemies/Runtime/*` (Archetype, Instance, Motor, Brain, Director, Projectile, Pool), `Assets/Enemies/Editor/EnemiesSetup.cs`, `Assets/Enemies/Validation/MinionCombatPlayTest.cs`.
- **Kết quả:** `MinionCombat` 31/0 (13 quái tới người chơi <30 s, vé ≤2 ở T0, báo hiệu ≥0,75 s, pool không phình sau 100 vòng, đạn bị Tường Hư Không chặn/né được, leo thang 8 m trong 3,7 s, AI 0,04 ms/khung). Hồi quy đầy đủ (`Artifacts/V2/P04`) khớp baseline, chỉ còn lỗi có sẵn.
