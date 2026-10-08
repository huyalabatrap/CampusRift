# Danh sách đánh bóng nhỏ (gom lại, làm trong một đợt polish sau P19)

- [x] P15 cảnh diễn Vạn Kiếm Quy Tông: camera vẫn ở trên tầng mây, không thấy campus; khung impact hơi bạc màu (thiếu tương phản/độ bão hòa vàng).
- [x] P16 thanh Kiếm Ý hiện "0% · 0/0" khi đợt chưa sinh quái → ẩn số đếm hoặc ghi "chờ đợt quái".
- [x] P16 panel Đổi kỹ năng (mobile): hàng kỹ năng thứ 3 bị cắt ngang ở mép dưới, chưa có dấu hiệu cuộn → thêm fade/mũi tên cuộn hoặc co lưới.
- [x] P19 hào quang tinh anh phụ tố Hỏa Tâm/lửa (`task/p19/screens/elite-two-affixes.png`, `level10-crowd.png`): đám khói lửa cam khổng lồ che quái và cả nhóm quái phía sau → thu nhỏ về sát thân (~1,2× bounds), lửa mảnh liếm theo thân + vòng dưới chân, alpha thấp, giới hạn kích thước trên màn hình.
- [x] P19 bảng tên tinh anh trên đầu quái (tên + phụ tố + icon) quá nhỏ, không đọc được → tăng cỡ chữ/icon, nền lót comic, chỉ hiện đầy đủ khi gần/đang khóa mục tiêu.
- [x] MODELS (`task/models/screens/*-comparison.png`): **Dực Yêu (dơi) quá to** — sải cánh gần bằng sân khấu, che cả khung hình; brief yêu cầu cỡ người → thu về sải cánh ~2,5–3,5 m, kiểm hitbox/telegraph theo scale mới. **Ảnh Yêu** trông như ma-nơ-canh trơn (thân người tím đồng màu, không chi tiết, đứng tư thế A) → thêm áo choàng rách/khăn che mặt/dao (mesh gắn xương hoặc model chi tiết hơn), texture có chi tiết; không để đứng tư thế A trong lúc idle. Hỏa Linh (golem dung nham) và Triệu Hồn Sư (pháp sư áo chùng) đạt.
- [x] P16 tên kỹ năng dài ("Thần Kiếm Ngự Lôi Chân Quyết") bị thu nhỏ chữ khác các ô còn lại → xuống dòng thay vì co chữ.

## Đợt 2 (sau P20) — làm trong P23 hoặc đợt polish kế tiếp
- [ ] P20 Linh Bia (`task/p20/screens/linh-bia-world.png`): khối hộp đen đơn giản với biểu tượng mũi tên xanh phẳng — trông như placeholder, lệch phong cách campus thực tế. Làm thành bia đá cổ: đá xám phong hóa có rêu/vết nứt, chữ/phù văn khắc phát sáng ngọc nhẹ, đế bậc đá; VFX ánh sáng nhẹ khi có thể tương tác.
- [ ] P20 HUD Thiên Hỏa trong nhà hiện mũi tên dẫn đường "ĐANG ĐƯỢC CHE" thừa → ẩn mũi tên khi đã Indoor.
- [ ] P21 Hỏa Long Vương (`task/p21/screens/identity-long-vuong.png`): tint đỏ phủ đặc toàn thân làm mất chi tiết vảy/khối; con rồng nhỏ giữa khung. Giữ màu đỏ-đen nhưng để lộ chi tiết (emission theo vân/viền, không phủ đặc), camera cảnh lộ mặt gần hơn để rồng chiếm ~1/3 khung.
- [ ] P20 `LearningPlayTest` legacy 19/2 (tìm vòng HUD cũ đã bị LevelHUD ẩn → NullReference trong fixture) → cập nhật fixture theo HUD hiện hành.

Hoàn tất04/10/2026: cả8mục (dòngMODELS gồm hai model). [Báo cáo STABILIZE](stabilize/REPORT-STABILIZE.md) có ảnh trước/sau và audit; polish13/0, model30/0.
