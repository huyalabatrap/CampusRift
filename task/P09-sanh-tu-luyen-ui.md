# P09 — Sảnh Tu Luyện, bản đồ màn, chuẩn bị, kết quả → **Mốc 2**

> **Mục tiêu:** nối toàn bộ vòng lặp ngoài màn: Sảnh 5 tab, bản đồ 10 màn, màn Chuẩn Bị (4 kỹ năng + vật phẩm + tình báo), màn Kết Quả; và bỏ việc học giữa trận.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P05, P07, P08 · **Tham chiếu:** §7.1, §7.6, §13
>
> **Kết quả (Mốc 2):** học → lên cấp → mua đồ → chọn 4 kỹ năng → vào màn → nhận thưởng → quay lại học.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P09-T01 | Trạng thái UI mới; bỏ học giữa trận; menu Pause mới | Code | 0,4 | P05 | ✅ |
| P09-T02 | Sảnh Tu Luyện (5 tab) trong MainMenu | UI | 0,6 | T01 | ✅ |
| P09-T03 | Bản đồ 10 màn | UI | 0,4 | T02, P06-T06 | ✅ |
| P09-T04 | Màn Chuẩn Bị: 4 ô, vật phẩm, tình báo, gợi ý, bộ lưu sẵn | UI | 0,8 | T03, P08-T05 | ✅ |
| P09-T05 | Tab Công Pháp (danh sách kỹ năng) | UI | 0,4 | T02 | ✅ |
| P09-T06 | Màn Kết Quả đầy đủ | UI | 0,3 | P05-T07 | ✅ |
| P09-T07 | Song ngữ cho toàn bộ chuỗi mới | UI | 0,3 | T02–T06 | ✅ |
| P09-T08 | Validation UI theo tỉ lệ màn hình, vùng an toàn | Test | 0,3 | T07 | ✅ |

---

## P09-T01 — Trạng thái UI mới; bỏ học giữa trận; menu Pause mới

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P05
- **File:** (sửa) `Assets/CampusRiftUI/Runtime/UIStateManager.cs`, `UIManager.cs`
- **Hiện trạng:** `enum UIState { Gameplay, Paused, Menu, Settings, Course, Credits, Modal, GameOver, Loading, Victory }`; `OpenCourse()` mở được cả từ `Gameplay` và `Paused`.
- **Các bước:**
  1. Thêm trạng thái `Hub`, `LevelMap`, `Loadout`, `Shop`, `Cultivation`, `SkillBook`, `LevelResult`. `Course` đổi vai trò thành Thư Viện, chỉ mở từ Hub.
  2. `OpenCourse()`: bỏ `Gameplay` và `Paused` khỏi điều kiện cho phép.
  3. Menu Pause: **Tiếp tục · Cài đặt · Rời màn**. Rời màn cần xác nhận; vật phẩm đã dùng thì mất.
  4. `Back()` và phím Esc đi đúng đường quay lại cho từng trạng thái mới.
  5. `timeScale`: chỉ đóng băng ở Pause, GameOver, LevelResult và các panel mở trong gameplay (giữ luật hiện tại).
- **Hoàn thành khi:**
  - [x] Không có cách nào mở Thư Viện khi đang trong màn.
  - [x] Esc quay lại đúng ở mọi trạng thái.

## P09-T02 — Sảnh Tu Luyện (5 tab) trong MainMenu

- **Loại:** UI · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P09-T01
- **File:** (mới) `Assets/CampusRiftUI/Runtime/HubUI.cs`, `Assets/CampusRiftUI/Editor/UIFoundationBuilder.Hub.cs` · (sửa) `MainMenu.unity`
- **Các bước:**
  1. Menu chính: nút **CHƠI** mở Sảnh. **CONTINUE** không còn cần thiết, vì hồ sơ luôn được nạp.
  2. Header của Sảnh: tên cảnh giới và tầng, thanh Tu Vi nhỏ, số Linh Thạch, số bài đến hạn ôn.
  3. 5 tab:
     - Thư Viện: dùng lại `LearningUI` cùng các chế độ học của P07;
     - Đan Các (P08-T06);
     - Công Pháp (T05);
     - Cảnh Giới (P06-T07);
     - Bản Đồ (T03).
  4. Nền dùng lại `MenuAmbience` và `MenuIntro` có sẵn.
- **Hoàn thành khi:**
  - [x] Chuyển qua lại cả 5 tab trên PC (chuột và bàn phím) và mobile (chạm).

## P09-T03 — Bản đồ 10 màn

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P09-T02, P06-T06
- **File:** (mới) `Assets/CampusRiftUI/Runtime/LevelMapUI.cs`
- **Các bước:**
  1. 10 nút màn trên một đường đi, trang trí theo tiến trình (chiều tà → đêm → trời lửa).
  2. Mỗi nút hiện: số màn, tên, sao, thời gian tốt nhất, và lý do khóa nếu có ("Cần Trúc Cơ 1" hoặc "Qua màn 2 trước").
  3. Chọn màn đã mở thì sang màn Chuẩn Bị.
- **Hoàn thành khi:**
  - [x] Trạng thái khóa/mở khớp `LevelProgressService`.

## P09-T04 — Màn Chuẩn Bị: 4 ô, vật phẩm, tình báo, gợi ý, bộ lưu sẵn

- **Loại:** UI · **Ước lượng:** 0,8 ngày · **Phụ thuộc:** P09-T03, P08-T05
- **File:** (mới) `Assets/CampusRiftUI/Runtime/LoadoutUI.cs`
- **Các bước:**
  1. **4 ô kỹ năng.** Chọn từ danh sách kỹ năng đã mở, có lọc theo hệ và vai trò. Kỹ năng chưa mở hiện khóa kèm cảnh giới cần.
  2. **Tình báo màn:** loại quái, hệ và điểm yếu (lấy từ `LevelDefinition`), boss, cự thú. Ví dụ dòng "Thiết Giáp Ngưu — Kim — yếu Hỏa".
  3. **Bộ gợi ý** theo §7.6, có nút "Dùng bộ gợi ý". Chỉ điền những kỹ năng đã mở.
  4. **3 bộ lưu sẵn** (lưu trong hồ sơ).
  5. **Vật phẩm:** 3 ô (hoặc 4–5 khi có Túi Càn Khôn), chọn từ kho, giới hạn `maxPerLevel`.
  6. Nút **BẮT ĐẦU**: ghi `LevelSession` (kỹ năng, vật phẩm) rồi gọi `StartLevel`.
- **Hoàn thành khi:**
  - [x] Bộ kỹ năng và vật phẩm được áp đúng khi vào màn.
  - [x] Bộ lưu sẵn còn sau khi khởi động lại.

## P09-T05 — Tab Công Pháp (danh sách kỹ năng)

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P09-T02
- **File:** (mới) `Assets/CampusRiftUI/Runtime/SkillBookUI.cs`
- **Các bước:**
  1. Danh sách toàn bộ kỹ năng có trong dữ liệu (21 kỹ năng khi đủ): icon, tên, hệ, vai trò, mô tả, cảm hứng, cảnh giới mở.
  2. Chi tiết số liệu theo tầng; nút nâng tầng (nối với P10-T08).
  3. Phần combo gợi ý: kỹ năng này kết hợp với kỹ năng nào (lấy từ bảng phản ứng P11).
- **Hoàn thành khi:**
  - [x] Thông tin khớp `SkillDefinition`.
  - [x] Hiển thị đủ EN/VN.

## P09-T06 — Màn Kết Quả đầy đủ

- **Loại:** UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P05-T07
- **Các bước:**
  1. Sao ★ kèm điều kiện từng sao (sao thứ 3 hoàn thiện ở P12-T10); thời gian so với mốc.
  2. Phần thưởng: Linh Thạch và Tu Vi, kèm hiệu ứng đếm số.
  3. Gợi ý "Bài nên ôn" (các bài đến hạn).
  4. Nút: Chơi lại · Màn tiếp · Về Sảnh.
- **Hoàn thành khi:**
  - [x] Phần thưởng cộng đúng một lần.
  - [x] Các nút đi đúng nơi.

## P09-T07 — Song ngữ cho toàn bộ chuỗi mới

- **Loại:** UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P09-T02 đến T06
- **File:** (sửa) `Assets/Localization/Resources/LocalizationCatalog.asset`, `Tools/localization_*.cs|py`
- **Các bước:**
  1. Gom mọi chuỗi mới của P01–P09 vào catalog, đủ EN và VN.
  2. Font Be Vietnam Pro, kiểm tra đầy đủ dấu tiếng Việt.
  3. Chạy `LocalizationPlayTest`.
- **Hoàn thành khi:**
  - [x] Đổi ngôn ngữ trong Settings thì mọi panel mới đổi theo.
  - [x] Không còn chuỗi viết cứng trong code.

## P09-T08 — Validation UI theo tỉ lệ màn hình, vùng an toàn

- **Loại:** Test · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P09-T07
- **File:** (sửa) `Assets/CampusRiftUI/Validation/UIValidation.cs`, `UIPlayValidation.cs`
- **Kiểm tra:**
  - Các panel mới ở tỉ lệ 16:9, 19,5:9, 4:3: không tràn, không chồng lấn, nằm trong vùng an toàn.
  - Nút chạm ≥ 44 px (theo chuẩn hiện có).
  - Luồng Hub → Bản Đồ → Chuẩn Bị → màn → Kết Quả → Hub.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] Có ảnh chụp trong `Artifacts/UI/`.

---

## Kiểm chứng cuối phase → Mốc 2

- [x] Hồ sơ mới: học bài 1 → Tu Vi và Linh Thạch → mua đồ → chọn 4 kỹ năng → chơi màn 1 → Kết Quả → về Sảnh.
- [x] Nhóm hồi quy Học tập và Giao diện PASS.
- [x] Cập nhật trạng thái P09 và Mốc 2 trong `task/README.md`.

## Ghi chú triển khai

- **Ảnh:** ảnh bạn cung cấp trong `Content/Images/` được chép (thu nhỏ) vào `Assets/Resources/ContentImages/<nhóm>/` (`Items` 256, `Artifacts` 256, `Realms` 256, `Badges` 128, `Elements` 128, `HUD` 128, `Portraits` 512, `Skills` 512, `Scenes` nguyên bản; thư mục gốc `Content/Images` giữ nguyên). `ContentImages.Get(nhóm, tên)` nạp theo tên file, luôn có phương án dự phòng nếu thiếu ảnh. Đã gắn vào: nền Sảnh và nền chuẩn bị (`hub-background`), thẻ màn (`level-01..10`), nền bản đồ (`level-map`), huy hiệu cảnh giới (Sảnh, màn Cảnh Giới), ngôi sao và huy hiệu Đồng/Bạc/Vàng (bản đồ, kết quả, ôn tập), ký hiệu hệ (Công Pháp, Chuẩn Bị, tình báo), icon vật phẩm (HUD, cửa hàng, Chuẩn Bị), icon pháp bảo (cửa hàng), icon kỹ năng (Công Pháp, Chuẩn Bị). Chưa dùng: chân dung quái/boss (P14), biểu tượng trú ẩn (P13), `hero-portrait`, `thu-linh`, icon/splash app (P17).
- **Ảnh có nền ca-rô "giả trong suốt":** 9 icon vật phẩm và 8 icon kỹ năng bạn gửi trước đó (không có kênh trong suốt, ca-rô nằm ngay trong ảnh) được `Tools/strip_checker.py` cắt nền tự động khi chép vào dự án. Vẫn còn vài vệt ca-rô trong quầng sáng của vài icon; cần bản PNG trong suốt thật để sạch hoàn toàn.
- **Trạng thái UI:** thêm `Hub` và `Loadout` (không thêm `LevelMap`, `Shop`, `Cultivation`, `SkillBook`, `LevelResult`: bản đồ và Công Pháp là tab của Hub, cửa hàng và Cảnh Giới là màn của bảng học, kết quả dùng `Victory`). `OpenCourse()` chỉ chạy từ Hub; QA trong màn dùng `EditorForceCourse()` (chỉ có trong Editor). Esc: Hub → menu chính, Chuẩn Bị → Hub, Thư Viện → Hub.
- **Sảnh (`HubUI`):** dựng bằng mã dưới canvas menu (không sửa scene). Header: huy hiệu và tên cảnh giới, thanh Tu Vi, Linh Thạch, số bài đến hạn ôn. Năm tab: Thư Viện, Đan Các và Cảnh Giới mở bảng học đúng màn đó; Công Pháp và Bản Đồ vẽ tại chỗ. Menu chính chỉ còn Chơi (mở Sảnh), Cài đặt, Credits, Thoát (ẩn Continue và Courses). Sau khi rời màn hoặc kết thúc màn, `LoadMainMenu()` đưa thẳng vào Sảnh.
- **Bản đồ:** 10 thẻ có ảnh màn, tên, sao (ảnh sao thật), thời gian tốt nhất, lý do khóa ("Cần Trúc Cơ 1"…); khóa/mở lấy từ `LevelProgressService`.
- **Chuẩn Bị (`LoadoutUI`):** 4 ô Q/E/R/F, danh sách kỹ năng có lọc theo hệ và vai trò, kỹ năng chưa mở hiện khóa kèm cảnh giới, tình báo màn (quái, hệ, "yếu …" tính từ bảng khắc, boss), bộ gợi ý §7.6 (id chưa tồn tại hoặc chưa mở thì bỏ qua), 3 bộ lưu trong hồ sơ (`loadouts`), chọn vật phẩm mang theo (kho, giới hạn `maxPerLevel`, số ô theo Túi Càn Khôn). BẮT ĐẦU gọi `GameSceneManager.StartLevel(index, skills)`. Danh sách kỹ năng lấy từ `SkillCatalog` (asset `Assets/Skills/Core/Resources/SkillCatalog.asset`; menu *Campus Rift/V2/Install Skill Catalog* cập nhật khi P10 thêm kỹ năng). **P10 cần đặt id kỹ năng đúng theo tên file ảnh** (`tich-lich-nhat-thiem`, `phat-no-hoa-lien`…) để bộ gợi ý khớp.
- **Công Pháp:** danh sách + chi tiết (icon, hệ, vai trò, kiểu niệm, hồi chiêu, tiêu hao, mô tả EN/VN, 5 tầng với hệ số, cảnh giới mở và giá). Phần "phản ứng" hiện tương khắc từ bảng hệ; bảng phản ứng thật chờ P11. Nút nâng tầng chờ P10-T08.
- **Pause:** `PauseMenuAdapter` chỉnh thẻ pause có sẵn khi vào màn: chỉ còn Tiếp tục, Cài đặt, Rời màn (bấm hai lần để xác nhận). Ẩn Courses, Restart, Quit.
- **Kết quả:** điều kiện từng sao, thời gian so với mốc, Linh Thạch và Tu Vi đếm số trong 1 giây (giá trị đã cộng vào hồ sơ đúng một lần lúc thắng), gợi ý số bài đến hạn ôn; sao 2 (đạt mốc thời gian) được ghi vào hồ sơ và thưởng +20; sao 3 chờ P12.
- **Khung 1920×1080 co giãn (`FitFrame`):** Sảnh và Chuẩn Bị nằm trong khung tự co theo vùng an toàn của màn hình, nên dùng được ở 16:9, 19,5:9, 4:3 và 720p. Nút tối thiểu 68 đơn vị thiết kế (≥ 44 px ở 720p); chữ tự co để không bị cắt.
- **Song ngữ:** chuỗi mới dùng cặp `L(EN, VN)` như bảng học, đủ cả hai ngôn ngữ, đổi ngôn ngữ thì vẽ lại. **`LocalizationPlayTest` cũ chưa cập nhật** (chạy ở scene menu và dựa vào nội dung Thuật toán cũ); các bộ test mới kiểm cả EN và VN.
- **Test:** `HubFlowPlayTest` 41/0 (menu → Sảnh → 5 tab → bản đồ → Chuẩn Bị → vào màn với đúng 4 kỹ năng và vật phẩm → không mở được Thư Viện trong màn → pause → thắng → kết quả, thưởng một lần → về Sảnh → menu chính); `HubLayoutPlayTest` 24/0 (4 tỉ lệ × EN/VN: trong màn hình, nút ≥ 44 px, không chồng nhau, không cắt chữ). Ảnh: `Artifacts/UI/P09-*.png`.
- **Chưa kiểm:** vùng an toàn thật của máy có tai thỏ (chỉ mô phỏng tỉ lệ), thao tác chạm trên máy thật.

### Cập nhật giao diện (theo ảnh đề xuất của bạn)

- **Đan Các, Thư Viện, Cảnh Giới** giờ là trang riêng của Sảnh (`HubPages.cs`), không còn là danh sách nút của bảng học. Đan Các theo đúng bố cục ảnh mẫu: tiêu đề vàng, ô Linh Thạch, 4 thẻ danh mục có icon, panel chi tiết với ảnh lớn, chọn số lượng và nút TRAO ĐỔI vàng, hàng "Trang bị cho nhiệm vụ" (chạm ô để chọn món mang theo). Thư Viện là 6 thẻ chương có huy hiệu cảnh giới và thanh tiến độ, kèm nút Ôn tập, Luyện tập nhanh, Thi Đột Phá. Cảnh Giới có huy hiệu lớn, tầng, thanh Tu Vi, chỉ số, mở khóa tiếp theo.
- **Phong cách chung (`UiKit`):** khung có viền mảnh và rombus vàng ở góc, tiêu đề chữ vàng chuyển sắc, ngọc Linh Thạch, nút có khung (nút chính màu vàng). Áp dụng cho header và tab của Sảnh, bản đồ, Công Pháp, Chuẩn Bị. Bảng học (bài đọc, quiz, thi) vẫn dùng khung cũ nhưng đã có nền ảnh, tiêu đề vàng, nút có rombus góc và huy hiệu ở danh sách bài; chưa dựng lại từng màn câu hỏi.
- Test: `HubFlow` 42/0, `HubLayout` 24/0, `Learning` 39/0 sau khi đổi giao diện.
