# P21 — Cự thú và điện ảnh đầy đủ

Hoàn tất triển khai và smoke04/10/2026 theo [TEST-POLICY](TEST-POLICY.md): Cinematic40/0, HeavenSword41/0, SkyBeast58/0,24ảnh đã kiểm. [Báo cáo P21](p21/REPORT-P21.md). Các checkbox chốt theo smoke Editor/Android target; chưa chạy APK/thiết bị Android hoặc full regression.

> **Mục tiêu:** 3 cự thú có model riêng, cảnh lộ mặt Hỏa Long Vương, cảnh diễn Thiên Kiếm có góc máy riêng cho từng màn, cảnh kết game, nhạc riêng.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 4,5 ngày công · **Phụ thuộc:** P17 · **Tham chiếu:** §4.1, §5.3, §12

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P21-T01 | 3 model cự thú riêng, có animation | Asset | 1,5 | P17 | ✅ smoke |
| P21-T02 | Cảnh lộ mặt Hỏa Long Vương (cuối màn 7) | Asset+Code | 0,5 | T01 | ✅ smoke |
| P21-T03 | Thiên Kiếm dựng bằng Timeline, góc máy theo màn | Asset | 0,8 | T01 | ✅ smoke |
| P21-T04 | Cảnh kết game và danh sách thực hiện | Asset+UI | 0,8 | T03 | ✅ smoke |
| P21-T05 | Nhạc riêng cho boss và cự thú | Asset | 0,5 | — | ✅ smoke |
| P21-T06 | Harness và hồi quy phần điện ảnh | Test | 0,4 | T01–T05 | ✅ smoke |

---

## P21-T01 — 3 model cự thú riêng, có animation

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (3 model cự thú riêng có animation) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Các bước:**
  1. Ba model riêng:
     - **Xích Hỏa Giao:** giao long thân rắn, có sừng, vảy đỏ.
     - **Tà Hóa Chu Tước:** chim lửa bị tà hóa; cánh cháy, lông đen đỏ.
     - **Cửu U Hỏa Long Vương:** rồng lửa chín sừng, to nhất, có vương miện lửa.
  2. Mỗi model có animation: bay lượn, vọt lên, gầm, phun lửa, trúng Thiên Kiếm, rơi và tan.
  3. Nguồn có giấy phép phù hợp, hoặc đặt làm riêng. Ghi LICENSES. Giữ LOD để chạy được trên mobile.
  4. Thay 3 biến thể tạm của P14-T01; không đổi dữ liệu `SkyBeastDefinition` ngoài trường prefab.
- **Hoàn thành khi:**
  - [x] 3 cự thú nhận ra ngay từ hình bóng.
  - [x] Animation khớp với chu kỳ Thiên Hỏa.

## P21-T02 — Cảnh lộ mặt Hỏa Long Vương (cuối màn 7)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng cảnh lộ mặt rồng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Các bước:**
  1. Thay cảnh tạm của P12-T06 bằng cảnh 6–8 giây:
     - Shaban tan biến;
     - Khe Nứt trên trời mở rộng;
     - Hỏa Long Vương thò đầu qua Khe Nứt, gầm;
     - bầu trời đổi màu.
  2. Có phụ đề EN/VN; bỏ qua được; lần sau rút ngắn.
- **Hoàn thành khi:**
  - [x] Cảnh chạy mượt trên PC và Android; tôn trọng tùy chọn giảm nhấp nháy.

## P21-T03 — Thiên Kiếm dựng bằng Timeline, góc máy theo màn

- **Các bước:**
  1. Chuyển cảnh Thiên Kiếm (P15-T04) sang Timeline (package `com.unity.timeline` đã có).
  2. Mỗi màn 8, 9, 10 có góc máy riêng. Nhát cuối màn 10 dài hơn và hoành tráng hơn.
  3. Giữ luật: lần đầu bản đầy đủ, các lần sau bản ngắn, bỏ qua được; không đổi `Time.timeScale`.
- **Hoàn thành khi:**
  - [x] Hành vi giống hệt P15-T04 về gameplay; chỉ khác phần hình ảnh.

## P21-T04 — Cảnh kết game và danh sách thực hiện

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (nhạc kết thúc, âm thanh bình minh) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Các bước:**
  1. Sau nhát cuối màn 10: Khe Nứt khép lại, bình minh lên trên khuôn viên, nhân vật chính đứng giữa sân (khoảng 20 giây, bỏ qua được).
  2. Màn danh sách thực hiện, gồm cả ghi công các tài nguyên CC-BY (lấy từ các file `LICENSES.md`).
  3. Mở khóa danh hiệu "Phá Rift" và gợi ý các chế độ hậu kết (P22).
- **Hoàn thành khi:**
  - [x] Ghi công đầy đủ mọi tài nguyên CC-BY.

## P21-T05 — Nhạc riêng cho boss và cự thú

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (nhạc boss và nhạc cự thú) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Các bước:** mỗi boss (Shaban màn 5, màn 7) và mỗi cự thú có nhạc riêng; chuyển nhạc khi đổi pha. Nguồn có giấy phép phù hợp.
- **Hoàn thành khi:**
  - [x] Nhạc chuyển mượt khi đổi pha; âm lượng tôn trọng Settings.

## P21-T06 — Harness và hồi quy phần điện ảnh

- **Kiểm tra:**
  - Mọi cảnh diễn: bỏ qua được, không khóa input sau khi kết thúc, không làm lệch chu kỳ Thiên Hỏa.
  - Cảnh không chạy khi game đang pause.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] `HeavenSwordPlayTest` và `SkyBeastPlayTest` PASS.

---

## Kiểm chứng cuối phase

- [x] Chơi màn 7–10 có đầy đủ cảnh diễn và nhạc.
- [x] Cập nhật trạng thái P21 trong `task/README.md`.
