# P12 — Quái mới, Shaban, màn 3–7 → **Mốc 3**

> **Mục tiêu:** thêm Thiết Giáp Ngưu và Bạo Thi, kỹ năng quái mở theo màn, đưa Shaban vào làm tinh anh và boss, AI T1–T2, sự kiện màn, hệ sao. Chơi được màn 1–7.
>
> **Phạm vi:** MVP · **Ước lượng:** 5 ngày công · **Phụ thuộc:** P05, P11 · **Tham chiếu:** §2.3, §3, §16
>
> **Kết quả (Mốc 3):** màn 1–7 chơi được, có boss màn 5 và màn 7, có sao ★★★.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P12-T01 | Thiết Giáp Ngưu: húc có báo trước | Code+Asset | 0,6 | P05 | ✅ |
| P12-T02 | Bạo Thi: nổ khi chết, lao vào tự nổ | Code+Asset | 0,5 | P05 | ✅ |
| P12-T03 | Kỹ năng quái mở theo màn | Code | 0,4 | T01, T02 | ✅ |
| P12-T04 | Shaban vào `LevelDirector` (tinh anh màn 3) | Code | 0,5 | P05 | 🔄 |
| P12-T05 | Boss màn 5: Săn Hồn Giả | Code | 0,6 | T04 | 🔄 |
| P12-T06 | Boss màn 7: Săn Hồn Thức Tỉnh | Code | 0,6 | T05 | ✅ |
| P12-T07 | AI T1–T2: vây, giữ khoảng cách, né chiêu | Code | 0,5 | P10-T01 | ✅ |
| P12-T08 | Sự kiện màn: mất điện (màn 4), Huyết Nguyệt (màn 6) | Code | 0,3 | P05 | ✅ |
| P12-T09 | Dữ liệu và cân bằng màn 3–7 | Dữ liệu | 0,4 | T01–T08 | 🔄 |
| P12-T10 | `StarEvaluator` và điều kiện ★★★ màn 1–7 | Code | 0,4 | P11 | ✅ |
| P12-T11 | Harness màn 3–7 và boss | Test | 0,2 | T09, T10 | 🔄 |

**Chốt02/10/2026 theo [TEST-POLICY](TEST-POLICY.md) cập nhật21:30:** triển khai
và smoke đã hoàn tất; không tiếp tục bot/benchmark/hồi quy toàn nhóm.
Các checkbox thời lượng hoặc full regression chưa có bằng chứng vẫn để mở.
[Báo cáo cuối](p12/REPORT-P12.md), [ảnh và tự soi](p12/VISUAL-QA.md).

**Fix1 02/10:** năm mục review đã sửa/kiểm lại; thêm two-sided shader để
model sống hết đốm và fill tạm cho ending. [Báo cáo fix1](p12/REPORT-P12-fix1.md),
[ảnh mới](p12/screens/fix1/), smoke21/0. Các checkbox cân bằng/full regression
đang mở vẫn giữ theo TEST-POLICY; AudioEnding6/1 và probe gầm thật ghi riêng.

---

## P12-T01 — Thiết Giáp Ngưu: húc có báo trước

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model quái bọc giáp có animation, âm thanh húc) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code+Asset · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P05
- **File:** (mới) `Assets/Enemies/Runtime/Abilities/ChargeAbility.cs`, prefab `ThietGiapNguu.prefab`, model trong `Assets/Enemies/Models/` (ghi LICENSES)
- **Các bước:**
  1. Model: quái to, bọc giáp, hệ Kim (theo quy trình P04-T02; có model tạm dự phòng).
  2. **Húc:**
     - báo trước 1 giây bằng vệt sáng trên mặt đất theo hướng húc;
     - lao 14 m/s, dừng khi gặp tường hoặc hết quãng;
     - trúng người chơi: sát thương và choáng 0,5 giây.
     - Người chơi né ngang được.
  3. Máu 220 × HP× của màn; phòng thủ 20%.
- **Hoàn thành khi:**
  - [x] Húc có báo trước, né được, không xuyên tường.

## P12-T02 — Bạo Thi: nổ khi chết, lao vào tự nổ

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model xác sống có animation, âm thanh nổ) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code+Asset · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P05
- **File:** (mới) `Assets/Enemies/Runtime/Abilities/ExplodeAbility.cs`, prefab `BaoThi.prefab`
- **Các bước:**
  1. Chết thì nổ bán kính 3,5 m, gây 30 × ST× (nguồn Reaction), có vòng báo 0,4 giây. Quái khác trong vùng nổ nhận 50%.
  2. Từ màn 7: tự lao vào người chơi; khi còn cách 2 m thì châm ngòi 0,8 giây (thân nháy đỏ), rồi nổ.
  3. Hư Không Kết Giới chặn được đường lao.
- **Hoàn thành khi:**
  - [x] Nổ đúng bán kính và đúng thời điểm.
  - [x] Có báo trước, né được.

## P12-T03 — Kỹ năng quái mở theo màn

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng kỹ năng quái (vồ, bắn quạt)) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P12-T01, P12-T02
- **File:** (mới) `Assets/Enemies/Runtime/Abilities/LeapAbility.cs`, `SpreadShotAbility.cs` · (sửa) các asset archetype
- **Các bước (§3.2):**
  1. Tiểu Yêu **Vồ** (từ màn 4): nhảy 6 m, báo trước 0,6 giây.
  2. Độc Nhãn **bắn 3 tia hình quạt** (từ màn 5).
  3. Thiết Giáp **húc phá Kết Giới** (từ màn 6): Void Wall vỡ ngay khi bị húc trúng.
  4. `EnemyArchetype.abilities[minLevel]`: chỉ gắn kỹ năng khi màn hiện tại ≥ `minLevel`.
  5. Hồi chiêu kỹ năng quái giảm dần theo màn, tới −40% ở màn 10.
- **Hoàn thành khi:**
  - [x] Ở màn 3 Tiểu Yêu chưa biết vồ; ở màn 4 thì đã vồ.

## P12-T04 — Shaban vào `LevelDirector` (tinh anh màn 3)

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P05
- **File:** (sửa) `LevelDirector.cs`, `Monster_Shaban.prefab` (hoặc tạo prefab variant `Monster_Shaban_Elite.prefab`)
- **Các bước:**
  1. `LevelDirector` sinh Shaban từ prefab theo `LevelDefinition`, thay cho instance cố định trong scene (đã tắt ở P05-T04).
  2. Máu = 500 × HP×. Sát thương: nhân bản `MonsterAIConfig` lúc runtime rồi đặt `Damage = 25 × ST×`. **Không** sửa asset gốc.
  3. Tắt escalation (P05-T04). Khi chết thì phát `EnemyDied` với trọng số 4. Hiện tên "Săn Hồn Giả".
  4. Trên mobile: đặt `BeliefParticles` 160 thay vì 320.
  5. Kiểm tra AI săn mồi (nghe, đoán, đi thang máy) vẫn chạy khi có nhiều quái khác cùng lúc.
- **Hoàn thành khi:**
  - [x] Shaban xuất hiện ở đợt 3 màn 3.
  - [x] Có thể bị hạ; màn tính là xong.
  - [ ] Nhóm hồi quy Quái PASS.

## P12-T05 — Boss màn 5: Săn Hồn Giả

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh gầm, sóng xung kích, nhạc boss) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P12-T04
- **File:** (mới) `Assets/Enemies/Runtime/Boss/BossController.cs`, `RoarAbility.cs`, `LeapSlamAbility.cs`, prefab variant `Monster_Shaban_Boss.prefab`, `Assets/CampusRiftUI/Runtime/BossHealthBarUI.cs`
- **Các bước:**
  1. Máu 500 × 3,0 = 1.500; cờ `resistHardControl`.
  2. **Gầm Hồn:** báo trước 1,2 giây (vòng đỏ bán kính 8 m); choáng 1 giây nếu người chơi còn trong vòng. Kim Chung Tráo chặn được.
  3. **Vồ Đập:** nhảy tới vị trí người chơi (xa tối đa 12 m), báo trước 0,8 giây; tạo sóng xung kích lan ra.
  4. Thanh máu boss ở trên cùng màn hình, kèm tên; nhạc boss.
  5. `BossController` quyết định lúc nào dùng kỹ năng. Phần di chuyển vẫn do `MonsterBrain` đảm nhiệm.
- **Hoàn thành khi:**
  - [x] Cả 2 kỹ năng đều có báo trước và né được.
  - [ ] Hạ boss trong 60–90 giây ở đúng cảnh giới đề nghị (Kết Đan 1).

## P12-T06 — Boss màn 7: Săn Hồn Thức Tỉnh

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh cuồng nộ, hiệu ứng lướt bóng, nhạc boss pha 2) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P12-T05
- **Các bước:**
  1. Máu 500 × 4,6 × 1,5 = 3.450.
  2. **Pha 2** khi còn ≤ 50% máu: gầm, nhanh hơn 30%, dùng **Lướt Bóng** 3 lần liên tiếp (lướt kèm bóng ảo, mỗi lần báo trước 0,4 giây).
  3. Gọi quái phụ ở pha 2. MVP gọi 3 Tiểu Yêu; P19 đổi thành 2 Ảnh Yêu. Quái gọi ra không tính Kiếm Ý.
  4. **Cảnh kết màn** (4 giây, bỏ qua được):
     - bầu trời chuyển đỏ (`SkyLightingController`);
     - bóng Hỏa Long Vương hiện xa trên trời (dùng model cự thú tạm của P14, hoặc một hình bóng đơn giản);
     - máy quay lia lên.
- **Hoàn thành khi:**
  - [x] Pha 2 kích hoạt đúng một lần.
  - [x] Cảnh kết màn chạy xong rồi mới hiện Kết Quả.

## P12-T07 — AI T1–T2: vây, giữ khoảng cách, né chiêu

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P10-T01
- **File:** (sửa) `EnemyDirector.cs`, `MinionBrain.cs`, các asset `AITierProfile`
- **Các bước:**
  1. **T1:** hoàn thiện vị trí vây (P04-T04); quái đánh xa giữ khoảng cách; đi cửa và cầu thang tự nhiên.
  2. **T2:**
     - khi `DangerZoneRegistry` có vùng phủ lên vị trí quái, quái có 40% cơ hội né ngang ra khỏi vùng (mỗi quái tối đa 1 lần né mỗi 3 giây);
     - quái chặn đầu theo hướng người chơi đang chạy (dùng ý tưởng của `InterceptionPlanner`).
  3. Rút lui về Triệu Hồn Sư khi còn 25% máu: để P19.
- **Hoàn thành khi:**
  - [x] Ở T2, tỉ lệ né đo được xấp xỉ 40% ±10% qua 50 lần thử.

## P12-T08 — Sự kiện màn: mất điện (màn 4), Huyết Nguyệt (màn 6)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh mất điện, âm thanh không khí đêm trăng máu) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P05
- **File:** (mới) `Assets/Levels/Runtime/LevelEvents/BlackoutEvent.cs`, `BloodMoonEvent.cs`
- **Các bước:**
  1. **Mất điện (màn 4):**
     - tắt đèn trong tòa D–E (tìm Light theo vùng hoặc BuildingID);
     - giảm ánh sáng môi trường trong tòa;
     - hiện gợi ý bật đèn pin (`PlayerFlashlight`).
  2. **Huyết Nguyệt (màn 6):** preset `BloodMoon`. Luật "quái ngoài trời nhanh hơn 15%" cần phát hiện trong nhà/ngoài trời, nên làm ở P13-T09; ở phase này tạm tăng 10% cho mọi quái.
- **Hoàn thành khi:**
  - [x] Màn 4 tối hẳn trong tòa và đèn pin có ích.
  - [x] Màn 6 có bầu trời trăng máu.

## P12-T09 — Dữ liệu và cân bằng màn 3–7

- **Loại:** Dữ liệu · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P12-T01 đến T08
- **Các bước:**
  1. Điền đợt quái và vùng khe nứt theo §2.3. Màn 6–7 bản MVP dùng 4 loại quái MVP với số lượng cao hơn (P19 sẽ bổ sung loại mới).
  2. Chơi thử ở đúng cảnh giới đề nghị (lệnh DEV đặt cảnh giới).
  3. So với §16:
     - quái thường chết sau 2–3 nhát;
     - mỗi đòn của quái lấy khoảng 8–9% máu;
     - thời lượng màn trong khoảng ±30% so với mốc.
- **Hoàn thành khi:**
  - [ ] 5 màn đều trong khoảng mục tiêu.
  - [x] Ghi chú cân bằng lưu vào `Artifacts/Levels/Balance-3-7.md`.

## P12-T10 — `StarEvaluator` và điều kiện ★★★ màn 1–7

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P11
- **File:** (mới) `Assets/Levels/Runtime/StarEvaluator.cs`, các lớp con của `StarCondition`
- **Các bước:**
  1. ★ hoàn thành; ★★ không dùng Hộ Mệnh Phù và dưới thời gian mốc; ★★★ theo điều kiện riêng của màn.
  2. Các loại điều kiện ★★★:

| Màn | Điều kiện |
|:-:|---|
| 1 | Hạ 3 quái bằng Đại Thủ Ấn (`KillsWithSkill`) |
| 2 | Trúng đạn độc ≤ 3 lần (`MaxHitsFrom`) |
| 3 | Hạ Shaban trong 60 giây kể từ lúc chạm trán (`KillEliteWithin`) |
| 4 | 5 lần Băng Lôi Liệt (`ReactionCount`) |
| 5 | Hạ boss không dùng đan hồi máu (`NoHealItems`) |
| 6 | MVP: dưới 11:00 (`TimeUnder`). P19 đổi sang điều kiện Triệu Hồn Sư |
| 7 | Dưới 13:00 (`TimeUnder`) |

  3. Lưu sao vào `LevelProgressService`; hiển thị ở Kết Quả và Bản Đồ.
- **Hoàn thành khi:**
  - [x] Mỗi điều kiện có test đạt và test không đạt.

## P12-T11 — Harness màn 3–7 và boss

- **Loại:** Test · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P12-T09, P12-T10
- **File:** (mới) `Assets/Levels/Validation/Level3to7PlayTest.cs`, `Assets/Enemies/Validation/ShabanBossPlayTest.cs`
- **Kiểm tra:**
  - Mỗi màn sinh đúng thành phần quái.
  - Boss: kỹ năng có báo trước, gây sát thương, chuyển pha.
  - Sao được lưu.
- **Hoàn thành khi:**
  - [x] PASS.
  - [ ] Nhóm hồi quy Quái PASS.

---

## Kiểm chứng cuối phase → Mốc 3

- [x] Chơi được màn 1 → 7 liên tục (cảnh giới DEV), boss màn 5 và màn 7 hoạt động đúng.
- [x] Cập nhật trạng thái P12 và Mốc 3 trong `task/README.md`.

Tiến độ và raw theo từng tiêu chí: [PROGRESS](p12/PROGRESS.md). Mốc3 đã có
DEV1→7 và sao/boss hoạt động; chưa chứng nhận cân bằng thực chiến hoặc full
regression. Không đánh dấu các tiêu chí còn mở là đạt sau khi bỏ phép đo.
