# P06 — Hồ sơ v2 và Tu luyện

> **Mục tiêu:** có file lưu mới cho toàn bộ tiến trình V2, hệ **Tu Vi và cảnh giới**, chia Tu Vi theo số bài, áp chỉ số lên người chơi, khóa/mở màn theo cảnh giới.
>
> **Phạm vi:** MVP · **Ước lượng:** 3 ngày công · **Phụ thuộc:** P03-T01 (`PlayerStats`) · **Tham chiếu:** §8, §14.5, §14.6
>
> **Kết quả:** học bài thì nhận Tu Vi, lên tầng, chỉ số tăng, và mở màn theo cảnh giới.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P06-T01 | `ProfileData`, `JsonProfileStore`, `ProfileService` | Code | 0,5 | P03-T01 |✅ |
| P06-T02 | Chuyển tiếp từ save v1 (sao lưu, thông báo) | Code | 0,2 | T01 |✅ |
| P06-T03 | `CultivationService`: cảnh giới, tầng, bình cảnh | Code | 0,5 | T01 |✅ |
| P06-T04 | Bảng chỉ số theo cảnh giới và bridge sang người chơi | Code | 0,4 | T03 |✅ |
| P06-T05 | Nối các nguồn Tu Vi vào việc học; bỏ mở bài bằng thời gian sống | Code | 0,5 | T03 |✅ |
| P06-T06 | Khóa/mở màn theo cảnh giới, lưu kết quả màn | Code | 0,3 | T03 |✅ |
| P06-T07 | Panel Cảnh Giới | UI | 0,4 | T04 |✅ |
| P06-T08 | Validation Tu Vi và kiểm tra lưu/nạp | Test | 0,2 | T05–T07 |✅ |

---

## P06-T01 — `ProfileData`, `JsonProfileStore`, `ProfileService`

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P03-T01
- **File:** (mới) `Assets/Progression/Runtime/ProfileData.cs`, `JsonProfileStore.cs`, `ProfileService.cs`
- **Các bước:**
  1. `ProfileData` theo JSON mẫu §14.6: `version = 2`, `cultivation`, `wallet`, `lessons`, `exams`, `skills`, `loadouts`, `inventory`, `artifacts`, `levels`, `daily`.
     - `JsonUtility` không hỗ trợ dictionary, nên dùng list các cặp key/value.
  2. `JsonProfileStore` làm theo đúng cách an toàn của `JsonLearningStore`:
     - ghi file `.tmp` rồi `File.Replace` kèm `.bak`;
     - nếu file chính hỏng thì đọc `.bak`;
     - **không bao giờ ghi đè** file không đọc được.
     - Đường dẫn: `persistentDataPath/campusrift-v2.json`.
  3. `ProfileService`:
     - tự khởi động giống `LearningService.Boot` (`RuntimeInitializeOnLoadMethod`, `DontDestroyOnLoad`);
     - lưu khi dữ liệu đổi (gộp trong 1 giây) và khi app pause/quit.
- **Hoàn thành khi:**
  - [ ] Lưu → thoát Play → vào lại vẫn còn dữ liệu.
  - [ ] Làm hỏng file chính thì tự đọc từ `.bak` và log cảnh báo.

## P06-T02 — Chuyển tiếp từ save v1 (sao lưu, thông báo)

- **Loại:** Code · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P06-T01
- **Các bước:**
  1. Lần đầu khởi động V2: nếu có `learning-v1.json`, copy sang `learning-v1.backup.json`. **Không sửa** file gốc.
  2. Hồ sơ V2 bắt đầu mới, vì nội dung và ID bài đã đổi sang Tư tưởng Hồ Chí Minh.
  3. Hiện thông báo một lần: "Đã chuyển sang nội dung mới. Tiến độ cũ được lưu trữ."
  4. File settings giữ nguyên.
- **Hoàn thành khi:**
  - [ ] Có file backup.
  - [ ] Thông báo chỉ hiện một lần.
  - [ ] Settings không đổi.

## P06-T03 — `CultivationService`: cảnh giới, tầng, bình cảnh

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P06-T01
- **File:** (mới) `Assets/Progression/Runtime/Realm.cs`, `CultivationService.cs`, `Assets/Progression/Data/CultivationTable.asset`
- **Các bước:**
  1. `enum Realm { LuyenKhi, TrucCo, KetDan, NguyenAnh, HoaThan, LuyenHu, DoKiep }`, mỗi cảnh giới 5 tầng.
  2. `CultivationTable` (ScriptableObject): Tu Vi mỗi tầng = 100 / 150 / 225 / 340 / 510 / 760 / 1.140 (§8.1), cùng tên hiển thị EN/VN.
  3. API:
     - `AddTuVi(amount, source)`: tự lên tầng khi đủ.
     - Tầng 5 đầy thì vào **bình cảnh**: Tu Vi dư bị bỏ, hiện "Bình cảnh — hãy Thi Đột Phá".
     - `CanAttemptBreakthrough`, `CompleteBreakthrough()` (lên tầng 1 cảnh giới kế).
     - event `TierChanged`, `RealmChanged`.
  4. Độ Kiếp là cảnh giới cuối: tầng 5 đầy thì dừng.
- **Hoàn thành khi:**
  - [ ] Validation T08 PASS cho mọi chuyển tầng và chuyển cảnh giới.

## P06-T04 — Bảng chỉ số theo cảnh giới và bridge sang người chơi

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P06-T03
- **File:** (mới) `Assets/Progression/Runtime/CultivationPlayerBridge.cs` · (sửa) `CultivationTable.asset`, `CampusExplorer.prefab` · (gỡ) `LearningPlayerBridge` khỏi prefab
- **Các bước:**
  1. Bảng §8.4: Máu, Công, Linh Lực, Phòng thủ ở tầng 1 và tầng 5 của từng cảnh giới; các tầng giữa nội suy tuyến tính. Mỗi cảnh giới thêm +1,5% tốc chạy, tối đa +10%.
  2. `CultivationPlayerBridge` đẩy modifier nguồn `Cultivation` vào `PlayerStats` (P03-T01), và cập nhật khi đổi tầng.
  3. Gỡ `LearningPlayerBridge` (thưởng theo phần trăm của hệ cũ).
- **Hoàn thành khi:**
  - [ ] Luyện Khí 1 cho 100 máu / 20 Công.
  - [ ] Độ Kiếp 1 cho 820 máu / 164 Công.
  - [ ] Nạp lại scene không cộng dồn.

## P06-T05 — Nối các nguồn Tu Vi vào việc học; bỏ mở bài bằng thời gian sống

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P06-T03
- **File:** (sửa) `Assets/Learning/Runtime/LearningEngine.cs`, `LearningService.cs`, `LessonData.cs` · (mới) `Assets/Progression/Runtime/StudyRewards.cs`
- **Các bước:**
  1. **Phần Tu Vi của bài** = (Tu Vi cả cảnh giới của chương × 85%) ÷ số bài trong chương. Tính động từ catalog (§8.2).
  2. Các nguồn Tu Vi:

| Nguồn | Tu Vi | Nối vào |
|---|---|---|
| Đọc hết bài lần đầu | 15% phần của bài | Trang cuối trong `ReadPage` |
| Quiz lần đầu | 70% phần của bài × tỉ lệ đúng | `Submit` |
| Ôn tập đúng hạn | 10% phần của bài | P07 |
| Luyện tập | 2 Tu Vi/câu đúng, tối đa 150/ngày | P07 |
| Qua màn lần đầu | 5% Tu Vi một tầng ở cảnh giới hiện tại | `LevelDirector` |

  3. Thay `LearningEngine.Award()` (chu kỳ 5 breakthrough và thưởng phần trăm) bằng `StudyRewards`. Phần thưởng Linh Thạch nối ở P08.
  4. Bỏ đếm thời gian sống sót trong `LearningService.Update` và điều kiện `requiredSurvivalSeconds`.
  5. Mở bài theo thứ tự chương: giữ `prerequisiteLessonIds`, bỏ `requiredSurvivalSeconds`.
- **Hoàn thành khi:**
  - [ ] Học hết một chương mẫu với điểm tối đa thì đạt khoảng 85% Tu Vi của cảnh giới (sai số theo số bài).

## P06-T06 — Khóa/mở màn theo cảnh giới, lưu kết quả màn

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P06-T03
- **File:** (mới) `Assets/Progression/Runtime/LevelProgressService.cs`
- **Các bước:**
  1. Màn mở khi cảnh giới/tầng ≥ `LevelDefinition.requiredRealm/Tier` **và** đã qua màn trước.
  2. Lưu `cleared`, `stars` (dạng bitmask 3 sao) và `bestTime` cho từng màn.
  3. API cho UI: `IsUnlocked(level, out reason)`, trong đó `reason` là chuỗi kiểu "Cần Trúc Cơ 1".
- **Hoàn thành khi:**
  - [ ] Không vào được màn đang khóa, kể cả gọi `StartLevel` trực tiếp.

## P06-T07 — Panel Cảnh Giới

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P06-T04
- **File:** (mới) `Assets/CampusRiftUI/Runtime/CultivationUI.cs` · (mới) `Assets/CampusRiftUI/Editor/UIFoundationBuilder.Cultivation.cs`
- **Các bước:**
  1. Hiện: tên cảnh giới, 5 chấm tầng, thanh Tu Vi (số hiện tại / số cần), chỉ số (máu, Công, Linh Lực, Phòng thủ, tốc chạy).
  2. Mục "Mở khóa tiếp theo": màn và kỹ năng lấy từ bảng §8.5.
  3. Nút **Thi Đột Phá**, chỉ bật khi ở bình cảnh. P07 sẽ nối vào bài thi.
- **Hoàn thành khi:**
  - [ ] Số liệu khớp `CultivationService`.
  - [ ] Hiển thị đúng ở 3 tỉ lệ màn hình, EN/VN.

## P06-T08 — Validation Tu Vi và kiểm tra lưu/nạp

- **Loại:** Test · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P06-T05 đến T07
- **File:** (mới) `Assets/Progression/Editor/CultivationValidation.cs`, `Assets/Progression/Validation/ProfilePlayTest.cs`
- **Kiểm tra:**
  - Chia Tu Vi khi chương có 3, 4, 5 bài.
  - Chuyển tầng, chuyển cảnh giới, bình cảnh, Độ Kiếp là cảnh giới cuối.
  - Khóa màn theo cảnh giới.
  - Lưu → khởi động lại → dữ liệu còn; `.bak` hoạt động.
- **Hoàn thành khi:**
  - [ ] PASS.
  - [ ] Nhóm hồi quy Học tập được cập nhật theo luật mới và PASS.

---

## Kiểm chứng cuối phase

- [ ] Học một bài mẫu → Tu Vi tăng → lên tầng → chỉ số người chơi tăng trong màn.
- [ ] Cập nhật trạng thái P06 trong `task/README.md`.


---

## Ghi chú triển khai

- **Save v2:** `campusrift-v2.json`. Tiến độ bài học nằm trong mục `learning` (dùng lại `LearningProgress`, thêm cờ `readRewarded/quizRewarded`), không tách thành từ điển `lessons` như JSON mẫu. Các mục để dành cho P07–P16 (`exams, skills, loadouts, inventory, artifacts, daily, wallet`) đã có sẵn nên không cần đổi phiên bản.
- **Chuyển tiếp v1:** nếu chưa có save v2 mà có `learning-v1.json` thì copy sang `learning-v1.backup.json` (không đè backup có sẵn), hồ sơ mới bắt đầu từ đầu, `MigrationNotice` hiện một lần trên menu ("Đã chuyển sang nội dung mới. Tiến độ cũ được lưu trữ."). Settings không bị đụng.
- **Sửa lỗi có sẵn:** `InvalidDataException` (save có phiên bản không hỗ trợ) không nằm trong danh sách bắt lỗi của `JsonLearningStore` nên sẽ làm văng chương trình thay vì chặn ghi; đã sửa ở cả hai store.
- **Chia Tu Vi:** bảng kế hoạch ghi "đọc 15%, quiz 70% phần của bài" nhưng mục tiêu ghi "học hết chương với điểm tối đa ≈ 85% cảnh giới". Hai câu chỉ khớp nhau nếu 15% và 70% là phần trăm của cảnh giới, nên trong phần chia theo bài (đã là 85%) đọc = 15/85 và quiz = 70/85. Kết quả đo: chương 5 bài cho 425/500 Tu Vi (tầng 5 của Luyện Khí, còn dư 25).
- **Thay `Award()`:** `LearningEngine` không còn "đột phá", chỉ số phần trăm và kỹ năng theo mốc 5 bài. `LearningEngine.Awarded` trả `StudyAward`. Bỏ điều kiện thời gian sống ở `Available` và `LearningService.Update`; các trường `requiredSurvivalSeconds` còn trong asset nhưng không dùng.
- **Chỉ số người chơi:** `CultivationPlayerBridge` thay `LearningPlayerBridge` (đã xóa, cả trong prefab và scene). Luyện Khí 1 = 100 máu / 20 Công, Độ Kiếp 1 = 820 / 164; áp lại nhiều lần không cộng dồn. Tốc chạy = 1,5% × số cảnh giới đã qua, tối đa 10%.
- **Kỹ năng:** `SkillUnlockService` mở kỹ năng theo `starter` hoặc `unlockRealm/unlockTier` của cảnh giới; đã bỏ ràng buộc `LearningSkillGate` (Đại Thủ Ấn giờ là kỹ năng khởi đầu như §8.5). Kỹ năng khác chưa có `SkillDefinition` (P10).
- **Vòng Đột Phá trên HUD:** `BreakthroughProgressUI` nay hiện 5 tầng của cảnh giới (sáng hết = bình cảnh). Trong màn chơi thì `LevelHUD` vẫn ẩn nó.
- **Khóa màn:** `GameSceneManager.StartLevel` từ chối màn đang khóa (`LastLockReason`); `NextLevel` về menu nếu màn kế bị khóa. Màn 2 cần Luyện Khí 3 nên người chơi mới phải học mới sang được màn 2 (Continue ở lại màn 1). Vì thế `Mốc 1` bây giờ là: học đủ để lên Luyện Khí 3, rồi chơi màn 2.
- **Panel Cảnh Giới:** không tạo panel riêng trong scene mà thành màn "CẢNH GIỚI" trong giao diện khóa học (nút đầu danh sách `Courses`, cả trong Pause và Menu). Nội dung dựng trong `CultivationUI.Describe` (tên, 5 chấm tầng, thanh Tu Vi, chỉ số, mở khóa tiếp theo, nút Thi Đột Phá — bật khi bình cảnh, bài thi làm ở P07), nên không cần builder trong scene.
- **Test:** `CultivationValidation` 52/0 (không cần Play), `LearningPlayTest` viết lại theo luật mới 29/0 (qua UI thật: đọc → quiz → Tu Vi → chỉ số → khóa màn), `LevelFlowPlayTest` 48/0. `LearningFollowupPlayTest` (mốc 5 bài, mức trần thưởng %) đã xóa vì luật đó không còn.
- **Giới hạn:** panel khóa học có khung cố định 1400 × 900 nên bị cắt ở màn hình dọc (không phải hướng của game). Chưa kiểm tra máy thật.
