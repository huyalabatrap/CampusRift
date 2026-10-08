# Phiên Kiểm Ấn khoảng 15 phút — Gói A

Dùng APK development `APK-Test/CampusRift-AR-dev-20261006-goiA.apk` trên Realme RMX3031. Lượt triển khai chưa đo nhận dạng trên máy thật.

## Chuẩn bị và mở

- Bàn phẳng rộng, đủ sáng; thêm một vị trí gần mép và một chỗ quá hẹp cho vòng trận.
- Thước đánh dấu **25 / 40 / 55 cm**, đo từ camera sau đến bàn tay. App không tự đo khoảng cách tay.
- Ngồi yên, dùng một tay quen thao tác. Xoay máy nhẹ để tâm ngắm trong trận. Giữ đủ đầu ngón trong khung, đưa ngón cầm máy khỏi ống kính.
- Cắm USB vào máy tính và cho phép USB debugging. Cắm cáp là đủ; người điều phối đọc kết quả, bạn không cần tải ảnh hay điểm tay.
- CHƠI → Sảnh → AR RIFT → đồng ý an toàn/camera → **☰ → Kiểm Ấn**. Chọn tay trái/phải, ánh sáng tự ước lượng, giữ bộ nhận **AUTO**. Bật **ĐỒNG Ý XUẤT CHỈ SỐ**, nhấn **BẮT ĐẦU**. Đồng ý chỉ cho phiên này; tắt sẽ xóa RAM.

## Làm theo màn hình

| Thời gian | Thao tác |
|---|---|
| 0–1 phút | Làm quen ở **40 cm**: giơ lòng rồi mu tay, hạ tay giữa các lần. Warm-up không tính trial. |
| 1–3 phút | Đặt **10 lần**, mỗi lượt 12 giây: 6 bàn rộng, 2 gần mép nhưng vòng nằm trọn trên bàn, 2 chỗ quá hẹp phải từ chối. Chạm **ĐẶT TRẬN** khi vòng hợp lệ; vòng mờ “đang tìm mặt” chưa được đặt. Lượt tìm mặt quá 10 giây được ghi timeout. |
| 3–3,5 phút | Đặt lại trên bàn rộng; máy tự bật tải sáu quái. Giữ tâm ngắm trong trận. |
| 3,5–9,5 phút | **120 lượt**: năm tư thế × 25/40/55 cm × lòng/mu × bốn lần. Theo tên/hình tay trên màn hình. Mỗi nhóm có 5 giây đổi khoảng cách/hướng; mỗi lượt 2,25 giây giơ/giữ rồi 0,5 giây hạ tay. |
| 9,5–12,5 phút | Sáu đoạn 30 giây: không tay; ngón cái lên/“I love you”; đổi tư thế liên tục; ngón cầm máy lọt mép rồi bỏ ra; giữ năm tư thế mỗi tư thế 6 giây; thử từ chối hồi chiêu/thiếu Linh Lực/ngắm sai. Ở đoạn cuối, **hạ tay khi đổi thông báo rồi giơ lại**; giữ nguyên sau từ chối. Sau 5 giây app bỏ lý do từ chối giả lập, vẫn không được tự ra chiêu lại. |
| 12,5–13,5 phút | Chơi với sáu quái; máy phát năm VFX theo lịch cách nhau 10 giây nếu tâm ngắm hợp lệ. Mở/đóng ☰ một lần; nhấn Home rồi trở lại nếu kịp. Sau trở lại phải hạ tay. |
| 13,5–15 phút | Đọc bảng, nhấn **XUẤT KẾT QUẢ**. Mỗi phiên xuất một lần. Phút cuối là dự phòng. |

Biết mình làm sai yêu cầu thì nhấn **BỎ LƯỢT**. “Chưa nhả tay xong” thì hạ tay; thời gian lượt vẫn chạy. Một tư thế chỉ phát một intent, kể cả khi bị từ chối. Sau từ chối phải hạ tay rồi giơ lại; hết hồi chiêu không tự thử lại khi còn giữ tay. Chuyển sang tư thế khác ổn định được phép sau cast thành công.

## Đọc và gửi kết quả

- Có 30 ô, mỗi ô bốn lượt; mục tiêu quan sát **≥3/4 đúng duy nhất**, xem riêng lòng/mu. Ô ≤2/4 cần điều tra. Giữ nguyên lỗi nhận dạng; skip chỉ khi biết mình làm sai yêu cầu.
- Báo chiêu sai/lặp/tự thử lại, đặt nhầm trên preview/chỗ hẹp. Tám lượt đặt hợp lệ giữ cả timeout trong mẫu, hai lượt hẹp phải từ chối.
- Menu/Home tiêu thụ thời gian lịch; app không kéo dài ngầm. Thiếu lượt hoặc tải hoạt động dưới 600 giây sẽ ghi **chưa đủ phiên**.
- Giữ app mở đến khi xuất/đọc log. Đóng app trước xuất sẽ mất phiên. Nếu lỗi nhận dạng, dùng **THỬ LẠI NHẬN DẠNG**, ghi thời điểm/thông báo. CPU/GPU dành cho lượt điều tra riêng.
- Cắm cáp là đủ. Người điều phối chạy `adb -s WGH6S8I7GIMBGQKR logcat -d -s Unity`, tìm và lưu nguyên dòng **`[ARCheck] {json}`**. Dòng này chứa số liệu tổng hợp, không ảnh, landmark hay tọa độ phòng; tối đa 3500 byte. Nếu không có dòng hoàn chỉnh, báo nguyên trạng.
- Độ trễ tay đến màn hình (`physical`) để trống vì chưa quay chậm. Một phiên một người là smoke, chưa chứng nhận độ chính xác cho mọi người/máy.
