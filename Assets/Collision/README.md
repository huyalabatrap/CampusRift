# Collider và dọn cảnh

Đã loại khỏi cảnh chơi các khối dựng hình P0/QA/SNAP, các mẫu kit đặt ngoài khuôn viên và thư viện mẫu cây, xe, ghế bị xuất chồng tại gốc tọa độ. Có 121 đối tượng phụ được tắt bằng prefab override (113 đối tượng có renderer); FBX nguồn giữ nguyên.

Cụm cột đèn số 3 gồm 6 chi tiết nằm ngay giữa lối vào E cũng được loại khỏi cảnh chơi để thông lối và không che camera.

Collider tĩnh được dựng lại từ hình học:

- BoxCollider chỉ dùng cho mesh là khối hộp kín thật; không bọc toàn bộ phòng, cửa hay cầu thang trong một hộp lớn.
- MeshCollider không convex giữ nguyên các lỗ cửa, lỗ sàn và giếng thang.
- Loại collider thừa của biển chữ, tay nắm, đèn, chi tiết cửa sổ và đường kẻ sân.
- Cầu thang có mặt dốc sử dụng collider mặt dốc thay cho collider bậc chồng lên nhau.
- Bổ sung 122 bề mặt kết cấu bị thiếu, gồm thân tòa nhà, vách, chiếu nghỉ và ngưỡng nối.
- Collider trên cửa và cabin chuyển động được quản lý riêng bởi hệ thống cửa tự động và thang máy.

Lối vào T bị mặt kính, tường chân và thân khối F/X chồng lên. Tám mesh tại đây được cắt đúng khoảng cửa rộng 2,2 m, cao 2,5 m; các phần ngoài khoảng cửa được giữ lại. Mesh đã sửa lưu trong thư mục này.

Menu **Campus Rift → Clean Scene and Rebuild Colliders** có thể chạy lại để cập nhật collider tĩnh. Menu **Repair Overlapping T Entrance** sửa phần chồng lấn đã xác định tại cửa T. Bản sao scene trước khi sửa nằm trong `Backups/`, bên ngoài Assets.

Kiểm tra Play Mode: đi qua cửa vào của 12 khối, leo vế cầu thang đại diện ở mỗi khối, và lên tầng cao nhất rồi ra cabin ở cả 12 cụm thang máy. Báo cáo từng collider được lưu trong `CleanupReport.json`; kết quả kiểm tra chi tiết ở thư mục `Temp/` của project.
