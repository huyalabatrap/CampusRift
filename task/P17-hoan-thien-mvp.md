# P17 — Hoàn thiện và phát hành thử MVP → **Mốc 5**

> **Mục tiêu:** hướng dẫn người mới, telemetry cục bộ, cân bằng với người chơi thật, hoàn thiện âm thanh/VFX, hiệu năng mobile, giảng viên duyệt nội dung, dọn tính năng DEV, hồi quy toàn bộ, build.
>
> **Phạm vi:** MVP · **Ước lượng:** 5 ngày công · **Phụ thuộc:** P00–P16 · **Tham chiếu:** §11.5, §15.2, §16, §18
>
> **Kết quả (Mốc 5):** bản MVP chạy trên Android và Windows, có nội dung thật đã được duyệt.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P17-T01 | Hướng dẫn người mới (màn 1 và Sảnh) | UI | 0,6 | P16 | ✅ code/smoke; chờ novice thật |
| P17-T02 | Telemetry cục bộ (người chơi đồng ý mới bật) | Code | 0,4 | P16 | ✅ |
| P17-T03 | Cân bằng với 3–5 người chơi thử | Dữ liệu | 0,8 | T02 | ⏳ đã chuẩn bị, chờ người dùng |
| P17-T04 | Âm thanh, VFX, tùy chọn giảm rung và nhấp nháy | Asset | 0,6 | P16 | ✅ code/smoke |
| P17-T05 | Hiệu năng trên điện thoại Android thật | Code | 0,7 | P16 | ⏳ Editor đã đo, chờ thiết bị |
| P17-T06 | Giảng viên duyệt nội dung | Nội dung | 0,4 | P07-T10 | ⏳ gói duyệt đã xuất |
| P17-T07 | Dọn bản phát hành: tắt DEV, chặn nội dung giữ chỗ | Code | 0,3 | T06 | ✅ guard/gate/package verification |
| P17-T08 | Hồi quy toàn bộ và báo cáo | Test | 0,5 | T01–T07 | ✅ 71 nhóm, giữ raw FAIL/giới hạn |
| P17-T09 | Build Android và Windows, kiểm tra nhanh | Công cụ | 0,7 | T08 | ✅ 4 build + Windows; ⏳ Android thật |

Phần AI hoàn tất ngày 03/10/2026: [REPORT-P17](p17/REPORT-P17.md), [hồi quy](../Artifacts/V2/Regression-MVP.md), [build](../Artifacts/P17-Builds/BUILD-INDEX.md). T03/T05/T06 và tiêu chí novice T01 còn chờ người; chưa xác nhận Mốc 5. Không tích “không có FAIL mới”: raw hồi quy còn FAIL, dù smoke phần sửa/triage đạt. Các giới hạn được ghi cho thử nội bộ, chưa thay xác nhận nghiệm thu.

---

## P17-T01 — Hướng dẫn người mới (màn 1 và Sảnh)

- **Loại:** UI · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P16
- **File:** (mới) `Assets/CampusRiftUI/Runtime/TutorialDirector.cs`, `TutorialStep` (dữ liệu)
- **Các bước:**
  1. **Lần đầu vào Sảnh:** hướng dẫn Thư Viện → học bài 1 → nhận Tu Vi và Linh Thạch → Đan Các (mua 1 Hồi Khí Đan) → Bản Đồ → Chuẩn Bị.
  2. **Màn 1:** gợi ý theo ngữ cảnh: di chuyển → đánh thường → né khi quái vung đòn → Đại Thủ Ấn → dùng vật phẩm → Tầm Yêu.
  3. Gợi ý đúng thiết bị (PC hoặc mobile). Bỏ qua được. Tiến độ hướng dẫn lưu trong hồ sơ.
  4. Màn 8 lần đầu: gợi ý về Thiên Hỏa ("Vào nhà!") và Thiên Kiếm ("Ra ngoài trời để triệu hồi").
- **Hoàn thành khi:**
  - [ ] Người chưa từng chơi hoàn thành màn 1 mà không cần ai giải thích.

## P17-T02 — Telemetry cục bộ (người chơi đồng ý mới bật)

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P16
- **File:** (mới) `Assets/Progression/Runtime/LocalTelemetry.cs`, `Assets/Progression/Editor/TelemetrySummary.cs`
- **Các bước:**
  1. Settings có mục "Ghi dữ liệu chơi để cải thiện game", **mặc định tắt**.
  2. Mỗi lượt chơi ghi một dòng JSON vào `persistentDataPath/telemetry/`, gồm: màn, thời gian, số lần chết và nguyên nhân, kỹ năng đã dùng, vật phẩm đã dùng, số lần trúng lửa, phản ứng, sao.
  3. Tổng kết học tập: tỉ lệ đúng theo từng câu (không lưu thông tin cá nhân).
  4. Menu editor tổng hợp nhiều file thành bảng, phục vụ T03.
- **Hoàn thành khi:**
  - [x] Tắt thì không ghi gì.
  - [x] Bật thì ghi đúng; file tổng hợp đọc được.

## P17-T03 — Cân bằng với 3–5 người chơi thử

- **Loại:** Dữ liệu · **Ước lượng:** 0,8 ngày · **Phụ thuộc:** P17-T02
- **Các bước:**
  1. Người chơi thử: sinh viên có học môn Tư tưởng Hồ Chí Minh. Mỗi người chơi 45–60 phút, bắt đầu từ hồ sơ mới.
  2. Quan sát, không hướng dẫn. Ghi lại: chỗ bị kẹt, lần chết đầu, cảm nhận về nhịp học và nhịp chơi.
  3. Chỉnh `LevelDefinition`, `FireBreathProfile`, giá trong `EconomyConfig`, `CultivationTable` theo §16 và số liệu telemetry.
  4. Kiểm tra điều kiện "1 buổi học ≈ 1 lượt thử màn khó" (§9.5).
- **Hoàn thành khi:**
  - [ ] Có báo cáo `Artifacts/V2/Playtest-MVP.md`.
  - [ ] Các thay đổi cân bằng đều được ghi lý do.

Đã tạo biểu mẫu/kịch bản [Playtest-MVP](../Artifacts/V2/Playtest-MVP.md) và công cụ tổng hợp telemetry; chưa có phiên 3–5 sinh viên và không đổi cân bằng bằng số liệu giả.

## P17-T04 — Âm thanh, VFX, tùy chọn giảm rung và nhấp nháy

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh trúng đòn theo hệ, âm thanh UI, nhạc nền theo nhóm màn) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P16
- **Các bước:**
  1. Âm thanh trúng đòn theo hệ; âm thanh UI của Sảnh; nhạc theo nhóm màn (chiều tà, đêm, trời lửa) và nhạc boss.
  2. Rà lại VFX: màu hệ đồng nhất, không che mất vòng báo của quái.
  3. Settings thêm: **giảm rung máy quay** và **giảm nhấp nháy** (sét, lửa, đột phá). Hai tùy chọn này bảo vệ người nhạy cảm ánh sáng.
- **Hoàn thành khi:**
  - [x] Hai tùy chọn giảm rung và giảm nhấp nháy có tác dụng ở mọi hiệu ứng mạnh hiện có (code audit + smoke P17; chờ phản hồi người chơi).

## P17-T05 — Hiệu năng trên điện thoại Android thật

- **Loại:** Code · **Ước lượng:** 0,7 ngày · **Phụ thuộc:** P16
- **Các bước:**
  1. Build APK bằng `Assets/Editor/AndroidApkBuild.cs`, chạy trên một máy tầm trung (RAM khoảng 4 GB). Profile ở màn 7 và màn 10.
  2. Mục tiêu 30 FPS. Các chỗ cần xem:
     - số quái cùng lúc (9);
     - Shaban `BeliefParticles` 160;
     - giảm tần suất AI theo khoảng cách;
     - số hạt Thiên Hỏa và số phi kiếm trong cảnh diễn;
     - draw call.
  3. Ghi số liệu trước và sau vào `Artifacts/V2/Perf-Mobile.md`.
- **Hoàn thành khi:**
  - [ ] Đạt ≥ 30 FPS trung bình ở màn 10 trên máy thử, hoặc ghi rõ các giới hạn còn lại.

## P17-T06 — Giảng viên duyệt nội dung

- **Loại:** Nội dung · **Ước lượng:** 0,4 ngày (chưa tính thời gian chờ) · **Phụ thuộc:** P07-T10
- **Các bước:**
  1. Xuất toàn bộ bài và câu hỏi (kèm nguồn trang) thành file dễ đọc: CSV hoặc bản in.
  2. Soát theo checklist §11.5:
     - đúng giáo trình;
     - tách lớp hư cấu;
     - không dùng tên, trích dẫn hay khái niệm của môn học làm tên kỹ năng, vật phẩm, quái;
     - câu chữ trang trọng.
  3. Sửa theo góp ý, nhập lại, chạy validation.
  4. Lưu biên bản vào `Artifacts/Content/Review-<ngày>.md`: người duyệt, ngày, các điểm đã sửa.
- **Hoàn thành khi:**
  - [ ] Có xác nhận của giảng viên (hoặc người được bạn chỉ định).

## P17-T07 — Dọn bản phát hành: tắt DEV, chặn nội dung giữ chỗ

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P17-T06
- **Các bước:**
  1. Lệnh DEV và harness chỉ có trong Editor (`#if UNITY_EDITOR`). Tắt HUD cũ `PlayerMonsterHealth.showLegacyHUD`. Bỏ phím Respawn.
  2. Build pipeline **dừng build** nếu catalog còn nội dung giữ chỗ (`isPlaceholder`) hoặc validation còn lỗi.
  3. Rà soát không còn đường mua bằng tiền thật hoặc quảng cáo (§10).
- **Hoàn thành khi:**
  - [x] Build thử khi còn nội dung giữ chỗ thì bị chặn, có thông báo rõ.

## P17-T08 — Hồi quy toàn bộ và báo cáo

- **Loại:** Test · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P17-T01 đến T07
- **Các bước:**
  1. Chạy toàn bộ harness cũ (README mục 8) và toàn bộ harness V2 của P01–P16.
  2. Viết `Artifacts/V2/MVP-Regression.md`: PASS/FAIL, so sánh với baseline P00-T02, các vấn đề đã biết.
- **Hoàn thành khi:**
  - [ ] Không có FAIL mới.
  - [ ] Các FAIL còn lại có lý do chấp nhận.

Đã báo cáo đầy đủ 71 nhóm và đối chiếu baseline; lỗi notch mobile đã sửa bằng smoke riêng. Raw FAIL giữ nguyên, lý do/triage và giới hạn thang máy/trú ẩn nằm trong Regression-MVP; hai checkbox nghiệm thu trên vẫn chờ đánh giá người dùng.

## P17-T09 — Build Android và Windows, kiểm tra nhanh

- **Loại:** Công cụ · **Ước lượng:** 0,7 ngày · **Phụ thuộc:** P17-T08
- **Các bước:**
  1. Build APK (có sẵn `AndroidApkBuild`, `Tools/verify_android_apk.py`) và build Windows.
  2. Kiểm tra nhanh trên máy thật:
     - cài đặt, mở game, tạo hồ sơ mới;
     - học 1 bài, mua đồ, chơi màn 1;
     - tắt game, mở lại, kiểm tra dữ liệu còn nguyên.
  3. Viết ghi chú phát hành: tính năng, giới hạn, cách gửi phản hồi.
- **Hoàn thành khi:**
  - [ ] Hai bản build chạy được trên máy thật.
  - [x] Có ghi chú phát hành.

Android và Windows đều có development + release Succeeded/0 lỗi build. Windows native mở Sảnh/màn1, học/mua và reopen QA save đạt; release startup đạt. APK chưa cài vì `adb devices` rỗng, nên tiêu chí cả hai nền tảng máy thật chưa tích.

---

## Kiểm chứng cuối phase → Mốc 5

- [ ] Bản MVP phát hành thử: 10 màn, 10 kỹ năng, 10 vật phẩm, nội dung Tư tưởng Hồ Chí Minh đã duyệt.
- [x] Cập nhật trạng thái P17 và Mốc 5 trong `task/README.md`.
