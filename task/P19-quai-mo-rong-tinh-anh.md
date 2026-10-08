# P19 — Quái mở rộng và tinh anh

> **Mục tiêu:** thêm 4 loại quái còn lại (Ảnh Yêu, Triệu Hồn Sư, Dực Yêu, Hỏa Linh), hệ tinh anh với 8 phụ tố, AI T2/T4 đầy đủ, và trả thành phần quái màn 6–10 về đúng kế hoạch.
>
> **Phạm vi:** Đầy đủ · **Ước lượng:** 6,7 ngày công · **Phụ thuộc:** P17 · **Tham chiếu:** §2.3, §3.2–§3.4

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P19-T01 | Ảnh Yêu: dịch chuyển, ẩn thân | Code+Asset | 0,8 | P17 | ✅ smoke |
| P19-T02 | Triệu Hồn Sư: gọi quái, vùng hồi máu, khiên | Code+Asset | 0,8 | P17 | ✅ smoke |
| P19-T03 | Dực Yêu: di chuyển bay, bổ nhào, thả cầu lửa | Code+Asset | 1,2 | P17 | ✅ smoke |
| P19-T04 | Hỏa Linh và việc cự thú thả Hỏa Linh (màn 10) | Code+Asset | 0,7 | P17 | ✅ smoke |
| P19-T05 | Hệ tinh anh và 8 phụ tố | Code | 1 | T01–T04 | ✅ smoke |
| P19-T06 | AI T2 rút lui; AI T4 thích nghi | Code | 0,8 | T02 | ✅ smoke |
| P19-T07 | Thành phần quái màn 6–10 theo kế hoạch | Dữ liệu | 0,4 | T01–T05 | ✅ smoke |
| P19-T08 | ★★★ màn 6 theo điều kiện Triệu Hồn Sư | Code | 0,2 | T02 | ✅ smoke |
| P19-T09 | Cân bằng lại màn 6–10 và harness | Test | 0,8 | T01–T08 | ✅ smoke |

---

## P19-T01 — Ảnh Yêu: dịch chuyển, ẩn thân

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model sát thủ bóng tối có animation, âm thanh dịch chuyển) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Số liệu gốc (§3.2):** hệ Âm · 80 máu · sát thương 14 · tốc 6,5 · sát thủ
- **Các bước:**
  1. Từ màn 6: **dịch chuyển** ra sau lưng người chơi. Trước đó có dấu hiệu 0,5 giây (khói tím tại điểm đến, kèm âm thanh).
  2. Từ màn 8: **ẩn thân** cho tới khi cách người chơi 5 m. Bị lộ bởi Thần Thức Linh Nhãn hoặc khi trúng đòn.
  3. Model sát thủ hệ bóng tối; ghi LICENSES.
- **Hoàn thành khi:**
  - [x] Né được nhờ dấu hiệu báo trước.
  - [x] Thần Thức làm lộ Ảnh Yêu đang ẩn.

## P19-T02 — Triệu Hồn Sư: gọi quái, vùng hồi máu, khiên

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model pháp sư có animation, âm thanh triệu hồi và hồi máu) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Số liệu gốc:** hệ Âm · 120 máu · sát thương 6 · tốc 3,5 · hỗ trợ · trọng số Kiếm Ý 2
- **Các bước:**
  1. Từ màn 6: gọi 2 Tiểu Yêu mỗi 12 giây. Quái gọi ra có `countsForSwordIntent = false` và **chết theo khi Triệu Hồn Sư chết**.
  2. Từ màn 8: vùng hồi máu bán kính 6 m, hồi 3%/giây cho đồng bọn.
  3. Từ màn 10: khiên cho 3 đồng bọn, mỗi khiên chịu 20% máu của con được che.
  4. Luôn đứng ở hàng sau, giữ khoảng cách với người chơi.
- **Hoàn thành khi:**
  - [x] Hạ Triệu Hồn Sư thì quái do nó gọi tan hết.

## P19-T03 — Dực Yêu: di chuyển bay, bổ nhào, thả cầu lửa

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model quái bay có animation, âm thanh vỗ cánh và bổ nhào) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Số liệu gốc:** hệ Mộc · 40 máu · sát thương 8 · tốc bay 7 · trọng số 1
- **Các bước:**
  1. `FlyingMotor`: bay cách mặt NavMesh 3–6 m, lái tránh vật cản bằng raycast. Không vào nhà (dùng `ShelterDetector` để nhận biết).
  2. Từ màn 6: **bổ nhào** với vòng báo 0,6 giây.
  3. Từ màn 9: thả cầu lửa từ trên cao, để lại vệt Dư Hỏa nhỏ.
  4. Kỹ năng tự tìm mục tiêu (Thần Kiếm Ngự Lôi) ưu tiên quái bay; đánh thường có thể ngắm lên cao.
- **Hoàn thành khi:**
  - [x] Không kẹt trong nhà hoặc trong tường.
  - [x] Người chơi có cách để đánh trúng.

## P19-T04 — Hỏa Linh và việc cự thú thả Hỏa Linh (màn 10)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model tinh linh lửa có animation, âm thanh lửa) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Số liệu gốc:** hệ Hỏa · 150 máu · sát thương 16 · tốc 5
- **Các bước:**
  1. Để lại vệt lửa khi chạy. Miễn nhiễm Thiên Hỏa. Nhanh hơn 20% trong 10 giây sau mỗi lần phun (đã có luật ở P13-T08).
  2. Màn 10 pha 3: Hỏa Long Vương thả 4 Hỏa Linh xuống mỗi 30 giây. Không tính Kiếm Ý; tự tan khi Kiếm Ý đầy.
  3. Màn 8–9: thay Bạo Thi bằng Hỏa Linh, đúng như kế hoạch.
- **Hoàn thành khi:**
  - [x] Việc thả Hỏa Linh không làm Kiếm Ý kẹt dưới 100%.

## P19-T05 — Hệ tinh anh và 8 phụ tố

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (hiệu ứng viền phát sáng và âm thanh cho tinh anh) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **File:** (mới) `Assets/Enemies/Runtime/EliteAffix.cs`, `Assets/Enemies/Data/Affixes/*.asset`
- **Các bước (§3.4):**
  1. Tinh anh: máu ×3, thân to ×1,3, viền phát sáng, tên hiện trên đầu (dùng `EnemyHealthBar`), trọng số Kiếm Ý 4.
  2. Số phụ tố: màn 6–7 có 1; màn 8–10 có 2 (không trùng nhau).
  3. 8 phụ tố:

| Phụ tố | Hiệu ứng |
|---|---|
| Cuồng Bạo | Nhanh hơn 30%, đánh nhanh hơn 30% |
| Kim Thân | −40% sát thương nhận, tới khi bị Phá Giáp |
| Phân Liệt | Chết thì tách thành 2 bản nhỏ, mỗi bản 30% máu |
| Hấp Huyết | Hồi 20% sát thương gây ra |
| Tự Bạo | Nổ khi chết, bán kính 4 m |
| Ẩn Hình | Vô hình khi đứng yên |
| Hộ Vệ | Quái xung quanh nhận ít hơn 30% sát thương |
| Hỏa Tâm | Miễn nhiễm Thiên Hỏa, chỉ ở màn 8–10 |

  4. Hai bản nhỏ của Phân Liệt: trọng số Kiếm Ý đã được tính gộp sẵn trong con gốc, nên không làm thanh vượt 100%.
- **Hoàn thành khi:**
  - [x] Mỗi phụ tố có test riêng.
  - [x] Kiếm Ý vẫn đạt đúng 100% khi dọn sạch đợt có tinh anh Phân Liệt.

## P19-T06 — AI T2 rút lui; AI T4 thích nghi

- **Các bước:**
  1. **T2 rút lui:** quái còn 25% máu thì rút về cạnh Triệu Hồn Sư gần nhất (nếu có) để được hồi.
  2. **T4 thích nghi:**
     - thống kê kỹ năng vùng người chơi dùng trong 60 giây gần nhất; nếu nhiều thì quái giãn khoảng cách với nhau (vây rộng hơn);
     - tỉ lệ né chiêu tăng lên 60%;
     - giữ luật chặn cửa của P16-T02.
- **Hoàn thành khi:**
  - [x] Đo được sự thay đổi hành vi trong harness: độ giãn của vòng vây và tỉ lệ né.

## P19-T07 — Thành phần quái màn 6–10 theo kế hoạch

- **Các bước:**
  1. Màn 6: thêm Ảnh Yêu, Triệu Hồn Sư, Dực Yêu, và 3 tinh anh (§2.3).
  2. Màn 7: thêm 5 tinh anh. Boss Shaban pha 2 gọi 2 Ảnh Yêu, thay cho 3 Tiểu Yêu.
  3. Màn 8–10: có Hỏa Linh; tinh anh 2 phụ tố.
  4. Chỉnh tổng số quái để vẫn đúng 26 / 30 / 32 / 36 / 45.
- **Hoàn thành khi:**
  - [x] `LevelValidation` PASS với thành phần mới.

## P19-T08 — ★★★ màn 6 theo điều kiện Triệu Hồn Sư

- **Các bước:** đổi điều kiện ★★★ màn 6 từ "dưới 11:00" (bản MVP) sang "hạ mỗi Triệu Hồn Sư trước lần triệu hồi thứ 2" (§2.3).
- **Hoàn thành khi:**
  - [x] Có test đạt và test không đạt.

## P19-T09 — Cân bằng lại màn 6–10 và harness

- **Các bước:**
  1. Chơi thử lại màn 6–10 ở đúng cảnh giới đề nghị, so với §16. Dùng telemetry (P17-T02).
  2. Harness `EnemyRosterPlayTest`: kiểm tra từng loại quái mới, phụ tố, AI T2/T4.
- **Hoàn thành khi:**
  - [ ] Thời lượng các màn trong khoảng ±30% so với mốc.
  - [x] Harness PASS.

---

## Kiểm chứng cuối phase

- [x] Đủ11loại quái thường (7model người dùng +4mới) và8phụ tố; màn6–10 đúng roster ưu tiên người dùng.
- [x] Đủ11loại quái thường (7model người dùng +4mới) và8phụ tố; màn6–10 đúng roster ưu tiên người dùng.

## Kết quả P19 · 03/10/2026

T01–T09 hoàn tất mức smoke theo PROMPT/TEST-POLICY. T04.3 được ghi đè bởi chỉ dẫn người dùng: **thêm Hỏa Linh, giữ Bạo Thi và đủ7loại gốc ở mỗi màn8–10**. Tổng26/30/32/36/45 gồm elite/boss; summon/split/drop không tăng tổng.

P19PlayTest88/0, EnemyRoster95/0, HeavenSword41/0, Level8to10 87/0, LevelValidation185/0. Thời lượng±30% giữ chưa đánh dấu: **chưa đo thực chiến**, bỏ theo TEST-POLICY, không suy ra từ DEV smoke. [Báo cáo](p19/REPORT-P19.md) · [Cân bằng](p19/BALANCE-NOTES.md) · [Progress](p19/PROGRESS.md).
