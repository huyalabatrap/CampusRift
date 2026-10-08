# Learning MVP — kiểm chứng 2026-09-28

Unity 6000.6.2f1. Triển khai trực tiếp trên MainMenu, SampleScene và CampusExplorer prefab. Hướng dẫn chơi, chỉnh content, reward, save và dev commands: `Assets/Learning/README.md`.

## Kết quả

| Play Mode suite | Passed | Failed |
|---|---:|---:|
| Course → lesson → quiz → result, FAIL/retry/review, stat/HUD, scene reload, storage recovery | 41 | 0 |
| Major breakthrough, chu kỳ 5/5 → 0/5, mobile unlock, cap cực lớn, component duplication | 9 | 0 |
| Giant Hand: cast thật, damage, cooldown, VFX, sprint, navigation, audio, defeat/victory | 75 | 0 |
| PC/mobile input, touch, skill, Settings, Pause, doors/elevator, các tỷ lệ màn hình | 41 | 0 |
| Energy, boost, exhaustion, recovery, Pause, HUD trên PC/mobile | 19 | 0 |
| Monster perception/tracking, hearing, pressure, chase, damage, stun/recovery | 32 | 0 |
| **Tổng assertions** | **217** | **0** |

Đây là harness tự động chạy trong Unity Play Mode, không phải kết quả suy luận từ code. Chi tiết từng assertion nằm trong `Artifacts/Learning/Summary.json` và các report liên quan. Không tính các lần chạy lại thành test mới.

Stop Play → Play lại: service load 5 breakthrough, 5 mastered lessons, HP bonus 0.25, movement bonus 0.10, skill ID `giant-hand-seal`; log tại `Artifacts/Learning/Restart.txt`. Scene reload áp bonus đúng một lần. Prefab gốc vẫn HP 100, energy 100, walk 6; chỉ có một bridge và một skill gate.

Validate Content chạy thành công: 1 course, 5 lesson, 30 câu. Console lần kiểm tra cuối không có error/warning. Chưa build/test standalone player hoặc điện thoại vật lý; mobile tests dùng Input System Touchscreen trong Editor.

## Vấn đề đã xử lý

- Energy được khởi tạo ở base capacity trước khi load bonus: đã thêm setter giữ tỷ lệ, nên lượt mới bắt đầu đầy theo capacity đã tăng.
- Autosave survival không còn áp lại bonus không thay đổi, tránh ghi đè các thay đổi stat runtime không liên quan.
- HUD major giữ 5/5 đến khi gameplay HUD xuất hiện, rồi hiện đủ hai giây trước chu kỳ mới.
- Learning panel được đưa lên trên trang trí menu; nội dung dài có scroll/swipe và footer RETURN cố định. Không có Canvas hay hệ Pause thứ hai.
- Runtime skill permission được kiểm tra ở cast/preview API, không chỉ trên hình ổ khóa. Cả PC và mobile dùng cùng quyền.
- Các harness cũ được cập nhật đúng contract hiện tại: F quick cast khi không có target không mở preview/tiêu cooldown; speed/energy assertions dùng giá trị cấu hình sau progression thay vì cố định 6/10/100.
- Test Shaban cũ đòi `CanSeePlayer` liên tục, kể cả khi monster vượt sát qua Player và Player ra khỏi góc nhìn 220°. Test mới dùng `TrackingPlayer`, ghi thời gian mất quan sát và vẫn buộc monster đuổi kịp/gây damage, nhận stun rồi truy đuổi lại. Không thay đổi AI/navigation. Lần cuối khoảng ngắt quan sát lớn nhất là 0.616 giây; diagnostic cũ được giữ tại `ShabanPressure-TransientLOS.json`.

## Tổ chức và chính sách progression

`LearningCatalog → CourseData → LessonData → QuestionBankData`, cộng `SkillRewardData` và bảng tier trên course. Content là ScriptableObject có ID ổn định; tiến độ là DTO riêng qua `ILearningStore`. Không gắn logic vào riêng môn Algorithms.

First PASS của mỗi lesson cấp một breakthrough; retake không farm power. Mỗi 5 breakthrough toàn Player chọn skill theo tier của course cấp lần PASS thứ 5. Sau năm bài mẫu: max HP +25%, walk/run +10%, energy capacity +5%, Giant Hand unlock. Tốc độ cap +10%, HP cap +100%, energy cap +50%; reward từng lesson có thể khác nhau.

Quiz lấy 5/6 câu, shuffle câu/đáp án, chấm bằng ID và tránh lặp chuỗi đề trước nếu có lựa chọn khác. PASS 80% configurable. FAIL không đổi power, không phạt gameplay. Result/explanation được lưu cho review; quiz đang làm dở không tiếp tục sau restart.

`learning-v1.json` trong persistentDataPath ghi ngay các mốc, có temp/backup/version; phục hồi backup nếu primary hỏng và bảo toàn file nếu cả hai hỏng. Course progress suy ra từ lesson records. Settings giữ cơ chế cũ. PLAY/RESTART không xóa kiến thức; Continue khởi động lượt sinh tồn mới với power đã lưu, không phục hồi vị trí/monster.

## File chính

- Tạo `Assets/Learning/Runtime/`: data assets types, LearningEngine, QuizSession, LearningProgress/store, LearningService, LearningUI, LearningPlayerBridge, LearningSkillGate.
- Tạo `Assets/Learning/Data/`, `Resources/LearningCatalog.asset`, editor setup/content validation và Play Mode harness trong `Validation/`.
- Sửa `Assets/Scenes/MainMenu.unity`, `SampleScene.unity`, `Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab`.
- Sửa UIManager, UIStateManager, GameSceneManager, BreakthroughProgressUI; PlayerMonsterHealth và CampusExplorer; ba skill/HUD cùng MobileControlsHUD.
- Sửa ba regression harness: MobileControlPlayTest, BoostEnergyPlayTest, ShabanPressurePlayTest để khớp contract gameplay hiện tại.
- Backup scene/prefab trước triển khai: `Backups/Learning-20260927/`. Save trước QA được backup riêng và khôi phục sau khi dừng Play Mode.

## Giới hạn

MVP có một course, năm lesson ngắn song ngữ EN/VN, multiple choice và một skill reward mẫu. Tier/course mới cần dữ liệu reward tương ứng; skill hoàn toàn mới cần ability/HUD adapter riêng. Chưa có video, adaptive/spaced repetition, cloud save, world checkpoint hoặc schema migration phiên bản sau.

Ưu tiên tiếp: thêm course/reward tiers, quest/pickup mở lesson, các question handler khác, polish breakthrough audio/VFX và test trên thiết bị thật.

## Bổ sung EN/VN theo yêu cầu

Settings hiện tại ở cả MainMenu và SampleScene có dropdown EN / VN; preview, Apply, Cancel và Escape cùng tuân theo cơ chế Settings. Mặc định VN. Menu, Pause, Settings, Courses, lesson/quiz/review/result/breakthrough, HUD và skill/mobile/elevator prompts dùng chung LocalizationService. Font Be Vietnam Pro có đủ dấu tiếng Việt.

`Artifacts/Localization/PlayMode.json`: **27 passed / 0 failed**, chạy sau khi thêm localization. Kiểm tra bản dịch đầy đủ 5 bài/30 câu/120 đáp án, quiz FAIL/PASS trong VN, đổi EN giữa quiz, canonical answer IDs và save không thay đổi, stat tăng thật, skill unlock thật, HUD, Settings và scene transitions. Các suite 217 assertions phía trên thuộc giai đoạn learning trước localization; không ghi nhận lại là đã chạy toàn bộ sau bổ sung EN/VN.

Kiểm tra bổ sung dropdown mở và pointer click vào EN hoạt động; Stop Play → Play xác nhận cả VN (`BẮT ĐẦU`) và EN (`PLAY`) load từ Settings đã lưu: `Artifacts/Localization/Restart-Vietnamese.json`, `Restart-English.json`. Screenshot Settings, lesson, explanation, breakthrough và dropdown trong cùng thư mục. Test localization dùng learning store riêng dưới Artifacts và khôi phục engine thật; production save không bị thay bằng tiến độ test. Ngôn ngữ cuối được để VN.

Các file bổ sung chính: `Assets/Localization/Runtime/LocalizationService.cs`, `LocalizedText.cs`, `LocalizationCatalog.cs`; `Resources/LocalizationCatalog.asset`; `Editor/UIVietnamese.txt`, `LearningVietnamese.json`, `LocalizationSetup.cs`. Cập nhật SettingsManager/GameSettings, SettingsUI, hai scene, font asset, trường VN trong data learning và ElevatorInteraction. Hướng dẫn tại `Assets/Localization/README.md`.
