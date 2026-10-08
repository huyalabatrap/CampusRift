# P08 — Linh Thạch, cửa hàng, vật phẩm

> **Mục tiêu:** học ra Linh Thạch, dùng Linh Thạch mua đan dược, phù chú và pháp bảo, mang vật phẩm vào màn. Có luật chống cày.
>
> **Phạm vi:** MVP · **Ước lượng:** 3 ngày công · **Phụ thuộc:** P06 (P03-T01 cho buff) · **Tham chiếu:** §9, §10
>
> **Kết quả:** vòng "học → Linh Thạch → mua đồ → dùng trong màn" chạy trọn vẹn, không có đường nạp tiền nào.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P08-T01 | `Wallet`, luật thưởng Linh Thạch, chống cày | Code | 0,5 | P06 | ✅ |
| P08-T02 | `ItemDefinition` và 10 vật phẩm MVP | Dữ liệu | 0,3 | — | ✅ |
| P08-T03 | `BuffSystem`: buff có thời hạn trên người chơi | Code | 0,4 | P03-T01 | ✅ |
| P08-T04 | Hiệu ứng của từng vật phẩm | Code | 0,4 | T02, T03 | ✅ |
| P08-T05 | `Inventory` và ô vật phẩm mang vào màn | Code | 0,3 | T02 | ✅ |
| P08-T06 | `ShopService` và giao diện Đan Các | UI | 0,5 | T01, T05 | ✅ |
| P08-T07 | 3 pháp bảo MVP | Code | 0,3 | T01 | ✅ |
| P08-T08 | HUD vật phẩm trong màn (PC và mobile) | UI | 0,2 | T04, T05 | ✅ |
| P08-T09 | Harness `EconomyPlayTest` | Test | 0,1 | T01–T08 | ✅ |

---

## P08-T01 — `Wallet`, luật thưởng Linh Thạch, chống cày

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P06
- **File:** (mới) `Assets/Progression/Runtime/Wallet.cs`, `StudyEconomy.cs`, `Assets/Progression/Data/EconomyConfig.asset`
- **Các bước:**
  1. `Wallet`: `Balance`, `Earn(amount, source)`, `TrySpend(amount)`, event `Changed`. Lưu trong hồ sơ.
  2. Luật thưởng (§9.1) đặt trong `EconomyConfig`:

| Nguồn | Linh Thạch |
|---|---|
| Câu đúng lần đầu | +5 |
| Câu đúng khi luyện lại (chưa đúng trong 24 giờ) | +2 |
| Hoàn thành bài ≥ 80% / đạt 100% | +30 / +50 |
| Ôn tập đúng hạn | +40 |
| Thi Đột Phá đậu | +300 |
| Qua màn lần đầu | +40 × số thứ tự màn |
| Mỗi sao đạt lần đầu | +20 |

  3. Chống cày (§9.2):
     - lưu mốc giờ trả lời đúng gần nhất của từng câu;
     - luyện lại tối đa 300 Linh Thạch/ngày theo ngày UTC;
     - 5 câu liên tiếp mỗi câu dưới 1,5 giây → nhắc "Đọc kỹ câu hỏi" và dừng thưởng 2 phút.
  4. Dùng chung `ILearningClock` (P07-T05) để chống chỉnh đồng hồ lùi.
- **Hoàn thành khi:**
  - [x] Mỗi nguồn cộng đúng.
  - [x] Trần và luật 24 giờ hoạt động với đồng hồ giả.

## P08-T02 — `ItemDefinition` và 10 vật phẩm MVP

- **Loại:** Dữ liệu · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** —
- **File:** (mới) `Assets/Progression/Runtime/ItemDefinition.cs`; `Assets/Progression/Data/Items/*.asset`; icon trong `Assets/Progression/Data/Icons/`
- **Các bước:**
  1. `ItemDefinition`: `id`, tên và mô tả EN/VN, `kind` (Heal, Buff, FireWard, Revive, Utility), `price`, `maxPerLevel`, `availableFrom` (cảnh giới), `effects[]`, `icon`.
  2. Tạo 10 asset theo §9.3:
     - Hồi Khí Đan, Hồi Xuân Đan, Tụ Linh Đan, Hộ Mệnh Phù;
     - Cuồng Lực Đan, Kim Cương Phù, Thần Hành Phù;
     - Tị Hỏa Châu, Băng Tâm Phù, Kiếm Tâm Đan.

     6 vật phẩm còn lại để P20.
  3. Icon đơn giản (hình viên đan, lá bùa, viên châu, tô màu theo công dụng), vẽ bằng `RiftGraphic` hoặc sprite tự tạo.
- **Hoàn thành khi:**
  - [x] Có 10 asset, giá và giới hạn đúng bảng §9.3.

## P08-T03 — `BuffSystem`: buff có thời hạn trên người chơi

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P03-T01
- **File:** (mới) `Assets/Progression/Runtime/BuffSystem.cs`, `BuffEffect.cs`
- **Các bước:**
  1. Các loại buff: `damagePercent`, `damageTakenPercent`, `speedPercent`, `cooldownPercent`, `critChance`, `fireResistance`, `swordChannelSpeed`.
  2. Dùng lại cùng vật phẩm thì làm mới thời gian, không cộng dồn.
  3. Đẩy modifier nguồn `Buff` vào `PlayerStats`.
  4. Trần: tổng `fireResistance` ≤ 80% (§4.5); `damageTakenPercent` ≥ −80%.
  5. Hiện icon buff kèm đồng hồ đếm ngược dưới thanh máu.
- **Hoàn thành khi:**
  - [x] Buff hết hạn thì chỉ số trở về đúng.
  - [x] Không vượt trần.

## P08-T04 — Hiệu ứng của từng vật phẩm

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P08-T02, P08-T03
- **Các bước:**
  1. Hồi máu ngay hoặc hồi dần theo thời gian; hồi Linh Lực.
  2. **Hộ Mệnh Phù:** đăng ký `PlayerMonsterHealth.BeforeDefeat` (P01-T03). Khi sắp chết: hủy cái chết, hồi 40% máu, miễn thương 2 giây, có hiệu ứng lá bùa cháy. Tối đa 1 lần mỗi màn.
  3. Buff (Cuồng Lực, Kim Cương, Thần Hành) chạy qua `BuffSystem`.
  4. **Tị Hỏa Châu, Băng Tâm Phù:** buff `fireResistance`. Băng Tâm Phù còn miễn Dư Hỏa (P13 đọc cờ này).
  5. **Kiếm Tâm Đan:** niệm Thiên Kiếm nhanh hơn 40%, không bị ngắt 1 lần (P15 đọc).
  6. Dùng vật phẩm thì hồi chiêu chung 1 giây.
- **Hoàn thành khi:**
  - [x] Từng vật phẩm cho đúng hiệu ứng §9.3.
  - [x] Hộ Mệnh Phù cứu đúng 1 lần.

## P08-T05 — `Inventory` và ô vật phẩm mang vào màn

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P08-T02
- **File:** (mới) `Assets/Progression/Runtime/Inventory.cs`
- **Các bước:**
  1. Kho đồ lưu trong hồ sơ: số lượng theo id.
  2. Mang vào màn: 3 ô (Túi Càn Khôn nâng lên 4 rồi 5). Mỗi ô một loại, số lượng ≤ `maxPerLevel`.
  3. Hết màn (thắng hoặc thua): món chưa dùng trả về kho; món đã dùng thì mất.
- **Hoàn thành khi:**
  - [x] Không mang quá giới hạn.
  - [x] Món chưa dùng được trả về đúng.

## P08-T06 — `ShopService` và giao diện Đan Các

- **Loại:** UI · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P08-T01, P08-T05
- **File:** (mới) `Assets/Progression/Runtime/ShopService.cs`, `Assets/CampusRiftUI/Runtime/ShopUI.cs`, `Assets/CampusRiftUI/Editor/UIFoundationBuilder.Shop.cs`
- **Các bước:**
  1. 3 nhóm: Hồi phục · Tăng sức mạnh · Màn 8–10. Thêm tab Pháp bảo (T07).
  2. Mỗi món hiện: icon, tên, mô tả, giá, số đang có, cảnh giới cần. Món chưa đến cảnh giới hiện khóa kèm lý do.
  3. Mua: chọn số lượng → xác nhận → trừ Linh Thạch. Thiếu Linh Thạch thì báo "Cần thêm X Linh Thạch — học thêm để tích lũy" và có nút mở Thư Viện.
  4. Không có bất kỳ nút mua bằng tiền thật nào (§10).
- **Hoàn thành khi:**
  - [x] Mua đúng giá, lưu đúng.
  - [x] Món bị khóa không mua được.
  - [x] Hiển thị đủ EN/VN.

## P08-T07 — 3 pháp bảo MVP

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P08-T01
- **File:** (mới) `Assets/Progression/Runtime/ArtifactService.cs`, `ArtifactDefinition.cs`, 3 asset
- **Các bước (§9.4):**
  1. Phi Kiếm Thanh Trúc: +6% sát thương đánh thường mỗi cấp.
  2. Hộ Tâm Kính: +5% máu tối đa mỗi cấp.
  3. Túi Càn Khôn: +1 ô vật phẩm, tối đa cấp 2.
  4. Cấp pháp bảo không vượt quá số cảnh giới đã đạt. Đẩy modifier nguồn `Artifact` vào `PlayerStats`.
- **Hoàn thành khi:**
  - [x] Nâng cấp đúng giá.
  - [x] Hiệu ứng áp vào màn.
  - [x] Trần theo cảnh giới hoạt động.

## P08-T08 — HUD vật phẩm trong màn (PC và mobile)

- **Loại:** UI · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P08-T04, P08-T05
- **Các bước:**
  1. Các ô vật phẩm cạnh thanh máu: icon, số lượng, phím 1/2/3, lớp phủ hồi chiêu.
  2. Mobile: thêm `TouchRole.Item1..3`, nút nhỏ cạnh thanh máu.
  3. Hiệu ứng khi dùng: vòng sáng màu theo công dụng.
- **Hoàn thành khi:**
  - [x] Dùng được trên PC và mobile; số lượng giảm đúng.

## P08-T09 — Harness `EconomyPlayTest`

- **Loại:** Test · **Ước lượng:** 0,1 ngày · **Phụ thuộc:** P08-T01 đến T08
- **File:** (mới) `Assets/Progression/Validation/EconomyPlayTest.cs`
- **Kiểm tra:**
  - Các nguồn thưởng, trần mỗi ngày, luật 24 giờ, phát hiện trả lời quá nhanh.
  - Mua hàng, giới hạn mang vào màn, trả món chưa dùng.
  - Hộ Mệnh Phù, buff hết hạn.
- **Hoàn thành khi:**
  - [x] PASS.

---

## Kiểm chứng cuối phase

- [x] Học → nhận Linh Thạch → mua Hồi Xuân Đan → mang vào màn 1 → dùng được.
- [x] Cập nhật trạng thái P08 trong `task/README.md`.

## Ghi chú triển khai

- **Linh Thạch:** `Wallet` (đọc hồ sơ hiện tại mỗi lần gọi, nên đổi hồ sơ trong test vẫn đúng), `EconomyConfig` (asset `Assets/Progression/Resources/EconomyConfig.asset`, mọi con số §9.1–9.2) và `StudyEconomy` (trong `Assets/Learning/Runtime/`, vì cần `LearningProgress`). `LearningEngine.Economy` trả thưởng theo từng lần nộp bài: câu đúng lần đầu +5, câu đúng luyện lại +2 (chưa đúng trong 24 giờ), bài ≥ 80% +30 / 100% +50 (chỉ trả phần chênh nếu điểm cao hơn sau này), ôn tập +40, Thi Đột Phá +300 (thay cho `LinhThachSink` cũ), qua màn +40 × số màn, mỗi sao lần đầu +20 (`LevelProgressService.LastLinhThach`, hiện trên màn kết quả).
- **Chống cày:** danh sách câu đã từng đúng (`solved`) trong save; luật 24 giờ dùng `correctLog`; trần luyện lại 300/ngày UTC (`linhRetryToday`); 5 câu liên tiếp dưới 1,5 giây thì từ câu thứ 5 trở đi không được thưởng và dừng thưởng 2 phút (nhắc "Đọc kỹ câu hỏi"). Đồng hồ dùng `ILearningClock` của P07 nên chỉnh giờ lùi không có tác dụng. `QuizSession` thêm `TimeSource`/`ResetTimer` và `AnswerRecord.seconds`.
- **Vật phẩm:** 10 asset trong `Assets/Progression/Data/Items/` cùng 3 pháp bảo trong `Data/Artifacts/` và `Resources/ItemCatalog.asset` (menu *Campus Rift/V2/Install Economy Data* tạo lại được, giữ GUID). Icon: ảnh bạn cung cấp (`Content/Images/Items/`, nạp qua `ContentImages`; xem P09) và, nếu thiếu ảnh, hình vẽ bằng mã (`ItemIcons`: viên đan, lá bùa, viên châu, tô màu theo công dụng). Shop hiện ảnh vật phẩm và pháp bảo. **Kiếm Tâm Đan** kéo dài 180 giây (kế hoạch không ghi thời hạn); cờ `SwordChannelSpeed`, `SwordUninterrupted`, `EmberImmune` đã có trong `BuffSystem` để P13/P15 đọc.
- **`BuffSystem`:** buff là modifier nguồn `Buff` trong `PlayerStats` nên các trần có sẵn (kháng lửa 80%, sát thương nhận −80%, tốc chạy) áp dụng; dùng lại cùng vật phẩm chỉ làm mới thời gian. Chạy theo thời gian scale nên tạm dừng/đang học thì buff đứng.
- **Kho và mang vào màn:** `Inventory` (kho + danh sách mang theo trong hồ sơ, mục mới `carry`). Không trừ kho khi vào màn; chỉ trừ khi dùng, nên món chưa dùng luôn còn nguyên dù thắng, thua hay tắt game. 3 ô, Túi Càn Khôn +1 mỗi cấp. `PlayerItems` (tự gắn vào người chơi qua `CultivationPlayerBridge`, không sửa scene/prefab) xử lý phím 1/2/3, hồi chiêu chung 1 giây, từ chối dùng vô ích (hồi máu khi đầy máu), và Hộ Mệnh Phù qua `PlayerMonsterHealth.BeforeDefeat` (40% máu, miễn thương 2 giây, 1 lần mỗi màn, không dùng tay được).
- **Pháp bảo:** 3 pháp bảo MVP; cấp không vượt quá số cảnh giới đã đạt (Luyện Khí = 1). Phi Kiếm Thanh Trúc cộng % vào chỉ số Công (`Attack`), nên cả kỹ năng dùng Công cũng mạnh lên, không chỉ đánh thường; Hộ Tâm Kính cộng % máu tối đa.
- **Cửa hàng:** không có `ShopUI.cs` riêng; `LearningUI.Shop.cs` (một phần của `LearningUI`) dựng Đan Các trong cùng bảng Khóa Học: nút "ĐAN CÁC" ở đầu danh sách, các mục Hồi phục / Tăng sức mạnh / Màn 8–10 / Pháp bảo / Mang vào màn. Mua chọn số lượng rồi xác nhận, món chưa tới cảnh giới hiện lý do khóa, thiếu tiền thì báo còn thiếu bao nhiêu và có nút mở Thư Viện. Không có đường mua bằng tiền thật. Giới hạn kho mỗi loại 99.
- **HUD:** `ItemBarUI` dựng bằng mã dưới `GameplayHUD` (icon, số còn lại, phím 1/2/3, lớp phủ hồi chiêu, vòng sáng màu khi dùng, nhãn TỰ ĐỘNG cho Hộ Mệnh Phù) và hàng icon buff kèm đếm ngược. Trên điện thoại các ô là **nút chạm** (bấm vào ô để dùng) thay vì thêm `TouchRole.Item1..3`.
- **Test:** `EconomyPlayTest` 75/0 (nguồn thưởng, trần, luật 24 giờ, trả lời quá nhanh, cửa hàng, khóa cảnh giới, mang vào màn, từng vật phẩm, buff hết hạn, trần, Hộ Mệnh Phù, pháp bảo); `LearningPlayTest` 39/0 (thêm luồng cửa hàng qua UI thật). Ảnh: `Artifacts/Economy/P08-hud.png`, `Artifacts/Learning/P08-shop*.png`.
- **Chưa làm (đúng phạm vi):** nhiệm vụ học hằng ngày và chuỗi 7 ngày (➕, thuộc P16); 6 vật phẩm còn lại và 2 pháp bảo còn lại (P20); màn chuẩn bị chọn vật phẩm đẹp (P09 — hiện chọn ở mục "Mang vào màn" của Đan Các).
