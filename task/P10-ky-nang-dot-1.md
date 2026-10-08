# P10 — Kỹ năng đợt 1 (7 kỹ năng mới)

> **Mục tiêu:** đủ 10 kỹ năng MVP (3 kỹ năng cũ + 7 kỹ năng mới), có tầng nâng cấp.
>
> **Phạm vi:** MVP · **Ước lượng:** 5 ngày công · **Phụ thuộc:** P03, P06 · **Tham chiếu:** §7.3, §7.4, §7.7
>
> **Quy ước cho mọi kỹ năng:**
> - Mỗi kỹ năng có: 1 asset `SkillDefinition`, 1 lớp con `SkillRuntime`, VFX/SFX, icon, cách ngắm trên mobile.
> - Sát thương tính theo **% Công** qua `DamageCalculator`, có hệ.
> - Code đặt tại `Assets/Skills/<TênTiếngAnh>/Runtime/`.
> - Số liệu ghi trong bảng là ở **tầng 1**.

| ID | Task | Hệ | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|:-:|:-:|---|:-:|
| P10-T01 | Hạ tầng chung: ngắm mặt đất, vùng nguy hiểm, pool VFX | — | 0,4 | P03 | ✅ |
| P10-T02 | Tích Lịch Nhất Thiểm | Lôi | 0,5 | T01 | ✅ |
| P10-T03 | Phật Nộ Hỏa Liên | Hỏa | 0,6 | T01 | ✅ |
| P10-T04 | Hàn Băng Phong Ấn | Thủy | 0,5 | T01 | ✅ |
| P10-T05 | Thần Kiếm Ngự Lôi Chân Quyết | Lôi | 0,5 | T01 | ✅ |
| P10-T06 | Kim Chung Tráo | Kim | 0,5 | T01 | ✅ |
| P10-T07 | Hắc Động Thần La | Không Gian | 0,6 | T01 | ✅ |
| P10-T08 | Vạn Kiếm Quyết | Kim | 0,5 | T01, P03-T03 | ✅ |
| P10-T09 | Tầng kỹ năng (5 tầng) và nâng cấp | — | 0,5 | T02–T08, P06 | ✅ |
| P10-T10 | Harness `SkillSet1PlayTest` | — | 0,4 | T02–T09 | ✅ |

---

## P10-T01 — Hạ tầng chung: ngắm mặt đất, vùng nguy hiểm, pool VFX

- **File:** (mới) `Assets/Skills/Core/Runtime/GroundAimIndicator.cs`, `DangerZoneRegistry.cs`, `SkillVfxPool.cs`
- **Các bước:**
  1. `GroundAimIndicator`: vòng tròn hoặc hình nón trên mặt đất cho kỹ năng dạng `Aimed`.
     - PC: theo tâm ngắm.
     - Mobile: theo `CampusInput.SkillDrag`, giống cách Giant Hand đang làm.
  2. `DangerZoneRegistry`: kỹ năng có vùng tác động đăng ký hình dạng và thời gian. AI cấp T2 trở lên đọc để né (P12-T07).
  3. `SkillVfxPool`: pool chung cho hiệu ứng nổ, tia, hạt.
- **Hoàn thành khi:**
  - [x] Vòng ngắm chạy trên PC và mobile.
  - [x] Registry trả đúng các vùng đang hoạt động.

## P10-T02 — Tích Lịch Nhất Thiểm (Lôi)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh sét/lướt, texture tia sét) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Lôi Hô Hấp, Nhất Thức (Zenitsu, *Kimetsu no Yaiba*). **ID:** `tich-lich-nhat-thiem` · **Mở:** Luyện Khí 3
- **Số liệu:** lướt 10 m về phía trước · 180% Công mỗi quái bị xuyên qua · gây Shock 0,5 giây · vô địch 0,2 giây · hồi chiêu 8 giây · 20 Linh Lực
- **Các bước:**
  1. Lướt bằng `CampusExplorer.Dash` (P03-T04); dừng khi gặp tường.
  2. Gây sát thương cho mọi quái nằm trong đường lướt (capsule cast).
  3. Hiệu ứng: thêm API `SpeedForceVFX.Burst(duration)` để kích hoạt tia sét của hiệu ứng chạy nhanh có sẵn; cộng thêm vệt sét vàng.
  4. Nếu đang khóa mục tiêu: lướt thẳng về phía mục tiêu.
- **Hoàn thành khi:**
  - [x] Xuyên được 3 quái xếp hàng.
  - [x] Không xuyên tường.
  - [x] Có miễn thương khi lướt.

## P10-T03 — Phật Nộ Hỏa Liên (Hỏa)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh lửa và vụ nổ, texture/hạt lửa) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Tiêu Viêm (*Đấu Phá Thương Khung*). **ID:** `phat-no-hoa-lien` · **Mở:** Trúc Cơ
- **Số liệu:** tụ lực 1,2 giây (vẫn đi chậm được) · ném tới điểm ngắm ≤ 18 m · nổ bán kính 7 m, 450% Công + Burn 3 giây · để lại vùng lửa 4 giây · hồi chiêu 20 giây · 45 Linh Lực
- **Các bước:**
  1. Trong lúc tụ lực: hoa sen xoay trên tay, sáng dần. Mesh hoa sen sinh bằng code (cánh sen xếp vòng).
  2. Ném theo đường cong, nổ khi chạm đất hoặc chạm quái.
  3. Vùng lửa tick theo nguồn Reaction; đăng ký vào `DangerZoneRegistry`.
- **Hoàn thành khi:**
  - [x] Đúng sát thương và bán kính.
  - [x] Quái hệ Kim nhận ×1,5.

## P10-T04 — Hàn Băng Phong Ấn (Thủy)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh băng vỡ, texture/mesh gai băng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** băng hệ (Todoroki, *My Hero Academia*). **ID:** `han-bang-phong-an` · **Mở:** Trúc Cơ
- **Số liệu:** hình nón 90°, dài 10 m · 120% Công + Freeze 2,5 giây (boss: Chill 50% trong 2,5 giây) · hồi chiêu 14 giây · 30 Linh Lực
- **Các bước:**
  1. Kiểm tra quái trong hình nón (góc và tầm nhìn).
  2. Gây `StatusEffectHost.Apply(Freeze)`. Quái có cờ `resistHardControl` thì nhận Chill thay vì Freeze.
  3. Hiệu ứng: gai băng mọc dọc hình nón; quái bị đóng băng có lớp băng phủ ngoài.
- **Hoàn thành khi:**
  - [x] Đóng băng đúng thời gian.
  - [x] Boss chỉ bị làm chậm.

## P10-T05 — Thần Kiếm Ngự Lôi Chân Quyết (Lôi)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh sét dây chuyền, texture tia sét) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Lục Tuyết Kỳ (*Tru Tiên*). **ID:** `than-kiem-ngu-loi` · **Mở:** Trúc Cơ
- **Số liệu:** sét dây chuyền nảy qua 6 mục tiêu, mỗi lần nảy xa tối đa 8 m · 200% Công, giảm 10% sau mỗi lần nảy · hồi chiêu 12 giây · 35 Linh Lực
- **Các bước:**
  1. Mục tiêu đầu: mục tiêu đang khóa, hoặc quái gần tâm ngắm nhất. Ưu tiên quái bay (chuẩn bị cho P19).
  2. Mỗi lần nảy cần nhìn thấy mục tiêu kế; không nảy lại vào mục tiêu đã trúng.
  3. Hiệu ứng: tia sét zigzag tím vàng giữa các mục tiêu, kèm tiếng nổ lách tách.
- **Hoàn thành khi:**
  - [x] Nảy đúng số lần và đúng mức giảm.
  - [x] Đánh quái hệ Âm (Shaban) nhận ×1,5.

## P10-T06 — Kim Chung Tráo (Kim)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh chuông vàng, texture khiên phát sáng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Thiếu Lâm (kiếm hiệp). **ID:** `kim-chung-trao` · **Mở:** Kết Đan
- **Số liệu:** khiên hấp thụ sát thương bằng 40% máu tối đa trong 6 giây · phản 20% sát thương cận chiến · −60% Thiên Hỏa khi khiên còn · hồi chiêu 30 giây · 40 Linh Lực
- **Các bước:**
  1. Khiên nằm trước `PlayerMonsterHealth.ApplyDamage`: trừ vào khiên trước khi trừ máu.
  2. Phản sát thương về kẻ tấn công cận chiến bằng `DamageInfo` (Reaction).
  3. Đăng ký buff `fireResistance` 0,6 qua `BuffSystem`, vẫn tuân thủ trần 80%.
  4. Hiệu ứng: chuông vàng mờ bao quanh người chơi, vỡ tan khi hết lượng hấp thụ.
- **Hoàn thành khi:**
  - [x] Hấp thụ đúng lượng.
  - [x] Phản đòn đúng.
  - [x] Hết 6 giây thì khiên biến mất.

## P10-T07 — Hắc Động Thần La (Không Gian)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh hố đen trầm, texture xoáy) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Địa Bạo Thiên Tinh và Thần La Thiên Chinh (Pain, *Naruto*). **ID:** `hac-dong-than-la` · **Mở:** Nguyên Anh
- **Số liệu:** đặt tại điểm ngắm ≤ 15 m · hút quái trong bán kính 9 m suốt 3 giây · sau đó hất văng, gây 250% Công · hồi chiêu 22 giây · 50 Linh Lực
- **Các bước:**
  1. Hút:
     - tắt tạm điều khiển của `NavMeshAgent`, di chuyển quái về tâm, giữ quái trên NavMesh;
     - gắn trạng thái `Pulled` (dùng cho phản ứng Tụ Sát ở P11);
     - boss không bị hút, chỉ bị Chill.
  2. Hất: đẩy ra xa 4 m, gây sát thương, rồi trả quyền điều khiển cho agent.
  3. Hiệu ứng: quả cầu đen có viền tím, hạt bị hút xoáy vào; tiếng trầm.
- **Hoàn thành khi:**
  - [x] Gom được nhóm quái.
  - [x] Không có quái nào bị kẹt ngoài NavMesh sau khi hất.

## P10-T08 — Vạn Kiếm Quyết (Kim)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh mưa kiếm) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Vô Danh (*Phong Vân*), Gate of Babylon (*Fate*). **ID:** `van-kiem-quyet` · **Mở:** Hóa Thần
- **Số liệu:** vùng bán kính 8 m · 30 thanh kiếm rơi trong 3 giây, vị trí ngẫu nhiên · mỗi thanh 40% Công · hồi chiêu 18 giây · 50 Linh Lực
- **Các bước:**
  1. Dùng lại `FlyingSword` và pool của P03-T03. Kiếm rơi từ trên cao, màu vàng kim.
  2. Mỗi thanh kiếm là một lần trúng riêng. Đăng ký vùng vào `DangerZoneRegistry`.
- **Hoàn thành khi:**
  - [x] Đủ 30 thanh.
  - [x] Không tăng số object sau 10 lần dùng.

## P10-T09 — Tầng kỹ năng (5 tầng) và nâng cấp

- **File:** (mới) `Assets/Skills/Core/Runtime/SkillProgressService.cs` · (sửa) `SkillBookUI.cs`
- **Các bước (§7.4):**
  1. 5 tầng: Nhập Môn → Tiểu Thành → Đại Thành → Viên Mãn → Hóa Cảnh.
  2. Mỗi tầng: +12% sát thương/hiệu quả, −5% hồi chiêu.
  3. Giá nâng tầng: 150 / 400 / 900 / 1.600 Linh Thạch. Mỗi tầng cần cảnh giới cao hơn cảnh giới mở kỹ năng 1 bậc (tối đa Độ Kiếp).
  4. Lưu tầng trong hồ sơ (`skills.ranks`). HUD hiện nhãn tầng qua `SkillSlotUI.UpgradeLabel`.
  5. Hiệu ứng đặc biệt ở Viên Mãn để P18. Riêng Ảnh Phân Thân nổ khi tan ở tầng 3 thì làm ngay tại đây.
- **Hoàn thành khi:**
  - [x] Nâng tầng đúng giá, đúng điều kiện.
  - [x] Số liệu kỹ năng đổi theo tầng.

## P10-T10 — Harness `SkillSet1PlayTest`

- **File:** (mới) `Assets/Skills/Core/Validation/SkillSet1PlayTest.cs` → `Artifacts/Skills/SkillSet1.json`
- **Kiểm tra với từng kỹ năng trong 7 kỹ năng mới:**
  - dùng được trên PC và mobile;
  - đúng sát thương, hồi chiêu, Linh Lực, hệ, trạng thái;
  - không có giá trị NaN;
  - VFX được trả về pool;
  - đúng hệ số theo tầng.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] Nhóm hồi quy Kỹ năng không có FAIL mới.

---

## Kiểm chứng cuối phase

- [x] Chọn đủ 10 kỹ năng MVP trong màn Chuẩn Bị (với cảnh giới DEV) và dùng được trong màn.
- [x] Cập nhật trạng thái P10 trong `task/README.md`.

## Kiểm chứng vòng sửa fix2

02/10/2026: xem [REPORT-P10-fix2](p10/REPORT-P10-fix2.md) và [PROGRESS-fix2](p10/PROGRESS-fix2.md). Main269/0, Pool121/0, Edge29/0, HubFlow42/0, HubLayout24/0; không có FAIL hồi quy mới. PC giảmFPS tối đa6,30%; pool1122→1122 và0B VFXGC/frame.14sheet mới sáng/tối cùng impact/mobile trong p10/screens/fix2/.
