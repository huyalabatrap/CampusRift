# P00 — Chuẩn bị dự án

> **Mục tiêu:** chuẩn bị an toàn trước khi thay đổi lớn: sao lưu, đo mốc hồi quy, tạo thư mục module, thêm layer vật lý.
>
> **Phạm vi:** MVP · **Ước lượng:** 1 ngày công · **Phụ thuộc:** — · **Tham chiếu:** §1, §14
>
> **Kết quả:** có bản sao lưu; biết test nào đang PASS/FAIL trước V2; khung thư mục và layer sẵn sàng.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P00-T01 | Sao lưu scene, prefab, dữ liệu, save | Công cụ | 0,1 | — | ✅ |
| P00-T02 | Chạy bộ hồi quy lấy mốc (baseline) | Test | 0,4 | T01 | ✅ |
| P00-T03 | Tạo thư mục module V2 | Code | 0,1 | — | ✅ |
| P00-T04 | Thêm layer vật lý, rà soát LayerMask | Code | 0,4 | T02 | ✅ |

---

## P00-T01 — Sao lưu scene, prefab, dữ liệu, save

- **Loại:** Công cụ · **Phạm vi:** MVP · **Ước lượng:** 0,1 ngày · **Phụ thuộc:** —
- **Mục tiêu:** có điểm quay lại cho mọi file mà V2 sẽ sửa.
- **File cần sao lưu** vào `Backups/V2-<yyyyMMdd>/`, giữ nguyên cấu trúc thư mục:
  - `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/SampleScene.unity`
  - `Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab`
  - `Assets/MonsterShaban/Monster_Shaban.prefab`, `Assets/MonsterShaban/MonsterAIConfig.asset`
  - `Assets/Skills/GiantHandSeal/GiantHandConfig.asset`, `Assets/Skills/VoidWall/VoidWallConfig.asset`
  - `Assets/Learning/Resources/LearningCatalog.asset` và toàn bộ `Assets/Learning/Data/`
  - `Assets/Localization/Resources/LocalizationCatalog.asset`
  - Save người chơi trong `Application.persistentDataPath`: `learning-v1.json`, `learning-v1.json.bak` (nếu có), file settings
- **Các bước:**
  1. Tạo thư mục `Backups/V2-<yyyyMMdd>/`.
  2. Copy các file trên. Với asset thì copy kèm file `.meta`.
  3. Viết `Backups/V2-<yyyyMMdd>/README.md`: liệt kê file đã sao lưu và cách khôi phục.
- **Hoàn thành khi:**
  - [x] Đủ file trong danh sách.
  - [x] Có README hướng dẫn khôi phục.
- **Kiểm thử:** so sánh dung lượng hoặc hash giữa file gốc và bản sao.

## P00-T02 — Chạy bộ hồi quy lấy mốc (baseline)

- **Loại:** Test · **Phạm vi:** MVP · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P00-T01
- **Mục tiêu:** biết chính xác test nào đang PASS/FAIL **trước** khi làm V2, để sau này không đổ lỗi nhầm cho V2.
- **Các bước:**
  1. Chạy toàn bộ harness ở mục 8 của README (theo quy trình Play Mode hiện có).
  2. Ghi kết quả vào `Artifacts/V2/Baseline.md`: tên harness, PASS/FAIL, số assertion, lỗi có sẵn (nếu có).
- **Hoàn thành khi:**
  - [x] `Baseline.md` có đủ mọi harness trong danh sách.
  - [x] Lỗi có sẵn được ghi rõ nguyên nhân hoặc ghi "chưa rõ".
- **Kiểm thử:** không áp dụng (đây chính là bước đo).

## P00-T03 — Tạo thư mục module V2

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,1 ngày · **Phụ thuộc:** —
- **Mục tiêu:** có khung thư mục thống nhất cho code mới.
- **Các bước:**
  1. Tạo các thư mục:
     - `Assets/Combat/{Runtime,Data,Editor,Validation}`
     - `Assets/Skills/Core/{Runtime,Data,Editor}`
     - `Assets/Enemies/{Runtime,Data,Models,Editor,Validation}`
     - `Assets/Levels/{Runtime,Data,Editor,Validation}`
     - `Assets/SkyBeast/{Runtime,Data,Models,Editor,Validation}`
     - `Assets/Progression/{Runtime,Data,Editor,Validation}`
  2. Mỗi module có một `README.md` ngắn: mục đích, namespace, các file chính (cập nhật dần).
  3. Refresh Unity để sinh file `.meta`.
- **Hoàn thành khi:**
  - [x] Đủ thư mục kèm `.meta`.
  - [x] Không có lỗi compile.

## P00-T04 — Thêm layer vật lý, rà soát LayerMask

- **Loại:** Code · **Phạm vi:** MVP · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P00-T02
- **Mục tiêu:** tách va chạm giữa người chơi, quái, đòn đánh và công trình. Cần cho đạn, kỹ năng và phát hiện trong nhà/ngoài trời.
- **Hiện trạng:** `ProjectSettings/TagManager.asset` chỉ có layer mặc định; mọi thứ trong scene nằm ở Default (0) hoặc UI (5).
- **File:** (sửa) `ProjectSettings/TagManager.asset`, `ProjectSettings/DynamicsManager.asset` (ma trận va chạm), `CampusExplorer.prefab`, `Monster_Shaban.prefab`.
- **Các bước:**
  1. Thêm các layer: `Player`, `Enemy`, `PlayerAttack`, `EnemyAttack`, `SkyBeast`, `Environment`.
  2. Rà soát mọi chỗ dùng mask trong code: grep `LayerMask`, `Physics.Raycast`, `SphereCast`, `OverlapSphere`, `cameraObstacles`, `ClearLine`. Rà cả các field LayerMask được serialize trong prefab và scene.
  3. Gán layer `Player` cho CampusExplorer, `Enemy` cho Shaban. Sửa mask nào đang ngầm dựa vào Default.
  4. Ma trận va chạm:
     - `PlayerAttack` bỏ qua `Player`.
     - `EnemyAttack` bỏ qua `Enemy`.
     - `SkyBeast` không va chạm với layer nào.
  5. **Chưa** gán `Environment` cho công trình campus. Việc đó làm ở P13-T01.
- **Hoàn thành khi:**
  - [x] Không có hành vi nào thay đổi: camera né vật cản, cửa tự động, thang máy, tầm nhìn của Shaban, Giant Hand nhắm mục tiêu.
  - [x] Nhóm harness Quái, Kỹ năng và `MobileChasePlayTest` cho kết quả như baseline.
- **Kiểm thử:** chạy lại các nhóm harness trên và so sánh với `Artifacts/V2/Baseline.md`.

---

## Kiểm chứng cuối phase

- [x] Có `Artifacts/V2/Baseline.md`.
- [x] Hồi quy không thay đổi so với baseline.
- [x] Cập nhật trạng thái P00 trong `task/README.md`.
