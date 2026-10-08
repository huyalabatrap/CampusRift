# Danh sách hình ảnh cần bạn cung cấp

> Tổng hợp từ các phase P00–P23. Chỉ gồm **ảnh 2D** (icon, chân dung, hình nền).
> Model 3D, texture hiệu ứng, khung UI thì tôi tự tìm hoặc tự tạo (xem mục "Không cần cung cấp" ở cuối).
>
> **Mô tả chi tiết từng ảnh "Bắt buộc" và "Nên có"** (chủ thể, bố cục, màu, điều cần tránh, prompt gợi ý cho AI): xem [HINH-ANH-MO-TA-CHI-TIET.md](HINH-ANH-MO-TA-CHI-TIET.md).
>
> **Nếu bạn chưa kịp gửi ảnh nào, tôi sẽ dùng ảnh tạm** (hình vẽ bằng code) để không chặn tiến độ. Khi có ảnh thật chỉ cần thả vào thư mục là thay được.

---

## 1. Quy cách chung

| Mục | Yêu cầu |
|---|---|
| Định dạng | PNG, **nền trong suốt** (trừ hình nền full màn hình) |
| Nơi đặt file | `Content/Images/<nhóm>/<tên-file>.png` ở gốc dự án (tạo thư mục nếu chưa có) |
| Tên file | Đúng **tên file** ghi trong bảng (chữ thường, gạch nối, không dấu) |
| Phong cách | Theo icon có sẵn `Assets/CampusRiftUI/Art/Skills/GiantHandSeal.png`: huy hiệu tròn, viền kim loại tím và vàng có góc nhọn, nền trong suốt, hiệu ứng phát sáng, chất tu tiên huyền ảo |
| Màu chủ đạo theo hệ | Kim: vàng kim · Mộc: xanh lá · Thủy: xanh băng · Hỏa: đỏ cam · Thổ: nâu vàng · Lôi: tím và vàng · Âm: xám tím · Không Gian: tím đen |
| Bản quyền | Ảnh do bạn vẽ, ảnh AI bạn có quyền dùng, hoặc ảnh có giấy phép cho phép dùng trong game. Mỗi nhóm ghi nguồn vào `Content/Images/NGUON.md` |
| **Nội dung môn học** | **Không** dùng hình Chủ tịch Hồ Chí Minh, trích dẫn hay biểu tượng của môn học trong icon kỹ năng, vật phẩm, quái, huy hiệu (§11.5 kế hoạch). Ảnh minh họa bài học xem mục 11 |

**Mức ưu tiên:**
- 🔴 **Bắt buộc:** cần cho bản MVP, trước phase ghi trong bảng.
- 🟡 **Nên có:** MVP chạy được bằng ảnh tạm, nhưng ảnh thật đẹp hơn nhiều.
- ⚪ **Tùy chọn / bản đầy đủ:** Phần B (P18–P23) hoặc tôi có thể tự làm.

---

## 2. Tổng quan

| # | Nhóm | Số ảnh | Kích thước | Cần trước | Ưu tiên |
|:-:|---|:-:|---|---|:-:|
| 3 | Icon kỹ năng (MVP) | 8 | 512×512 | P10, P15 | 🔴 |
| 3 | Icon kỹ năng (bản đầy đủ) | 11 (+3 làm lại, tùy chọn) | 512×512 | P18 | ⚪ |
| 4 | Icon vật phẩm | 10 MVP + 6 | 256×256 | P08, P20 | 🔴 / ⚪ |
| 5 | Icon pháp bảo | 3 MVP + 2 | 256×256 | P08, P20 | 🔴 / ⚪ |
| 6 | Ký hiệu hệ | 9 | 128×128 | P09 | 🔴 |
| 7 | Huy hiệu cảnh giới | 7 | 256×256 | P06 | 🟡 |
| 8 | Huy hiệu thành thạo, sao | 5 | 128×128 | P07, P09 | ⚪ |
| 9 | Chân dung quái, boss, cự thú | 9 + 2 + 3 | 512×512 | P09, P14 | 🟡 |
| 10 | Biểu tượng trú ẩn | 3 | 128×128 | P13 | 🟡 |
| 11 | Hình nền, nhân vật, bản đồ, màn | 1 + 1 + 1 + 10 + 2 | xem bảng | P09 | 🔴 / 🟡 |
| 12 | Minh họa bài học | tùy nội dung | 1024×576 | P07-T10 | ⚪ |
| 13 | Thành tựu, danh hiệu | ~20 | 256×256 | P22 | ⚪ |
| 14 | Icon app, màn khởi động | 2 | 1024×1024, 1920×1080 | P17 | 🔴 |

---

## 3. Icon kỹ năng — `Content/Images/Skills/` (512×512)

Mô tả là gợi ý hình ảnh; bạn có thể sáng tạo thêm.

### MVP — cần trước P10 (Thiên Kiếm trước P15)

| Tên file | Kỹ năng | Hệ | Gợi ý hình ảnh | Ưu tiên |
|---|---|:-:|---|:-:|
| `tich-lich-nhat-thiem.png` | Tích Lịch Nhất Thiểm | Lôi | Vệt sét vàng lướt ngang, bóng người rút kiếm, tia điện tím | 🔴 |
| `phat-no-hoa-lien.png` | Phật Nộ Hỏa Liên | Hỏa | Hoa sen lửa nhiều tầng cánh, lõi trắng nóng, tàn lửa bay | 🔴 |
| `han-bang-phong-an.png` | Hàn Băng Phong Ấn | Thủy | Gai băng hình nón tỏa ra, tinh thể băng xanh, sương lạnh | 🔴 |
| `than-kiem-ngu-loi.png` | Thần Kiếm Ngự Lôi Chân Quyết | Lôi | Thanh kiếm dựng đứng, sét dây chuyền nhiều nhánh tỏa ra | 🔴 |
| `kim-chung-trao.png` | Kim Chung Tráo | Kim | Chiếc chuông vàng lớn trùm quanh bóng người, hoa văn cổ | 🔴 |
| `hac-dong-than-la.png` | Hắc Động Thần La | Không Gian | Hố đen xoáy, viền tím, mảnh đá bị hút vào tâm | 🔴 |
| `van-kiem-quyet.png` | Vạn Kiếm Quyết | Kim | Hàng chục thanh kiếm vàng rơi từ trời xuống một vòng tròn | 🔴 |
| `thien-kiem.png` | **Thiên Kiếm** (tuyệt kỹ, nút riêng) | Kim | Thanh kiếm vàng khổng lồ đâm xuyên mây, sau lưng là bóng rồng lửa; viền đặc biệt hơn icon thường | 🔴 |

### Bản đầy đủ — cần trước P18

| Tên file | Kỹ năng | Hệ | Gợi ý hình ảnh | Ưu tiên |
|---|---|:-:|---|:-:|
| `hang-long-thap-bat-chuong.png` | Hàng Long Thập Bát Chưởng | Kim | Chưởng hình đầu rồng vàng lao thẳng | ⚪ |
| `tam-muoi-chan-hoa.png` | Tam Muội Chân Hỏa | Hỏa | Luồng lửa hình nón phun ra từ lòng bàn tay | ⚪ |
| `bac-minh-than-cong.png` | Bắc Minh Thần Công | Thủy | Ba luồng năng lượng xanh bị hút về lòng bàn tay | ⚪ |
| `con-bang-cuc-toc.png` | Côn Bằng Cực Tốc | Mộc | Chim bằng khổng lồ sải cánh, vệt gió xanh lá, bóng ảo | ⚪ |
| `thien-loi-dan.png` | Thiên Lôi Dẫn | Lôi | Năm tia sét giáng từ mây đen xuống vòng pháp trận | ⚪ |
| `moc-linh-hoi-xuan.png` | Mộc Linh Hồi Xuân | Mộc | Cây liễu phát sáng, lá xanh bay, vòng hồi phục | ⚪ |
| `than-thuc-linh-nhan.png` | Thần Thức Linh Nhãn | Vô hệ | Con mắt thứ ba phát sáng, sóng quét xuyên tường | ⚪ |
| `tru-tien-kiem-tran.png` | Tru Tiên Kiếm Trận | Kim | Vòng pháp trận với bốn thanh kiếm cắm bốn góc | ⚪ |
| `am-binh-quy-hon.png` | Âm Binh Quy Hồn | Âm | Bóng binh lính tím đen trỗi dậy từ mặt đất, mắt xanh | ⚪ |
| `banh-truong-lanh-dia.png` | Bành Trướng Lãnh Địa | Không Gian | Mái vòm năng lượng phủ xuống, không gian vặn xoắn | ⚪ |
| `vo-hon-chan-than.png` | Võ Hồn Chân Thân | Thổ | Pháp tướng khổng lồ bán trong suốt sau lưng người | ⚪ |

### Đã có sẵn — chỉ làm lại nếu bạn muốn đồng bộ phong cách

| Tên file | Kỹ năng | Ảnh hiện có | Ưu tiên |
|---|---|---|:-:|
| `dai-thu-an.png` | Đại Thủ Ấn | `Assets/CampusRiftUI/Art/Skills/GiantHandSeal.png` | ⚪ |
| `hu-khong-ket-gioi.png` | Hư Không Kết Giới | `Assets/CampusRiftUI/Art/Skills/VoidWall.png` | ⚪ |
| `anh-phan-than.png` | Ảnh Phân Thân | `Assets/CampusRiftUI/Art/Skills/PhantomDecoy.png` | ⚪ |

---

## 4. Icon vật phẩm — `Content/Images/Items/` (256×256)

Dạng đan dược (viên thuốc trong đĩa hoặc bình), phù chú (lá bùa vàng chữ đỏ, **dùng ký tự trang trí, không dùng chữ có nghĩa**), hoặc châu (viên ngọc). Màu theo công dụng: hồi máu đỏ, Linh Lực xanh ngọc, buff vàng, chống lửa xanh băng.

| Tên file | Vật phẩm | Gợi ý hình ảnh | Cần trước | Ưu tiên |
|---|---|---|:-:|:-:|
| `hoi-khi-dan.png` | Hồi Khí Đan | Viên đan đỏ nhạt nhỏ | P08 | 🔴 |
| `hoi-xuan-dan.png` | Hồi Xuân Đan | Viên đan đỏ tươi, lá xanh quấn quanh | P08 | 🔴 |
| `tu-linh-dan.png` | Tụ Linh Đan | Viên đan xanh ngọc phát sáng | P08 | 🔴 |
| `ho-menh-phu.png` | Hộ Mệnh Phù | Lá bùa vàng viền đỏ, hào quang trắng | P08 | 🔴 |
| `cuong-luc-dan.png` | Cuồng Lực Đan | Viên đan cam đỏ, tia năng lượng | P08 | 🔴 |
| `kim-cuong-phu.png` | Kim Cương Phù | Lá bùa vàng kim, hoa văn khiên | P08 | 🔴 |
| `than-hanh-phu.png` | Thần Hành Phù | Lá bùa xanh lá, hình chân chạy / vệt gió | P08 | 🔴 |
| `ti-hoa-chau.png` | Tị Hỏa Châu | Viên ngọc xanh băng có ngọn lửa bị giam bên trong | P08 | 🔴 |
| `bang-tam-phu.png` | Băng Tâm Phù | Lá bùa trắng xanh, tinh thể băng | P08 | 🔴 |
| `kiem-tam-dan.png` | Kiếm Tâm Đan | Viên đan vàng có hình thanh kiếm nhỏ bên trong | P08 | 🔴 |
| `thanh-tam-dan.png` | Thanh Tâm Đan | Viên đan trắng trong, hoa sen nhỏ | P20 | ⚪ |
| `cuu-chuyen-hoan-hon-dan.png` | Cửu Chuyển Hoàn Hồn Đan | Viên đan vàng rực chín vòng sáng | P20 | ⚪ |
| `tu-khi-dan.png` | Tụ Khí Đan | Viên đan tím, xoáy năng lượng | P20 | ⚪ |
| `bao-kich-dan.png` | Bạo Kích Đan | Viên đan đỏ sẫm, tia chớp | P20 | ⚪ |
| `ngu-hanh-phu.png` | Ngũ Hành Phù | Lá bùa có vòng 5 màu ngũ hành | P20 | ⚪ |
| `tam-yeu-phu.png` | Tầm Yêu Phù | Lá bùa có con mắt, sóng dò tìm | P20 | ⚪ |

---

## 5. Icon pháp bảo — `Content/Images/Artifacts/` (256×256)

| Tên file | Pháp bảo | Gợi ý hình ảnh | Cần trước | Ưu tiên |
|---|---|---|:-:|:-:|
| `phi-kiem-thanh-truc.png` | Phi Kiếm Thanh Trúc | Ba thanh phi kiếm xanh ngọc như lá trúc | P08 | 🔴 |
| `ho-tam-kinh.png` | Hộ Tâm Kính | Tấm gương đồng tròn đeo ngực | P08 | 🔴 |
| `tui-can-khon.png` | Túi Càn Khôn | Túi gấm nhỏ, miệng túi phát sáng | P08 | 🔴 |
| `linh-luc-ho-lo.png` | Linh Lực Hồ Lô | Hồ lô xanh ngọc tỏa khí | P20 | ⚪ |
| `ngoc-boi-ngu-hanh.png` | Ngọc Bội Ngũ Hành | Miếng ngọc bội khắc vòng ngũ hành | P20 | ⚪ |

---

## 6. Ký hiệu hệ — `Content/Images/Elements/` (128×128)

Hình **đơn giản, nét rõ, nhận ra được khi rất nhỏ** (hiện cạnh tên quái, trên số sát thương, ở màn Chuẩn Bị). Mỗi hệ phải khác nhau **cả về hình dạng**, không chỉ về màu, để người mù màu vẫn phân biệt được (P23-T03).

| Tên file | Hệ | Gợi ý hình | Ưu tiên |
|---|---|---|:-:|
| `kim.png` | Kim | Lưỡi kiếm / thỏi vàng | 🔴 |
| `moc.png` | Mộc | Chiếc lá | 🔴 |
| `thuy.png` | Thủy | Giọt nước | 🔴 |
| `hoa.png` | Hỏa | Ngọn lửa | 🔴 |
| `tho.png` | Thổ | Ngọn núi / khối đá | 🔴 |
| `loi.png` | Lôi | Tia sét | 🔴 |
| `am.png` | Âm | Trăng lưỡi liềm / đốm ma trơi | 🔴 |
| `khong-gian.png` | Không Gian | Vòng xoáy | 🔴 |
| `vo-he.png` | Vô hệ | Vòng tròn trống | 🔴 |

---

## 7. Huy hiệu cảnh giới — `Content/Images/Realms/` (256×256)

Hiện ở panel Cảnh Giới và header của Sảnh. Nên tăng dần độ hoành tráng: số vòng sáng, màu (đồng → bạc → vàng → ngọc → tím → trắng → cầu vồng).

| Tên file | Cảnh giới | Ưu tiên |
|---|---|:-:|
| `luyen-khi.png` | Luyện Khí | 🟡 |
| `truc-co.png` | Trúc Cơ | 🟡 |
| `ket-dan.png` | Kết Đan | 🟡 |
| `nguyen-anh.png` | Nguyên Anh | 🟡 |
| `hoa-than.png` | Hóa Thần | 🟡 |
| `luyen-hu.png` | Luyện Hư | 🟡 |
| `do-kiep.png` | Độ Kiếp | 🟡 |

---

## 8. Huy hiệu thành thạo và sao — `Content/Images/Badges/` (128×128)

Tôi có thể tự vẽ bằng code nếu bạn không gửi.

| Tên file | Dùng cho | Ưu tiên |
|---|---|:-:|
| `mastery-dong.png`, `mastery-bac.png`, `mastery-vang.png` | Huy hiệu ôn tập Đồng / Bạc / Vàng của mỗi bài (P07) | ⚪ |
| `star-on.png`, `star-off.png` | Sao ★ đạt / chưa đạt ở Bản Đồ và Kết Quả (P09) | ⚪ |

---

## 9. Chân dung quái, boss, cự thú — `Content/Images/Portraits/` (512×512)

Dùng ở màn Chuẩn Bị (tình báo màn), Sổ tay, thanh máu boss. Khung tròn giống icon kỹ năng; màu viền theo hệ của quái. Nếu bạn không gửi, tôi sẽ **chụp từ model 3D trong game** (sẽ kém đẹp hơn).

| Tên file | Quái | Hệ | Gợi ý hình ảnh | Cần trước | Ưu tiên |
|---|---|:-:|---|:-:|:-:|
| `tieu-yeu.png` | Tiểu Yêu | Thổ | Yêu quái nhỏ nhanh nhẹn, móng vuốt, mắt phát sáng | P09 | 🟡 |
| `doc-nhan.png` | Độc Nhãn Xạ Thủ | Mộc | Quái một mắt lớn, miệng nhả độc xanh | P09 | 🟡 |
| `thiet-giap-nguu.png` | Thiết Giáp Ngưu | Kim | Trâu yêu bọc giáp sắt, sừng lớn | P09 | 🟡 |
| `bao-thi.png` | Bạo Thi | Hỏa | Xác sống phình to, nứt ra lửa bên trong | P09 | 🟡 |
| `shaban.png` | Shaban — Săn Hồn Giả | Âm | Theo model Shaban hiện có | P09 | 🟡 |
| `shaban-thuc-tinh.png` | Shaban Thức Tỉnh (boss màn 7) | Âm | Shaban cuồng nộ, hào quang đỏ tím | P12 | 🟡 |
| `anh-yeu.png` | Ảnh Yêu | Âm | Sát thủ bóng tối, nửa thân tan vào khói | P19 | ⚪ |
| `trieu-hon-su.png` | Triệu Hồn Sư | Âm | Pháp sư đội mũ trùm, cầm trượng đầu lâu | P19 | ⚪ |
| `duc-yeu.png` | Dực Yêu | Mộc | Dơi yêu cánh lá | P19 | ⚪ |
| `hoa-linh.png` | Hỏa Linh | Hỏa | Tinh linh lửa hình người | P19 | ⚪ |
| `xich-hoa-giao.png` | Xích Hỏa Giao (cự thú màn 8) | Hỏa | Giao long lửa thân rắn có sừng | P14 | 🟡 |
| `chu-tuoc.png` | Tà Hóa Chu Tước (cự thú màn 9) | Hỏa | Chim lửa khổng lồ bị tà hóa, cánh cháy đen đỏ | P14 | 🟡 |
| `hoa-long-vuong.png` | Cửu U Hỏa Long Vương (cự thú màn 10) | Hỏa | Rồng lửa chín sừng, vương miện lửa | P14 | 🟡 |

---

## 10. Biểu tượng trú ẩn — `Content/Images/HUD/` (128×128)

Hiện trên HUD màn 8–10 (P13-T07). Cần rõ ràng, đơn giản.

| Tên file | Ý nghĩa | Gợi ý | Ưu tiên |
|---|---|---|:-:|
| `shelter-indoor.png` | Trong nhà, an toàn | Mái nhà màu xanh lá | 🟡 |
| `shelter-partial.png` | Bán che | Mái hiên màu vàng, dấu chấm than | 🟡 |
| `shelter-outdoor.png` | Ngoài trời, nguy hiểm | Ngọn lửa đỏ rơi từ trên xuống | 🟡 |

---

## 11. Hình nền, nhân vật, bản đồ — `Content/Images/Scenes/`

| Tên file | Dùng cho | Kích thước | Gợi ý hình ảnh | Cần trước | Ưu tiên |
|---|---|---|---|:-:|:-:|
| `thu-linh.png` | **Thư Linh**, linh thể hư cấu của thư viện: dẫn đường, bán hàng ở Đan Các, hướng dẫn người mới | 1024×1024, nền trong suốt, nửa người | Linh thể nữ hoặc nam áo trắng xanh, cầm quyển sách phát sáng, phong cách tiên hiệp. **Nhân vật hư cấu**, không mô phỏng người thật | P09 | 🔴 |
| `hub-background.png` | Nền Sảnh Tu Luyện | 1920×1080 | Thư viện cổ kiểu tu tiên nhìn ra sân trường đêm, có Khe Nứt tím trên trời | P09 | 🟡 (tạm dùng `MenuBackdrop.png` có sẵn) |
| `level-map.png` | Nền Bản Đồ 10 Màn | 1920×1080 | Khuôn viên nhìn từ trên cao, con đường uốn qua 10 điểm, chuyển dần từ hoàng hôn → đêm → trời lửa | P09 | 🟡 |
| `level-01.png` … `level-10.png` | Ảnh thẻ của từng màn | 512×288 (16:9) | Cảnh đặc trưng từng màn theo §2.3 kế hoạch (ví dụ màn 8: cự thú lửa trên trời). Nếu không có, tôi chụp từ game | P09 | ⚪ |
| `hero-portrait.png` | Chân dung nhân vật chính ở Sảnh | 1024×1024, nền trong suốt | Nữ sinh (theo model SchoolGirl hiện có) trong tư thế ngự kiếm | P09 | ⚪ |

---

## 12. Minh họa bài học — `Content/Images/Lessons/` (1024×576)

⚪ **Tùy chọn**, gửi kèm nội dung môn học ở P07-T10. Mỗi trang bài học có thể có một ảnh minh họa (field `illustration` của `LessonPage`).

- Tên file: `<bai_id>-trang-<số>.png`, ví dụ `ch1-b1-trang-2.png`.
- Chỉ dùng **ảnh tư liệu có nguồn rõ ràng và được phép sử dụng** (ảnh trong giáo trình, bảo tàng, cổng thông tin chính thức), ghi nguồn vào `NGUON.md`.
- Trình bày trang trọng, **không** chỉnh sửa, cắt ghép hay tô vẽ ảnh tư liệu lịch sử; **không** dùng ảnh AI để mô tả nhân vật, sự kiện lịch sử có thật.
- Sơ đồ, bảng tóm tắt do bạn tự vẽ thì dùng thoải mái.

---

## 13. Thành tựu và danh hiệu — `Content/Images/Achievements/` (256×256)

⚪ Bản đầy đủ (P22). Khoảng 20 huy hiệu; danh sách tên cụ thể sẽ chốt ở P22-T03. Tôi sẽ bổ sung bảng tên file vào đây khi làm tới phase đó.

---

## 14. Icon app và màn khởi động — `Content/Images/App/`

| Tên file | Dùng cho | Kích thước | Gợi ý | Cần trước | Ưu tiên |
|---|---|---|---|:-:|:-:|
| `app-icon.png` | Icon ứng dụng Android và Windows | 1024×1024, **không** trong suốt, không bo góc (hệ điều hành tự bo) | Thanh Thiên Kiếm vàng trước Khe Nứt tím, chữ "CR" hoặc không chữ | P17 | 🔴 |
| `splash.png` | Màn khởi động | 1920×1080 | Logo "Campus Rift" và khẩu hiệu "Học Để Thắng" | P17 | 🟡 |

---

## Không cần cung cấp (tôi tự lo)

- **Model 3D** (quái, cự thú): tìm nguồn CC0/CC-BY (Quaternius, Poly Pizza), chuyển qua Blender, ghi giấy phép (P04-T02, P14-T01, P21-T01). Nếu bạn **muốn** gửi model riêng: định dạng FBX hoặc GLB, có animation idle, run, attack, death (quái) hoặc fly, roar, breath (cự thú).
- **Texture hiệu ứng** (lửa, sét, băng, phi kiếm, hố đen): sinh bằng code hoặc shader.
- **Khung UI, nút, thanh**: dùng bộ Celestial có sẵn trong `Assets/CampusRiftUI/Art/Celestial/`.
- **Âm thanh, nhạc**: tìm nguồn có giấy phép phù hợp (P17-T04, P21-T05). Bạn có thể gửi nếu có sẵn.

---

## Thứ tự gửi đề xuất

1. **Ngay bây giờ (trước P08–P10):** 7 icon kỹ năng MVP (mục 3), 10 icon vật phẩm và 3 icon pháp bảo (mục 4–5), 9 ký hiệu hệ (mục 6).
2. **Trước P09:** Thư Linh; nếu được thì thêm hình nền Sảnh, Bản Đồ và 5 chân dung quái MVP.
3. **Trước P13–P15:** icon Thiên Kiếm, 3 biểu tượng trú ẩn, 3 chân dung cự thú.
4. **Trước P17:** icon app, màn khởi động.
5. **Phần B:** phần còn lại.

Khi gửi ảnh, cập nhật cột trạng thái trong file này hoặc chỉ cần báo tôi tên nhóm đã gửi.
