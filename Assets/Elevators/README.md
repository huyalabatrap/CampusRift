# Thang máy Campus Rift

Mở `Assets/Scenes/SampleScene.unity` và nhấn Play. Hệ thống có 12 cụm thang, phục vụ 102 điểm dừng trong mô hình gốc.

## Sử dụng

1. Đi vào sảnh tòa nhà, đến gần cửa thang (trong khoảng 2,3 m).
2. Đến trong khoảng 1,8 m để tự gọi cabin về tầng đang đứng; cũng có thể nhấn **E** để gọi trong khoảng 2,3 m.
3. Khi cửa mở, bước hẳn vào cabin và nhấn **E** để mở bảng chọn tầng.
4. Bấm số tầng, hoặc dùng **↑ / ↓** và **Enter**. **Esc** đóng bảng.
5. Chờ cửa đóng và cabin di chuyển. Khi đến nơi, cửa tự mở; dùng WASD để bước ra.

Thang E gần điểm xuất phát: đi vào sảnh E phía sau nhân vật, rồi tìm biển **E 01** ở cuối sảnh. Tầng trên giao diện đánh số từ 01, tương ứng các cửa `F01` trong FBX.

| Khối | Số tầng phục vụ |
| --- | ---: |
| A | 9 |
| B | 7 |
| C | 3 |
| D | 6 |
| E | 6 |
| F | 6 |
| G | 8 |
| H | 11 |
| I | 14 |
| T | 4 |
| V | 15 |
| X | 13 |

## Hoạt động

- Cửa tầng và cửa cabin trượt đồng bộ, chỉ mở tại tầng có cabin.
- Cabin tăng/giảm tốc, mang nhân vật theo cùng độ dịch chuyển. Có thể xoay camera trong hành trình; di chuyển và nhảy tạm khóa khi thang chạy.
- Khi tường cabin ép camera quá sát nhân vật, mô hình nhân vật tạm ẩn để không che tầm nhìn; hiện lại khi camera có đủ khoảng cách.
- Cửa giữ mở 5 giây; nếu nhân vật đứng ở ngưỡng cửa thì giữ mở hoặc mở lại.
- Yêu cầu đến nhiều tầng được xếp hàng; yêu cầu trùng được gộp.
- Bảng số tầng cập nhật theo hành trình, có chuông báo khi đến tầng.
- Các cụm có hai cabin trong cùng mesh gốc hiện di chuyển đồng bộ và dùng chung hàng đợi.

## Cấu trúc

`Campus Elevators` trong scene chứa toàn bộ cabin, cửa tầng và bảng hiển thị. `CampusElevator` quản lý hành trình; `ElevatorInteraction` trên `Campus Explorer` xử lý phím E và bảng chọn tầng. Tốc độ, gia tốc và thời gian cửa có thể chỉnh trên từng `CampusElevator` trong Inspector.

Cabin và cửa giữ vật liệu, hình dáng từ FBX. Các renderer/collider tĩnh tương ứng được tắt bằng prefab override; cửa động được tách thành các mesh trong `DoorMeshes.asset`. `WorldText.shader` giúp bảng số tầng bị che đúng bởi tường. File FBX nguồn không bị sửa.

Menu **Campus Rift → Set Up Elevators** dùng để thiết lập trong scene mới có cùng mô hình và nhân vật; có chặn chạy trùng khi đã có `Campus Elevators`. Không cần chạy lại trong SampleScene.

Kiểm tra đã thực hiện trong Play Mode: lên tầng cao nhất và ra cabin ở cả 12 khối; gọi từ tầng khác; đi xuống có trọng lực; cửa chống kẹt; hàng đợi; cabin thứ hai của thang E. Kết quả kiểm tra lưu tại `Temp/elevator-travel-tests.json` và `Temp/elevator-behavior-tests.json` (Unity có thể xóa thư mục Temp khi khởi động lại).
