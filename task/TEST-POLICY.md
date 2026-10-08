# Chính sách kiểm thử (người dùng, 02/10/2026 21:30) — ÁP DỤNG NGAY, ưu tiên hơn yêu cầu kiểm thử trong mọi brief

Người dùng yêu cầu: **các bước tiếp theo không cần test quá kĩ, chỉ test đơn giản để tiết kiệm thời gian.**

- Chỉ chạy **smoke test**: Unity 0 lỗi biên dịch; vào được màn liên quan, tính năng chính hoạt động (spawn, đánh, chết, kỹ năng/nút bấm phản hồi); không exception mới trong Console.
- Chỉ chạy lại harness **trực tiếp liên quan** tới phần vừa sửa, mỗi harness 1 lượt; không chạy lại toàn bộ nhóm hồi quy P10/P11/Hub trừ khi phần sửa chạm vào code của chúng.
- **Bỏ**: benchmark PC standalone/native build, đo FPS nhiều lượt, bot cân bằng tự động nhiều vòng (boss 60–90 s/90–120 s), 50 trial thống kê, ảnh nhiều độ phân giải × ngôn ngữ × nền sáng/tối. Cân bằng: chọn số hợp lý theo đặc tả/§16, ghi chú "chưa đo thực chiến", để người dùng chơi thử điều chỉnh sau.
- Ảnh kiểm tra: **mỗi hạng mục 1–2 ảnh** đủ để người điều phối soi hình (không cần sheet 8 frame sáng/tối/mobile).
- Không mất công sửa harness/bot đo lường. Lỗi gameplay thật phát hiện được thì vẫn sửa.
- Báo cáo ngắn gọn: đã làm gì, ảnh nào, phần nào chưa kiểm/chưa đạt.
- Vẫn bắt buộc: sao lưu trước khi sửa, không phá geometry/NavMesh, trả Unity về **Android** + Edit Mode + 0 lỗi console khi xong.
