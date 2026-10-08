# P22 — Hậu kết (Tháp Thí Luyện, Ác Mộng, thành tựu)

> **Mục tiêu:** có lý do chơi tiếp sau màn 10 mà không phá luật "học để thắng". Phần thưởng chủ yếu là danh hiệu, trang phục, kỷ lục; Linh Thạch có trần.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 4,5 ngày công · **Phụ thuộc:** P18, P19 · **Tham chiếu:** §10, §12

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P22-T01 | Tháp Thí Luyện: tầng vô tận | Code+Dữ liệu | 1,5 | P19 | ✅ smoke |
| P22-T02 | Chế độ Ác Mộng cho màn 1–10 | Code | 0,8 | P19 | ✅ smoke |
| P22-T03 | Thành tựu và danh hiệu | Code+UI | 0,8 | P17 | ✅ smoke |
| P22-T04 | Bảng kỷ lục cục bộ | UI | 0,5 | T01, T02 | ✅ smoke |
| P22-T05 | Harness hậu kết | Test | 0,5 | T01–T04 | ✅ smoke |
| P22-T06 | Trang phục mở bằng thành tích | Asset | 0,4 | T03 | ✅ smoke |

---

## P22-T01 — Tháp Thí Luyện: tầng vô tận

- **Các bước:**
  1. Mở khi đã qua màn 10.
  2. Mỗi tầng là một đợt quái sinh theo công thức từ các archetype:
     - máu và sát thương +8% mỗi tầng;
     - mỗi 5 tầng có tinh anh; mỗi 10 tầng có boss (Shaban);
     - dùng lại các vùng khe nứt của 10 màn.
  3. Mỗi tuần một luật riêng, đổi luân phiên theo tuần UTC, ví dụ: "Hỏa Nguyệt" (quái Hỏa +30%), "Lôi Kiếp" (sét giáng ngẫu nhiên), "Tĩnh Lặng" (không có Tầm Yêu).
  4. Thưởng:
     - danh hiệu theo mốc tầng;
     - Linh Thạch **tối đa 100/ngày**, để việc học vẫn là nguồn chính (§10).
  5. Lưu tầng cao nhất vào hồ sơ.
- **Hoàn thành khi:**
  - [ ] Leo được 30 tầng liên tục mà không lỗi. **Bỏ lượt chơi dài theo TEST-POLICY**: smoke runtime tầng 1→5 và tầng30/Shaban; kiểm công thức tầng1→30, chưa leo30tầng liên tục.
  - [x] Trần Linh Thạch hoạt động.

## P22-T02 — Chế độ Ác Mộng cho màn 1–10

- **Các bước:**
  1. Mở từng màn ở chế độ Ác Mộng sau khi màn đó đạt ★★★.
  2. Quái +50% chỉ số, mỗi tinh anh 2 phụ tố, Thiên Hỏa ngắn hơn 20%.
  3. Thưởng: huy hiệu riêng và trang phục. Linh Thạch dùng chung trần của Tháp.
- **Hoàn thành khi:**
  - [x] Hệ số được áp đúng.
  - [x] Sao Ác Mộng được lưu riêng, không ghi đè sao thường.

## P22-T03 — Thành tựu và danh hiệu

- **Các bước:**
  1. Khoảng 20 thành tựu, chia 3 nhóm:
     - **Học tập:** ví dụ "Bác Học" (Vàng mọi bài), "Kiên Trì" (chuỗi 30 ngày);
     - **Chiến đấu:** ví dụ "Kiếm Tiên" (★★★ cả 10 màn), "Ngũ Hành Viên Mãn" (kích hoạt đủ 9 phản ứng);
     - **Khám phá:** ví dụ "Người Gác Trời" (không trúng Thiên Hỏa ngoài trời trong cả màn 8–10).
  2. Danh hiệu hiển thị ở header Sảnh. Chỉ mang tính thẩm mỹ, không cộng chỉ số.
  3. Tên thành tựu và danh hiệu tuân thủ §11.5: không dùng tên, trích dẫn hay khái niệm của môn học.
- **Hoàn thành khi:**
  - [x] Mỗi thành tựu có điều kiện kiểm chứng được và có test.

## P22-T04 — Bảng kỷ lục cục bộ

- **Các bước:**
  1. Thời gian tốt nhất của từng màn (thường và Ác Mộng) và tầng Tháp cao nhất.
  2. Chỉ lưu trên máy. Bảng xếp hạng online cho lớp học là tùy chọn sau này; phải xem lại quy định pháp lý (§11.5 và Nghị định 147/2024/NĐ-CP về game có tương tác qua máy chủ) trước khi làm.
- **Hoàn thành khi:**
  - [x] Kỷ lục cập nhật đúng; không cần kết nối mạng.

## P22-T05 — Harness hậu kết

- **Kiểm tra:**
  - Công thức sinh tầng của Tháp; luật tuần; trần Linh Thạch.
  - Hệ số Ác Mộng.
  - Điều kiện các thành tựu.
- **Hoàn thành khi:**
  - [x] PASS.

## P22-T06 — Trang phục mở bằng thành tích

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model/texture trang phục, hiệu ứng phi kiếm) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Các bước:** 3–5 bộ trang phục hoặc hiệu ứng phi kiếm (đổi màu, đổi vệt sáng) mở bằng thành tựu hoặc Ác Mộng. Không bán bằng tiền thật (§10).
- **Hoàn thành khi:**
  - [x] Đổi được trang phục trong Sảnh; hiển thị đúng trong màn.

---

## Kiểm chứng cuối phase

- [x] Sau màn 10 còn Tháp, Ác Mộng và thành tựu để chơi tiếp. Học vẫn là nguồn Linh Thạch chính.
- [x] Cập nhật trạng thái P22 trong `task/README.md`.

## Bàn giao P22 · 04/10/2026

Đã triển khai T01–T06 ở mức smoke theo TEST-POLICY. [Báo cáo](p22/REPORT-P22.md) · [Tiến độ](p22/PROGRESS.md). Endgame112/0; visual9/0; luồng Sảnh/Chuẩn Bị/NextFloor đạt; 9ảnh ComicTextAudit0. Tháp dùng roster11 loại, tăng8%/tầng tới x5, tổng tối đa32 đã gồm boss. Tháp/Ác Mộng chung100LinhThạch/ngàyUTC; không thưởng TuVi hậu kết. 20thành tựu, danh hiệu thẩm mỹ,4biến thể màu trên rig hiện có. **Chưa đo cân bằng thực chiến**, không test thiết bị/native/full regression.
