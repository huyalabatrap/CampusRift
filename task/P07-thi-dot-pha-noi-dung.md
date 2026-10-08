# P07 — Thi Đột Phá, dạng câu hỏi, nhập nội dung

> **Mục tiêu:** có Thi Đột Phá cuối chương, dạng câu Đúng/Sai và Sắp xếp, ôn tập giãn cách, luyện tập nhanh, công cụ nhập nội dung từ CSV, và bộ khung Tư tưởng Hồ Chí Minh thay course Algorithms.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P06 · **Tham chiếu:** §8.3, §11, Phụ lục A
>
> **Kết quả:** học → ôn → thi đột phá → lên cảnh giới. Nội dung thật chỉ cần nhập file CSV là chạy.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P07-T01 | Mô hình dữ liệu: chương, đề thi, nguồn trang | Code | 0,3 | P06 | ✅ |
| P07-T02 | Bộ chấm theo dạng câu: Đúng/Sai, Sắp xếp | Code | 0,4 | T01 | ✅ |
| P07-T03 | Giao diện cho câu Đúng/Sai và Sắp xếp | UI | 0,5 | T02 | ✅ |
| P07-T04 | Luồng Thi Đột Phá | Code+UI | 0,7 | T02 | ✅ |
| P07-T05 | Ôn tập giãn cách, huy hiệu Đồng/Bạc/Vàng | Code | 0,4 | T01 | ✅ |
| P07-T06 | Chế độ luyện tập nhanh | Code+UI | 0,3 | T02 | ✅ |
| P07-T07 | Công cụ nhập CSV và mở rộng validation | Công cụ | 0,5 | T01 | ✅ |
| P07-T08 | Thay catalog: gỡ Algorithms, tạo khung 6 chương | Dữ liệu | 0,2 | T07 | ✅ |
| P07-T09 | Harness học tập và thi | Test | 0,2 | T03–T08 | ✅ |
| P07-T10 | **Nhập nội dung Tư tưởng Hồ Chí Minh thật** | Nội dung | 0,5–2 | T07 | ✅ |

---

## P07-T01 — Mô hình dữ liệu: chương, đề thi, nguồn trang

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P06
- **File:** (sửa) `Assets/Learning/Runtime/CourseData.cs`, `QuestionBankData.cs`, `LessonData.cs`
- **Các bước:**
  1. Quy ước: **`CourseData` = một Chương**; catalog là cả môn học.
  2. `CourseData` thêm: `chapterIndex`, `examBank` (QuestionBankData), `examSize = 20`, `examPassPercent = 80`, `examMinutes = 20`, `isPlaceholder`.
  3. `QuestionData` thêm: `source` (ví dụ "GT 2021, tr. 45"), `chapterId`.
  4. `LessonData` thêm: `isPlaceholder`. Mặc định `quizSize` đổi thành 10.
  5. Bỏ `requiredSurvivalSeconds` khỏi UI và logic, nhưng giữ field để asset cũ không lỗi.
- **Hoàn thành khi:**
  - [x] Compile sạch.
  - [x] Asset cũ vẫn mở được.

## P07-T02 — Bộ chấm theo dạng câu: Đúng/Sai, Sắp xếp

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P07-T01
- **File:** (sửa) `Assets/Learning/Runtime/QuizSession.cs`, `LearningEngine.cs` · (mới) `Assets/Learning/Runtime/Graders.cs`
- **Hiện trạng:** `LearningEngine.StartQuiz` luôn dùng `new SingleChoiceGrader()`; `QuizSession.Answer(string)` chỉ nhận một đáp án.
- **Các bước:**
  1. `GraderRegistry` chọn bộ chấm theo `type`:
     - `single-choice` (có sẵn);
     - `true-false`: 2 lựa chọn, id `true`/`false`;
     - `ordering`: `correctOptionIds` là thứ tự đúng.
  2. `QuizSession`:
     - thêm `Answer(IReadOnlyList<string> selected)`, giữ bản cũ để tương thích;
     - nhận vào pool câu hỏi (danh sách) để dùng lại cho đề thi chương;
     - câu `ordering` **không** xáo đáp án đúng vào kết quả hiển thị.
  3. `AnswerRecord` lưu đáp án đã chọn và đáp án đúng cho cả dạng sắp xếp.
- **Hoàn thành khi:**
  - [x] Validation chấm đúng/sai cho 3 dạng (kể cả sắp xếp sai một vị trí thì tính sai).

## P07-T03 — Giao diện cho câu Đúng/Sai và Sắp xếp

- **Loại:** UI · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P07-T02
- **File:** (sửa) `Assets/Learning/Runtime/LearningUI.cs`
- **Các bước:**
  1. Đúng/Sai: 2 nút lớn.
  2. Sắp xếp:
     - danh sách kéo thả;
     - trên mobile có thêm cách **chạm 2 mục để đổi chỗ**, vì kéo thả dễ nhầm;
     - nút "Xác nhận thứ tự".
  3. Màn xem lại hiện thứ tự người chơi chọn và thứ tự đúng, kèm giải thích và nguồn trang.
  4. Chữ trình bày **trang trọng**, font Be Vietnam Pro, không có hiệu ứng che chữ (§11.5).
- **Hoàn thành khi:**
  - [x] Làm được cả 2 dạng trên PC và mobile.
  - [x] Nội dung dài thì cuộn được.

## P07-T04 — Luồng Thi Đột Phá

- **Loại:** Code+UI · **Ước lượng:** 0,7 ngày · **Phụ thuộc:** P07-T02
- **File:** (mới) `Assets/Learning/Runtime/BreakthroughExam.cs`, `Assets/CampusRiftUI/Runtime/BreakthroughExamUI.cs`
- **Các bước (§8.3):**
  1. Chỉ mở khi `CultivationService` đang ở bình cảnh của cảnh giới tương ứng chương.
  2. Đề: 20 câu ngẫu nhiên từ `examBank` cộng câu của các bài trong chương (ít nhất 40 câu trong pool). Đồng hồ 20 phút tính bằng `unscaledTime`.
  3. Đạt ≥ 80%:
     - `CultivationService.CompleteBreakthrough()`;
     - thưởng 300 Linh Thạch (P08);
     - cảnh diễn đột phá: sét vàng và hào quang quanh khung UI, 3 giây, bỏ qua được.
  4. Trượt: xem lại câu sai kèm giải thích; thi lại sau 30 phút (lưu mốc giờ UTC trong hồ sơ).
  5. Giao diện câu hỏi trang trọng. Hiệu ứng "Độ Kiếp" chỉ nằm ở khung ngoài và **sau khi** đậu.
  6. Thoát giữa chừng thì tính là trượt, không thưởng. Khởi động lại game khi đang thi cũng tính trượt.
- **Hoàn thành khi:**
  - [x] Đậu thì lên cảnh giới.
  - [x] Trượt thì bị khóa 30 phút.
  - [x] Hết giờ thì tự nộp bài.

## P07-T05 — Ôn tập giãn cách, huy hiệu Đồng/Bạc/Vàng

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P07-T01
- **File:** (mới) `Assets/Learning/Runtime/ILearningClock.cs`, `ReviewScheduler.cs`
- **Các bước:**
  1. `ILearningClock`: bản thật dùng `DateTime.UtcNow`; bản giả dùng cho test. Chống chỉnh đồng hồ lùi: nếu thời gian hiện tại nhỏ hơn lần gần nhất đã thấy thì lấy lần gần nhất.
  2. Qua bài lần đầu thì hẹn ôn sau **1 ngày**. Ôn đạt thì lên huy hiệu và hẹn lần kế: Đồng → 3 ngày → Bạc → 7 ngày → Vàng.
  3. Một buổi ôn gồm 5 câu từ bài, đạt ≥ 80%. Mỗi lần đạt thưởng 10% phần Tu Vi của bài (P06-T05). Trượt thì không tụt huy hiệu, hẹn lại sau 1 ngày.
  4. Danh sách "Đến hạn ôn" có badge đếm số lượng ở Thư Viện.
- **Hoàn thành khi:**
  - [x] Dùng đồng hồ giả: đi đủ chuỗi Đồng → Bạc → Vàng.
  - [x] Chỉnh đồng hồ lùi không gian lận được.

## P07-T06 — Chế độ luyện tập nhanh

- **Loại:** Code+UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P07-T02
- **Các bước:**
  1. 10 câu trong 5 phút, trộn từ các bài đã đọc.
  2. Thưởng 2 Tu Vi mỗi câu đúng, tối đa 150/ngày. Thưởng Linh Thạch nối ở P08.
  3. Chỉ tính câu **chưa trả lời đúng trong 24 giờ qua**, để chống cày.
- **Hoàn thành khi:**
  - [x] Trần mỗi ngày và luật 24 giờ hoạt động (kiểm bằng đồng hồ giả).

## P07-T07 — Công cụ nhập CSV và mở rộng validation

- **Loại:** Công cụ · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P07-T01
- **File:** (mới) `Assets/Learning/Editor/CourseCsvImporter.cs`, thư mục `Content/` ở gốc dự án (`Content/lessons.csv`, `Content/questions.csv`, `Content/README.md`) · (sửa) `Assets/Learning/Editor/LearningContentValidation.cs`
- **Các bước:**
  1. Định dạng theo Phụ lục A của kế hoạch, UTF-8:
     - `lessons.csv`: `chuong, bai_id, bai_ten, trang_so, noi_dung_trang, ghi_nho`
     - `questions.csv`: `cau_id, bai_id, loai, do_kho, cau_hoi, A, B, C, D, dap_an, giai_thich, nguon`
  2. Menu `Campus Rift/V2/Import Course CSV`: tạo hoặc cập nhật `CourseData`, `LessonData`, `QuestionBankData` trong `Assets/Learning/Data/TTHCM/`, giữ nguyên ID.
     - Map `loai` sang `type`: `mot-dap-an` → `single-choice`, `dung-sai` → `true-false`, `sap-xep` → `ordering`.
     - Các dạng `nhieu-dap-an`, `noi-cap`, `dien-khuyet` làm ở P20.
  3. Báo lỗi theo dòng CSV: thiếu trường, trùng ID, đáp án không nằm trong lựa chọn, thiếu `nguon`.
  4. Mở rộng `LearningContentValidation`:
     - mỗi bài ≥ 15 câu, mỗi chương ≥ 40 câu;
     - mọi câu có `source` và nội dung tiếng Việt;
     - **cảnh báo nếu còn nội dung giữ chỗ** (`isPlaceholder`).
- **Hoàn thành khi:**
  - [x] Nhập file CSV mẫu (dữ liệu giả, ghi rõ là giả) tạo đúng asset.
  - [x] Validation bắt được mọi lỗi cố ý cài vào.

## P07-T08 — Thay catalog: gỡ Algorithms, tạo khung 6 chương

- **Loại:** Dữ liệu · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P07-T07
- **File:** (sửa) `Assets/Learning/Resources/LearningCatalog.asset` · (di chuyển) course Algorithms sang `Assets/Learning/Legacy/`, không còn tham chiếu trong catalog
- **Các bước:**
  1. Gỡ course Algorithms (5 bài, 30 câu) khỏi catalog. Giữ asset ở thư mục Legacy để tham khảo.
  2. Tạo 6 `CourseData` theo tên chương của giáo trình 2021 (§11.1). Mỗi chương có 1 bài và đề thi **giữ chỗ**, đánh dấu `isPlaceholder`.
     - Câu giữ chỗ phải ghi rõ, ví dụ "Câu hỏi mẫu 01 — [chờ nội dung]" với đáp án "Lựa chọn A/B/C/D".
     - **Không** tự viết nội dung môn học.
  3. Bản build phát hành bị chặn nếu catalog còn nội dung giữ chỗ (kiểm tra ở P17).
- **Hoàn thành khi:**
  - [x] Luồng học, ôn và thi chạy được hết với dữ liệu giữ chỗ.
  - [x] Validation cảnh báo là còn nội dung giữ chỗ.

## P07-T09 — Harness học tập và thi

- **Loại:** Test · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P07-T03 đến T08
- **File:** (sửa) `LearningPlayTest.cs`, `LearningFollowupPlayTest.cs`, `LearningRegressionRunner.cs` · (mới) `Assets/Learning/Validation/BreakthroughExamPlayTest.cs`
- **Kiểm tra:**
  - Đọc → quiz → Tu Vi.
  - Đúng/Sai và Sắp xếp.
  - Thi đậu → lên cảnh giới; thi trượt → khóa 30 phút.
  - Chuỗi ôn Đồng → Bạc → Vàng (đồng hồ giả).
  - Trần luyện tập.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] Nhóm hồi quy Học tập PASS.

## P07-T10 — Nhập nội dung Tư tưởng Hồ Chí Minh thật

- **Loại:** Nội dung · **Ước lượng:** 0,5–2 ngày (tùy khối lượng) · **Phụ thuộc:** P07-T07 · **Trạng thái:** ✅ (giáo trình 2021 do bạn cung cấp)
- **Các bước:**
  1. Nhận nội dung (Excel, CSV hoặc Word) → chuyển về 2 file CSV theo mẫu.
  2. Chạy Import, rồi Validate. Sửa các lỗi thiếu nguồn trang hoặc thiếu câu.
  3. Gỡ nội dung giữ chỗ. Kiểm tra Tu Vi tự chia lại theo số bài thật.
  4. Gửi danh sách câu hỏi (file export) cho giảng viên duyệt. Việc duyệt được theo dõi ở P17-T06.
- **Hoàn thành khi:**
  - [x] Validation không còn lỗi và không còn cảnh báo giữ chỗ.
  - [x] Mỗi bài ≥ 15 câu, mỗi chương ≥ 40 câu.

---

## Kiểm chứng cuối phase

- [x] Học chương 1 → ôn → Thi Đột Phá → lên Trúc Cơ → màn 3 được mở.
- [x] Cập nhật trạng thái P07 trong `task/README.md`.

## Ghi chú triển khai

- **Nội dung thật, không giữ chỗ:** bạn đưa giáo trình *Tư tưởng Hồ Chí Minh* (2021), nên T08 và T10 gộp làm một: không tạo dữ liệu giữ chỗ. Nội dung soạn trong `Content/src/ch1..ch6.txt` (bài đọc là tóm lược bám giáo trình, không thêm kiến thức ngoài sách; mỗi câu ghi "GT 2021, tr. N"), `python Tools/content_build.py` sinh ra `Content/chapters.csv`, `lessons.csv`, `questions.csv`, rồi menu *Import Course CSV* dựng asset trong `Assets/Learning/Data/TTHCM/`. Kết quả: **6 chương, 21 bài, 93 trang, 440 câu**; Validate Content 0 lỗi, 0 cảnh báo. Mỗi chương k ứng với cảnh giới k (Chương 1 → Luyện Khí → thi lên Trúc Cơ).
- **Cần giảng viên duyệt:** phần tóm lược và câu hỏi do tôi soạn từ sách; số trang nguồn của một số câu là phạm vi trang của mục (không phải từng dòng). Danh sách xuất từ `Content/questions.csv` để giảng viên rà soát (theo dõi ở P17-T06).
- **Algorithms:** 11 asset chuyển sang `Assets/Learning/Legacy/` (giữ GUID), catalog chỉ còn 6 chương. `SampleContent.json` và menu *Install MVP* giữ lại nhưng chỉ chạy khi catalog rỗng.
- **Dạng câu:** `single-choice`, `true-false`, `ordering` (bộ chấm trong `Graders.cs`, chọn qua `GraderRegistry`). Sắp xếp sai một vị trí là sai. Quiz bài 10 câu, đạt 80%.
- **Sắp xếp trên UI:** chỉ có cách **chạm hai mục để đổi chỗ** (dùng được như nhau trên PC và mobile), chưa làm kéo thả.
- **Thi Đột Phá:** 20 câu, 20 phút (đồng hồ `Time.unscaledTime`), đạt ≥ 80% → `CompleteBreakthrough` + 300 Linh Thạch qua `LearningEngine.LinhThachSink` (nối ví ở P08); trượt / thoát giữa chừng / tắt game giữa chừng → khóa 30 phút; hết giờ tự nộp, câu chưa trả lời tính sai. Không có tệp `BreakthroughExamUI.cs` riêng: giao diện thi nằm trong `LearningUI.cs`; hiệu ứng vàng chỉ hiện ở khung ngoài sau khi đậu.
- **Ôn tập / luyện tập:** `ReviewScheduler` (1 → 3 → 7 ngày, huy hiệu Đồng/Bạc/Vàng, 5 câu, đạt 80%, +10% phần bài), đồng hồ `ILearningClock` + `FakeLearningClock`, không cho lùi giờ (`lastSeenUtc`). Luyện tập nhanh 10 câu/5 phút, 2 Tu Vi mỗi câu đúng, trần 150/ngày, không lặp câu đã đúng trong 24 giờ.
- **Nhập CSV:** `CourseCsvImporter` báo lỗi theo dòng và không ghi gì khi có lỗi; sửa lỗi tra cột theo chữ thường (tiêu đề `A..D`). Câu sắp xếp 3 mục để trống cột D.
- **Test:** `BreakthroughExamPlayTest` (bộ mới, 48/0, đồng hồ giả + save trong bộ nhớ), `LearningPlayTest` viết lại theo Chương 1 (33/0, qua UI thật, gồm thi đột phá bằng UI). `LearningFollowupPlayTest` không còn (đã gỡ ở P06). Ảnh: `Artifacts/Learning/P07-exam-*.png`.
- **Giới hạn:** chưa kiểm tra máy thật; huy hiệu và số bài đến hạn hiện ở màn Khóa Học, chưa có badge ở Thư Viện (Sảnh ở P09).
