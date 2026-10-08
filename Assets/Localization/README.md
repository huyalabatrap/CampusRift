# Campus Rift — EN / VN

Trong Main Menu hoặc Pause mở **Settings / Cài đặt → Language / Ngôn ngữ**. Chọn **EN — English** hoặc **VN — Tiếng Việt** để xem trước; chọn **Apply / Áp dụng** để lưu. Cancel/Back khôi phục lựa chọn đã lưu. Mặc định VN. Settings giữ lựa chọn trong khóa PlayerPrefs `CampusRift.Settings.v1`; learning save độc lập và không bị đổi khi chuyển ngôn ngữ.

## Tổ chức

- `Resources/LocalizationCatalog.asset`: bảng UI EN/VN và font Be Vietnam Pro dùng cho tiếng Việt.
- `LocalizationService`: bootstrap một lần, theo Settings và qua scene; dịch exact text trước, sau đó các cụm từ có ranh giới để hỗ trợ nhãn HUD có số. Cache có giới hạn. Tự phát hiện TMP label ở scene và UI runtime; bỏ qua input fields.
- `LocalizedText`: giữ nguồn EN riêng với bản đang render để đổi qua lại không mất nguồn. Cập nhật nhãn động trong LateUpdate, điều chỉnh font/autosize và chiều cao đoạn lesson. Không thay model/gameplay.
- `LocalizationService.T`: dùng cho prompt thang máy IMGUI cũ.
- `SettingsUI.Language`: dropdown tích hợp Settings hiện có, không tạo Canvas hay state machine khác.
- `LocalizationPlayTest`: harness chỉ compile trong Editor; learning store cô lập dưới `Artifacts/Localization`, trả lại engine thật khi kết thúc.

## Thêm hoặc sửa bản dịch

**Nội dung học:** sửa trực tiếp asset trong `Assets/Learning/Data` hoặc asset mới được gắn vào LearningCatalog. Trường gốc là EN, trường cùng tên kết thúc bằng `VN` là bản tiếng Việt. Course: title/description; Lesson: title; LessonPage: title/content/example/takeaway; Question: prompt/explanation; AnswerOption: text; SkillReward: displayName/description. Chấm bài và reward luôn dùng ID ổn định, không dùng chuỗi đã dịch. Thiếu bản VN dùng nguồn EN.

**UI:** thêm cặp `en` / `vi` vào LocalizationCatalog trong Inspector. Script tiếp tục gán chuỗi EN canonical; LocalizedText lo hiển thị theo ngôn ngữ. Trường hợp hiển thị mới cần có bản dịch trong bảng. Không dịch IDs, phím bấm, mã như BFS/DFS, công thức toán hoặc tên riêng Campus Rift/Shaban.

`Editor/UIVietnamese.txt` và `Editor/LearningVietnamese.json` là nguồn cài đặt nội dung mẫu. Menu **Campus Rift → Localization → Install EN-VN** tái tạo bảng UI từ TXT và điền lại bản VN của course mẫu. **Lệnh này ghi đè các bản dịch mẫu:** nếu đã tùy chỉnh trực tiếp asset, không chạy lại hoặc cập nhật nguồn tương ứng trước. Nó cũng nối Language dropdown trên hai scene hiện tại và chuẩn bị glyph tiếng Việt. Thêm nội dung bình thường không cần chạy installer.

Đổi ngôn ngữ trong quiz không tạo đề mới, không đổi đáp án và không reset session. Kết quả lưu giữ nguồn EN; review dịch lúc render. Thay bản dịch VN sẽ không làm mất tiến độ.

## Kiểm tra và giới hạn

27 assertions Play Mode: Settings preview/Apply/Cancel, font, toàn bộ dữ liệu học mẫu, quiz FAIL/PASS/review, đổi giữa quiz, stat/skill thật, HUD PC/mobile và scene transitions. Dropdown cũng được mở và chọn bằng pointer event; Stop Play → Play kiểm chứng lưu cả EN và VN. Báo cáo và ảnh: `Artifacts/Localization`; tổng hợp: `LEARNING_VALIDATION_REPORT.md`.

MVP có hai ngôn ngữ; khóa bảng UI hiện là chuỗi nguồn EN. Hai ngữ cảnh có cùng nguồn EN dùng chung bản dịch; nếu cần dịch khác theo ngữ cảnh hoặc thêm nhiều locale, nên chuyển sang localization keys/string tables theo ID. Quiz result snapshot cũ có nội dung đã xóa khỏi catalog có thể fallback EN. Tên môn/topic nội bộ chưa được render và không cần dịch. Chưa dịch texture có chữ hoặc voiceover. Chưa kiểm tra build standalone/thiết bị thật; test mobile chạy trong Editor.

UI runtime hiện được dò định kỳ 0,4 giây để tương thích các HUD cũ. Với lượng UI lớn hơn, nên gắn LocalizedText sẵn trên prefab hoặc đăng ký label lúc tạo để bỏ bước dò toàn scene.
