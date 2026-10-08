# Enemies

Quái dạng dữ liệu (`EnemyArchetype`), AI nhẹ (`MinionBrain`, `MinionMotor`), điều phối tấn công (`EnemyDirector`), đạn, pool, tinh anh và phụ tố, boss.

- Namespace: `CampusRift.Enemies`
- Model và giấy phép: `Models/LICENSES.md`
- Task: `task/P04`, `task/P12`, `task/P16`, `task/P19`

`EnemyDirector.Squad` phân vai và tuyến riêng mỗi0,5s, tối đa3query NavMesh/frame chung với Warning. T1–T2 giữ nhịp nhẹ; T3–T4 phân8slot trong4cung, chọn quái nhanh/gần cho hai cánh trước và quái phía sau cho Chaser. Prediction2–4s dùng vận tốc quan sát + RoomGraph. Cánh tới sector trước khi đánh; interceptor giữ điểm phía trước khi player đang chạy. Khi coverage vị trí thật≥250° thì khép từ4,3m xuống≥2,7m trong2s; dưới205° thì tạo vòng lại.

T3–T4 chừa một lối35° có đường NavMesh hoàn chỉnh, giữ7s rồi đổi phía; Warning giữ cửa shelter gần nhất. Nhóm≥6 dành1rear giữ cửa, giữ cùng owner qua các lần scan để không kéo cả đội khỏi vòng. Guard chiếu nghỉ giữ8s hoặc tới khi player vượt tầng; escape adjustment giữ nguyên y. Attack caps, telegraph, slow/stun, retreat/special và T4 mở radius8m sau3AoE vẫn ưu tiên. Shaban dùng AI săn riêng.

`AITierProfiles` cấu hình `phasedEncirclement`, `closeCoverage`, `formationSpeed`, `escapeDegrees`. Sprint chiến thuật hiện dùng hệ số2 khi chạy tới cánh, chỉ trên FollowPath squad; tới slot/đánh/rút lui/giữ cửa trả tốc độ thường. Dùng animation và FootPlant sẵn có. Đây là thay đổi gameplay cần người dùng chơi thử: **cân bằng chưa đo thực chiến**, chưa đo Android native/GC/FPS.

[Báo cáo fix1](../../task/ai/REPORT-AI-SQUAD-fix1.md), [dataset](../../Artifacts/AI/Squad-fix1.json), [ảnh thật](../../task/ai/screens/fix1/): Squad33PASS/0FAIL; sân248,12°/Warning248,94° average vị trí thật, Jaccard tốt hơn bản1,2interceptor/case yêu cầu,3guard ở landing và escape100% mẫu. Đây là smoke4case8s, không chứng minh mọi va chạm/tuyến/player tốc độ khác đều còn thoát được. Các lượt lỗi/giới hạn được giữ trong PROGRESS và báo cáo.
