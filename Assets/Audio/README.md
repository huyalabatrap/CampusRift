# Audio

Hiệu ứng âm thanh do người dùng cung cấp (`Resources/Sfx`): quai-nho-2 và quai-nho-3 (quái nhỏ), quai-lon-1 (quái lớn),
that-bai-1..2 (thua), win (thắng). Nguồn: người dùng gửi ngày 2026-09-30; giấy phép do người dùng chịu trách nhiệm.

- `Runtime/GameSfx.cs`: phát ngẫu nhiên trong nhóm (không lặp liền một clip), có giới hạn số âm cùng lúc.
- Quái nhỏ dùng đúng một clip cho mỗi màn: `SmallMonsterForLevel` chọn quai-nho-2 ở màn lẻ và quai-nho-3 ở màn chẵn. Mọi quái nhỏ và quái triệu hồi dùng chung clip; chỉ con gần người chơi nhất được nghe. Tháp/Ác Mộng dùng số màn nguồn. Quái lớn (Shaban, `MonsterCombat`) dùng `quai-lon-1`.
- Thắng/thua phát khi `UIStateManager` vào `Victory`/`GameOver` (bỏ qua pause âm thanh).
- Âm thanh của kỹ năng bị tắt (`Skills.SkillAudio.Enabled = false`).
