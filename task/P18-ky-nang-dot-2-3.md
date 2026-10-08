# P18 — Kỹ năng đợt 2–3 và hiệu ứng Viên Mãn

> **Mục tiêu:** đủ 21 kỹ năng; mỗi kỹ năng có hiệu ứng đặc biệt ở tầng Viên Mãn; đủ 9 phản ứng.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 7,3 ngày công · **Phụ thuộc:** P17 · **Tham chiếu:** §7.3–§7.5
>
> **Quy ước:** giống P10 (`SkillDefinition` + `SkillRuntime` + VFX/SFX + icon + ngắm trên mobile). Số liệu ghi ở tầng 1.

> **Hoàn tất 03/10/2026 theo [TEST-POLICY](TEST-POLICY.md):** triển khai T01–T14, smoke PASS, [báo cáo và ảnh](p18/REPORT-P18.md). Cân bằng chưa đo thực chiến. T07 kiểm phá ẩn thân qua hợp đồng `IEnemyConcealment`; kiểm với Ảnh Yêu thực tế khi P19 triển khai quái này.

| ID | Kỹ năng / Task | Hệ | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|:-:|:-:|---|:-:|
| P18-T01 | Hàng Long Thập Bát Chưởng | Kim | 0,5 | P17 | ✅ |
| P18-T02 | Tam Muội Chân Hỏa | Hỏa | 0,5 | P17 | ✅ |
| P18-T03 | Bắc Minh Thần Công | Thủy | 0,5 | P17 | ✅ |
| P18-T04 | Côn Bằng Cực Tốc | Mộc | 0,4 | P17 | ✅ |
| P18-T05 | Thiên Lôi Dẫn | Lôi | 0,4 | P17 | ✅ |
| P18-T06 | Mộc Linh Hồi Xuân | Mộc | 0,4 | P17 | ✅ |
| P18-T07 | Thần Thức Linh Nhãn | Vô Hệ | 0,5 | P17 | ✅ |
| P18-T08 | Tru Tiên Kiếm Trận | Kim | 0,6 | P17 | ✅ |
| P18-T09 | Âm Binh Quy Hồn | Âm | 0,8 | P17 | ✅ |
| P18-T10 | Bành Trướng Lãnh Địa | Không Gian | 0,6 | P17 | ✅ |
| P18-T11 | Võ Hồn Chân Thân | Thổ | 0,8 | P17 | ✅ |
| P18-T12 | Hiệu ứng Viên Mãn cho cả 21 kỹ năng | — | 0,6 | T01–T11 | ✅ |
| P18-T13 | 4 phản ứng còn lại | — | 0,3 | T04, T08–T10 | ✅ |
| P18-T14 | Harness `SkillSet2PlayTest` | — | 0,4 | T01–T13 | ✅ |

---

## P18-T01 — Hàng Long Thập Bát Chưởng (Kim)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng chưởng rồng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Kiều Phong, Quách Tĩnh (Kim Dung). **ID:** `hang-long-thap-bat-chuong` · **Mở:** Kết Đan
- **Số liệu:** chưởng hình rồng vàng xuyên thẳng 15 m · 350% Công · đẩy lùi 3 m · hồi chiêu 10 giây · 35 Linh Lực
- **Các bước:**
  1. Kiểm tra trúng bằng capsule dọc đường thẳng.
  2. Đẩy lùi qua `MinionMotor` hoặc `MonsterNavigation`; boss không bị đẩy.
  3. Hiệu ứng: đầu rồng vàng, dạng mesh sinh bằng code hoặc hạt.
- **Hoàn thành khi:**
  - [x] Xuyên được nhiều quái; không đẩy quái ra khỏi NavMesh.

## P18-T02 — Tam Muội Chân Hỏa (Hỏa)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hạt lửa phun) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Hồng Hài Nhi (*Tây Du Ký*). **ID:** `tam-muoi-chan-hoa` · **Mở:** Kết Đan
- **Số liệu:** phun lửa hình nón dài 8 m trong 3 giây · 90% Công mỗi 0,25 giây + Burn · vẫn đi chậm được · hồi chiêu 14 giây · 40 Linh Lực
- **Hoàn thành khi:**
  - [x] Số nhịp sát thương đúng.
  - [x] Phản ứng Bạo Viêm kích hoạt khi mục tiêu đang bị bỏng.

## P18-T03 — Bắc Minh Thần Công (Thủy)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng hút năng lượng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Đoàn Dự, Hư Trúc (*Thiên Long Bát Bộ*). **ID:** `bac-minh-than-cong` · **Mở:** Nguyên Anh
- **Số liệu:** vận công 3 giây, hút 3 mục tiêu gần nhất · 70% Công mỗi 0,5 giây · hồi máu 50% sát thương gây ra · gắn trạng thái Wet · hồi chiêu 16 giây · 30 Linh Lực
- **Hoàn thành khi:**
  - [x] Hồi máu đúng.
  - [x] Bị ngắt khi người chơi né hoặc bị choáng.

## P18-T04 — Côn Bằng Cực Tốc (Mộc)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh gió, hiệu ứng tốc độ) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Côn Bằng Bảo Thuật (*Thế Giới Hoàn Mỹ*), Lăng Ba Vi Bộ (Kim Dung). **ID:** `con-bang-cuc-toc` · **Mở:** Nguyên Anh
- **Số liệu:** 6 giây: nhanh hơn 40%, né không tốn Thể Lực, để lại bóng ảo (`CharacterAfterimageTrail`) · hồi chiêu 25 giây · 25 Linh Lực
- **Hoàn thành khi:**
  - [x] Tốc độ vẫn nằm trong trần di chuyển.
  - [x] Bóng ảo hiển thị đúng.

## P18-T05 — Thiên Lôi Dẫn (Lôi)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và texture sét giáng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** lôi kiếp (tu tiên). **ID:** `thien-loi-dan` · **Mở:** Hóa Thần
- **Số liệu:** đánh dấu một vùng, 1,2 giây sau 5 tia sét giáng xuống · 220% Công mỗi tia, +50% với hệ Âm · hồi chiêu 20 giây · 45 Linh Lực
- **Hoàn thành khi:**
  - [x] Có vòng báo; đăng ký `DangerZoneRegistry`.

## P18-T06 — Mộc Linh Hồi Xuân (Mộc)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng hồi phục) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Liễu Thần (*Thế Giới Hoàn Mỹ*). **ID:** `moc-linh-hoi-xuan` · **Mở:** Hóa Thần
- **Số liệu:** hồi 25% máu trong 5 giây, cộng vùng hồi máu 8 giây (hồi cả Âm Binh) · hồi chiêu 35 giây · 40 Linh Lực
- **Hoàn thành khi:**
  - [x] Tổng lượng hồi đúng.
  - [x] Hồi cho đồng minh (Âm Binh) trong vùng.

## P18-T07 — Thần Thức Linh Nhãn (Vô Hệ)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh quét, shader/texture nhìn xuyên tường) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** thần thức (tu tiên), Byakugan (*Naruto*), Haki quan sát (*One Piece*). **ID:** `than-thuc-linh-nhan` · **Mở:** Hóa Thần
- **Số liệu:** thấy mọi quái xuyên tường trong 10 giây · lộ điểm yếu: +25% sát thương chí mạng lên chúng · hồi chiêu 30 giây · 20 Linh Lực
- **Các bước:**
  1. Shader viền nhìn xuyên tường (hoặc dấu HUD), dùng chung với phần Tầm Yêu của P05-T06.
  2. Làm lộ Ảnh Yêu đang ẩn thân (P19).
- **Hoàn thành khi:**
  - [x] Nhìn thấy quái sau tường; phá được ẩn thân qua `IEnemyConcealment` (smoke hook; Ảnh Yêu P19 chưa có để kiểm thực tế).

## P18-T08 — Tru Tiên Kiếm Trận (Kim)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng kiếm trận) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** *Phong Thần Diễn Nghĩa*; Đại Canh Kiếm Trận (*Phàm Nhân Tu Tiên*). **ID:** `tru-tien-kiem-tran` · **Mở:** Luyện Hư
- **Số liệu:** kiếm trận bán kính 8 m trong 10 giây · quái bên trong bị chém mỗi 0,5 giây, 60% Công · người chơi đứng trong trận +20% tỉ lệ chí mạng · hồi chiêu 28 giây · 60 Linh Lực
- **Hoàn thành khi:**
  - [x] Kiếm trận có ranh giới rõ; tick sát thương đúng.

## P18-T09 — Âm Binh Quy Hồn (Âm)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh triệu hồi, hiệu ứng bóng tối) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** "Arise" (*Solo Leveling*). **ID:** `am-binh-quy-hon` · **Mở:** Luyện Hư
- **Số liệu:** dựng dậy tối đa 3 quái vừa chết (trong 8 giây gần nhất) làm đồng minh trong 20 giây · đồng minh có 60% chỉ số gốc · hồi chiêu 40 giây · 60 Linh Lực
- **Các bước:**
  1. Thêm phe (faction) cho `MinionBrain`: đồng minh tìm quái để đánh; quái cũng coi đồng minh là mục tiêu.
  2. Đồng minh **không** tính Kiếm Ý khi chết lần hai; không thể dựng boss hay tinh anh.
  3. Hiệu ứng: bóng tím đen, mắt xanh.
- **Hoàn thành khi:**
  - [x] Đồng minh đánh đúng phe, tự tan sau 20 giây.
  - [x] Không ảnh hưởng tới luật Kiếm Ý.

## P18-T10 — Bành Trướng Lãnh Địa (Không Gian)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng lãnh địa) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** Domain Expansion (*Jujutsu Kaisen*), Lĩnh vực (*Đấu La Đại Lục*). **ID:** `banh-truong-lanh-dia` · **Mở:** Độ Kiếp
- **Số liệu:** lãnh địa bán kính 12 m trong 8 giây · quái bên trong chậm 40% · kỹ năng của người chơi hồi nhanh gấp đôi · hồi chiêu 60 giây · 80 Linh Lực
- **Hoàn thành khi:**
  - [x] Hiệu ứng chỉ áp trong bán kính.
  - [x] Hết giờ thì mọi thứ trở về bình thường.

## P18-T11 — Võ Hồn Chân Thân (Thổ)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng biến thân) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Cảm hứng:** *Đấu La Đại Lục*; Susanoo (*Naruto*); Kim Giác Cự Thú (*Thôn Phệ Tinh Không*). **ID:** `vo-hon-chan-than` · **Mở:** Độ Kiếp
- **Số liệu:** trong 10 giây: pháp tướng khổng lồ bao quanh người chơi · +50% sát thương · đánh thường đổi thành chém quét bán kính 4 m · −30% sát thương nhận · hồi chiêu 75 giây · 100 Linh Lực
- **Các bước:**
  1. Pháp tướng: mesh bán trong suốt phát sáng (dùng lại kỹ thuật `GiantHandVisual`).
  2. Không cần animation mới cho nhân vật.
  3. Máy quay lùi xa hơn trong lúc biến thân.
- **Hoàn thành khi:**
  - [x] Đổi kiểu đánh thường đúng.
  - [x] Hết giờ thì trở lại bình thường.

## P18-T12 — Hiệu ứng Viên Mãn cho cả 21 kỹ năng

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P18-T01 đến T11
- **Bảng hiệu ứng tầng Viên Mãn:**

| Kỹ năng | Viên Mãn |
|---|---|
| Đại Thủ Ấn | Để lại Ngũ Chỉ Sơn chắn đường 5 giây |
| Hư Không Kết Giới | Tường phản lại đạn |
| Ảnh Phân Thân | 2 phân thân |
| Tích Lịch Nhất Thiểm | Lướt được 2 lần liên tiếp |
| Phật Nộ Hỏa Liên | Tách thành 3 hoa sen nhỏ |
| Hàn Băng Phong Ấn | Băng vỡ để lại vùng Chill 3 giây |
| Thần Kiếm Ngự Lôi | Nảy thêm 3 lần |
| Kim Chung Tráo | Chuông vỡ thì nổ 200% Công |
| Hàng Long Thập Bát Chưởng | 2 luồng rồng hình chữ V |
| Tam Muội Chân Hỏa | Để lại tường lửa 4 giây |
| Hắc Động Thần La | Hút cả đạn |
| Bắc Minh Thần Công | Hút thêm Linh Lực |
| Côn Bằng Cực Tốc | Bóng ảo gây 60% Công khi chạm quái |
| Vạn Kiếm Quyết | 50 thanh kiếm |
| Thiên Lôi Dẫn | 8 tia sét |
| Mộc Linh Hồi Xuân | Xóa mọi hiệu ứng xấu |
| Thần Thức Linh Nhãn | Đánh dấu 3 quái yếu nhất, chúng nhận +30% sát thương |
| Tru Tiên Kiếm Trận | Kéo dài 15 giây |
| Âm Binh Quy Hồn | Tối đa 5 đồng minh |
| Bành Trướng Lãnh Địa | Quái trong lãnh địa không dùng được kỹ năng |
| Võ Hồn Chân Thân | Miễn khống chế khi đang biến thân |

- **Hoàn thành khi:**
  - [x] Hiệu ứng chỉ bật ở tầng 4 (Viên Mãn) trở lên.
  - [x] Mô tả trong tab Công Pháp khớp với hiệu ứng.

## P18-T13 — 4 phản ứng còn lại

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh riêng cho 4 phản ứng mới) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P18-T04, T08–T10
- **Các bước (§7.5):**

| Phản ứng | Điều kiện | Hiệu ứng |
|---|---|---|
| **Phong Hỏa Liệu Nguyên** | Quái đang Burn + bóng ảo Côn Bằng đi qua | Lửa lan sang quái lân cận |
| **Kiếm Hồn** | Âm Binh đứng trong Tru Tiên Kiếm Trận | Âm Binh +50% sát thương |
| **Hộ Thể Hấp Nguyên** | Kim Chung Tráo + Bắc Minh | Lượng máu hút về ×2 |
| **Lãnh Địa Cộng Hưởng** | Kỹ năng dùng trong Bành Trướng Lãnh Địa | +20% sát thương |

- **Hoàn thành khi:**
  - [x] Cả 4 phản ứng có trong `ReactionPlayTest`.

## P18-T14 — Harness `SkillSet2PlayTest`

- **Loại:** Test · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P18-T01 đến T13
- **Kiểm tra:** giống P10-T10, cho 11 kỹ năng mới, các hiệu ứng Viên Mãn và 4 phản ứng mới.
- **Hoàn thành khi:**
  - [x] PASS.
  - [x] Nhóm hồi quy Kỹ năng không có FAIL mới.

---

## Kiểm chứng cuối phase

- [x] Tab Công Pháp đủ 21 kỹ năng. Bộ gợi ý §7.6 của màn 7–10 được đổi sang đúng bộ đầy đủ.
- [x] Cập nhật trạng thái P18 trong `task/README.md`.
