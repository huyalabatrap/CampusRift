# Campus Rift — Tri thức = Sức mạnh

> **V2 (P06):** luật "đột phá", thưởng phần trăm, kỹ năng theo mốc 5 bài và thời gian sinh tồn đã bị thay bằng Tu Vi và cảnh giới. Tiến độ bài học nằm trong `campusrift-v2.json`. Xem `Assets/Progression/README.md`. Các mục Progression, Save và Developer test bên dưới là mô tả bản cũ.

> **V2 (P07):** nội dung là giáo trình *Tư tưởng Hồ Chí Minh* (2021), 6 chương / 21 bài / 440 câu, nguồn soạn ở `Content/src/chN.txt`. Quy trình: sửa file nguồn → `python Tools/content_build.py` → menu *Campus Rift/V2/Import Course CSV* → *Campus Rift/Learning/Validate Content*. Dạng câu: một đáp án, đúng/sai, sắp xếp. Cuối chương có Thi Đột Phá (20 câu, 20 phút, đạt 80%); có ôn tập giãn cách (Đồng/Bạc/Vàng) và luyện tập nhanh. Môn Thuật toán cũ nằm ở `Legacy/`, không còn trong catalog. Đoạn "Chơi thử" và phần Breakthrough bên dưới là của V1.

## Chơi thử

Mở `Assets/Scenes/MainMenu.unity`, Play → COURSES → CHƯƠNG 1 → bài 1 → đọc → COMPLETE READING → START QUIZ. Mỗi đề 10 câu, đạt từ 80%. Sau Result chọn BREAKTHROUGH để xem reward.

Trong gameplay: ESC / nút Pause trên mobile → COURSES. Học đóng băng thời gian, năng lượng, AI và audio giống Pause. RETURN trở về trạng thái đã mở Courses; nếu mở từ Pause, chọn RESUME để chơi tiếp. FAIL cho review/retry/return, không lấy máu, item hay level.

Lesson 2–5 yêu cầu đã PASS bài trước và tổng thời gian sinh tồn 15/30/45/60 giây. Thời gian chỉ tăng khi Player còn sống và gameplay nhận input, không tăng trong Menu/Pause/Courses. Không yêu cầu thắng monster để tiếp tục học. Bài đầu mở sẵn để giới thiệu loop.

## Dữ liệu và cách thêm nội dung

Tạo asset bằng **Create → Campus Rift → Learning**. Chỉnh trực tiếp các asset trong `Data`, không sửa JSON mẫu để cập nhật runtime.

- `Resources/LearningCatalog.asset`: các Course, số lần đột phá mỗi skill (mặc định 5), cap stat.
- `CourseData`: ID ổn định, tên, môn, mô tả, lesson, thời gian sinh tồn để mở course, bảng skill theo tier.
- `LessonData`: ID duy nhất toàn catalog, topic, pages (nội dung, ví dụ, ghi nhớ, Sprite tùy chọn), prerequisites, survival gate, bank, số câu, passPercent, reward.
- `QuestionBankData`: ID câu, type key, lessonId/topic, tags, difficulty, prompt, options có ID riêng, correctOptionIds, explanation. MVP render/chấm `single-choice`.
- `SkillRewardData`: ID kỹ năng, tên, mô tả, icon. Không chứa tên Course trong code.

Các ID phải giữ nguyên sau khi phát hành để save cũ tiếp tục khớp. Đổi ID được xem là nội dung mới. Question có thể nằm cùng bank nhưng được lọc theo lessonId. Thêm lesson vào course và course vào catalog để xuất hiện trên UI. Chạy **Campus Rift → Learning → Validate Content** sau khi sửa.

`Editor/SampleContent.json` chỉ là nguồn tạo mẫu cho lần Install đầu tiên: 5 lesson / 30 câu về BFS, DFS, complexity, binary search và graph strategy. Install không ghi đè course đã có trong catalog. Không cần chạy lại Install khi thay nội dung.

## Progression và skill

Mỗi lesson chỉ cấp reward ở lần PASS đầu tiên; đọc lại và retake vẫn lưu best score/lần thi gần nhất. Mỗi quiz session chỉ được submit một lần. Đề và đáp án được Fisher–Yates shuffle, chấm bằng ID, tránh lặp đúng chuỗi câu của lần trước nếu có phương án khác.

Reward mặc định: +5% HP gốc, +2% walk/run speed gốc; 2 lesson cuối thêm +2.5% energy capacity. Cộng theo base stat, không nhân chồng mỗi lần load. Cap catalog: speed +10%, HP +100%, energy +50%. Sau 5 bài mẫu: HP 125% base, speed 110% base, energy 105% base. HP và energy hiện tại giữ tỷ lệ khi capacity đổi; không hồi đầy và không hồi sinh người đã chết. Lượt chơi mới bắt đầu đầy theo capacity đã nâng cấp.

Breakthrough count là **toàn Player**. Mốc 5/10/15… tương ứng tier 1/2/3… Course của bài vượt qua tại mốc đó quyết định SkillTier. Ví dụ Algorithms tier 1 → GiantHandReward. Khi thêm course/tier, cần cấu hình reward cho những tier có thể đạt; không cấu hình thì mốc vẫn tăng stat nhưng không tự sinh skill. Reward ID đã mở không bị thu hồi hoặc cấp trùng. HUD hiện 5/5 trong hai giây gameplay rồi sang 0/5 của chu kỳ mới.

Trên prefab Player, `LearningSkillGate.bindings` liên kết SkillReward asset với component ability hiện có. Mẫu khóa `GiantHandSkill`; Void Wall và Phantom vẫn sẵn có để bảo toàn công cụ sinh tồn. Có thể gate hai skill này chỉ bằng binding, không sửa logic course. Cast/preview thật kiểm tra quyền; PC và mobile HUD dùng cùng quyền. Sau unlock dùng F hoặc nút Hand trên mobile; damage, targeting, cooldown và VFX vẫn do skill cũ đảm nhiệm.

## Save

`LearningService` bootstrap từ Resources, tồn tại qua scene. `LearningEngine` không phụ thuộc Canvas/Player; `ILearningStore` là ranh giới lưu trữ. `JsonLearningStore` ghi `Application.persistentDataPath/learning-v1.json`, có version 1, temp file và `.bak`, thay thế file atomically. Lưu ngay khi đọc page, tạo đề, submit/reward; lưu survival định kỳ 10 giây và khi pause application/quit.

Save chứa pagesRead/completed, rewarded, số lần thi, best score, kết quả và explanation lần gần nhất, thứ tự đề trước, tổng survival, tổng breakthrough/stat bonus, ID skill unlocked. Course progress được suy ra từ lesson, tránh hai nguồn trạng thái lệch nhau. File chính hỏng thì đọc backup. Hai file hỏng thì giữ nguyên file và hiển thị lỗi save; không âm thầm xóa tiến độ. Settings tiếp tục dùng save cũ độc lập.

PLAY/RESTART bắt đầu lượt sinh tồn mới nhưng giữ tri thức. CONTINUE bật khi đã có đọc bài/progression và bắt đầu lượt mới với power đã lưu. MVP không lưu vị trí, trạng thái monster, HP hiện tại hay quiz đang làm dở. Lần thi chưa submit không cấp reward.

## Điểm tích hợp

- `LearningEngine`, `QuizSession`, `LearningProgress`: luật, chấm, random, persistence DTO/store.
- `LearningUI`: view course/lesson/quiz/review/result/presentation trong Canvas, PanelTransition, RiftButton và theme hiện tại; ScrollRect hỗ trợ nội dung dài.
- `LearningPlayerBridge`: áp reward lên PlayerMonsterHealth/CampusExplorer, chỉ có một health source.
- `LearningSkillGate`: quyền sử dụng các component skill hiện hữu.
- `LearningSetup`, `LearningContentValidation`: cài đặt asset/scene và kiểm tra nội dung.
- Đã sửa `MainMenu.unity`, `SampleScene.unity`, `CampusExplorer.prefab`; UIManager, UIStateManager, GameSceneManager, BreakthroughProgressUI; PlayerMonsterHealth; ba skill/HUD và MobileControlsHUD.
- Monster AI, navigation, spatial audio, Settings và movement controller không viết lại.

## Developer test

Editor Play Mode: **Campus Rift → Learning → DEV - Pass Next Lesson** hoàn thành lesson chưa mastered kế tiếp (bỏ qua survival để test nhanh). Chạy 5 lần để mở skill. **DEV - Reset Learning Progress** xóa tiến độ learning của save hiện tại; không chạm Settings. Hai lệnh chỉ compile trong Editor, không có cheat UI ở release.

`LearningPlayTest.Begin()` chạy end-to-end qua UI thật và ghi `Artifacts/Learning/PlayMode.json`, ảnh chụp. Harness backup save ban đầu vào `Artifacts/Learning/Original*`, để lại save test cho kiểm tra restart; **cần restore backup sau kiểm tra**. Không chạy harness như gameplay bình thường. Các test skill cũ có thể làm Player/Monster chết: reload scene sau khi chạy.

## Giới hạn và hướng mở rộng

MVP có nội dung/UI song ngữ EN/VN, lesson đọc ngắn, một skill reward mẫu; chưa có video hay authoring wizard. Grader đã có interface `IQuestionGrader`, câu hỏi có type key và answer IDs; thêm loại câu cần bổ sung grader/render/input tương ứng, không thay reward/save pipeline. Không triển khai adaptive quiz, mastery nhiều cấp, spaced repetition, LMS, leaderboard hoặc AI tutor.

Ưu tiên tiếp theo: nhiều course với reward tier đầy đủ; các pickup/quest phát unlock thay survival gate; các dạng câu mới; lưu lịch sử attempts giới hạn dung lượng; schema migrations/cloud save; polish âm thanh/VFX breakthrough và kiểm tra thiết bị mobile thật.

## EN / VN

Settings → Language / Ngôn ngữ → EN hoặc VN → Apply / Áp dụng. Mặc định VN; xem trước đổi ngay, Hủy khôi phục ngôn ngữ đã lưu. Lựa chọn dùng save Settings hiện có, độc lập với learning progression.

Các trường gốc `title`, `content`, `prompt`, `text`, `explanation`, `displayName` là tiếng Anh; điền các trường tương ứng kết thúc bằng `VN` trên Course/Lesson/LessonPage/Question/AnswerOption/SkillReward để thêm bản tiếng Việt. Không dịch ID, type key hoặc correctOptionIds. Toàn bộ 5 lesson / 30 question / 120 answer mẫu đã có bản dịch. Quiz và review đổi ngôn ngữ theo Settings mà không thay câu hỏi, đáp án đã chọn hoặc cách chấm. Thiếu bản VN sẽ hiển thị nguồn EN.

Chi tiết UI table, font, authoring và kiểm chứng tại `Assets/Localization/README.md`.

## Tham khảo kiến trúc

Unity: [ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html) dùng cho nội dung asset; [persistentDataPath](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/application/persistentdatapath) cho save runtime. Nội dung asset và tiến độ người chơi được lưu riêng.

## P20 · Học tập mở rộng (bản V2 hiện hành)

Sảnh → Thư Viện có thẻ ghi nhớ, sổ tay câu sai và nhiệm vụ ngày. CSV hiện có449câu/6dạng;93thẻ lấy từ Ghi nhớ kèm nguồn. Xem task/p20/README.md cho thao tác và các fixture smoke. Hướng dẫn MVP phía trên là lịch sử; V2 không khóa bài bằng thời gian sinh tồn.

Tác giả sửa Content/src/chN.txt; chạy Tools/p20_content.py để xây CSV và flashcards.csv, Import Course CSV rồi Validate Content trong Unity. Nhiều đáp án lưu A,B; nối cặp dùng A/B trái(a/b), C/D phải(1/2), đáp án a:1,b:2; điền khuyết cần ___ và đáp án1cụm từ. Runtime grader hỗ trợ số cặp nhiều hơn nếu nhập trực tiếp bank assets.

Gói duyệt hiện hành: Artifacts/Content/P20/review-print.html và Review-P20-2026-10-04.md, ⏳ chờ giảng viên. P17 được giữ nguyên làm lịch sử.
