# Shaban: tấn công và máu văng

- `Shaban_ClawAttack.anim`: clip tạo riêng cho rig Generic hiện có. Lấy đà, vung tay phải, nghiêng người tới trước và thu tay. Không thay model hoặc chuyển rig sang Humanoid.
- `ShabanAnimator.controller`: trạng thái `Attack` trở lại `OriginalAnimation` sau đòn đánh. Tham số `AttackSpeed` đồng bộ tốc độ clip với `MonsterAIConfig.AttackWindup`.
- Mặc định: trúng đòn ở 0,35 giây, kết thúc động tác ở 0,9 giây; giữ cooldown 1,8 giây và sát thương 25. Quái dừng bước trong lúc đánh. Người chơi ra ngoài tầm hoặc có vật cản trước thời điểm trúng đòn sẽ tránh được sát thương.
- `BloodImpact.prefab`: giọt máu và tia máu mịn, màu đỏ sẫm, có trọng lực và tự tan sau tối đa 0,65 giây. Shader URP có sẵn trong project, không cần texture hoặc package bên ngoài.
- `PlayerBloodFeedback` đã gắn vào prefab nhân vật và nhân vật trong `SampleScene`; hiệu ứng chỉ phát khi `PlayerMonsterHealth` chấp nhận sát thương. Mỗi nhân vật tái sử dụng tối đa 4 bộ hiệu ứng. Đòn kết liễu phát hạt ngay trước khi màn Game Over dừng thời gian.

Chỉnh số lượng, tốc độ, kích thước và màu giọt máu trực tiếp trong prefab. Để tạo lại asset mặc định: **Campus Rift > Shaban > Build Attack and Blood Effects**. Lệnh này ghi lại clip và prefab hiệu ứng; lưu scene sau khi chạy.

Kiểm thử Play Mode: `../Validation/CombatPlayMode.json` — 15 đạt, 0 lỗi. Bao gồm thời điểm sát thương, animation, cooldown/recovery, miễn sát thương, né, tường chắn, đổi windup, hạt tự hết và đòn kết liễu. Harness `ShabanCombatPlayTest` chỉ dùng trong Editor, không gắn vào scene/prefab sản phẩm.

Ảnh kiểm tra thực tế: `Artifacts/Combat/01-Windup.png` và `Artifacts/Combat/02-Impact.png` (tính từ thư mục project). Bản sao trước chỉnh sửa: `Backups/CombatAnimation-20260927`.
