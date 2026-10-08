# Nguồn tham khảo và thiết kế chuyển động rồng

Các clip được dựng mới bằng keyframe, không tải hoặc sao chép animation thương mại. Nguồn được dùng để chọn trạng thái và nguyên tắc chuyển động.

- [Realistic Dragons & Wyverns — danh mục của tác giả](https://www.artstation.com/marketplace/p/klYxz/realistic-dragons-wyverns-mega-pack): tham khảo nhóm idle, fly, glide, takeoff, land, attack, dive, get hit và death.
- [Stylized Fantasy Dragon — trang sản phẩm chính thức](https://www.unrealengine.com/marketplace/ja/product/stylized-fantasy-dragon): tham khảo cách phân chia hành vi mặt đất/trên không và các giai đoạn bắt đầu, lặp, kết thúc.
- [Dragon Flight — Randall Glass](https://vimeo.com/303429199): tham khảo việc kết hợp nhịp đập cánh và lượn, cùng chuyển động phụ của đầu và cánh.
- [Brown University — Fold and flap](https://archive2.news.brown.edu/2007-2015/articles/2012/04/foldflap.html): cánh dơi thu gọn khi hồi cánh. [Nghiên cứu cơ màng cánh](https://engineering.brown.edu/news/2014-05-24/tiny-muscles-help-bats-fine-tune-flight): màng cánh có tính chất thay đổi trong nhịp bay. Đây là tham chiếu cơ học cho rồng có màng cánh, không phải chứng minh cách rồng giả tưởng bay.

## 020 — Silver Cloud Dragon

Rồng dài không cánh, hai chân trước, râu và đuôi có lông. Dùng sóng uốn truyền dọc thân, đuôi trễ pha, đầu ổn định tương đối và râu dao động nhẹ. Bay mang tính huyền ảo; rẽ bằng nghiêng toàn thân. Thêm Cloud_Coil, Celestial_Spiral, Cloud_Dash, Fur_Ripple và Graceful_Bow. Giữ hình cuộn vốn có của mesh để tránh ép duỗi thân làm rách/méo da.

## 023 — Azure Serpent Dragon

Rồng dạng rắn với bốn chân và thân cong. Sóng thân ngắn hơn, đuôi có biên độ lớn hơn 020; chân co nhẹ khi bay, đầu theo hướng nhìn. Thêm Serpent_Coil, Sky_Weave, Serpent_Dash, Coil_Strike và Tail_Whip. Đây là thiết kế suy ra từ hình dáng model; không gán chuyển động của loài vật có thật cho rồng không cánh.

## 026 — Lava Wing Dragon

Bốn chân và hai cánh màng. Nhịp đập gồm pha đẩy xuống ngắn và pha hồi dài hơn; cẳng cánh, cổ tay và các ngón màng cánh chuyển động trễ pha. Lượn giảm nhịp đập; rẽ có chênh lệch hai cánh; chân co khi bay. Có 8 đoạn đuôi, ngón chân và xương hàm bổ sung. Thêm Wing_Fold, Wing_Unfold, Idle_Ground, Walk_Ground, Run_Ground, Ground_Roar, FireBreath_Ground, Ground_Bite, Ground_Death, Ground_Hit và Lava_Pulse.

## Kiểm tra và giới hạn

58.000 triangles mỗi mesh. Giữ UV, texture gốc và tối đa 4 trọng số/xương mỗi vertex. So sánh bề mặt trước/sau bằng khoảng 25.000 điểm mẫu mỗi model; kiểm tra ảnh silhouette và các tư thế bay. Animation được kiểm tra giãn cạnh tại 6 thời điểm/clip và khép vòng bằng ma trận xương đầu/cuối. Những phép kiểm tra này có giới hạn và không đảm bảo tuyệt đối không có lỗi ở mọi góc nhìn hay mọi phối trộn.

Animation dựng bằng quy trình procedural, có keyframe riêng theo hình dáng từng model, chưa phải diễn xuất được animator chỉnh tay từng khung hình. Đi/bay tại chỗ; đường bay và va chạm do gameplay điều khiển. Socket_Mouth, Socket_Rider và Socket_TailTip hỗ trợ gắn VFX/đồ vật; không kèm hạt lửa hay phép thuật. Hai rồng không cánh giữ dáng thân cuộn nguyên bản, không có clip duỗi thẳng hoàn toàn.
