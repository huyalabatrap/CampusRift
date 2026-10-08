# Mô tả chi tiết ảnh "Bắt buộc" và "Nên có"

> Bổ sung cho [HINH-ANH-CAN-CUNG-CAP.md](HINH-ANH-CAN-CUNG-CAP.md). File đó ghi tên file, kích thước, thư mục; file này mô tả **nội dung từng ảnh** đủ chi tiết để bạn tự vẽ, thuê họa sĩ, hoặc tạo bằng công cụ AI.
>
> Mỗi ảnh gồm: **Chủ thể · Bố cục · Màu và ánh sáng · Chi tiết bắt buộc · Tránh**, và một **prompt tiếng Anh gợi ý** (ghép với "Phong cách chung" bên dưới nếu dùng AI).

---

## 0. Phong cách chung

### 0.1 Tham chiếu có sẵn trong game

| Thứ | Mô tả | File tham chiếu |
|---|---|---|
| Icon kỹ năng hiện có | Huy hiệu **tròn**, viền kim loại tím đậm và vàng đồng, **4 mũi nhọn** hình ngôi sao ở 4 hướng (trên, dưới, trái, phải), bên trong là cảnh phép thuật phát sáng, nền ngoài **trong suốt** | `Assets/CampusRiftUI/Art/Skills/GiantHandSeal.png` |
| Nhân vật chính | Nữ sinh, tóc đen ngắn ngang vai, **mũ tròn vành màu vàng**, áo thủy thủ trắng cổ xanh navy, nơ đỏ nhỏ, váy xếp ly xanh navy, tất trắng, giày nâu | Ảnh chụp: `Artifacts/Combat/01-Windup.png` |
| Shaban | Hồn ma thiếu nữ: da trắng bệch, **tóc đen dài rối che kín mặt**, váy trắng ngắn tay, chân trần, dáng đi vặn vẹo | Như trên |
| Khuôn viên | Tòa nhà hiện đại xanh dương và trắng, sân lát đá sáng, cây hoa hồng phấn, cột đèn, ghế đá | `Assets/CampusRiftUI/Art/CampusBackdrop.png` |

### 0.2 Quy tắc chung cho mọi icon

1. **Đọc được ở cỡ nhỏ.** Icon hiển thị khoảng 96 px trên PC và 150 px trên điện thoại. Chủ thể phải chiếm **60–70%** lòng huy hiệu, hình khối rõ, không chi tiết li ti. Thử thu ảnh xuống 64×64: vẫn phải nhận ra.
2. **Một chủ thể, một màu chủ đạo** theo hệ; màu phụ chỉ để làm nổi bật.
3. **Không chữ, không số, không logo** bên trong icon; game tự hiển thị tên và phím.
4. **Nền ngoài viền trong suốt thật** (alpha = 0), không phải nền trắng hay ca-rô giả.
5. Ánh sáng chính từ **trên–trái**, có quầng phát sáng quanh chủ thể.
6. **Không** dùng hình ảnh, tên, trích dẫn hay biểu tượng của môn Tư tưởng Hồ Chí Minh, không dùng quốc kỳ, quốc huy hay biểu tượng chính trị (§11.5 kế hoạch).

### 0.3 Đoạn prompt chung (nếu dùng AI)

Ghép vào sau prompt riêng của từng ảnh:

> `…, xianxia fantasy game skill icon, circular medallion badge, ornate dark purple and antique gold metal frame with four sharp star points at top, bottom, left and right, glowing magical effect inside, high contrast, clean readable silhouette, centered composition, transparent background, no text, no letters, no watermark, digital painting, 512x512`

Với icon vật phẩm, pháp bảo, ký hiệu hệ: bỏ phần "circular medallion…four star points", thay bằng `single object icon, soft glow, transparent background`.

---

## 1. Icon kỹ năng — 🔴 Bắt buộc (512×512, khung huy hiệu như 0.1)

### 1.1 `tich-lich-nhat-thiem.png` — Tích Lịch Nhất Thiểm (Lôi)
- **Chủ thể:** một vệt sét vàng rực chém ngang từ trái sang phải; ở đầu vệt là **bóng người đang lướt tới trong tư thế rút kiếm** (chỉ là bóng đen viền sáng, không cần rõ mặt).
- **Bố cục:** vệt sét chéo nhẹ từ dưới–trái lên trên–phải, chiếm toàn bộ chiều ngang lòng huy hiệu; bóng người nằm ở 1/3 bên phải.
- **Màu và ánh sáng:** nền tím đêm; sét **vàng chanh lõi trắng**, tia điện nhỏ màu tím nhạt tỏa ra hai bên; có vệt bóng ảo mờ phía sau.
- **Chi tiết bắt buộc:** cảm giác **tốc độ cực nhanh** (vệt kéo dài, bụi bắn).
- **Tránh:** vẽ rõ mặt nhân vật; lẫn với icon Thần Kiếm Ngự Lôi (icon này là **vệt lướt ngang**, icon kia là **sét tỏa nhiều nhánh**).
- **Prompt:** `a blazing yellow lightning streak slashing horizontally, silhouette of a swordsman dashing at the tip of the streak drawing a sword, purple sparks, motion blur speed lines, night purple background`

### 1.2 `phat-no-hoa-lien.png` — Phật Nộ Hỏa Liên (Hỏa)
- **Chủ thể:** **một đóa hoa sen làm bằng lửa**, nhiều tầng cánh, đang nở, lơ lửng giữa huy hiệu.
- **Bố cục:** hoa sen ở chính giữa, nhìn hơi từ trên xuống (góc 30°); dưới hoa là vòng sóng lửa sắp nổ.
- **Màu và ánh sáng:** cánh ngoài **đỏ cam**, cánh trong vàng, **lõi trắng nóng** chói sáng; tàn lửa nhỏ bay lên; nền đỏ sẫm pha tím.
- **Chi tiết bắt buộc:** nhận ra rõ **hình hoa sen** (cánh nhọn đối xứng), không chỉ là quả cầu lửa.
- **Tránh:** hoa sen màu hồng tự nhiên (phải là lửa); hình tượng Phật hay biểu tượng tôn giáo.
- **Prompt:** `a lotus flower made entirely of fire, layered petals blooming, red-orange outer petals, golden inner petals, white-hot glowing core, embers rising, dark crimson background`

### 1.3 `han-bang-phong-an.png` — Hàn Băng Phong Ấn (Thủy)
- **Chủ thể:** chùm **gai băng nhọn** mọc tỏa ra hình nón từ dưới lên, như một đợt băng vừa quét qua.
- **Bố cục:** gốc chùm gai ở dưới–giữa, các gai tỏa lên trên theo hình quạt 90°; vài mảnh tinh thể băng bay lơ lửng.
- **Màu và ánh sáng:** **xanh băng và trắng**, viền gai phát sáng xanh lơ, sương lạnh trắng mờ ở chân; nền xanh đậm.
- **Chi tiết bắt buộc:** cảm giác **lạnh, cứng, sắc**.
- **Tránh:** nước lỏng hay sóng nước (đây là băng); dùng màu tím làm chủ đạo.
- **Prompt:** `a fan-shaped cone of sharp ice spikes bursting upward, crystalline shards floating, frosty mist at the base, icy cyan and white glow, deep blue background`

### 1.4 `than-kiem-ngu-loi.png` — Thần Kiếm Ngự Lôi Chân Quyết (Lôi)
- **Chủ thể:** **một thanh kiếm dựng thẳng đứng** giữa huy hiệu; từ mũi kiếm, **sét tỏa ra thành nhiều nhánh** nối tới 4–6 đốm sáng nhỏ xung quanh (tượng trưng sét nảy qua nhiều mục tiêu).
- **Bố cục:** kiếm ở giữa, mũi hướng lên; các nhánh sét tỏa hình mạng nhện ra rìa.
- **Màu và ánh sáng:** sét **tím pha vàng**; thân kiếm bạc sáng; nền mây giông tím đen.
- **Chi tiết bắt buộc:** thấy rõ **nhiều nhánh sét nối nhau** (chuỗi dây chuyền).
- **Tránh:** giống icon Tích Lịch (không có vệt lướt ngang); thêm hình người.
- **Prompt:** `a vertical sword floating upright, chain lightning branching from the blade tip to several glowing points around it, violet and gold electricity, stormy dark purple clouds`

### 1.5 `kim-chung-trao.png` — Kim Chung Tráo (Kim)
- **Chủ thể:** **một chiếc chuông đồng vàng khổng lồ, trong suốt một nửa**, úp trùm lên bóng một người đang ngồi thiền hoặc đứng thủ thế.
- **Bố cục:** chuông chiếm 70% huy hiệu; bóng người nhỏ bên trong, ở giữa–dưới.
- **Màu và ánh sáng:** **vàng kim** chủ đạo, hoa văn cổ khắc trên thân chuông (vân mây, vân sóng, **không chữ**); vòng sáng hào quang quanh chuông; nền nâu đỏ sẫm.
- **Chi tiết bắt buộc:** đọc ra ngay là **cái chuông bảo vệ** (khiên).
- **Tránh:** chữ Hán hoặc chữ bất kỳ trên chuông; biểu tượng tôn giáo cụ thể.
- **Prompt:** `a giant translucent golden bronze bell shielding a small meditating silhouette inside, ancient cloud patterns engraved on the bell, radiant golden aura, dark red-brown background`

### 1.6 `hac-dong-than-la.png` — Hắc Động Thần La (Không Gian)
- **Chủ thể:** **một hố đen xoáy** ở giữa, viền phát sáng tím; các mảnh đá vụn và đốm sáng bị hút xoắn ốc vào tâm.
- **Bố cục:** tâm hố đen chính giữa; các vệt xoắn ốc chiếm toàn bộ lòng huy hiệu.
- **Màu và ánh sáng:** lõi **đen tuyệt đối**, đĩa bồi tụ viền **tím và hồng tím** rực; mảnh đá xám; nền vũ trụ tím đen.
- **Chi tiết bắt buộc:** cảm giác **lực hút mạnh** (vật thể méo và kéo dài khi gần tâm).
- **Tránh:** lẫn với Hư Không Kết Giới (icon đó là bức tường); dùng quá nhiều màu xanh.
- **Prompt:** `a swirling black hole vortex, pitch black core, glowing violet and magenta accretion ring, rock fragments and light particles spiraling inward and stretching, cosmic dark purple background`

### 1.7 `van-kiem-quyet.png` — Vạn Kiếm Quyết (Kim)
- **Chủ thể:** **hàng chục thanh kiếm vàng rơi thẳng từ trên trời xuống**, cắm vào một vòng pháp trận phát sáng dưới đất.
- **Bố cục:** các thanh kiếm dày đặc ở nửa trên, mũi hướng xuống; vòng pháp trận hình elip ở 1/4 dưới.
- **Màu và ánh sáng:** kiếm **vàng kim** có vệt sáng phía sau; pháp trận vàng nhạt; nền trời đêm tím.
- **Chi tiết bắt buộc:** thấy **số lượng nhiều** (ít nhất 12–20 thanh), cảm giác mưa kiếm.
- **Tránh:** một thanh kiếm lớn duy nhất (đó là Thiên Kiếm).
- **Prompt:** `dozens of golden swords raining straight down from the sky, glowing trails, a glowing magic circle on the ground where they land, night purple sky`

### 1.8 `thien-kiem.png` — Thiên Kiếm (tuyệt kỹ, nút riêng) ⭐ quan trọng nhất
- **Chủ thể:** **một thanh kiếm vàng khổng lồ** đâm xuống từ bầu trời xuyên qua tầng mây; phía sau là **bóng rồng lửa đỏ** trên nền trời đỏ.
- **Bố cục:** kiếm chéo nhẹ từ trên–phải xuống dưới–trái, chiếm gần hết chiều cao; bóng rồng mờ ở nền phía sau.
- **Màu và ánh sáng:** kiếm **vàng rực lõi trắng**, hào quang tỏa tia; trời **đỏ cam**; mây tối.
- **Chi tiết bắt buộc:** **viền huy hiệu đặc biệt hơn** icon thường: vàng nhiều hơn tím, 8 mũi nhọn thay vì 4, có tia sáng tỏa ra ngoài viền. Đây là nút tuyệt kỹ, phải nổi bật nhất trên HUD.
- **Tránh:** rồng quá rõ lấn át thanh kiếm; lẫn với Vạn Kiếm Quyết.
- **Prompt:** `a colossal golden sword piercing down through clouds from the heavens, blinding white-gold core, radiant light rays, silhouette of a fire dragon in a burning red sky behind it, epic ultimate skill, ornate gold medallion frame with eight star points and light rays`

---

## 2. Icon vật phẩm — 🔴 Bắt buộc (256×256, không khung huy hiệu)

Quy tắc chung cho cả nhóm:
- **Một vật thể duy nhất** ở giữa, chiếm khoảng 70% khung, góc nhìn hơi nghiêng từ trên (3/4).
- Quầng sáng mềm màu theo công dụng: **hồi máu đỏ**, **Linh Lực xanh ngọc**, **buff vàng cam**, **chống lửa xanh băng**, **hồi sinh trắng vàng**.
- Nền trong suốt. **Không chữ**; hoa văn trên lá bùa chỉ là **nét trang trí trừu tượng** (xoắn, sóng), không phải chữ có nghĩa.
- Ba dạng thống nhất để người chơi nhận ra loại:
  - **Đan dược:** viên tròn bóng đặt trên đĩa sứ nhỏ hoặc nằm trong bình hồ lô.
  - **Phù chú:** lá bùa giấy chữ nhật dọc, hơi cong.
  - **Châu:** viên ngọc tròn trong suốt.

| Tên file | Vật phẩm | Mô tả chi tiết | Prompt gợi ý |
|---|---|---|---|
| `hoi-khi-dan.png` | Hồi Khí Đan | Viên đan **nhỏ, đỏ nhạt**, trên đĩa sứ trắng; quầng sáng đỏ nhạt yếu. Đơn giản nhất nhóm hồi máu | `a small pale red glossy pill on a tiny white porcelain dish, faint red glow` |
| `hoi-xuan-dan.png` | Hồi Xuân Đan | Viên đan **đỏ tươi to hơn**, có **một dây lá xanh non quấn quanh**; quầng đỏ ấm mạnh hơn | `a large bright red glossy pill wrapped by a thin green leafy vine, warm red healing glow` |
| `tu-linh-dan.png` | Tụ Linh Đan | Viên đan **xanh ngọc phát sáng**, bên trong có xoáy khí nhỏ; hạt sáng xanh bay quanh | `a glowing jade-cyan pill with a tiny swirling mist inside, cyan sparkles orbiting` |
| `ho-menh-phu.png` | Hộ Mệnh Phù | Lá bùa **vàng viền đỏ**, hoa văn xoắn đỏ; **hào quang trắng vàng** hình vòng tròn sau lá bùa (cảm giác "cứu mạng") | `a vertical yellow paper talisman with red border and abstract red swirl ornaments, holy white-gold halo behind it` |
| `cuong-luc-dan.png` | Cuồng Lực Đan | Viên đan **cam đỏ**, vân như dung nham, có **tia năng lượng** bắn ra | `an orange-red pill with lava-like veins, crackling energy sparks bursting out` |
| `kim-cuong-phu.png` | Kim Cương Phù | Lá bùa **vàng kim ánh kim loại**, hoa văn **hình khiên lục giác** | `a metallic gold talisman with an abstract hexagonal shield ornament, sturdy golden glow` |
| `than-hanh-phu.png` | Thần Hành Phù | Lá bùa **xanh lá nhạt**, hoa văn **vệt gió xoáy**, có đường gió bay qua | `a light green paper talisman with abstract wind swirl ornaments, gusts of wind streaking past` |
| `ti-hoa-chau.png` | Tị Hỏa Châu | Viên ngọc **xanh băng trong suốt**; bên trong **giam một ngọn lửa đỏ nhỏ** bị đóng băng; bề mặt đọng sương | `a transparent icy blue orb with a small red flame frozen inside, frost on the surface, cold glow` |
| `bang-tam-phu.png` | Băng Tâm Phù | Lá bùa **trắng xanh**, viền bạc, gắn **tinh thể băng** ở giữa | `a white and pale blue talisman with silver border and an ice crystal in the center, cold mist` |
| `kiem-tam-dan.png` | Kiếm Tâm Đan | Viên đan **vàng kim trong suốt**, bên trong có **hình một thanh kiếm nhỏ** phát sáng | `a translucent golden pill with a tiny glowing sword inside it, golden radiance` |

---

## 3. Icon pháp bảo — 🔴 Bắt buộc (256×256)

Pháp bảo là **đồ vĩnh viễn**, nên trông **quý hơn** vật phẩm tiêu hao: chất liệu ngọc, đồng, gấm, có đế hoặc vòng sáng bên dưới như đang "được trưng bày".

| Tên file | Pháp bảo | Mô tả chi tiết | Prompt gợi ý |
|---|---|---|---|
| `phi-kiem-thanh-truc.png` | Phi Kiếm Thanh Trúc | **Ba thanh phi kiếm mảnh** màu **xanh ngọc như lá trúc**, xếp hình quạt, lơ lửng; vài lá trúc bay quanh. Đây cũng là hình đòn đánh thường của nhân vật, nên màu phải khớp: xanh ngọc | `three slender jade-green flying swords shaped like bamboo leaves, fanned out and floating, bamboo leaves drifting, jade glow` |
| `ho-tam-kinh.png` | Hộ Tâm Kính | **Tấm gương đồng tròn** đeo ngực, mặt gương phản chiếu ánh sáng, viền chạm hoa văn mây, có dây đeo đỏ | `a round bronze chest mirror amulet, reflective polished surface, cloud-pattern engraved rim, red cord, protective glow` |
| `tui-can-khon.png` | Túi Càn Khôn | **Túi gấm nhỏ** màu tím, thêu vân mây vàng, dây rút; **miệng túi hé mở phát ra ánh sáng** như chứa cả không gian bên trong | `a small purple brocade drawstring pouch embroidered with golden clouds, mouth slightly open emitting mystical light` |

---

## 4. Ký hiệu hệ — 🔴 Bắt buộc (128×128)

Đây là **ký hiệu phẳng**, không phải tranh vẽ: hiện rất nhỏ (24–40 px) cạnh tên quái, trên số sát thương, ở màn Chuẩn Bị.

- **Dạng:** hình phẳng một màu chính, viền tối 3–4 px để nổi trên mọi nền, có quầng sáng nhẹ. Chủ thể chiếm 80% khung.
- **Yêu cầu quan trọng:** mỗi hệ có **hình dạng khác hẳn nhau**, nếu in đen trắng vẫn phân biệt được (cho người mù màu).
- **Tránh:** chi tiết nhỏ, chữ, bóng đổ phức tạp.

| Tên file | Hệ | Hình | Màu | Prompt gợi ý |
|---|---|---|---|---|
| `kim.png` | Kim | **Lưỡi kiếm ngắn dựng đứng**, mũi nhọn hướng lên | Vàng kim `#FFD14D` | `flat icon of an upright short sword blade, golden, bold dark outline` |
| `moc.png` | Mộc | **Chiếc lá** có gân giữa, hơi nghiêng | Xanh lá `#5AE673` | `flat icon of a single leaf with center vein, green, bold dark outline` |
| `thuy.png` | Thủy | **Giọt nước** đứng | Xanh băng `#73CCFF` | `flat icon of a water droplet, icy cyan, bold dark outline` |
| `hoa.png` | Hỏa | **Ngọn lửa** ba lưỡi | Đỏ cam `#FF7333` | `flat icon of a three-tongued flame, red-orange, bold dark outline` |
| `tho.png` | Thổ | **Ngọn núi** hai đỉnh | Nâu vàng `#CC994D` | `flat icon of a twin-peak mountain, ochre brown, bold dark outline` |
| `loi.png` | Lôi | **Tia sét** zigzag | Tím nhạt `#CC99FF` viền vàng | `flat icon of a zigzag lightning bolt, light violet with gold edge, bold dark outline` |
| `am.png` | Âm | **Trăng lưỡi liềm** có một đốm ma trơi | Xám tím `#9980B3` | `flat icon of a crescent moon with a small will-o-wisp, grey-purple, bold dark outline` |
| `khong-gian.png` | Không Gian | **Vòng xoáy** xoắn ốc | Tím đậm `#8C4DE6` | `flat icon of a spiral vortex, deep violet, bold dark outline` |
| `vo-he.png` | Vô hệ | **Vòng tròn rỗng** có một chấm ở tâm | Trắng xám `#DDDDDD` | `flat icon of an empty ring with a dot in the center, light grey, bold dark outline` |

Mã màu khớp với màu hệ đang dùng trong game (`ElementChart.ColorOf`).

---

## 5. Thư Linh — 🔴 Bắt buộc (`Scenes/thu-linh.png`, 1024×1024, nền trong suốt)

Nhân vật hư cấu, xuất hiện ở Sảnh Tu Luyện (dẫn đường, hướng dẫn người mới, bán đan dược ở Đan Các).

- **Chủ thể:** **linh thể của thư viện trường**, dáng thanh niên hoặc thiếu nữ khoảng 20 tuổi (bạn chọn giới tính), vẻ **hiền, thông thái, hơi huyền bí**.
- **Trang phục:** áo dài tay rộng kiểu **tiên hiệp**, màu **trắng pha xanh lam nhạt**; thắt lưng có treo một chiếc thẻ ngọc nhỏ; tà áo hơi trong suốt ở phần dưới như đang tan vào ánh sáng (cho thấy là linh thể).
- **Đạo cụ:** tay trái cầm **quyển sách cổ đang mở phát sáng**, các trang giấy và đốm sáng bay lên xoắn quanh người; tay phải giơ nhẹ như đang mời chào hoặc chỉ đường.
- **Bố cục:** **nửa người** (đầu đến hông), hơi nghiêng 3/4 sang **trái** (hộp thoại hiện bên phải); mắt nhìn về người xem; chừa khoảng trống phía trên đầu 10%.
- **Màu và ánh sáng:** ánh sáng chính từ quyển sách hắt lên mặt; viền sáng xanh lam quanh tóc; tóc dài màu **bạc ánh xanh** hoặc đen, buộc cao có trâm.
- **Biểu cảm:** mỉm cười nhẹ. Nếu có thể, làm thêm **3 biến thể** cùng tư thế: `thu-linh-vui.png` (cười tươi), `thu-linh-nghiem.png` (nghiêm túc, dùng khi nhắc Thiên Hỏa), `thu-linh-ngac-nhien.png`. Không bắt buộc.
- **Tránh:** giống người thật hoặc nhân vật có bản quyền; hở hang; đồng phục học sinh (để phân biệt với nhân vật chính); bất kỳ biểu tượng chính trị hay tôn giáo cụ thể nào.
- **Prompt:** `half-body portrait of an ethereal library spirit, gentle wise young person around 20, flowing white and pale blue xianxia robes with wide sleeves, lower robes fading into light, holding an open ancient glowing book in the left hand, pages and light motes swirling around, right hand raised in a welcoming gesture, long silver-blue hair tied up with a hairpin, soft smile, three-quarter view facing left, lit from below by the book, fantasy anime illustration, transparent background`

---

## 6. Icon ứng dụng — 🔴 Bắt buộc (`App/app-icon.png`, 1024×1024)

- **Chủ thể:** **thanh Thiên Kiếm vàng** dựng đứng ở giữa, phía sau là **Khe Nứt tím** xé toạc bầu trời đêm (vết nứt hình tia chớp phát sáng tím hồng).
- **Bố cục:** kiếm chiếm 70% chiều cao, chuôi kiếm ở trên; vết nứt chéo sau lưng kiếm. Hình phải còn nhận ra khi thu nhỏ còn **48×48** (cỡ trên màn hình điện thoại).
- **Màu:** nền **tím than đậm → tím** (gradient), kiếm vàng rực, vết nứt tím hồng. **Không** trong suốt; phủ kín 1024×1024; **không tự bo góc** (Android và Windows tự bo).
- **Chữ:** không chữ, hoặc tối đa 2 chữ cái "CR" nhỏ ở góc dưới (tùy chọn).
- **Tránh:** chi tiết nhỏ; nhiều nhân vật; nền quá tối làm kiếm chìm.
- **Prompt:** `app icon, a glowing golden sword standing vertically in the center, behind it a jagged glowing purple-pink rift tearing through a dark night sky, deep indigo to purple gradient background, bold simple silhouette readable at small size, no text, full bleed square`

---

## 7. Huy hiệu cảnh giới — 🟡 Nên có (`Realms/`, 256×256)

Bảy huy hiệu **cùng một khuôn** (hình lục giác hoặc tròn có cánh), **tăng dần độ hoành tráng**. Người chơi phải thấy ngay "cảnh giới sau cao hơn cảnh giới trước".

| Tên file | Cảnh giới | Chất liệu, màu | Biểu tượng ở giữa | Độ hoành tráng |
|---|---|---|---|---|
| `luyen-khi.png` | Luyện Khí | **Đồng**, nâu cam | Làn khí xoáy nhỏ | 1 vòng sáng, không cánh |
| `truc-co.png` | Trúc Cơ | **Bạc** | Viên đá nền móng | 1 vòng sáng, cánh nhỏ |
| `ket-dan.png` | Kết Đan | **Vàng** | Viên kim đan tròn | 2 vòng sáng |
| `nguyen-anh.png` | Nguyên Anh | **Ngọc bích xanh** | Đóa sen nhỏ | 2 vòng, cánh vừa |
| `hoa-than.png` | Hóa Thần | **Tím thạch anh** | Ngọn lửa tím | 3 vòng, cánh lớn |
| `luyen-hu.png` | Luyện Hư | **Trắng pha lê** | Ngôi sao trong suốt | 3 vòng, tia sáng tỏa |
| `do-kiep.png` | Độ Kiếp | **Ánh cầu vồng / vàng trắng** | Tia sét vàng xuyên mây | Hào quang lớn, sét quanh viền |

- **Tránh:** chữ; ký hiệu tôn giáo.
- **Prompt mẫu (thay phần in hoa):** `cultivation rank emblem, hexagonal winged badge made of BRONZE, central symbol of A SMALL SWIRLING QI, ONE glowing ring, fantasy game UI, transparent background, no text`

---

## 8. Chân dung quái — 🟡 Nên có (`Portraits/`, 512×512, khung tròn như icon kỹ năng; viền màu theo hệ)

Quy tắc chung:
- Chân dung **từ ngực trở lên**, nhìn thẳng hoặc 3/4, **đe dọa nhưng không ghê rợn quá mức** (game hướng tới sinh viên; không máu me, không nội tạng).
- Nền trong lòng huy hiệu mang màu hệ.
- **Viền huy hiệu đổi màu theo hệ** thay cho tím và vàng mặc định.

| Tên file | Quái | Hệ (màu viền) | Mô tả chi tiết | Prompt gợi ý |
|---|---|---|---|---|
| `tieu-yeu.png` | Tiểu Yêu | Thổ (nâu vàng) | Yêu quái **nhỏ con, gầy, lưng gù**, da xám nâu như đất nứt, **móng vuốt dài**, hai sừng ngắn, **mắt vàng phát sáng**, miệng nhe răng nhọn. Tinh nghịch, hung hăng, không to lớn | `small hunched goblin-like demon, cracked earthen grey-brown skin, long claws, two short horns, glowing yellow eyes, sharp grin, ochre earth-toned background` |
| `doc-nhan.png` | Độc Nhãn Xạ Thủ | Mộc (xanh lá) | Quái **một con mắt khổng lồ** giữa mặt, thân như cây gỗ mục có rêu, miệng dạng ống **nhỏ giọt độc xanh lá** | `a one-eyed demon with a single huge eye, body like rotting mossy wood, tube-like mouth dripping green poison, toxic green background` |
| `thiet-giap-nguu.png` | Thiết Giáp Ngưu | Kim (vàng kim) | **Trâu yêu to lớn** mặc giáp sắt tán đinh, **sừng lớn cong** bọc kim loại, hơi thở phả khói, mắt đỏ, dáng cúi đầu chuẩn bị húc | `massive armored ox demon, riveted iron plate armor, huge curved metal-capped horns, steaming breath, red eyes, head lowered ready to charge, golden metallic background` |
| `bao-thi.png` | Bạo Thi | Hỏa (đỏ cam) | Xác sống **phình to**, da xám căng nứt, **qua các vết nứt thấy lửa đỏ cam cháy bên trong** như sắp nổ; mắt trắng dã | `a bloated undead creature, stretched cracked grey skin with glowing red-orange fire visible through the cracks as if about to explode, blank white eyes, fiery background, not gory` |
| `shaban.png` | Shaban — Săn Hồn Giả | Âm (xám tím) | **Theo đúng model trong game** (mục 0.1): hồn ma thiếu nữ, **tóc đen dài rối che kín mặt**, da trắng bệch, váy trắng; một tay đưa lên dáng vặn vẹo; vài sợi tóc bay; ánh sáng lạnh từ dưới hắt lên | `a pale ghost girl with long messy black hair completely covering her face, deathly white skin, white short-sleeved dress, one arm raised in a twisted pose, cold light from below, eerie grey-purple background, horror but not gory` |
| `shaban-thuc-tinh.png` | Shaban Thức Tỉnh (boss màn 7) | Âm (đỏ tím) | Như `shaban.png` nhưng **cuồng nộ**: tóc bay ngược lên để lộ **một con mắt đỏ rực**, hào quang **đỏ tím** bùng quanh người, váy rách và cháy xém ở gấu, vết nứt năng lượng trên da | `awakened enraged ghost girl, long black hair flying upward revealing one glowing red eye, red-purple aura erupting around her, torn scorched white dress, energy cracks on skin, boss portrait` |
| `xich-hoa-giao.png` | Xích Hỏa Giao (cự thú màn 8) | Hỏa | **Giao long** (rồng thân rắn, không cánh), **vảy đỏ rực**, hai sừng dài, râu lửa, miệng há **đang tụ lửa**; thân uốn cong chiếm cả khung, trên nền trời đỏ | `a serpentine wingless fire flood-dragon, glowing crimson scales, two long horns, fiery whiskers, mouth open gathering flames, body coiling through a burning red sky` |
| `chu-tuoc.png` | Tà Hóa Chu Tước (cự thú màn 9) | Hỏa (đỏ đen) | **Chim lửa khổng lồ bị tà hóa**: lông cánh **cháy đen ở mép**, lõi lửa đỏ cam, **mắt tím** (dấu hiệu bị Khe Nứt nhiễm), đuôi dài như dải lửa; sải cánh rộng hết khung | `a colossal corrupted phoenix-like fire bird, wing feathers charred black at the edges, red-orange fire core, glowing purple eyes, long flame-ribbon tail, wings spread wide, ash-filled red sky` |
| `hoa-long-vuong.png` | Cửu U Hỏa Long Vương (cự thú màn 10) | Hỏa (đỏ, đen, vàng) | **Rồng lửa to nhất**, có **chín chiếc sừng xếp thành vương miện**, vảy đen viền dung nham đỏ, mắt vàng; đầu rồng nhìn thẳng từ trên xuống, uy nghi và đáng sợ nhất nhóm | `a supreme fire dragon king with nine horns forming a crown, black scales with glowing lava-red edges, golden eyes, looking down menacingly, eclipse red sky, most imposing boss portrait` |

---

## 9. Biểu tượng trú ẩn — 🟡 Nên có (`HUD/`, 128×128)

Ký hiệu **phẳng** như nhóm ký hiệu hệ (mục 4): viền tối, hình đơn giản, nhận ra ngay khi đang chạy trốn lửa.

| Tên file | Ý nghĩa | Hình | Màu | Prompt gợi ý |
|---|---|---|---|---|
| `shelter-indoor.png` | Trong nhà, an toàn | Ngôi nhà có mái kín, **dấu tích** nhỏ ở góc | Xanh lá `#5AE673` | `flat icon of a house with a closed roof and a small check mark, green, bold dark outline` |
| `shelter-partial.png` | Bán che | Mái hiên hở một bên, **dấu chấm than** ở góc | Vàng `#FFC833` | `flat icon of a half-open awning roof with an exclamation mark, amber yellow, bold dark outline` |
| `shelter-outdoor.png` | Ngoài trời, nguy hiểm | **Ba ngọn lửa rơi từ trên xuống** một bóng người | Đỏ `#FF4D33` | `flat icon of three flames falling from above onto a small human silhouette, red, bold dark outline` |

---

## 10. Hình nền — 🟡 Nên có (`Scenes/`, 1920×1080, không trong suốt)

### 10.1 `hub-background.png` — nền Sảnh Tu Luyện
- **Cảnh:** bên trong **thư viện cổ kiểu tu tiên** đặt trong khuôn viên trường: kệ sách gỗ cao, đèn lồng treo, sàn gỗ; **một ô cửa sổ tròn lớn ở giữa** nhìn ra sân trường về đêm (tòa nhà xanh trắng, cây hoa hồng như trong game); trên bầu trời có **Khe Nứt tím** phát sáng.
- **Bố cục:** **khoảng giữa và bên phải để tương đối trống, tối** (UI 5 tab và Thư Linh sẽ đè lên); chi tiết dày ở hai mép và phía xa.
- **Màu:** ấm (vàng đèn lồng) bên trong, lạnh (tím, xanh) bên ngoài cửa sổ.
- **Tránh:** chữ trên gáy sách; nhân vật; quá sáng làm chữ UI khó đọc.
- **Prompt:** `interior of an ancient xianxia library inside a modern university campus, tall wooden bookshelves, hanging lanterns, a large round moon window in the center overlooking a night campus courtyard with blue-white buildings and pink blossom trees, a glowing purple rift crack in the sky, warm lantern light inside, cool purple outside, calm empty center area for UI, no text, no people, 16:9`

### 10.2 `level-map.png` — nền Bản Đồ 10 Màn
- **Cảnh:** **khuôn viên nhìn từ trên cao** (góc chim bay 45°), phong cách bản đồ vẽ tay; **một con đường uốn lượn** qua 10 điểm dừng (vị trí để trống, game tự đặt nút màn lên).
- **Tiến trình màu từ trái sang phải:** **hoàng hôn cam** (màn 1–2) → **đêm xanh tím** (màn 3–5) → **trăng máu đỏ** (màn 6–7) → **trời lửa đỏ cam, bóng rồng trên trời** (màn 8–10).
- **Bố cục:** con đường chiếm dải giữa màn hình, uốn hình chữ S; mép trên và dưới chừa khoảng 12% cho tiêu đề và nút.
- **Tránh:** vẽ sẵn số hoặc nút màn; chữ.
- **Prompt:** `stylized hand-painted bird's-eye map of a university campus, a winding S-shaped path with ten empty stop points, color progression from left to right: orange sunset, blue-purple night, blood-red moon, burning red sky with a dragon silhouette, fantasy game world map, no text, no numbers, 16:9`

### 10.3 `App/splash.png` — màn khởi động
- **Cảnh:** nhân vật chính (xem 0.1) **nhìn từ sau lưng**, đứng giữa sân trường về đêm, **ba thanh phi kiếm xanh ngọc** lơ lửng quanh người; trên trời là Khe Nứt tím và **bóng rồng lửa** xa xa.
- **Chữ:** tên game **"CAMPUS RIFT"** và dòng nhỏ **"Học Để Thắng"** ở 1/3 trên, căn giữa. Nếu bạn không tự làm chữ được, gửi ảnh không chữ, tôi sẽ đặt chữ bằng font game.
- **Bố cục:** nhân vật nhỏ, ở 1/3 dưới giữa màn hình; bầu trời chiếm phần lớn.
- **Prompt:** `back view of a schoolgirl in a yellow bucket hat and white sailor uniform standing in a night campus courtyard, three jade-green flying swords floating around her, a glowing purple rift in the sky and a distant fire dragon silhouette, epic cinematic wide shot, empty upper third for title, 16:9`

---

## Kiểm tra trước khi gửi

- [ ] Đúng tên file, đúng thư mục trong `Content/Images/`.
- [ ] Đúng kích thước; icon có **nền trong suốt thật**.
- [ ] Thu nhỏ còn 64×64 (icon) hoặc 48×48 (icon app) vẫn nhận ra.
- [ ] Không chữ, không logo, không watermark (trừ splash nếu bạn tự đặt chữ).
- [ ] Không dùng hình ảnh, biểu tượng liên quan môn Tư tưởng Hồ Chí Minh hay biểu tượng chính trị, tôn giáo.
- [ ] Đã ghi nguồn hoặc công cụ tạo vào `Content/Images/NGUON.md`.
