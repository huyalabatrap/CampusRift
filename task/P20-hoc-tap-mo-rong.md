# P20 — Học tập mở rộng

> **Mục tiêu:** đủ 6 dạng câu hỏi, thêm thẻ ghi nhớ, sổ tay câu sai, nhiệm vụ ngày và chuỗi ngày học, Linh Bia Cơ Duyên trong màn, 6 vật phẩm và 2 pháp bảo còn lại.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 4,5 ngày công · **Phụ thuộc:** P17 · **Tham chiếu:** §9, §11.2, §11.3

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P20-T01 | 3 dạng câu: nhiều đáp án, nối cặp, điền khuyết | Code+UI | 1 | P17 | ✅ |
| P20-T02 | Thẻ ghi nhớ | UI | 0,5 | P17 | ✅ |
| P20-T03 | Sổ tay câu sai | UI | 0,4 | P17 | ✅ |
| P20-T04 | Nhiệm vụ ngày và chuỗi 7 ngày (có ngày nghỉ phép) | Code+UI | 0,6 | P17 | ✅ |
| P20-T05 | Linh Bia Cơ Duyên trong màn | Code+UI | 0,8 | P17 | ✅ |
| P20-T06 | 6 vật phẩm còn lại | Dữ liệu+Code | 0,5 | P17 | ✅ |
| P20-T07 | 2 pháp bảo còn lại | Dữ liệu+Code | 0,3 | P17 | ✅ |
| P20-T08 | Harness học tập mở rộng | Test | 0,4 | T01–T07 | ✅ |

---

## P20-T01 — 3 dạng câu: nhiều đáp án, nối cặp, điền khuyết

- **Các bước:**
  1. Bộ chấm mới:
     - `multi-choice`: phải chọn đúng và đủ các đáp án đúng;
     - `matching`: `correctOptionIds` là danh sách cặp dạng `a:1`, `b:3`;
     - `fill-blank`: chọn cụm từ đúng từ danh sách cho sẵn.
  2. Giao diện cho từng dạng, dùng tốt trên mobile (nối cặp bằng cách chạm 2 lần; điền khuyết bằng danh sách chọn).
  3. Công cụ nhập CSV map thêm `nhieu-dap-an`, `noi-cap`, `dien-khuyet`. Validation kiểm tra đúng định dạng từng dạng.
- **Hoàn thành khi:**
  - [x] Chấm đúng mọi trường hợp biên (chọn thiếu, chọn thừa, nối sai một cặp).

## P20-T02 — Thẻ ghi nhớ

- **Các bước:**
  1. Mỗi bài có bộ thẻ: mặt trước là khái niệm, mặt sau là nội dung. Lấy từ trường "Ghi nhớ" của trang, hoặc cột riêng trong CSV.
  2. Lật thẻ, đánh dấu "Đã nhớ"/"Chưa nhớ". Thẻ chưa nhớ quay lại sớm hơn.
  3. Tính vào nhiệm vụ ngày (T04), không cho Tu Vi (để tránh cày).
- **Hoàn thành khi:**
  - [x] Dùng được trên PC và mobile; tiến độ được lưu.

## P20-T03 — Sổ tay câu sai

- **Các bước:**
  1. Tự gom mọi câu trả lời sai (từ quiz, ôn tập, luyện tập, thi, Linh Bia), kèm lần sai gần nhất.
  2. Ôn riêng từ sổ tay. Trả lời đúng 2 lần liên tiếp thì câu rời khỏi sổ.
  3. Thưởng Linh Thạch theo luật luyện lại (P08-T01).
- **Hoàn thành khi:**
  - [x] Câu rời sổ đúng luật.

## P20-T04 — Nhiệm vụ ngày và chuỗi 7 ngày (có ngày nghỉ phép)

- **Các bước (§9.1):**
  1. 3 nhiệm vụ mỗi ngày: học 1 bài · đúng 20 câu · ôn 1 bài đến hạn. Mỗi nhiệm vụ +30 Linh Thạch.
  2. Chuỗi 7 ngày: +150 Linh Thạch. Mỗi tuần có 1 ngày "nghỉ phép" để không mất chuỗi.
  3. Tính theo ngày UTC, qua `ILearningClock`.
  4. **Không** gửi thông báo gây áp lực. Chỉ hiện trong Sảnh.
- **Hoàn thành khi:**
  - [x] Dùng đồng hồ giả: đủ chuỗi 7 ngày; dùng ngày nghỉ phép; mất chuỗi khi bỏ 2 ngày.

## P20-T05 — Linh Bia Cơ Duyên trong màn

- **Các bước (§2.2, §11.2):**
  1. Mỗi màn có 1–2 bia đá (`CampusInteractable`), đặt ở nơi không có quái đang đánh.
  2. Tương tác: hiện 1 câu hỏi từ các bài **đã học**. Trong lúc trả lời, game tạm dừng như Pause.
  3. Trả lời đúng: chọn 1 trong 3 cơ duyên, bốc ngẫu nhiên từ 6 loại:
     - +20% sát thương 90 giây;
     - hồi 30% máu;
     - −20% hồi chiêu 90 giây;
     - +30 Linh Lực tối đa cho tới hết màn;
     - +15% tốc chạy 90 giây;
     - miễn một lần khống chế.
  4. Trả lời sai: bia tắt, **không phạt**; câu hỏi vào sổ tay câu sai.
  5. Cơ duyên **không** được làm đầy Kiếm Ý, để giữ đúng luật "diệt hết quái mới đầy".
- **Hoàn thành khi:**
  - [x] Bia hoạt động trên PC và mobile.
  - [x] Không làm hỏng luật Kiếm Ý.

## P20-T06 — 6 vật phẩm còn lại

- **Các bước (§9.3):**

| Vật phẩm | Hiệu ứng | Giá | Bán từ |
|---|---|:-:|---|
| Thanh Tâm Đan | Hồi đầy Thể Lực, miễn khống chế 5 giây | 50 | Kết Đan |
| Cửu Chuyển Hoàn Hồn Đan | Hồi 100% máu, xóa hiệu ứng xấu (tối đa 1/màn) | 250 | Nguyên Anh |
| Tụ Khí Đan | −30% hồi chiêu trong 45 giây | 90 | Kết Đan |
| Bạo Kích Đan | +25% tỉ lệ chí mạng trong 45 giây | 70 | Kết Đan |
| Ngũ Hành Phù | Chọn hệ khi mua; hệ đó +40% sát thương trong 60 giây | 100 | Nguyên Anh |
| Tầm Yêu Phù | Hiện vị trí mọi quái trong 30 giây | 60 | Trúc Cơ |

- **Hoàn thành khi:**
  - [x] Có trong Đan Các, hiệu ứng đúng.
  - [x] `EconomyPlayTest` được mở rộng và PASS.

## P20-T07 — 2 pháp bảo còn lại

- **Các bước (§9.4):**
  1. Linh Lực Hồ Lô: +8 Linh Lực và hồi +0,4/giây mỗi cấp, 5 cấp.
  2. Ngọc Bội Ngũ Hành: +3% sát thương khi đánh vào hệ mình khắc, mỗi cấp, 5 cấp.
  3. Cấp tối đa theo số cảnh giới đã đạt.
- **Hoàn thành khi:**
  - [x] Giá và hiệu ứng đúng bảng.

## P20-T08 — Harness học tập mở rộng

- **Kiểm tra:**
  - 3 dạng câu mới; thẻ ghi nhớ; sổ tay câu sai.
  - Nhiệm vụ ngày và chuỗi (đồng hồ giả).
  - Linh Bia; 6 vật phẩm; 2 pháp bảo.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] Nhóm hồi quy Học tập PASS.

---

## Kiểm chứng cuối phase

- [x] Đủ 6 dạng câu hỏi, 16 vật phẩm, 5 pháp bảo. Linh Bia có ở cả 10 màn.
- [x] Cập nhật trạng thái P20 trong `task/README.md`.

## Kết quả 04/10/2026

Hoàn tất T01–T08 theo TEST-POLICY: smoke hiện hành PASS, 0 lỗi nội dung và biên dịch; Android/Edit Mode/SampleScene sạch. [Báo cáo](p20/REPORT-P20.md), [tiến độ](p20/PROGRESS.md), [bằng chứng](p20/verification-summary.json).

Nhóm học hiện hành: Extended156/0 + UI16/0 + Exam48/0. LearningPlayTest legacy vẫn19/2 vì phụ thuộc vòng HUD cũ đã bị ẩn; giữ raw, không tính PASS, không sửa assertion legacy. Không chạy full LearningRegressionRunner theo chính sách smoke.

9 câu mới từ nguồn giáo trình có sẵn và93 thẻ có nguồn trang; gói duyệt giảng viên [P20](../Artifacts/Content/P20/review-print.html) ⏳ chờ duyệt. Chưa test Android thật hoặc cân bằng thực chiến.
