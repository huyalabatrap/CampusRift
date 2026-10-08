# Cửa tự động

Trong `SampleScene`, đến gần cửa để cửa tự mở; đi ra xa để cửa đóng sau khoảng 1,2 giây. Không cần bấm phím.

- **12 cửa chính:** hai cánh kính trượt sang hai bên.
- **395 cửa nội thất:** tách từ các mesh cửa gốc và xoay quanh bản lề.
- **79 cửa cầu thang/hội trường:** chuyển cánh cửa tĩnh thành cửa bản lề.
- Cửa nhận diện nhân vật từ hai phía. Cánh bản lề mở về phía tránh người đang đến; giữ mở nếu người còn đứng trong ngưỡng cửa.
- Thang máy tự được gọi khi người đứng cách cửa khoảng 1,8 m. Cửa tầng chỉ mở khi cabin đến đúng tầng. Vào cabin và nhấn **E** để chọn tầng.

`Campus Automatic Doors` chứa 486 bộ điều khiển `CampusAutomaticDoor`. Trong Inspector có thể chỉnh khoảng cách nhận diện, thời gian hoạt ảnh và thời gian chờ đóng. Hình học, vật liệu các cửa nội thất và cửa cầu thang được lấy từ FBX. Cửa kính chính bổ sung cánh trượt vào các khoảng mở có sẵn.

Menu **Campus Rift → Set Up Automatic Doors** chỉ dùng cho scene chưa thiết lập. Menu có chặn tạo trùng; không cần chạy lại trong SampleScene.

Đã kiểm tra logic mở hai phía, giữ mở chống kẹt và tự đóng trên cả 486 cửa; kiểm tra nhân vật đi xuyên qua các cửa đại diện bằng CharacterController. Chỉ các cửa có lối đi thật được triển khai; các tấm cửa phụ trang trí trên tường đặc vẫn là chi tiết mô hình.
