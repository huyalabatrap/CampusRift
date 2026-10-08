# Progression

Hồ sơ v2 (`campusrift-v2.json`), Tu Vi và cảnh giới, Linh Thạch, vật phẩm, buff, cửa hàng, pháp bảo, telemetry cục bộ.

- Namespace: `CampusRift.Progression`
- Task: `task/P06`, `task/P08`, `task/P17`, `task/P20`

## Thành phần (P06)

| File | Việc |
|---|---|
| `Runtime/ProfileData.cs` | Toàn bộ save v2: cultivation, wallet, learning (tiến độ từng bài), exams, skills, loadouts, inventory, artifacts, levels, daily, migration. Dạng "cặp key/count" vì `JsonUtility` không có dictionary |
| `Runtime/JsonProfileStore.cs` | Ghi `.tmp` → `File.Replace` kèm `.bak`; file chính hỏng thì đọc `.bak`; hai file hỏng hoặc phiên bản mới hơn thì **không bao giờ ghi đè** |
| `Runtime/ProfileService.cs` | Tự khởi động, lưu sau 1 giây kể từ lần đổi đầu tiên và khi pause/quit; `UseTransient/EndTransient` cho test; lưu chuyển tiếp từ `learning-v1.json` |
| `Runtime/CultivationService.cs` | Cảnh giới, tầng, Tu Vi, bình cảnh, `CompleteBreakthrough`, sự kiện `TierChanged/RealmChanged` |
| `Runtime/CultivationTable.cs` | 7 cảnh giới: Tu Vi mỗi tầng, máu/Công/Linh Lực tầng 1→5, phòng thủ, tốc chạy (`Resources/CultivationTable.asset`) |
| `Runtime/StudyRewardRules.cs` | Chia Tu Vi: (Tu Vi cảnh giới × 85%) ÷ số bài. Đọc = 15/85, quiz lần đầu = 70/85 × tỉ lệ đúng, qua màn lần đầu = 5% một tầng |
| `Runtime/CultivationPlayerBridge.cs` | Đẩy chỉ số cảnh giới vào `PlayerStats` (nguồn `Cultivation`, khóa `cultivation`) |
| `Runtime/LevelProgressService.cs` | Khóa/mở màn (cảnh giới + đã qua màn trước), lưu sao/thời gian tốt nhất |
| `Runtime/MigrationNotice.cs` | Thông báo một lần trên menu sau khi save cũ được lưu trữ |
| `Editor/CultivationSetup.cs` | Menu `Campus Rift/V2/Setup Cultivation` |
| `Editor/CultivationValidation.cs` | Menu `Campus Rift/V2/Validate Cultivation` (52 kiểm tra, không cần Play Mode) |

Save cũ `learning-v1.json` được sao thành `learning-v1.backup.json` (không sửa file gốc). Hồ sơ V2 bắt đầu mới.
Settings vẫn lưu riêng.
