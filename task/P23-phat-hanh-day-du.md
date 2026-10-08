# P23 — Phát hành bản đầy đủ → **Mốc 6**

> **Mục tiêu:** cân bằng lần cuối, duyệt nội dung lần cuối, trợ năng, hiệu năng, hồi quy toàn bộ, build và lưu trữ phiên bản.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P18–P22 · **Tham chiếu:** §11.5, §16, §18

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P23-T01 | Cân bằng cuối dựa trên telemetry | Dữ liệu | 0,8 | P22 | ⬜ |
| P23-T02 | Duyệt nội dung lần cuối | Nội dung | 0,3 | P20 | ⬜ |
| P23-T03 | Trợ năng: phụ đề, cỡ chữ, người mù màu | UI | 0,8 | P22 | ⬜ |
| P23-T04 | Hiệu năng cuối (PC và Android) | Code | 0,5 | P22 | ⬜ |
| P23-T05 | Hồi quy toàn bộ | Test | 0,4 | T01–T04 | ⬜ |
| P23-T06 | Build, ghi chú phát hành, lưu trữ phiên bản | Công cụ | 0,7 | T05 | ⬜ |

---

## P23-T01 — Cân bằng cuối dựa trên telemetry

- **Các bước:**
  1. Gom telemetry từ người chơi thử (P17-T02) cho đủ 10 màn, Tháp và Ác Mộng.
  2. Kiểm tra lại các mục tiêu §16:
     - quái thường chết sau 2–3 nhát; boss trong khoảng thời gian mục tiêu;
     - thời lượng màn;
     - tỉ lệ đúng câu hỏi 60–80%;
     - "1 buổi học ≈ 1 lượt thử màn khó".
  3. Chỉnh dữ liệu và ghi lý do vào `Artifacts/V2/Balance-Final.md`.
- **Hoàn thành khi:**
  - [ ] Mọi chỉ số nằm trong khoảng mục tiêu, hoặc được chấp nhận có lý do.

## P23-T02 — Duyệt nội dung lần cuối

- **Các bước:**
  1. Giảng viên soát lại toàn bộ nội dung, gồm cả phần thêm ở P20: thẻ ghi nhớ, dạng câu mới.
  2. Soát tên thành tựu, danh hiệu, trang phục theo §11.5.
  3. Lưu biên bản vào `Artifacts/Content/Review-Final-<ngày>.md`.
- **Hoàn thành khi:**
  - [ ] Có xác nhận cuối cùng.

## P23-T03 — Trợ năng: phụ đề, cỡ chữ, người mù màu

- **Các bước:**
  1. Phụ đề cho mọi lời thoại và cảnh diễn; tùy chọn cỡ chữ (3 mức) cho HUD và Thư Viện.
  2. **Người mù màu:** mỗi hệ có **ký hiệu hình** riêng, không chỉ phân biệt bằng màu. Áp cho số sát thương, icon kỹ năng, tình báo màn.
  3. Giữ các tùy chọn giảm rung và giảm nhấp nháy của P17-T04; mở rộng cho cảnh diễn P21.
  4. Tùy chọn thời gian trả lời câu hỏi ×1,5 cho người đọc chậm. Áp cho quiz và Linh Bia; **không** áp cho Thi Đột Phá, để công bằng.
- **Hoàn thành khi:**
  - [ ] Chơi được toàn bộ game khi bật chế độ mù màu.
  - [ ] Mọi cảnh diễn có phụ đề.

## P23-T04 — Hiệu năng cuối (PC và Android)

- **Các bước:** profile lại sau khi có đủ quái, tinh anh, cự thú và cảnh diễn mới. Giữ mục tiêu 30 FPS trên Android tầm trung, 60 FPS trên PC tầm trung. Ghi vào `Artifacts/V2/Perf-Final.md`.
- **Hoàn thành khi:**
  - [ ] Đạt mục tiêu, hoặc ghi rõ các giới hạn còn lại.

## P23-T05 — Hồi quy toàn bộ

- **Các bước:** chạy toàn bộ harness cũ và harness V2 (P01–P22). Viết `Artifacts/V2/Final-Regression.md`, so với baseline P00-T02 và báo cáo MVP P17-T08.
- **Hoàn thành khi:**
  - [ ] Không có FAIL mới.

## P23-T06 — Build, ghi chú phát hành, lưu trữ phiên bản

- **Các bước:**
  1. Build Android và Windows; kiểm tra nhanh trên máy thật, giống P17-T09.
  2. Ghi chú phát hành đầy đủ; ghi công tài nguyên.
  3. Sao lưu toàn bộ dự án ở phiên bản phát hành vào `Backups/V2-Release-<ngày>/`, hoặc gắn tag nếu dự án đã dùng git.
- **Hoàn thành khi:**
  - [ ] Hai bản build chạy được.
  - [ ] Có bản lưu trữ phiên bản.

---

## Kiểm chứng cuối phase → Mốc 6

- [ ] Bản đầy đủ: 10 màn, 21 kỹ năng, 9 loại quái, 3 cự thú, 16 vật phẩm, 5 pháp bảo, 6 dạng câu hỏi, hậu kết, nội dung đã duyệt.
- [ ] Cập nhật trạng thái P23 và Mốc 6 trong `task/README.md`.
