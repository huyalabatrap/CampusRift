# Campus Rift — Kế hoạch phát triển V2: "HỌC ĐỂ THẮNG"

> Bản kế hoạch mới, xây lại từ đầu dựa trên ý tưởng của bạn (2026-09-28). Bản này **thay thế** `GAME_DESIGN_PLAN.md`; bản cũ chỉ còn để tham khảo và có thể xóa.
>
> **Ký hiệu:** ⭐ = ý tưởng gốc của bạn · ➕ = ý tưởng tôi bổ sung theo phong cách của bạn (bỏ được nếu không hợp).
>
> **Nội dung học:** môn **Tư tưởng Hồ Chí Minh**. Bạn sẽ gửi nội dung chi tiết sau; chỗ nào ghi *(chờ nội dung)* sẽ cập nhật khi có. Tài liệu này **không** tự viết nội dung môn học. Mọi ví dụ câu hỏi chỉ minh họa *dạng* câu.
>
> Mọi con số là giá trị khởi điểm để cân bằng, sẽ chỉnh qua chơi thử.

---

## Mục lục

0. [Tóm tắt một trang](#0-tóm-tắt-một-trang)
1. [Giữ lại và thay đổi so với bản hiện tại](#1-giữ-lại-và-thay-đổi-so-với-bản-hiện-tại)
2. [10 màn chơi](#2-10-màn-chơi-)
3. [Quái vật](#3-quái-vật-)
4. [Cự thú bầu trời và Thiên Hỏa (màn 8–10)](#4-cự-thú-bầu-trời-và-thiên-hỏa-màn-810-)
5. [Thiên Kiếm](#5-thiên-kiếm-)
6. [Chiến đấu của người chơi](#6-chiến-đấu-của-người-chơi-)
7. [Hệ thống kỹ năng — 4 ô, combo, tham khảo anime và tu tiên](#7-hệ-thống-kỹ-năng--4-ô-combo-tham-khảo-anime-và-tu-tiên-)
8. [Tu luyện — học để lên cấp](#8-tu-luyện--học-để-lên-cấp-)
9. [Linh Thạch và cửa hàng — học để mua vật phẩm](#9-linh-thạch-và-cửa-hàng--học-để-mua-vật-phẩm-)
10. [Cam kết "Study to Win"](#10-cam-kết-study-to-win-)
11. [Nội dung học: Tư tưởng Hồ Chí Minh](#11-nội-dung-học-tư-tưởng-hồ-chí-minh-)
12. [Cốt truyện và thế giới](#12-cốt-truyện-và-thế-giới-)
13. [Giao diện và HUD](#13-giao-diện-và-hud)
14. [Triển khai kỹ thuật trên code hiện tại](#14-triển-khai-kỹ-thuật-trên-code-hiện-tại)
15. [Lộ trình](#15-lộ-trình)
16. [Bảng cân bằng tổng hợp](#16-bảng-cân-bằng-tổng-hợp)
17. [Kiểm thử](#17-kiểm-thử)
18. [Rủi ro](#18-rủi-ro)
19. [Quyết định cần bạn chốt](#19-quyết-định-cần-bạn-chốt)
- [Phụ lục A — Mẫu nhập nội dung](#phụ-lục-a--mẫu-nhập-nội-dung)
- [Phụ lục B — Thuật ngữ](#phụ-lục-b--thuật-ngữ)
- [Phụ lục C — Nguồn tham khảo](#phụ-lục-c--nguồn-tham-khảo)

---

## 0. Tóm tắt một trang

**Thể loại:** hành động tu tiên 3D, góc nhìn thứ ba, kết hợp học tập. Người chơi là sinh viên tu luyện bằng việc học, chiến đấu qua **10 màn** trong khuôn viên trường bị Khe Nứt Rift xâm chiếm.

| Trụ cột | Nội dung |
|---|---|
| ⭐ 10 màn leo thang | Độ khó tăng theo 4 trục: **số lượng quái · độ linh hoạt · kỹ năng của quái · máu trâu hơn** |
| ⭐ Cự thú bầu trời (màn 8–10) | Cứ một khoảng thời gian lại **phun Thiên Hỏa** xuống toàn khuôn viên. **Ở trong nhà mất ít máu, ở ngoài trời mất nhiều** |
| ⭐ Thiên Kiếm | Diệt sạch quái trên sân thì đầy **Kiếm Ý**, rồi triệu hồi **thanh kiếm siêu khổng lồ** chém cự thú |
| ⭐ Học để thắng | Học Tư tưởng Hồ Chí Minh để nhận **Tu Vi** (lên cấp, lên cảnh giới) và **Linh Thạch** (mua đan dược hồi máu, buff sức mạnh) |
| ⭐ 4 ô kỹ năng | Có 21 kỹ năng lấy cảm hứng từ anime và phim 3D tu tiên Trung Quốc, nhưng **chỉ mang được 4** vào màn, nên phải tính combo cho từng màn |
| ⭐ Không pay-to-win | Không có cách nạp tiền mua sức mạnh. Sức mạnh chỉ đến từ **học** và **kỹ năng chơi** |

**Vòng lặp chính:**

```
 ┌──────────────── HỌC (Thư Viện) ────────────────┐
 │ Đọc bài → Luyện tập → Ôn tập → Thi Đột Phá      │
 └────────┬───────────────────────────┬───────────┘
          ▼                           ▼
     TU VI (EXP)                LINH THẠCH (điểm)
     lên tầng / cảnh giới       mua đan dược, phù chú,
     mở kỹ năng, mở màn         nâng pháp bảo
          └─────────────┬─────────────┘
                        ▼
        CHUẨN BỊ: chọn 4 kỹ năng + 3 ô vật phẩm
                        ▼
        VÀO MÀN: diệt quái
        (màn 8–10: né Thiên Hỏa → tích Kiếm Ý → Thiên Kiếm)
                        ▼
        KẾT QUẢ: sao ★, Linh Thạch thưởng → quay lại HỌC
```

---

## 1. Giữ lại và thay đổi so với bản hiện tại

| Hạng mục đang có | Trong V2 |
|---|---|
| Map campus: 12 tòa nhà (A–X), tòa cao tới 15 tầng, cửa tự động, thang máy | **Giữ.** Đây là sân khấu của cả 10 màn; các tòa nhà là nơi trú Thiên Hỏa |
| Shaban và AI săn mồi (nhìn, nghe, dự đoán, đi thang máy) | **Giữ.** Shaban thành quái tinh anh ở màn 3 và boss ở màn 5, màn 7 |
| Giant Hand / Void Wall / Phantom Decoy | **Giữ, đổi tên** thành Đại Thủ Ấn / Hư Không Kết Giới / Ảnh Phân Thân, là 3 kỹ năng khởi đầu |
| `SpeedForceVFX` (sét quanh người khi chạy), `CharacterAfterimageTrail` (bóng ảo) | **Dùng lại** cho kỹ năng hệ Lôi và kỹ năng di chuyển |
| `PlayerFlashlight` | **Dùng lại** cho màn đêm mất điện (màn 4) |
| `SkyLightingController`, màn Victory, SkyVictory | **Dùng lại** cho bầu trời đỏ lửa màn 8–10, bình minh khi thắng |
| Learning engine (bài, quiz, lưu tiến độ, song ngữ) | **Giữ khung.** Thay nội dung Algorithms bằng **Tư tưởng Hồ Chí Minh**; thay cơ chế "5 bài mở 1 skill" bằng hệ **Tu Vi và cảnh giới** |
| Mục tiêu "chạy trốn" và áp lực tăng theo thời gian | **Bỏ.** Mục tiêu mỗi màn là diệt quái, hạ boss hoặc hạ cự thú |
| Mở bài học bằng thời gian sống sót | **Bỏ.** Bài mở theo thứ tự chương và cảnh giới |
| Mở Courses giữa trận | **Bỏ.** Chỉ học ở Sảnh Tu Luyện, ngoài màn chơi |
| Void Wall 20 lượt dùng | **Đổi** thành 3 lượt nạp, tự hồi theo thời gian |

---

## 2. 10 màn chơi ⭐

### 2.1 Bảng tổng quan

HP× và ST× là hệ số nhân máu và sát thương quái so với màn 1. Cấp AI xem [mục 3.3](#33-độ-linh-hoạt-ai-t0t4-).

| Màn | Tên | Bối cảnh | Quái (tổng / đợt) | HP× | ST× | Tốc× | AI | Boss / Cự thú | Yêu cầu cảnh giới | Thời lượng |
|:-:|---|---|---|:-:|:-:|:-:|:-:|---|---|:-:|
| 1 | Khe Nứt Đầu Tiên | Chiều tà, sân trung tâm | 6 / 2 | 1.0 | 1.0 | 1.00 | T0 | — | Luyện Khí 1 | ~4′ |
| 2 | Độc Vụ Hành Lang | Hoàng hôn, khu B | 10 / 2 | 1.3 | 1.25 | 1.03 | T0 | — | Luyện Khí 3 | ~5′ |
| 3 | Kẻ Săn Hồn | Đêm, khu A–B nhiều tầng | 14 / 3 | 1.8 | 1.8 | 1.06 | T1 | Shaban (tinh anh) | Trúc Cơ 1 | ~7′ |
| 4 | Đêm Mất Điện | Đêm, khu D–E, một tòa mất điện | 18 / 3 | 2.2 | 2.1 | 1.09 | T1 | — | Trúc Cơ 3 | ~8′ |
| 5 | Săn Hồn Giả | Nửa đêm, khu trung tâm | 22 / 3 | 3.0 | 2.8 | 1.12 | T2 | **BOSS** Shaban Săn Hồn Giả | Kết Đan 1 | ~10′ |
| 6 | Huyết Nguyệt | Trăng máu, khu H–V | 26 / 4 | 3.5 | 3.2 | 1.16 | T2 | Tinh anh có phụ tố | Kết Đan 3 | ~11′ |
| 7 | Săn Hồn Thức Tỉnh | Trăng máu, toàn campus, tòa X cao tầng | 30 / 4 | 4.6 | 4.2 | 1.20 | T3 | **BOSS** Shaban Thức Tỉnh (2 pha) | Nguyên Anh 1 | ~13′ |
| 8 | Xích Hỏa Giáng Thế | Trời đỏ lửa | 32 / 1 | 6.4 | 5.8 | 1.25 | T3 | **CỰ THÚ** Xích Hỏa Giao, 1 nhát Thiên Kiếm | Hóa Thần 1 | ~12′ |
| 9 | Chu Tước Phần Thiên | Trời đỏ, mưa tro | 36 / 2 | 8.4 | 7.6 | 1.30 | T4 | **CỰ THÚ** Tà Hóa Chu Tước, 2 nhát | Luyện Hư 1 | ~15′ |
| 10 | Tam Thú Lâm Không | Nhật thực đỏ | 45 / 3 | 10.5 | 9.6 | 1.35 | T4 | **3 CỰ THÚ** Giao + Chu Tước + Cửu U Hỏa Long Vương, 3 nhát | Độ Kiếp 1 | ~18′ |

### 2.2 Quy tắc chung

- **Thắng (màn 1–7):** diệt hết quái của mọi đợt; màn 5 và 7 có thêm boss.
- **Thắng (màn 8–10):** diệt hết quái một đợt thì Kiếm Ý đầy; triệu hồi Thiên Kiếm và cự thú mất một giai đoạn. Đủ số nhát thì thắng.
- **Thua:** máu về 0 (Hộ Mệnh Phù cho hồi sinh 1 lần). Thua **không mất** Tu Vi hay Linh Thạch kiếm từ việc học, chỉ mất vật phẩm đã dùng trong màn.
- **Khe Nứt:** quái chui ra từ các khe nứt ở sân và hành lang. Số quái hoạt động cùng lúc có giới hạn: **PC 14, mobile 9**. Mỗi con chết thì khe nứt nhả con tiếp theo.
- ➕ **Tầm Yêu tự động:** khi còn ≤ 3 quái, cứ 20 giây hiện vị trí chúng trong 3 giây. Campus rất rộng (tòa cao tới 15 tầng), nên cần tránh việc lùng con quái cuối cùng quá lâu.
- ➕ **Nghỉ giữa đợt:** 15 giây, hồi 20% Linh Lực và được dùng vật phẩm. Riêng màn 9–10 được đổi kỹ năng trong lúc nghỉ.
- **Chơi lại:** màn đã mở chơi lại tùy ý để lấy sao.
- **Sao:** ★ hoàn thành · ★★ hoàn thành, không dùng Hộ Mệnh Phù và dưới thời gian mốc · ★★★ hoàn thành thử thách riêng của màn.
- ➕ **Linh Bia Cơ Duyên:** mỗi màn có 1–2 bia đá. Trả lời đúng 1 câu từ bài đã học thì chọn 1 trong 3 buff tạm thời cho màn đó. Trả lời sai thì bia tắt, không bị phạt (xem [11.2](#112-chế-độ-học)).

### 2.3 Chi tiết từng màn

**Màn 1 — Khe Nứt Đầu Tiên** · Luyện Khí 1 · mốc 4:00
- Chiều tà, sân trung tâm. Khe nứt đầu tiên mở giữa sân.
- 2 đợt × 3 Tiểu Yêu.
- Dạy cách chơi: di chuyển, đánh thường, né, Đại Thủ Ấn; Linh Bia hướng dẫn cách trả lời câu hỏi.
- ★★★: hạ 3 quái bằng Đại Thủ Ấn.

**Màn 2 — Độc Vụ Hành Lang** · Luyện Khí 3 · mốc 5:00
- Hoàng hôn, khu B, hành lang tầng 1–2.
- 10 quái / 2 đợt: 7 Tiểu Yêu, 3 Độc Nhãn Xạ Thủ đứng xa bắn độc.
- Điểm nhấn: học cách áp sát quái đánh xa bằng Tích Lịch Nhất Thiểm, dùng Kết Giới chắn đạn.
- ★★★: trúng đạn độc không quá 3 lần.

**Màn 3 — Kẻ Săn Hồn** · Trúc Cơ 1 · mốc 7:00
- Đêm, khu A–B nhiều tầng. Lần đầu phải đi cầu thang và thang máy để truy quái.
- 14 quái / 3 đợt: 8 Tiểu Yêu, 4 Độc Nhãn, 1 Thiết Giáp Ngưu, và **Shaban tinh anh** ở đợt 3.
- Điểm nhấn: Shaban dùng nguyên AI săn mồi hiện có (nghe tiếng, đoán hướng chạy, đi thang máy), nên có cảm giác bị săn ngay giữa trận đánh.
- ★★★: hạ Shaban trong 60 giây kể từ lúc chạm trán.

**Màn 4 — Đêm Mất Điện** · Trúc Cơ 3 · mốc 8:00
- Đêm, khu D–E. Một tòa mất điện, phải dùng đèn pin (`PlayerFlashlight` có sẵn).
- 18 quái / 3 đợt: thêm 3 Bạo Thi (nổ khi chết) và 2 Thiết Giáp.
- Quái bắt đầu có kỹ năng: Tiểu Yêu biết vồ, Thiết Giáp biết húc.
- ★★★: kích hoạt 5 lần phản ứng Băng Lôi Liệt.

**Màn 5 — Săn Hồn Giả (BOSS)** · Kết Đan 1 · mốc 10:00
- Nửa đêm, khu trung tâm.
- 22 quái / 3 đợt, sau đó là boss **Shaban Săn Hồn Giả** (500 × 3.0 = 1.500 máu).
- Kỹ năng boss:
  - **Gầm Hồn:** choáng 1 giây trong bán kính 8 m, báo trước 1,2 giây.
  - **Vồ Đập:** nhảy xa 12 m, tạo sóng xung kích.
  - Đi thang máy đuổi theo người chơi.
- ★★★: hạ boss mà không dùng đan hồi máu.

**Màn 6 — Huyết Nguyệt** · Kết Đan 3 · mốc 11:00
- Trăng máu (bầu trời nhuộm đỏ bằng `SkyLightingController`), khu H–V.
- 26 quái / 4 đợt, có thêm:
  - Ảnh Yêu: dịch chuyển ra sau lưng người chơi.
  - Triệu Hồn Sư: gọi thêm Tiểu Yêu, hồi máu cho đồng bọn.
  - Dực Yêu: bay và bổ nhào.
- Có 3 quái tinh anh mang phụ tố.
- Luật Huyết Nguyệt: quái ở ngoài trời nhanh hơn 15%.
- ★★★: hạ mỗi Triệu Hồn Sư trước khi nó triệu hồi lần thứ 2.

**Màn 7 — Săn Hồn Thức Tỉnh (BOSS)** · Nguyên Anh 1 · mốc 13:00
- Trăng máu, toàn campus. Trận boss diễn ra trong tòa X cao tầng.
- 30 quái / 4 đợt và 5 tinh anh. Boss **Shaban Thức Tỉnh** có 500 × 4.6 × 1.5 = 3.450 máu, đánh 2 pha:
  - Pha 2 bắt đầu khi còn ≤ 50% máu: cuồng nộ, nhanh hơn 30%, lướt bóng liên hoàn 3 lần, gọi 2 Ảnh Yêu.
- Kết màn: trước khi tan biến, Shaban để lộ **Cửu U Hỏa Long Vương** đứng sau Khe Nứt, bầu trời bắt đầu đỏ lửa (dẫn vào màn 8).
- ★★★: hoàn thành dưới 13:00.

**Màn 8 — Xích Hỏa Giáng Thế (CỰ THÚ)** · Hóa Thần 1 · mốc 12:00
- Trời đỏ lửa. Cự thú **Xích Hỏa Giao** (giao long lửa dài khoảng 120 m) lượn quanh campus.
- 32 quái trong 1 đợt lớn, có Hỏa Linh miễn nhiễm lửa.
- Thiên Hỏa mỗi 45 giây. Cần **1 nhát Thiên Kiếm**.
- ★★★: không trúng Thiên Hỏa lần nào khi đang ở ngoài trời.

**Màn 9 — Chu Tước Phần Thiên (CỰ THÚ)** · Luyện Hư 1 · mốc 15:00
- Trời đỏ, mưa tro làm giảm tầm nhìn. Cự thú **Tà Hóa Chu Tước** (chim lửa sải cánh khoảng 150 m).
- 36 quái / 2 đợt (16 + 20). Chu Tước có 2 giai đoạn, cần **2 nhát Thiên Kiếm**.
- Đòn thêm **Lông Vũ Hỏa:** giữa hai lần phun, cứ 8 giây thả 3 lông lửa nhắm vào vị trí người chơi nếu đang ở ngoài trời (vòng báo trước 1,2 giây).
- Thiên Hỏa: pha 1 mỗi 40 giây, pha 2 mỗi 32 giây.
- ★★★: triệu hồi Thiên Kiếm trong vòng 10 giây sau khi Kiếm Ý đầy, cả 2 lần.

**Màn 10 — Tam Thú Lâm Không (3 CỰ THÚ)** · Độ Kiếp 1 · mốc 18:00
- Nhật thực đỏ. Trên trời có **nhiều cự thú cùng lúc**.
- 45 quái / 3 đợt (13 + 15 + 17). Mỗi đợt dọn sạch thì chém được một cự thú:

| Giai đoạn | Trên trời | Thiên Hỏa | Đòn thêm | Kết thúc giai đoạn |
|---|---|---|---|---|
| 1 | Xích Hỏa Giao + Tà Hóa Chu Tước | Hai con thay nhau phun, **mỗi 20 giây** có một lần | Lông Vũ Hỏa | Thiên Kiếm diệt Giao |
| 2 | Chu Tước (cuồng nộ) | Mỗi 28 giây | Lông Vũ Hỏa dày hơn | Thiên Kiếm diệt Chu Tước |
| 3 | **Cửu U Hỏa Long Vương** giáng xuống | Mỗi 25 giây | Mưa thiên thạch (6 điểm rơi mỗi 15 giây, có vòng báo; trong nhà an toàn). Thả 4 Hỏa Linh xuống sân mỗi 30 giây. Khi đợt 3 bị dọn sạch, tung **Long Nộ**: mưa lửa liên tục 12 giây | Thiên Kiếm cuối cùng: Khe Nứt khép lại, bình minh lên |

- ★★★: kích hoạt ít nhất 3 lần Tương Sinh Liên Hoàn (xem [7.5](#75-phản-ứng-và-combo-)).

---

## 3. Quái vật ⭐

### 3.1 Bốn trục độ khó

| Trục ⭐ | Cách tăng | Màn 1 → Màn 10 |
|---|---|---|
| **Số lượng** | Tổng số quái, số đợt, số quái hoạt động cùng lúc | 6 → 45 quái; 2 → 3–4 đợt |
| **Linh hoạt** | Cấp AI T0 → T4 | Từ đuổi thẳng → bao vây, né chiêu, phục kích, chặn cửa khi Thiên Hỏa sắp phun |
| **Có kỹ năng** | Số kỹ năng mỗi loại quái, hồi chiêu ngắn dần | 0 → 2–3 kỹ năng mỗi loại; hồi chiêu giảm tới 40% |
| **Máu trâu** | HP× | 1.0 → 10.5. Tiểu Yêu từ 60 máu lên 630 máu, và vẫn trâu hơn khoảng 25% *so với sức người chơi* dù người chơi đạt đúng cảnh giới đề nghị (xem [16](#16-bảng-cân-bằng-tổng-hợp)) |
| ➕ Sát thương, tốc độ | ST×, Tốc× | ST 1.0 → 9.6; Tốc 1.00 → 1.35 (quái vẫn chậm hơn người chơi khi chạy nhanh) |

### 3.2 Danh sách quái

Máu và sát thương gốc nhân với HP× và ST× của màn. Hệ của quái xem ngũ hành ở [7.2](#72-ngũ-hành-và-khắc-chế-).

| Quái | Hệ | Vai trò | Máu gốc | ST gốc | Tốc | Từ màn | Kỹ năng mở dần theo màn | Khắc chế |
|---|:-:|---|:-:|:-:|:-:|:-:|---|---|
| **Tiểu Yêu** | Thổ | Cận chiến, đi bầy | 60 | 8 | 5.5 | 1 | M4 **Vồ** (nhảy 6 m) · M8 **Bầy Đàn** (+10% tốc với mỗi đồng loại ở gần, tối đa 3) | Đánh thường (Mộc khắc Thổ), kỹ năng vùng |
| **Độc Nhãn Xạ Thủ** | Mộc | Đánh xa | 45 | 10 | 4.0 | 2 | M2 đạn độc đơn · M5 3 tia hình quạt · M9 để lại vũng độc | Hệ Kim, lướt áp sát, Kết Giới chắn đạn |
| **Thiết Giáp Ngưu** | Kim | Đỡ đòn, húc | 220 | 18 | 4.0 (húc 14) | 3 | M3 **Húc** (báo trước 1 giây) · M6 cú húc phá Kết Giới · M9 húc kép kèm sóng chấn | Hệ Hỏa, né ngang khi nó húc |
| **Bạo Thi** | Hỏa | Tự bạo | 50 | 30 (nổ bán kính 3,5 m) | 6.0 | 4 | M4 nổ khi chết · M7 lao vào người chơi để nổ (ngòi 0,8 giây) | Hệ Thủy, hạ từ xa, Kết Giới chặn |
| **Ảnh Yêu** | Âm | Sát thủ | 80 | 14 | 6.5 | 6 | M6 **dịch chuyển** ra sau lưng · M8 **ẩn thân** cho tới khi cách 5 m | Hệ Lôi, Thần Thức Linh Nhãn |
| **Triệu Hồn Sư** | Âm | Hỗ trợ, triệu hồi | 120 | 6 | 3.5 | 6 | M6 gọi 2 Tiểu Yêu mỗi 12 giây · M8 vùng hồi máu · M10 tạo khiên cho đồng bọn | Hạ trước tiên; hệ Lôi, lướt |
| **Dực Yêu** | Mộc | Bay, bổ nhào | 40 | 8 | 7.0 (bay) | 6 | M6 bổ nhào · M9 thả cầu lửa | Hệ Kim, kỹ năng tự tìm mục tiêu |
| **Hỏa Linh** | Hỏa | Tinh linh lửa | 150 | 16 | 5.0 | 8 | Để lại vệt lửa khi chạy · miễn nhiễm Thiên Hỏa · +20% tốc trong 10 giây sau mỗi lần phun | Hệ Thủy |
| **Shaban** | Âm | Kẻ săn (AI hiện có) | 500 | 25 | theo AI | 3 | M3 tinh anh · M5 boss: Gầm Hồn, Vồ Đập · M7 boss thức tỉnh: Lướt Bóng ×3, gọi Ảnh Yêu, cuồng nộ | Hệ Lôi, Hàn Băng làm chậm, Kim Chung Tráo |

Ghi chú về con số của Shaban: 500 máu và 25 sát thương là giá trị hiện có trong `MonsterVitality` và `MonsterAIConfig`.

### 3.3 Độ linh hoạt AI (T0–T4) ⭐

| Cấp | Màn | Hành vi mới | Dựa trên code |
|:-:|:-:|---|---|
| **T0** | 1–2 | Đuổi thẳng theo NavMesh; đòn đánh báo trước lâu (0,8 giây); không đi thang máy | `MinionBrain` mới (máy trạng thái nhẹ) + `CampusNavMesh` có sẵn |
| **T1** | 3–4 | Chia vị trí bao vây người chơi. Quái đánh xa giữ khoảng cách 8–12 m. Biết đi cầu thang, qua cửa. Báo trước 0,6 giây | `EnemyDirector` mới: phát "vé tấn công" và phân vị trí vây |
| **T2** | 5–6 | Né chiêu có vùng báo (40% né ngang). Chặn đầu theo hướng người chơi chạy. Còn 25% máu thì rút về cạnh Triệu Hồn Sư. Tinh anh biết đi thang máy | `InterceptionPlanner`, `MonsterPrediction`, `MonsterElevatorAwareness` (có sẵn, dùng cho tinh anh) |
| **T3** | 7–8 | Tấn công phối hợp: 2–3 con ra đòn lệch nhịp nhau. Phục kích ở cửa ra vào. Nghe được tiếng động của người chơi | `MonsterHearing` + nút Door trong `RoomGraph` |
| **T4** | 9–10 | Thích nghi: người chơi dùng nhiều kỹ năng vùng thì quái tản ra; né 60%. **Khi Thiên Hỏa sắp phun, quái chặn các cửa vào nhà** để ép người chơi ở ngoài trời | Director đọc thống kê kỹ năng; `ShelterDetector` + nút Door |

### 3.4 Tinh anh và phụ tố ➕ (màn 6 trở đi)

Tinh anh có máu ×3, thân to ×1,3, viền phát sáng và tên hiện trên đầu. Màn 6–7 mỗi tinh anh có 1 phụ tố; màn 8–10 có 2.

| Phụ tố | Hiệu ứng |
|---|---|
| Cuồng Bạo | Nhanh hơn 30%, đánh nhanh hơn 30% |
| Kim Thân | Giảm 40% sát thương nhận vào cho tới khi bị Phá Giáp (Choáng + đòn hệ Kim) |
| Phân Liệt | Chết thì tách thành 2 bản nhỏ, mỗi bản 30% máu |
| Hấp Huyết | Hồi máu bằng 20% sát thương gây ra |
| Tự Bạo | Nổ khi chết, bán kính 4 m |
| Ẩn Hình | Vô hình khi đứng yên |
| Hộ Vệ | Quái xung quanh nhận ít hơn 30% sát thương |
| Hỏa Tâm | Miễn nhiễm Thiên Hỏa (chỉ ở màn 8–10) |

### 3.5 Shaban qua 3 màn

| Màn | Dạng | Máu | Điểm mới |
|:-:|---|:-:|---|
| 3 | Tinh anh | 500 × 1.8 = 900 | AI săn mồi nguyên bản |
| 5 | Boss Săn Hồn Giả | 1.500 | Gầm Hồn (choáng), Vồ Đập (sóng xung kích); thanh máu boss |
| 7 | Boss Thức Tỉnh | 3.450 | 2 pha, Lướt Bóng liên hoàn, gọi Ảnh Yêu; cảnh diễn lộ mặt Hỏa Long Vương |

### 3.6 Ngân sách hiệu năng

- Quái hoạt động cùng lúc: PC 14, mobile 9.
- Quái ở xa (> 40 m, không nhìn thấy) cập nhật AI 2 lần/giây thay vì mỗi khung hình.
- Chỉ Shaban dùng hệ đoán vị trí nặng (`BeliefParticles` 320). Trên mobile giảm xuống 160.
- Dùng pool cho quái, thanh máu, số sát thương và hiệu ứng.

---

## 4. Cự thú bầu trời và Thiên Hỏa (màn 8–10) ⭐

### 4.1 Ba cự thú

| Cự thú | Xuất hiện | Hình dạng | Bay | Đòn riêng |
|---|---|---|---|---|
| **Xích Hỏa Giao** | Màn 8, màn 10 (giai đoạn 1) | Giao long lửa, thân dài khoảng 120 m | Cao 90 m, lượn vòng quanh campus | Thiên Hỏa |
| **Tà Hóa Chu Tước** | Màn 9, màn 10 (giai đoạn 1–2) | Chim lửa bị tà hóa, sải cánh khoảng 150 m | Cao 100 m, bay hình số 8 | Thiên Hỏa + Lông Vũ Hỏa |
| **Cửu U Hỏa Long Vương** | Màn 10 (giai đoạn 3) | Rồng lửa chín sừng, khoảng 200 m | Cao 120 m | Thiên Hỏa + mưa thiên thạch + thả Hỏa Linh + Long Nộ |

- Cự thú bay trên cao, nhìn thấy từ mọi nơi trong campus. Đòn thường của người chơi **không** chạm được tới chúng. Chỉ Thiên Kiếm hạ được cự thú ⭐.
- Thanh máu cự thú chia khúc: mỗi khúc là một nhát Thiên Kiếm.
- Tài nguyên: cần 3 model 3D biết bay, có hoạt ảnh bay, gầm, phun lửa. Bản rút gọn dùng 1 model đổi màu, đổi kích thước, đổi sừng.

### 4.2 Chu kỳ Thiên Hỏa

| Pha | Thời lượng | Điều gì xảy ra | Người chơi thấy và nghe |
|---|---|---|---|
| **Báo động** | 6 giây (màn 10 giai đoạn 3: 4 giây) | Cự thú bay vọt lên, há miệng tụ lửa, bầu trời đỏ rực dần | Còi báo, chữ đếm ngược "THIÊN HỎA 6…5…", biểu tượng trú ẩn 🏠 / ⚠ / 🔥, mũi tên chỉ lối vào nhà gần nhất |
| **Phun lửa** | 4 giây | Mưa lửa phủ toàn khuôn viên; gây sát thương mỗi 0,5 giây (8 nhịp) | Màn hình ám cam, rung nhẹ, hạt lửa rơi |
| **Dư Hỏa** | 10 giây | Các vệt lửa còn cháy trên sân ngoài trời | Vũng lửa trên mặt đất |
| **Nghỉ** | Phần còn lại của chu kỳ | Cự thú lượn; màn 9–10 có đòn phụ | — |

Chu kỳ: màn 8 là 45 giây; màn 9 là 40 → 32 giây; màn 10 là 20 → 28 → 25 giây.

Mẹo hiệu năng: mưa lửa chỉ sinh hạt trong bán kính khoảng 60 m quanh máy quay, cộng thêm ánh đỏ trên bầu trời, nên nhìn vẫn như cháy toàn campus mà máy không bị nặng.

### 4.3 Trong nhà và ngoài trời ⭐

Sát thương là **số cố định theo màn**, được tính bằng % máu của người chơi ở đúng cảnh giới đề nghị. Người học nhiều hơn, cảnh giới cao hơn thì máu nhiều hơn và mất ít % hơn. Học càng nhiều thì càng sống sót tốt.

**Tổng sát thương một lần phun (8 nhịp):**

| Vị trí | Hệ số | Màn 8 (máu đề nghị 500) | Màn 9 (650) | Màn 10 (820) |
|---|:-:|:-:|:-:|:-:|
| 🔥 **Ngoài trời** | 100% | 280 (56%) | 416 (64%) | 600 (73%) |
| ⚠ **Bán che** ➕ (hiên, sảnh mở, dưới mái che, cạnh cửa sổ) | 45% | 126 (25%) | 187 (29%) | 270 (33%) |
| 🏠 **Trong nhà** | 12% | 34 (7%) | 50 (8%) | 72 (9%) |

- Mỗi nhịp gây 1/8 tổng trên. Sát thương môi trường **bỏ qua** khoảng miễn thương 0,5 giây hiện có của người chơi (xem [14.5](#145-những-chỗ-phải-sửa-trong-code-hiện-tại)).
- **Dư Hỏa:** đứng trên vũng lửa mất 4%/giây máu đề nghị (màn 8 là 20/giây).
- **Long Nộ** (màn 10, trước nhát kiếm cuối): mưa lửa liên tục 12 giây. Ngoài trời mất 10%/giây, gần như chắc chết nếu không có phòng hộ. Bán che mất 4%/giây, trong nhà 1%/giây.
- Cách game biết người chơi đang ở đâu: xem [14.4](#144-phát-hiện-trong-nhà-và-ngoài-trời).

### 4.4 Thiên Hỏa và quái ➕

- Quái **không** thuộc hệ Hỏa cũng dính lửa khi ở ngoài trời, mất 50% mức sát thương ngoài trời. Có thể **dụ quái ra sân** trước lúc phun lửa.
- Hỏa Linh và tinh anh có phụ tố Hỏa Tâm miễn nhiễm, và còn nhanh hơn 20% sau mỗi lần phun.
- Âm Binh (đồng minh do người chơi triệu hồi) cũng dính lửa.

### 4.5 Giảm sát thương lửa

| Nguồn | Hiệu quả |
|---|---|
| Tị Hỏa Châu (vật phẩm) | −50% Thiên Hỏa trong 90 giây |
| Băng Tâm Phù (vật phẩm) | Miễn nhiễm Dư Hỏa, −25% Thiên Hỏa trong 120 giây |
| Kim Chung Tráo (kỹ năng) | −60% Thiên Hỏa khi chuông còn |
| Hư Không Kết Giới (kỹ năng) | Vùng 3 m phía sau bức tường được tính là "bán che" |
| Cảnh giới cao hơn, Hộ Tâm Kính (pháp bảo) | Nhiều máu hơn |
| **Trần giảm tối đa** | Tổng giảm ngoài trời không quá 80% (không có chuyện bất tử) |

---

## 5. Thiên Kiếm ⭐

### 5.1 Kiếm Ý

- Kiếm Ý = tổng trọng số quái đã hạ ÷ tổng trọng số của đợt hiện tại.
- Trọng số: quái thường 1, Thiết Giáp và Triệu Hồn Sư 2, tinh anh 4.
- Quái do Triệu Hồn Sư gọi ra và Hỏa Linh do cự thú thả xuống **không tính**, và **tự tan** khi Kiếm Ý đầy.
- Kiếm Ý không giảm theo thời gian.
- **Kiếm Ý chỉ đầy khi toàn bộ quái của đợt đã chết** ⭐. Không vật phẩm nào làm đầy sớm hơn.

### 5.2 Triệu hồi

- Nút **THIÊN KIẾM** riêng (**không** chiếm 1 trong 4 ô kỹ năng) sáng lên, có tiếng kiếm ngân.
- ➕ Phải đứng **ngoài trời** (cần thấy bầu trời). Người chơi phải canh thời điểm giữa hai lần Thiên Hỏa, tạo thêm kịch tính. Nếu đang ở trong nhà, HUD nhắc "Ra ngoài trời để triệu hồi Thiên Kiếm".
- Niệm chú 2,5 giây, đứng yên, dưới chân hiện vòng kiếm trận vàng. Nếu Thiên Hỏa trúng giữa chừng thì bị ngắt, trừ khi có Kim Chung Tráo hoặc Kiếm Tâm Đan.

### 5.3 Cảnh diễn

Lần đầu dài 6 giây, những lần sau 3 giây, bỏ qua được.

1. Máy quay kéo lên cao. Hàng nghìn phi kiếm nhỏ bay lên từ khắp sân trường, theo phong cách **Vạn Kiếm Quy Tông** (Vô Danh, truyện *Phong Vân*).
2. Các phi kiếm tụ thành một thanh kiếm vàng dài khoảng 150 m trên đỉnh trời.
3. Thanh kiếm lao xuyên qua cự thú: chậm hình, sóng xung kích, tiếng nổ lớn.
4. Nếu là nhát cuối: cự thú rơi và tan thành tro, bầu trời chuyển từ đỏ sang bình minh.

**Tái dùng từ Giant Hand:** mesh sinh bằng code (`GiantHandVisual`), rung máy quay (`GiantHandCameraImpulse`), và chuỗi âm thanh 7 bước cast → charge → rift → descent → impact → aftershock → dissipate (`GiantHandConfig` đã có đúng 7 ô âm thanh này).

### 5.4 Số nhát theo màn

| Màn | Số nhát | Sau mỗi nhát (trừ nhát cuối) |
|:-:|:-:|---|
| 8 | 1 | — |
| 9 | 2 | Chu Tước lên giai đoạn 2, phun nhanh hơn; nghỉ 15 giây; đợt 2 xuất hiện |
| 10 | 3 | Mỗi nhát diệt một cự thú (Giao → Chu Tước → Hỏa Long Vương); nghỉ 15 giây giữa các đợt |

### 5.5 Nâng cấp theo cảnh giới

| Cảnh giới | Thiên Kiếm |
|---|---|
| Hóa Thần | Mở khóa, niệm 2,5 giây |
| Luyện Hư | Niệm 2,0 giây; sau khi chém có "Kiếm Ý hộ thể" 10 giây (−50% lửa) |
| Độ Kiếp | Niệm 1,5 giây; cảnh diễn ngắn hơn |

---

## 6. Chiến đấu của người chơi ➕

Hiện nhân vật **chưa có đòn đánh thường**, nên phần này là bắt buộc để "diệt hết quái" được.

- **Đánh thường — Ngự Kiếm Thuật:** phi kiếm tự tìm mục tiêu trong nón 60°, tầm 12 m.
  - Chuỗi 3 nhát: 100% / 100% / 160% Công.
  - Giữ nút 0,8 giây thành **Kiếm Xuyên**: 250% Công, xuyên theo đường thẳng.
  - Thuộc hệ **Mộc**, cảm hứng từ Thanh Trúc Phong Vân Kiếm (bộ phi kiếm hệ Mộc của Hàn Lập, *Phàm Nhân Tu Tiên*).
- **Né:** lướt 5 m, vô địch 0,3 giây, tốn 25 Thể Lực, hồi 0,6 giây. Dùng action `Dash` đã khai báo sẵn trong `CampusAction` nhưng chưa dùng.
- **Ba thanh tài nguyên:**
  - Máu.
  - **Linh Lực** (mới, dùng cho kỹ năng): gốc 100, hồi 4/giây, mỗi đòn đánh thường trúng +5.
  - Thể Lực: chính là thanh Energy hiện có, dùng cho chạy nhanh và né.
- **Khóa mục tiêu:** phím Tab hoặc nút trên mobile; tự khóa quái gần tâm màn hình nhất.
- **Cảm giác đánh:** số sát thương bay lên (màu theo hệ), khựng hình 40 ms khi trúng đòn mạnh, rung máy quay, thanh máu trên đầu quái, viền đỏ khi quái sắp ra đòn.

**Phím PC**

| Phím | Chức năng |
|---|---|
| WASD · Shift · Space | Di chuyển · chạy nhanh · nhảy |
| Chuột trái (giữ) | Đánh thường (Kiếm Xuyên) |
| Ctrl | Né |
| **Q · E · R · F** | **Kỹ năng ô 1–4** |
| V | Thiên Kiếm |
| 1 · 2 · 3 | Vật phẩm |
| G | Tương tác (đổi từ E) |
| Tab | Khóa mục tiêu |

Hiện Q/F/G đang gán cứng cho Wall/Hand/Phantom và R là Respawn. Tất cả sẽ gom về 4 ô chung.

**Mobile:**
- Nút đánh thường lớn ở góc phải; 4 nút kỹ năng xếp vòng cung quanh nút đánh; nút né ở cạnh.
- Nút Thiên Kiếm lớn hiện ở phía trên khi sẵn sàng.
- 3 ô vật phẩm cạnh thanh máu.
- Giữ nguyên cơ chế giữ–kéo–thả để ngắm như hiện tại.

---

## 7. Hệ thống kỹ năng — 4 ô, combo, tham khảo anime và tu tiên ⭐

### 7.1 Luật 4 ô ⭐

- Chọn 4 kỹ năng ở màn **Chuẩn Bị** trước mỗi màn, **không đổi giữa trận**. Ngoại lệ ➕: màn 9–10 được đổi trong 15 giây nghỉ giữa đợt.
- Màn Chuẩn Bị hiện **tình báo màn**: loại quái, hệ của chúng, boss hoặc cự thú, và **bộ 4 gợi ý** (xem [7.6](#76-bộ-4-kỹ-năng-gợi-ý-theo-màn-)).
- Lưu được **3 bộ kỹ năng** để đổi nhanh.
- Đánh thường, né và Thiên Kiếm **không** tính vào 4 ô.

### 7.2 Ngũ hành và khắc chế ➕

```
Tương sinh:  Mộc → Hỏa → Thổ → Kim → Thủy → Mộc
Tương khắc:  Kim ▶ Mộc ▶ Thổ ▶ Thủy ▶ Hỏa ▶ Kim
Đặc biệt:    Lôi ▶ Âm (+50%) · Âm kháng Kim (−20%, lưỡi kiếm khó chém hồn ma)
             Không Gian và Vô Hệ: trung tính
```

- Đánh vào hệ mình khắc: **+50%** sát thương. Đánh vào hệ khắc mình: **−25%**.
- Mỗi quái có hệ riêng (bảng [3.2](#32-danh-sách-quái)). Ví dụ: đánh thường hệ Mộc rất tốt với Tiểu Yêu (Thổ) nhưng yếu với Thiết Giáp (Kim), buộc phải mang kỹ năng hệ Hỏa. Đây chính là cách "phối hợp theo từng màn".
- Màu hiệu ứng theo hệ:

| Hệ | Màu |
|---|---|
| Kim | Vàng kim |
| Mộc | Xanh lá |
| Thủy | Xanh băng |
| Hỏa | Đỏ cam |
| Thổ | Nâu vàng |
| Lôi | Tím và vàng (đúng tông của `SpeedForceVFX`) |
| Không Gian | Tím đen (đúng tông của Void Wall) |
| Âm | Xám tím |

### 7.3 Danh sách kỹ năng (21 kỹ năng + Thiên Kiếm)

Sát thương tính theo **% Công** ở tầng 1. Kỹ năng số 1–3 đã có trong code và chỉ cần chuyển sang khung mới.

| # | Kỹ năng | Hệ | Vai trò | Cảm hứng | Cơ chế | Số liệu | Hồi chiêu | Linh Lực | Mở ở |
|:-:|---|:-:|---|---|---|---|:-:|:-:|---|
| 1 | **Đại Thủ Ấn** | Thổ | Bộc phá, khống chế | Như Lai Thần Chưởng | Bàn tay khổng lồ giáng xuống vùng chọn (bán kính 2,8 m, tầm 16 m) | 300%, choáng 3 giây (boss 1 giây) | 18 giây | 30 | Có sẵn (Giant Hand) |
| 2 | **Hư Không Kết Giới** | Không Gian | Phòng thủ | Cấm chế (*Tiên Nghịch*), kết giới tu tiên | Dựng tường năng lượng chặn quái và đạn; phía sau tường tính là "bán che" với Thiên Hỏa | Tường có máu bằng 40% máu người chơi; 3 lượt nạp, 12 giây/lượt | — | 15 | Có sẵn (Void Wall) |
| 3 | **Ảnh Phân Thân** | Âm | Đánh lạc hướng | Ảnh Phân Thân Thuật (*Naruto*) | Phân thân chạy đi khiêu khích quái 6 giây; từ tầng 3 nổ khi tan | Nổ 150% | 18 giây | 20 | Có sẵn (Phantom Decoy) |
| 4 | **Tích Lịch Nhất Thiểm** | Lôi | Lướt gây sát thương | Lôi Hô Hấp, Nhất Thức (*Kimetsu no Yaiba*, Zenitsu) | Lướt 10 m xuyên qua quái, vô địch khi lướt, gây tê | 180% | 8 giây | 20 | Luyện Khí 3 · dùng lại `SpeedForceVFX` |
| 5 | **Phật Nộ Hỏa Liên** | Hỏa | Bộc phá vùng | Tiêu Viêm (*Đấu Phá Thương Khung*) | Tụ 1,2 giây, ném hoa sen lửa nổ bán kính 7 m, để lại lửa 4 giây | 450% + Bỏng | 20 giây | 45 | Trúc Cơ |
| 6 | **Hàn Băng Phong Ấn** | Thủy | Khống chế | Băng hệ, Todoroki (*My Hero Academia*) | Nón băng 90°, dài 10 m, đóng băng 2,5 giây (boss chỉ bị chậm 50%) | 120% | 14 giây | 30 | Trúc Cơ |
| 7 | **Thần Kiếm Ngự Lôi Chân Quyết** | Lôi | Sát thương lan | Lục Tuyết Kỳ (*Tru Tiên*) | Sét dây chuyền nảy qua 6 mục tiêu; tự tìm cả quái bay | 200% mỗi mục tiêu, giảm 10% sau mỗi lần nảy | 12 giây | 35 | Trúc Cơ |
| 8 | **Kim Chung Tráo** | Kim | Phòng thủ | Thiếu Lâm (kiếm hiệp) | Chuông vàng hấp thụ sát thương bằng 40% máu tối đa trong 6 giây; phản 20% sát thương cận chiến; −60% Thiên Hỏa | — | 30 giây | 40 | Kết Đan |
| 9 | **Hàng Long Thập Bát Chưởng** | Kim | Sóng xuyên thẳng | Kiều Phong, Quách Tĩnh (Kim Dung) | Chưởng hình rồng vàng xuyên thẳng 15 m, đẩy lùi | 350% | 10 giây | 35 | Kết Đan |
| 10 | **Tam Muội Chân Hỏa** | Hỏa | Phun lửa liên tục | Hồng Hài Nhi (*Tây Du Ký*) | Phun lửa hình nón dài 8 m trong 3 giây, vẫn đi chậm được | 90% mỗi 0,25 giây | 14 giây | 40 | Kết Đan |
| 11 | **Hắc Động Thần La** | Không Gian | Gom quái | Địa Bạo Thiên Tinh và Thần La Thiên Chinh (*Naruto*, Pain) | Hố đen hút quái trong bán kính 9 m suốt 3 giây rồi hất văng | 250% khi hất | 22 giây | 50 | Nguyên Anh |
| 12 | **Bắc Minh Thần Công** | Thủy | Hút máu | Đoàn Dự, Hư Trúc (*Thiên Long Bát Bộ*) | Vận công 3 giây hút 3 mục tiêu, hồi máu bằng 50% sát thương gây ra | 70% mỗi 0,5 giây | 16 giây | 30 | Nguyên Anh |
| 13 | **Côn Bằng Cực Tốc** | Mộc | Di chuyển | Côn Bằng Bảo Thuật (*Thế Giới Hoàn Mỹ*), Lăng Ba Vi Bộ | 6 giây: nhanh hơn 40%, né không tốn Thể Lực, để lại bóng ảo | — | 25 giây | 25 | Nguyên Anh · dùng lại `CharacterAfterimageTrail` |
| 14 | **Vạn Kiếm Quyết** | Kim | Mưa kiếm | Vô Danh (*Phong Vân*), Gate of Babylon (*Fate*) | 30 thanh kiếm rơi xuống vùng bán kính 8 m trong 3 giây | 40% mỗi kiếm | 18 giây | 50 | Hóa Thần |
| 15 | **Thiên Lôi Dẫn** | Lôi | Sét giáng trễ | Lôi kiếp, thiên kiếp (tu tiên) | Đánh dấu một vùng, 1,2 giây sau 5 tia sét giáng xuống | 220% mỗi tia, +50% với hệ Âm | 20 giây | 45 | Hóa Thần |
| 16 | **Mộc Linh Hồi Xuân** | Mộc | Hồi phục | Liễu Thần (*Thế Giới Hoàn Mỹ*) | Hồi 25% máu trong 5 giây, tạo vùng hồi máu 8 giây (hồi cả Âm Binh) | — | 35 giây | 40 | Hóa Thần |
| 17 | **Thần Thức Linh Nhãn** | Vô Hệ | Trinh sát | Thần thức (tu tiên), Byakugan (*Naruto*), Haki quan sát (*One Piece*) | Thấy mọi quái xuyên tường 10 giây, lộ điểm yếu (+25% sát thương chí mạng) | — | 30 giây | 20 | Hóa Thần |
| 18 | **Tru Tiên Kiếm Trận** | Kim | Vùng trận pháp | *Phong Thần Diễn Nghĩa*; Đại Canh Kiếm Trận (*Phàm Nhân Tu Tiên*) | Kiếm trận bán kính 8 m trong 10 giây; quái bên trong bị chém mỗi 0,5 giây; người chơi đứng trong trận +20% tỉ lệ chí mạng | 60% mỗi 0,5 giây | 28 giây | 60 | Luyện Hư |
| 19 | **Âm Binh Quy Hồn** | Âm | Triệu hồi | "Arise" (*Solo Leveling*) | Dựng dậy tối đa 3 quái vừa chết (trong 8 giây gần nhất) làm đồng minh 20 giây | Đồng minh có 60% chỉ số gốc | 40 giây | 60 | Luyện Hư |
| 20 | **Bành Trướng Lãnh Địa** | Không Gian | Tối thượng | Domain Expansion (*Jujutsu Kaisen*), Lĩnh vực (*Đấu La Đại Lục*) | Lãnh địa bán kính 12 m trong 8 giây: quái chậm 40%, kỹ năng của người chơi hồi nhanh gấp đôi | — | 60 giây | 80 | Độ Kiếp |
| 21 | **Võ Hồn Chân Thân** | Thổ | Biến thân | *Đấu La Đại Lục*; Susanoo (*Naruto*); Kim Giác Cự Thú (*Thôn Phệ Tinh Không*) | 10 giây hóa pháp tướng khổng lồ: +50% sát thương, đánh thường thành chém quét, −30% sát thương nhận | — | 75 giây | 100 | Độ Kiếp |
| ★ | **Thiên Kiếm** | Kim | Tuyệt kỹ diệt cự thú | Vạn Kiếm Quy Tông (*Phong Vân*), ngự kiếm Thục Sơn | Xem [mục 5](#5-thiên-kiếm-) | Hạ 1 giai đoạn cự thú | Theo Kiếm Ý | — | Hóa Thần (nút riêng) |

### 7.4 Tầng kỹ năng

Mỗi kỹ năng có 5 tầng: **Nhập Môn → Tiểu Thành → Đại Thành → Viên Mãn → Hóa Cảnh**.

- Mỗi tầng: +12% sát thương hoặc hiệu quả, −5% hồi chiêu.
- Chi phí: Tiểu Thành 150 · Đại Thành 400 · Viên Mãn 900 · Hóa Cảnh 1.600 Linh Thạch.
- Mỗi tầng cần cảnh giới cao hơn cảnh giới mở kỹ năng 1 bậc (tối đa Độ Kiếp).
- **Viên Mãn mở hiệu ứng đặc biệt**, ví dụ:

| Kỹ năng | Hiệu ứng Viên Mãn |
|---|---|
| Đại Thủ Ấn | Để lại **Ngũ Chỉ Sơn** chắn đường 5 giây |
| Phật Nộ Hỏa Liên | Tách thành 3 hoa sen nhỏ |
| Hắc Động Thần La | Hút cả đạn của quái |
| Tích Lịch Nhất Thiểm | Lướt được 2 lần liên tiếp |
| Kim Chung Tráo | Chuông vỡ thì nổ gây 200% Công |
| Ảnh Phân Thân | Tạo 2 phân thân |

### 7.5 Phản ứng và combo ⭐➕

Kết hợp hiệu ứng trạng thái, tương tự phản ứng nguyên tố của *Genshin Impact* nhưng theo ngũ hành. Cửa sổ combo là 3 giây.

| Kết hợp | Phản ứng | Hiệu ứng |
|---|---|---|
| Đóng Băng + đòn Lôi | **Băng Lôi Liệt** | Vỡ băng: đòn Lôi +150% sát thương, nổ lan 3 m |
| Chậm hoặc Ướt (Hàn Băng, Bắc Minh) + đòn Lôi | **Điện Lưu** | Tê liệt lan sang quái trong 5 m |
| Bỏng + đòn Hỏa | **Bạo Viêm** | Nổ thêm 80% Công, bán kính 4 m |
| Bỏng + bóng ảo Côn Bằng đi qua | **Phong Hỏa Liệu Nguyên** | Lửa lan sang quái lân cận |
| Hắc Động đang hút + bất kỳ kỹ năng vùng nào | **Tụ Sát** | Kỹ năng vùng +40% sát thương lên nhóm bị gom |
| Choáng (Đại Thủ Ấn) + đòn Kim | **Phá Giáp** | Quái mất 30% phòng thủ trong 8 giây; phá được phụ tố Kim Thân |
| Âm Binh đứng trong Tru Tiên Kiếm Trận | **Kiếm Hồn** | Âm Binh +50% sát thương |
| Kim Chung Tráo + Bắc Minh Thần Công | **Hộ Thể Hấp Nguyên** | Lượng máu hút về ×2 |
| Bất kỳ kỹ năng nào trong Bành Trướng Lãnh Địa | **Lãnh Địa Cộng Hưởng** | +20% sát thương |

**Tương Sinh Liên Hoàn:** dùng 3 kỹ năng có hệ nối tiếp theo vòng tương sinh (ví dụ Mộc → Hỏa → Thổ) trong vòng 6 giây. Kỹ năng thứ 3 được +30% sát thương và người chơi hồi 20 Linh Lực. HUD có vòng nhỏ báo "Tương Sinh ●●○".

### 7.6 Bộ 4 kỹ năng gợi ý theo màn ⭐

Đây là gợi ý hiện trong màn Chuẩn Bị. Người chơi tự do chọn bộ khác.

| Màn | Bộ gợi ý | Vì sao |
|:-:|---|---|
| 1 | Đại Thủ Ấn · Kết Giới · Phân Thân · (trống) | Bộ khởi đầu |
| 2 | Tích Lịch Nhất Thiểm · Đại Thủ Ấn · Kết Giới · Phân Thân | Áp sát quái bắn độc, chắn đạn |
| 3 | Phật Nộ Hỏa Liên · Hàn Băng · Thần Kiếm Ngự Lôi · Tích Lịch | Hỏa hạ Thiết Giáp; Lôi hạ Shaban (Âm); Băng + Lôi ra Băng Lôi Liệt |
| 4 | Hàn Băng · Thần Kiếm Ngự Lôi · Kết Giới · Phật Nộ Hỏa Liên | Thủy khắc Bạo Thi (Hỏa); Kết Giới chặn quái lao vào tự nổ |
| 5 | Thần Kiếm Ngự Lôi · Kim Chung Tráo · Hàng Long · Hàn Băng | Lôi khắc boss; chuông vàng đỡ Gầm Hồn |
| 6 | Thần Kiếm Ngự Lôi · Tam Muội Chân Hỏa · Kim Chung Tráo · Hàng Long | Đông quái hệ Âm; sét tự tìm cả Dực Yêu đang bay |
| 7 | Hắc Động · Tam Muội Chân Hỏa · Thần Kiếm Ngự Lôi · Bắc Minh | Gom quái rồi phun lửa (Tụ Sát); hút máu để trụ |
| 8 | Hàn Băng · Kim Chung Tráo · Vạn Kiếm Quyết · Hắc Động | Thủy khắc Hỏa Linh; chuông vàng giảm Thiên Hỏa. Nên mang Tị Hỏa Châu |
| 9 | Tru Tiên Kiếm Trận · Hắc Động · Hàn Băng · Kim Chung Tráo | Gom quái vào kiếm trận; dọn nhanh để canh giờ Thiên Kiếm |
| 10 | Bành Trướng Lãnh Địa · Tru Tiên Kiếm Trận · Hắc Động · Kim Chung Tráo (hoặc Võ Hồn Chân Thân) | Lãnh địa cộng hưởng mọi kỹ năng; trụ được qua Long Nộ |

### 7.7 Thứ tự làm kỹ năng

- **Đợt 1 (MVP, 10 kỹ năng):** 1–3 (chuyển sang khung mới), 4 Tích Lịch, 5 Hỏa Liên, 6 Hàn Băng, 7 Ngự Lôi, 8 Kim Chung, 11 Hắc Động, 14 Vạn Kiếm, và Thiên Kiếm.
- **Đợt 2:** 9, 10, 12, 13, 15, 16, 17.
- **Đợt 3:** 18–21.

---

## 8. Tu luyện — học để lên cấp ⭐

### 8.1 Cảnh giới

Người chơi có **7 cảnh giới**, mỗi cảnh giới **5 tầng**. Muốn sang cảnh giới mới cần 2 điều kiện: tầng 5 đầy Tu Vi (gọi là "bình cảnh") **và** đậu **Thi Đột Phá** của chương tương ứng.

| Cảnh giới | Tu Vi mỗi tầng | Muốn lên cảnh giới sau cần | Mở màn |
|---|:-:|---|---|
| **Luyện Khí** | 100 | Đậu Thi Đột Phá **Chương 1** | 1 (tầng 1), 2 (tầng 3) |
| **Trúc Cơ** | 150 | Đậu **Chương 2** | 3 (tầng 1), 4 (tầng 3) |
| **Kết Đan** | 225 | Đậu **Chương 3** | 5 (tầng 1), 6 (tầng 3) |
| **Nguyên Anh** | 340 | Đậu **Chương 4** | 7 (tầng 1) |
| **Hóa Thần** | 510 | Đậu **Chương 5** | 8 (tầng 1) + mở Thiên Kiếm |
| **Luyện Hư** | 760 | Đậu **Chương 6** | 9 (tầng 1) |
| **Độ Kiếp** | 1.140 | Cảnh giới cuối; tầng 2–5 lên bằng ôn tập, luyện tập và chơi màn | 10 (tầng 1) |

Tên cảnh giới là thuật ngữ tu tiên chung (theo kiểu *Phàm Nhân Tu Tiên*), **không** gắn với nội dung môn học.

→ **Muốn chơi màn 10 thì phải học hết 6 chương.** "Học để thắng" theo đúng nghĩa đen.

### 8.2 Tu Vi đến từ đâu

**Quy tắc chia Tu Vi:** mỗi bài của chương *k* có một "phần Tu Vi" bằng:

> (Tu Vi của cả cảnh giới *k* × 85%) ÷ số bài trong chương

Học hết chương lần đầu với điểm tối đa được khoảng 85% cảnh giới. 15% còn lại đến từ ôn tập và luyện tập, nên người học phải **ôn** mới chạm bình cảnh. Hệ thống tự chia theo số bài thật khi bạn gửi nội dung.

*Ví dụ:* chương 1 có 4 bài. Luyện Khí có 5 tầng × 100 = 500 Tu Vi. Mỗi bài chiếm 500 × 85% ÷ 4 ≈ 106 Tu Vi.

| Nguồn | Tu Vi nhận được |
|---|---|
| Đọc hết một bài lần đầu | 15% phần Tu Vi của bài |
| Làm quiz bài lần đầu | 70% phần của bài × tỉ lệ trả lời đúng |
| Ôn tập đúng hạn (sau 1, 3, 7 ngày) | 10% phần của bài mỗi lần; được huy hiệu Đồng / Bạc / Vàng |
| Luyện tập lặp lại (chỉ câu chưa làm đúng trong 24 giờ qua) | 2 Tu Vi mỗi câu đúng, tối đa 150/ngày |
| Hoàn thành màn lần đầu | 5% Tu Vi của một tầng ở cảnh giới hiện tại (chơi chỉ phụ, học là chính) |

### 8.3 Thi Đột Phá ➕

Khung thi mang hình ảnh "Độ Kiếp" (vượt thiên kiếp), nhưng phần câu hỏi trình bày trang trọng.

- Mở khi tầng 5 đầy Tu Vi.
- 20 câu lấy ngẫu nhiên từ ngân hàng câu của chương (ngân hàng cần ít nhất 40 câu), làm trong 20 phút, **đạt từ 80%**.
- **Trượt:** được xem lại câu sai kèm giải thích; thi lại sau 30 phút (ôn trong lúc chờ); không mất gì.
- **Đậu:** cảnh diễn đột phá (sét vàng, hào quang), chỉ số tăng vọt, mở kỹ năng mới và màn mới, thưởng 300 Linh Thạch.
- **Giao diện câu hỏi trang trọng, rõ ràng**, không có hiệu ứng che chữ. Hiệu ứng tu tiên chỉ nằm ở khung ngoài và cảnh diễn *sau khi* đậu.

### 8.4 Chỉ số theo cảnh giới

Mỗi ô ghi giá trị ở tầng 1 → tầng 5. Phòng thủ là % giảm sát thương nhận vào.

| Cảnh giới | Máu | Công | Linh Lực | Phòng thủ |
|---|:-:|:-:|:-:|:-:|
| Luyện Khí | 100 → 140 | 20 → 28 | 100 → 120 | 0% |
| Trúc Cơ | 170 → 220 | 34 → 44 | 130 → 150 | 3% |
| Kết Đan | 260 → 320 | 52 → 64 | 160 → 180 | 6% |
| Nguyên Anh | 370 → 440 | 74 → 88 | 190 → 210 | 9% |
| Hóa Thần | 500 → 580 | 100 → 116 | 220 → 240 | 12% |
| Luyện Hư | 650 → 740 | 130 → 148 | 250 → 270 | 15% |
| Độ Kiếp | 820 → 920 | 164 → 184 | 280 → 300 | 18% |

Mốc khởi đầu 100 máu khớp với prefab hiện tại. Ngoài chỉ số, mỗi cảnh giới còn nâng tốc chạy +1,5% (tối đa +10%).

### 8.5 Bảng mở khóa tổng hợp

| Cảnh giới | Màn | Kỹ năng mới | Vật phẩm mở bán |
|---|---|---|---|
| Luyện Khí | 1–2 | Đại Thủ Ấn, Hư Không Kết Giới, Ảnh Phân Thân; Tích Lịch Nhất Thiểm (tầng 3) | Hồi Khí Đan, Tụ Linh Đan, Thần Hành Phù |
| Trúc Cơ | 3–4 | Phật Nộ Hỏa Liên, Hàn Băng Phong Ấn, Thần Kiếm Ngự Lôi | Hồi Xuân Đan, Cuồng Lực Đan, Kim Cương Phù, Tầm Yêu Phù |
| Kết Đan | 5–6 | Kim Chung Tráo, Hàng Long Thập Bát Chưởng, Tam Muội Chân Hỏa | Thanh Tâm Đan, Tụ Khí Đan, Bạo Kích Đan, Hộ Mệnh Phù |
| Nguyên Anh | 7 | Hắc Động Thần La, Bắc Minh Thần Công, Côn Bằng Cực Tốc | Cửu Chuyển Hoàn Hồn Đan, Ngũ Hành Phù |
| Hóa Thần | 8 | Vạn Kiếm Quyết, Thiên Lôi Dẫn, Mộc Linh Hồi Xuân, Thần Thức Linh Nhãn, **Thiên Kiếm** | Tị Hỏa Châu, Băng Tâm Phù, Kiếm Tâm Đan |
| Luyện Hư | 9 | Tru Tiên Kiếm Trận, Âm Binh Quy Hồn | — |
| Độ Kiếp | 10 | Bành Trướng Lãnh Địa, Võ Hồn Chân Thân | — |

---

## 9. Linh Thạch và cửa hàng — học để mua vật phẩm ⭐

### 9.1 Kiếm Linh Thạch

| Nguồn | Linh Thạch |
|---|---|
| Câu đúng lần đầu (mọi chế độ học) | +5 |
| Câu đúng khi luyện lại (chưa làm đúng câu đó trong 24 giờ) | +2 |
| Hoàn thành bài (đạt từ 80%) / đạt 100% | +30 / +50 |
| Ôn tập đúng hạn | +40 mỗi bài |
| Đậu Thi Đột Phá | +300 |
| ➕ Nhiệm vụ học hằng ngày (3 nhiệm vụ: học 1 bài · đúng 20 câu · ôn 1 bài đến hạn) | +30 mỗi nhiệm vụ |
| ➕ Học liên tục 7 ngày | +150 (có 1 ngày "nghỉ phép" mỗi tuần, không mất chuỗi) |
| Hoàn thành màn lần đầu | +40 × số thứ tự màn |
| Mỗi sao ★ đạt lần đầu | +20 |

### 9.2 Chống "cày" vô nghĩa

- Câu đã trả lời đúng trong 24 giờ qua: luyện lại **không** được Linh Thạch.
- Luyện tập lặp lại tối đa **300 Linh Thạch/ngày**. Ôn tập đúng hạn và nhiệm vụ ngày không tính vào trần này.
- Trả lời liên tiếp 5 câu, mỗi câu dưới 1,5 giây: hiện nhắc "Đọc kỹ câu hỏi" và tạm dừng thưởng 2 phút.
- Không có quảng cáo đổi thưởng.

### 9.3 Vật phẩm tiêu hao (đan dược, phù chú)

Cột "Tối đa/màn" là số lượng mang được vào một màn.

**Hồi phục**

| Vật phẩm | Hiệu ứng | Giá | Tối đa/màn | Bán từ |
|---|---|:-:|:-:|---|
| Hồi Khí Đan | Hồi ngay 20% máu | 25 | 5 | Luyện Khí |
| Hồi Xuân Đan | Hồi 45% máu trong 3 giây | 60 | 3 | Trúc Cơ |
| Tụ Linh Đan | Hồi 50% Linh Lực | 40 | 3 | Luyện Khí |
| Thanh Tâm Đan | Hồi đầy Thể Lực, miễn khống chế 5 giây | 50 | 2 | Kết Đan |
| Cửu Chuyển Hoàn Hồn Đan | Hồi 100% máu, xóa mọi hiệu ứng xấu | 250 | 1 | Nguyên Anh |
| Hộ Mệnh Phù | Tự hồi sinh 1 lần với 40% máu | 200 | 1 | Kết Đan |

**Tăng sức mạnh**

| Vật phẩm | Hiệu ứng | Giá | Tối đa/màn | Bán từ |
|---|---|:-:|:-:|---|
| Cuồng Lực Đan | +30% sát thương trong 60 giây | 80 | 2 | Trúc Cơ |
| Kim Cương Phù | −35% sát thương nhận vào trong 45 giây | 80 | 2 | Trúc Cơ |
| Thần Hành Phù | +30% tốc chạy trong 40 giây | 50 | 2 | Luyện Khí |
| Tụ Khí Đan | −30% hồi chiêu trong 45 giây | 90 | 2 | Kết Đan |
| Bạo Kích Đan | +25% tỉ lệ chí mạng trong 45 giây | 70 | 2 | Kết Đan |
| Ngũ Hành Phù | Chọn 1 hệ khi mua: hệ đó +40% sát thương trong 60 giây | 100 | 1 | Nguyên Anh |
| Tầm Yêu Phù | Hiện vị trí mọi quái trong 30 giây | 60 | 2 | Trúc Cơ |

**Dành cho màn 8–10**

| Vật phẩm | Hiệu ứng | Giá | Tối đa/màn | Bán từ |
|---|---|:-:|:-:|---|
| Tị Hỏa Châu | −50% sát thương Thiên Hỏa trong 90 giây | 120 | 2 | Hóa Thần |
| Băng Tâm Phù | Miễn nhiễm Dư Hỏa, −25% Thiên Hỏa trong 120 giây | 90 | 2 | Hóa Thần |
| Kiếm Tâm Đan | Niệm Thiên Kiếm nhanh hơn 40% và không bị ngắt 1 lần | 150 | 1 | Hóa Thần |

**Mang vào màn:** 3 ô vật phẩm, mỗi ô một loại. Pháp bảo Túi Càn Khôn nâng lên 4 rồi 5 ô.

### 9.4 Pháp bảo (nâng cấp vĩnh viễn)

| Pháp bảo | Mỗi cấp | Cấp tối đa | Giá từng cấp |
|---|---|:-:|---|
| Phi Kiếm Thanh Trúc | +6% sát thương đánh thường | 5 | 300 / 600 / 1.000 / 1.600 / 2.500 |
| Hộ Tâm Kính | +5% máu tối đa | 5 | 300 / 600 / 1.000 / 1.600 / 2.500 |
| Linh Lực Hồ Lô | +8 Linh Lực, hồi +0,4/giây | 5 | 250 / 500 / 850 / 1.300 / 2.000 |
| Ngọc Bội Ngũ Hành | +3% sát thương khi đánh hệ mình khắc | 5 | 400 / 800 / 1.300 / 2.000 / 3.000 |
| Túi Càn Khôn | +1 ô vật phẩm | 2 | 800 / 2.000 |

Cấp pháp bảo không vượt quá số cảnh giới đã đạt, để không ai "cày tiền" vượt được việc học.

### 9.5 Cân bằng kinh tế

- Một buổi học 20–25 phút kiếm khoảng **250–350 Linh Thạch**.
- Một lượt thử màn khó (8–10) thường tốn **150–300 Linh Thạch** vật phẩm.
- Tức là **1 buổi học ≈ 1 lượt thử màn khó**. Màn dễ không cần vật phẩm.
- Nâng hết pháp bảo tốn khoảng 25.000 Linh Thạch, tương đương 80–100 buổi học. Đây là mục tiêu dài hạn để giữ chân người chơi.

---

## 10. Cam kết "Study to Win" ⭐

1. **Không** có nạp tiền nào đổi được Tu Vi, Linh Thạch, vật phẩm, kỹ năng hay pháp bảo.
2. **Không** bán đáp án hay gợi ý câu hỏi, dưới mọi hình thức.
3. **Không** có quảng cáo đổi thưởng và không có hộp quà ngẫu nhiên.
4. Sức mạnh đến từ **học** (Tu Vi, Linh Thạch, cảnh giới) và **kỹ năng chơi** (combo, né, chọn bộ kỹ năng).
5. Mỗi con số thưởng được công khai trong màn hình "Hướng dẫn".
6. Nếu sau này có bán gì, chỉ bán **trang phục thuần thẩm mỹ**, và phải có cách mở chúng bằng thành tích.

---

## 11. Nội dung học: Tư tưởng Hồ Chí Minh ⭐

### 11.1 Khung theo giáo trình

Khung dưới đây theo *Giáo trình Tư tưởng Hồ Chí Minh* (Bộ GD&ĐT, 2021, bậc đại học hệ không chuyên lý luận chính trị), gồm 6 chương. **Đây chỉ là khung tạm.** Khi bạn gửi nội dung, số chương và số bài sẽ theo nội dung của bạn, và Tu Vi tự chia lại.

| Chương | Tên chương | Đậu Thi Đột Phá thì lên | Mở màn |
|:-:|---|---|---|
| 1 | Khái niệm, đối tượng, phương pháp nghiên cứu và ý nghĩa học tập môn Tư tưởng Hồ Chí Minh | Trúc Cơ | 3–4 |
| 2 | Cơ sở, quá trình hình thành và phát triển tư tưởng Hồ Chí Minh | Kết Đan | 5–6 |
| 3 | Tư tưởng Hồ Chí Minh về độc lập dân tộc và chủ nghĩa xã hội | Nguyên Anh | 7 |
| 4 | Tư tưởng Hồ Chí Minh về Đảng Cộng sản Việt Nam và Nhà nước của nhân dân, do nhân dân, vì nhân dân | Hóa Thần | 8 |
| 5 | Tư tưởng Hồ Chí Minh về đại đoàn kết toàn dân tộc và đoàn kết quốc tế | Luyện Hư | 9 |
| 6 | Tư tưởng Hồ Chí Minh về văn hóa, đạo đức, con người | Độ Kiếp | 10 |

Màn 1–2 mở ngay khi bắt đầu học Chương 1.

### 11.2 Chế độ học

| Chế độ | Mô tả | Thưởng chính |
|---|---|---|
| **Đọc bài** | Trang ngắn, cuối trang có ô "Ghi nhớ". Giữ `LearningUI` hiện có (cuộn, song ngữ giao diện) | Tu Vi |
| **Quiz bài** | 10 câu, lấy ngẫu nhiên từ ngân hàng câu của bài | Tu Vi + Linh Thạch |
| **Luyện tập nhanh** ➕ | 10 câu trong 5 phút, trộn từ các bài đã đọc | Linh Thạch |
| **Ôn tập theo lịch** ➕ | Bài đến hạn sau 1 / 3 / 7 ngày; huy hiệu Đồng / Bạc / Vàng | Tu Vi + Linh Thạch |
| **Thẻ ghi nhớ** ➕ | Lật thẻ: khái niệm ↔ nội dung | Nhiệm vụ ngày |
| **Sổ tay câu sai** ➕ | Tự gom các câu trả lời sai để ôn lại | Linh Thạch |
| **Thi Đột Phá** | Cuối mỗi chương (xem [8.3](#83-thi-đột-phá-)) | Cảnh giới + 300 Linh Thạch |
| **Linh Bia Cơ Duyên** ➕ | Trong màn: trả lời 1 câu thì chọn 1 trong 3 buff tạm thời (ví dụ +20% sát thương, hồi 30% máu, −20% hồi chiêu). Sai thì bia tắt, không phạt | Buff trong màn |

### 11.3 Dạng câu hỏi

Mọi dạng câu xây trên `IQuestionGrader` và type key có sẵn trong `QuestionBankData`.

| Dạng | Cách làm | Trạng thái |
|---|---|---|
| Một đáp án | Chọn 1 trong 4 | **Có sẵn** |
| Đúng / Sai | Nhận định đúng hay sai | Mới, dễ làm |
| Nhiều đáp án | Chọn tất cả ý đúng | Mới |
| Sắp xếp thứ tự | Xếp các sự kiện hoặc mốc theo trước–sau | Mới, **rất hợp môn này** |
| Nối cặp | Nối khái niệm với nội dung | Mới |
| Điền khuyết | Chọn cụm từ đúng điền vào chỗ trống | Mới |

### 11.4 Bạn cần cung cấp *(chờ nội dung)*

- Danh sách chương và bài (tiêu đề).
- **Nội dung đọc** của từng bài, chia theo trang, mỗi trang khoảng 250 chữ trở xuống, kèm ý "Ghi nhớ".
- **Câu hỏi:** mỗi bài ít nhất 15 câu (quiz lấy 10); mỗi chương có ngân hàng ít nhất 40 câu cho Thi Đột Phá (dùng chung câu của các bài cũng được).
- Mỗi câu gồm: đáp án đúng, **giải thích**, **trang giáo trình làm nguồn**, độ khó 1–3.
- Định dạng nào cũng được (Excel, CSV, Word). Tôi sẽ chuyển thành asset trong game. Mẫu ở [Phụ lục A](#phụ-lục-a--mẫu-nhập-nội-dung).

### 11.5 Nguyên tắc trình bày và tuân thủ

Phần này quan trọng vì đây là môn lý luận chính trị.

1. **Nội dung học bám sát giáo trình chính thức.** Mỗi câu ghi rõ trang nguồn. Nên nhờ **giảng viên bộ môn duyệt** trước khi phát hành.
2. **Tách bạch hai lớp.** Thế giới tu tiên là **hư cấu**: quái, kỹ năng, đan dược, cảnh giới, cự thú. Nội dung học là **thật** và được trình bày **trang trọng** trong Thư Viện: không pha trò, không meme, không hiệu ứng che chữ.
3. **Không** đưa hình ảnh Chủ tịch Hồ Chí Minh vào phần chiến đấu. **Không** đặt tên kỹ năng, vật phẩm, quái hay cảnh giới theo tên Người, theo trích dẫn hay khái niệm của môn học.
4. Game thưởng cho **nỗ lực học tập**: Tu Vi và Linh Thạch là phần thưởng của sự chăm chỉ. Game không gán sức mạnh siêu nhiên cho tư tưởng hay nhân vật lịch sử.
5. **Căn cứ pháp lý:**
   - Luật An ninh mạng 2018, Điều 16, cấm thông tin xúc phạm lãnh tụ, danh nhân, anh hùng dân tộc.
   - Nghị định 147/2024/NĐ-CP quản lý nội dung trò chơi điện tử trên mạng.

   Các nguyên tắc trên giúp tránh rủi ro. Nên hỏi ý kiến giảng viên hoặc nhà trường trước khi công bố rộng rãi.
6. Môn học giảng bằng tiếng Việt, nên nội dung học **chỉ cần bản tiếng Việt**. Giao diện game vẫn giữ song ngữ EN/VN như hiện tại.

### 11.6 Thay nội dung hiện tại

- Gỡ course Algorithms (5 bài, 30 câu) khỏi `LearningCatalog`. Các asset cũ chuyển vào thư mục Backups.
- Save học tập đổi sang v2: ID bài thay đổi, save cũ được sao lưu, không xóa.
- Bỏ điều kiện mở bài bằng thời gian sống sót (15/30/45/60 giây).

---

## 12. Cốt truyện và thế giới ➕

**Bối cảnh.** Một buổi chiều cuối kỳ, **Khe Nứt Hư Không** mở trên bầu trời học viện, và yêu thú từ cõi Cửu U tràn xuống sân trường. Linh khí trời đất đổ về khuôn viên, nhưng chỉ những sinh viên **kiên trì học tập** mới hấp thụ được: tu vi của họ tăng theo từng bài học.

**Nhân vật** (tất cả đều hư cấu):
- **Nhân vật chính:** nữ sinh (model SchoolGirl hiện có). Sau này có thể thêm nam sinh.
- **Thư Linh:** linh thể trú trong thư viện trường. Là người dẫn đường, mở Thư Viện, và bán hàng ở Đan Các.
- **Shaban, Săn Hồn Giả:** sứ giả đầu tiên của Khe Nứt.
- **Cửu U Hỏa Long Vương:** chủ nhân của Khe Nứt.

**Mạch 10 màn**

| Màn | Diễn biến |
|:-:|---|
| 1–2 | Khe nứt nhỏ xuất hiện; nhân vật chính thức tỉnh linh căn |
| 3 | Shaban lần đầu săn đuổi |
| 4 | Campus mất điện; yêu thú mạnh dần |
| 5 | Hạ Shaban lần đầu; Shaban trốn vào Khe Nứt |
| 6 | Huyết Nguyệt: Khe Nứt lan rộng |
| 7 | Shaban thức tỉnh; lộ mặt Hỏa Long Vương |
| 8–9 | Cự thú giáng thế, bầu trời bốc lửa |
| 10 | Ba cự thú cùng xuất hiện; Thiên Kiếm cuối cùng; Khe Nứt khép lại; bình minh |

**Sau khi phá đảo ➕:**
- **Tháp Thí Luyện:** chuỗi tầng vô tận, độ khó tăng dần, bảng kỷ lục.
- **Chế độ Ác Mộng:** màn 1–10 với quái +50% chỉ số và 2 phụ tố.
- **Thành tựu và danh hiệu:** ví dụ "Kiếm Tiên" (★★★ cả 10 màn), "Bác Học" (Vàng mọi bài).

---

## 13. Giao diện và HUD

### 13.1 Luồng màn hình

```
Menu chính
   └─► SẢNH TU LUYỆN
         ├─ [Thư Viện]     học: đọc, quiz, ôn tập, thẻ ghi nhớ, Thi Đột Phá
         ├─ [Đan Các]      cửa hàng: đan dược, phù chú, pháp bảo
         ├─ [Công Pháp]    kỹ năng: xem, nâng tầng, lưu 3 bộ
         ├─ [Cảnh Giới]    chỉ số, Tu Vi, tiến độ đột phá
         └─ [Bản Đồ 10 Màn] ─► [Chuẩn Bị: 4 kỹ năng + vật phẩm + tình báo]
                                   ─► MÀN CHƠI ─► [Kết Quả: sao, thưởng, gợi ý bài cần ôn]
Pause trong màn: Tiếp tục · Cài đặt · Rời màn  (không còn nút Courses)
```

### 13.2 HUD trong màn

```
┌───────────────────────────────────────────────────────────────────┐
│ Máu ████████░░  Linh Lực ██████░░  Thể Lực ██████████   Kết Đan 3 │
│                        Quái còn lại: 12/26 · Đợt 2/4              │
│        ╔═ XÍCH HỎA GIAO  ▓▓▓▓▓▓▓▓▓▓▓▓ (1 nhát) ═══╗  (màn 8–10)    │
│        ╚═ KIẾM Ý ████████████░░░ 80% ════════════╝                │
│             ⚠ THIÊN HỎA 5…    🔥 NGOÀI TRỜI → vào nhà ↗           │
│                                                                   │
│ [1 Hồi Xuân ×2] [2 Tị Hỏa ×1] [3 Cuồng Lực ×1]                     │
│                        Tương Sinh ●●○          [Q][E][R][F]  [V ⚔] │
└───────────────────────────────────────────────────────────────────┘
```

- Biểu tượng trú ẩn luôn hiện ở màn 8–10: 🏠 an toàn / ⚠ bán che / 🔥 ngoài trời.
- Mũi tên chỉ lối vào nhà gần nhất chỉ hiện trong lúc báo động Thiên Hỏa; vị trí lấy từ nút Door của `RoomGraph`.
- Ô kỹ năng dùng lại `SkillBarUI` và `SkillSlotUI` (đã có biểu tượng, lớp phủ hồi chiêu, khóa, nhãn cấp).

---

## 14. Triển khai kỹ thuật trên code hiện tại

### 14.1 Tái sử dụng

| Code có sẵn | Dùng cho |
|---|---|
| `CampusExplorer` | Di chuyển, chạy nhanh, Thể Lực (Energy) |
| `PlayerMonsterHealth` | Máu người chơi, `TryTakeDamage` có hướng đòn |
| `MonsterVitality` | Máu quái, choáng, nháy sáng khi trúng; dùng chung cho mọi loại quái |
| `MonsterBrain`, `MonsterPerception`, `MonsterNavigation`, `MonsterHearing`, `InterceptionPlanner`, `MonsterElevatorAwareness` | Shaban và AI tinh anh cấp T2–T4 |
| `CampusNavMesh`, `CampusRoomGraph` (2.126 nút, có nút Door và Outdoor, 12 tòa nhà) | Điểm Khe Nứt, lối vào nhà gần nhất, phục kích ở cửa |
| `GiantHandSkill`, `GiantHandVisual`, `GiantHandCameraImpulse`, `GiantHandConfig` | Đại Thủ Ấn và **nền tảng cho cảnh diễn Thiên Kiếm** |
| `VoidWallSkill`, `PhantomDecoySkill` | Hư Không Kết Giới, Ảnh Phân Thân |
| `SpeedForceVFX`, `CharacterAfterimageTrail` | Hiệu ứng Tích Lịch Nhất Thiểm, Côn Bằng Cực Tốc |
| `PlayerFlashlight` | Màn 4 mất điện |
| `SkyLightingController` | Trăng máu (màn 6–7), trời lửa (màn 8–10), bình minh khi thắng |
| `LearningEngine`, `QuizSession`, `IQuestionGrader`, `ILearningStore` / `JsonLearningStore` | Lõi học tập và lưu trữ (có bản tạm và bản sao lưu) |
| `LocalizationService`, `SettingsManager`, `UIStateManager`, `GameSceneManager` | Hạ tầng UI, cài đặt, chuyển cảnh |
| `SkillBarUI` / `SkillSlotUI`, `MobileControlsHUD`, `CampusInput` | HUD kỹ năng, điều khiển PC và mobile |

### 14.2 Module mới

| Thư mục | Lớp | Nhiệm vụ |
|---|---|---|
| `Assets/Combat/` | `DamageInfo`, `Element`, `ElementChart`, `StatusEffect`, `ReactionResolver`, `PlayerCombat` (Ngự Kiếm), `DodgeAbility`, `SpiritPower` (Linh Lực), `DamageNumberPool`, `EnemyHealthBar` | Sát thương có hệ, trạng thái, phản ứng, đòn đánh thường, né |
| `Assets/Skills/Core/` | `SkillDefinition` (ScriptableObject), `SkillRuntime` (lớp cha), `SkillLoadout` (4 ô), `SkillUnlockService`, `SkillRankData` | Khung kỹ năng dữ liệu hóa; 3 kỹ năng cũ chuyển sang khung này |
| `Assets/Enemies/` | `EnemyArchetype` (ScriptableObject), `MinionBrain`, `EnemyAbility`, `EliteAffix`, `EnemyDirector` (vé tấn công, vây), `RiftSpawner` | Quái thường nhẹ, tinh anh, phụ tố, khe nứt |
| `Assets/Levels/` | `LevelDefinition` ×10, `WaveDefinition`, `LevelDirector`, `StarEvaluator`, `LevelResult` | 10 màn, các đợt, sao, kết quả |
| `Assets/SkyBeast/` | `SkyBeastDefinition`, `SkyBeastController` (bay theo đường spline), `FireBreathCycle`, `ShelterDetector`, `IndoorVolume`, `BurningGround`, `SwordIntent` (Kiếm Ý), `HeavenSwordUltimate` | Cự thú, Thiên Hỏa, trong nhà / ngoài trời, Thiên Kiếm |
| `Assets/Progression/` | `CultivationService` (Tu Vi, cảnh giới, tầng), `Wallet` (Linh Thạch), `Inventory`, `ItemDefinition`, `ShopService`, `BuffSystem`, `ArtifactService`, `DailyQuestService` | Học để lên cấp, học để mua đồ |
| `Assets/CampusRiftUI/` (thêm) | `HubUI`, `LevelMapUI`, `LoadoutUI`, `ShopUI`, `CultivationUI`, `BreakthroughExamUI`, `LevelResultUI`, phần HUD mới | Giao diện mới, dựng theo pattern `UIFoundationBuilder` hiện có |

### 14.3 Dữ liệu (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Campus Rift/Level")]
public sealed class LevelDefinition : ScriptableObject
{
    public string id; public int index;                 // 1..10
    public string displayName, displayNameVN;
    public Realm requiredRealm; public int requiredTier;
    public SkyPreset sky;                               // Dusk, Night, BloodMoon, Inferno, RedEclipse
    public List<WaveDefinition> waves;
    public int aiTier;                                  // 0..4
    public float healthMultiplier, damageMultiplier, speedMultiplier;
    public List<EnemyArchetype> bosses;
    public List<SkyBeastPhase> skyPhases;               // rỗng ở màn 1–7
    public StarCondition[] stars;                       // ★★★ riêng từng màn
    public float parTimeSeconds;
}

[Serializable] public sealed class WaveDefinition
{
    public List<SpawnEntry> entries;                    // archetype + count + eliteCount
    public string[] riftZones;                          // RoomID hoặc BuildingID được phép spawn
}

[CreateAssetMenu(menuName = "Campus Rift/Skill")]
public sealed class SkillDefinition : ScriptableObject
{
    public string id; public string displayName, displayNameVN;
    public Element element; public SkillRole role;
    public Sprite icon;
    public Realm unlockRealm; public int unlockTier;
    public float cooldown, spiritCost;
    public CastType castType;                           // Instant, Aimed, Channel, Toggle
    public SkillRank[] ranks = new SkillRank[5];        // Nhập Môn … Hóa Cảnh
    public SkillRuntime runtimePrefab;
}

[CreateAssetMenu(menuName = "Campus Rift/Item")]
public sealed class ItemDefinition : ScriptableObject
{
    public string id; public string displayName, displayNameVN;
    public ItemKind kind;                               // Heal, Buff, FireWard, Artifact
    public int price, maxPerLevel;
    public Realm availableFrom;
    public BuffEffect[] effects;
}
```

### 14.4 Phát hiện trong nhà và ngoài trời

```csharp
public enum Shelter { Outdoor, Partial, Indoor }

// Chạy 5 lần/giây cho người chơi; với quái thì chạy lệch nhịp 2 lần/giây.
Shelter Evaluate(Vector3 head)
{
    if (IndoorVolume.Contains(head)) return Shelter.Indoor;      // vùng designer đặt tay để ghi đè
    int roofHits = 0;
    foreach (var offset in probeOffsets)                           // tâm + 2 điểm lệch 0,6 m
        if (Physics.Raycast(head + offset, Vector3.up, 60f, roofMask)) roofHits++;
    return roofHits == probeOffsets.Length ? Shelter.Indoor
         : roofHits > 0 ? Shelter.Partial : Shelter.Outdoor;
}
```

- `roofMask` là một layer "Roof" gán cho mái và sàn các tầng.
- Giếng trời, mái kính: đặt `IndoorVolume` hoặc gán layer riêng.
- Harness sẽ lấy mẫu toàn campus (nút Outdoor và Door của `RoomGraph`) để kiểm tra phân loại đúng.

### 14.5 Những chỗ phải sửa trong code hiện tại

| Chỗ | Vấn đề | Sửa |
|---|---|---|
| `PlayerMonsterHealth.TryTakeDamage` | Mỗi đòn trúng cho **miễn thương 0,5 giây**, nên các nhịp Thiên Hỏa và đòn của bầy quái bị "nuốt" mất | Thêm `DamageInfo` (lượng, hệ, nguồn, `ignoreIFrames`). Sát thương môi trường bỏ qua miễn thương; đòn cận chiến chỉ cho miễn 0,25 giây |
| `CampusInput` | Gán cứng Wall/Hand/Phantom vào Q/F/G, và R là Respawn | Đổi sang action chung `Skill1..4`, `Attack`, `Dash`, `Ultimate`, `Item1..3`. Enum `CampusAction` đã có sẵn `Attack` và `Dash` nhưng chưa dùng |
| `MobileControlsHUD` | 3 nút kỹ năng dựng cứng | Sinh nút theo bộ kỹ năng; thêm nút đánh, né, Thiên Kiếm, vật phẩm |
| `LearningEngine.Award`, `LearningSkillGate` | Mỗi 5 bài mở 1 skill, cộng chỉ số theo phần trăm có trần | Thay bằng `CultivationService` và `SkillUnlockService` |
| `LearningService` (đếm giờ sống sót) | Mở bài theo thời gian sống | Bỏ |
| `UIStateManager.OpenCourse` | Mở được học ngay giữa gameplay | Chỉ mở ở Sảnh; thêm các trạng thái `Hub`, `Loadout`, `Shop`, `LevelResult` |
| `MonsterBrain` (tăng tốc tới 20 theo thời gian) | Áp lực vô hạn | Tắt; độ khó lấy từ `LevelDefinition` |
| `MonsterVitality.maxHealth` = 500 cố định; `MonsterCombat` lấy sát thương từ config chung | Chỉ số không đổi theo màn | Lấy từ `EnemyArchetype` × hệ số của màn |
| `GameOverUI.Retry` | Nạp lại scene | Chơi lại đúng màn đó với bộ kỹ năng đã chọn |

### 14.6 Save v2

Save cũ `learning-v1.json` được sao lưu thành `learning-v1.backup.json`. Save mới là `campusrift-v2.json`. Cài đặt vẫn lưu riêng như cũ.

```json
{
  "version": 2,
  "cultivation": { "realm": "TrucCo", "tier": 3, "tuVi": 220 },
  "wallet": { "linhThach": 560 },
  "lessons": { "ch1-b1": { "pagesRead": 4, "bestPercent": 90, "mastery": "Bac", "nextReviewUtc": "2026-10-02T00:00:00Z" } },
  "exams": { "ch1": { "passed": true, "best": 85, "attempts": 2 } },
  "skills": { "unlocked": ["dai-thu-an", "hu-khong-ket-gioi", "anh-phan-than", "tich-lich"], "ranks": { "dai-thu-an": 2 } },
  "loadouts": [["dai-thu-an", "phat-no-hoa-lien", "han-bang", "ngu-loi"]],
  "inventory": { "hoi-xuan-dan": 4, "ti-hoa-chau": 1 },
  "artifacts": { "phi-kiem": 2, "tui-can-khon": 1 },
  "levels": { "1": { "cleared": true, "stars": 3, "bestTime": 212.4 } },
  "daily": { "date": "2026-09-28", "quests": [true, false, false], "streak": 3, "restDayUsed": false }
}
```

---

## 15. Lộ trình

### 15.1 Bản đầy đủ (1–2 người, khoảng 16 tuần)

| Mốc | Thời lượng | Việc chính | Kiểm chứng |
|---|:-:|---|---|
| **M0 Nền móng** | 1 tuần | Khung kỹ năng dữ liệu hóa, input 4 ô cho PC và mobile, `DamageInfo`, save v2, gỡ Algorithms | 3 kỹ năng cũ chạy qua khung mới; test cũ vẫn pass |
| **M1 Chiến đấu lõi** | 2 tuần | Ngự Kiếm, né, Linh Lực, khóa mục tiêu, thanh máu quái, số sát thương; `MinionBrain` và Tiểu Yêu; `LevelDefinition`, `LevelDirector`, Khe Nứt; màn 1–2 | Chơi được màn 1–2 từ đầu tới màn Kết Quả |
| **M2 Học để thắng** | 2 tuần | `CultivationService`, Thi Đột Phá, Linh Thạch, Đan Các với 16 vật phẩm, Sảnh Tu Luyện, câu Đúng/Sai và Sắp xếp; nhập nội dung Tư tưởng Hồ Chí Minh khi có | Học → lên cấp → mua đồ → mở màn 3 |
| **M3 Kỹ năng và combo** | 3 tuần | 7 kỹ năng mới (đủ 10 kỹ năng MVP), ngũ hành, phản ứng, tầng kỹ năng, màn Chuẩn Bị kèm gợi ý | Test từng phản ứng; mỗi kỹ năng chạy trên PC và mobile |
| **M4 Quái và màn 3–7** | 3 tuần | 6 loại quái mới, AI T1–T3, tinh anh và phụ tố, Shaban boss 2 dạng, hệ sao | Màn 3–7 chơi được, đã cân bằng sơ bộ |
| **M5 Cự thú và Thiên Kiếm** | 3 tuần | `ShelterDetector`, Thiên Hỏa, Dư Hỏa, 3 cự thú, Kiếm Ý, Thiên Kiếm kèm cảnh diễn, AI T4, màn 8–10 | Phá đảo được màn 10 |
| **M6 Hoàn thiện** | 2 tuần | Cân bằng, hướng dẫn, âm thanh và VFX, hiệu năng mobile, rà nội dung cùng giảng viên, chơi thử | Bản phát hành thử |

### 15.2 Bản rút gọn (khoảng 6–7 tuần, nếu hạn gấp, ví dụ đồ án môn học)

- 10 màn trên cùng một map.
- 5 loại quái: Tiểu Yêu, Độc Nhãn, Thiết Giáp, Bạo Thi, Shaban. AI T0–T2, cộng thêm chặn cửa đơn giản ở màn 9–10.
- **1 model cự thú** dùng cho cả 3 màn cuối (đổi màu, kích thước, tham số). Màn 10 dùng 2–3 bản của model đó.
- 10 kỹ năng, 10 vật phẩm, 3 pháp bảo.
- Câu hỏi: Một đáp án, Đúng/Sai, Sắp xếp.
- Tạm cắt: Linh Bia, phụ tố tinh anh, Tháp Thí Luyện, thẻ ghi nhớ, nhiệm vụ ngày.

---

## 16. Bảng cân bằng tổng hợp

Người chơi ở đúng cảnh giới đề nghị (tầng yêu cầu của màn). Đánh thường hệ Mộc đánh vào Tiểu Yêu hệ Thổ, nên được +50% (Mộc khắc Thổ). Chưa tính phòng thủ.

| Màn | Cảnh giới | Máu | Công | Máu Tiểu Yêu | Nhát đánh thường để hạ | ST Tiểu Yêu | % máu mất mỗi đòn |
|:-:|---|:-:|:-:|:-:|:-:|:-:|:-:|
| 1 | Luyện Khí 1 | 100 | 20 | 60 | 2 | 8 | 8,0% |
| 2 | Luyện Khí 3 | 120 | 24 | 78 | 2 | 10 | 8,3% |
| 3 | Trúc Cơ 1 | 170 | 34 | 108 | 2 | 14 | 8,5% |
| 4 | Trúc Cơ 3 | 195 | 39 | 132 | 2 | 17 | 8,6% |
| 5 | Kết Đan 1 | 260 | 52 | 180 | 2 | 22 | 8,6% |
| 6 | Kết Đan 3 | 290 | 58 | 210 | 2 | 26 | 8,8% |
| 7 | Nguyên Anh 1 | 370 | 74 | 276 | 3 | 34 | 9,1% |
| 8 | Hóa Thần 1 | 500 | 100 | 384 | 3 | 46 | 9,3% |
| 9 | Luyện Hư 1 | 650 | 130 | 504 | 3 | 61 | 9,4% |
| 10 | Độ Kiếp 1 | 820 | 164 | 630 | 3 | 77 | 9,4% |

**Đọc bảng:**
- Quái **trâu dần và đau dần** so với sức người chơi (từ 2 nhát lên 3 nhát; từ 8% lên 9,4% máu mỗi đòn).
- Phần khó chính đến từ **số lượng, kỹ năng và AI** của quái.
- Người chơi **học vượt** cảnh giới đề nghị sẽ thấy màn dễ hơn rõ rệt. Đó là "học để thắng".

**Ví dụ về khắc hệ:** ở màn 3, Thiết Giáp có 220 × 1,8 = 396 máu. Đánh thường hệ Mộc bị Kim khắc nên cần khoảng 13 nhát. Phật Nộ Hỏa Liên (Hỏa khắc Kim) gây khoảng 230 mỗi phát. → Người chơi **phải** tính hệ khi chọn 4 kỹ năng.

**Thời gian hạ mục tiêu:**

| Mục tiêu | Thời gian |
|---|---|
| Quái thường | 2–4 giây |
| Tinh anh | 10–15 giây |
| Boss màn 5 | 60–90 giây |
| Boss màn 7 | 90–120 giây |

---

## 17. Kiểm thử

Theo đúng pattern hiện có: harness Play Mode ghi kết quả vào `Artifacts/<Tính năng>/*.json` và `Temp/*-progress.txt`, kết thúc bằng `DONE`.

| Harness mới | Kiểm tra |
|---|---|
| `LevelDirectorPlayTest` | Mỗi màn: số quái và số đợt đúng bảng 2.1; giới hạn quái hoạt động cùng lúc; hệ số máu, sát thương, tốc độ được áp đúng |
| `ShelterDetectorTest` | Lấy mẫu toàn campus: nút Outdoor phải là "Ngoài trời", điểm trong tòa nhà phải là "Trong nhà", hiên là "Bán che" |
| `FireBreathPlayTest` | Sát thương 3 mức đúng bảng 4.3; bỏ qua miễn thương; vật phẩm và kỹ năng giảm đúng; không vượt trần 80% |
| `HeavenSwordPlayTest` | Kiếm Ý chỉ đạt 100% khi đợt chết hết; quái triệu hồi không tính và tự tan; phải ra ngoài trời mới niệm; bị ngắt khi trúng lửa; cự thú mất đúng một giai đoạn |
| `SkillLoadoutTest` | 4 ô trên PC và mobile; không đổi được giữa trận (trừ màn 9–10 lúc nghỉ); Thiên Kiếm không chiếm ô |
| `ReactionPlayTest` | Từng phản ứng ở bảng 7.5 và Tương Sinh Liên Hoàn |
| `CultivationTest` | Chia Tu Vi theo số bài; bình cảnh; Thi Đột Phá mở đúng điều kiện; màn khóa và mở theo cảnh giới |
| `EconomyTest` | Trần luyện tập 300/ngày; câu đúng trong 24 giờ không có thưởng; giới hạn vật phẩm mang vào màn |
| `SaveMigrationTest` | Sao lưu save v1; tạo save v2; hồ sơ không hỏng |
| `LearningContentValidation` (mở rộng) | Mọi câu có ID, đáp án, giải thích, **trang nguồn**; mỗi bài từ 15 câu; mỗi chương từ 40 câu |

Cập nhật các harness cũ bị ảnh hưởng: `ShabanPressurePlayTest`, `VoidWallPlayTest`, `GiantHandPlayTest`, `MobileControlPlayTest`, `LearningPlayTest`.

---

## 18. Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|:-:|---|
| Khối lượng lớn: 21 kỹ năng, 9 loại quái, 3 cự thú | Cao | Làm bản rút gọn trước (15.2); kỹ năng làm theo 3 đợt |
| Model 3D: cự thú biết bay và quái mới | Cao | Dùng nguồn có giấy phép (Poly Pizza, Asset Store) qua pipeline Blender sẵn có; 1 model cự thú làm nhiều biến thể |
| Hiệu năng mobile khi đông quái và hiệu ứng lửa | Trung bình | Giới hạn 9 quái, AI cấp theo khoảng cách, pool, mưa lửa chỉ quanh máy quay |
| Phân loại trong nhà / ngoài trời sai ở mái kính, giếng trời | Trung bình | `IndoorVolume` ghi đè; harness lấy mẫu toàn map |
| Nội dung môn học nhạy cảm | Cao | Nguyên tắc 11.5; giảng viên duyệt; ghi nguồn trang |
| Người chơi "cày" câu hỏi thay vì học | Trung bình | Các luật ở 9.2; ôn tập giãn cách; câu hỏi xáo trộn |
| Game chuyển từ kinh dị sang hành động nên AI săn mồi ít được dùng | Thấp | Shaban vẫn là tinh anh và boss; AI T3–T4 tái dùng các module nghe, đoán, chặn đầu |

---

## 19. Quyết định cần bạn chốt

Cột "Đề xuất mặc định" là cách tôi sẽ làm nếu bạn không đổi.

| # | Câu hỏi | Đề xuất mặc định |
|:-:|---|---|
| 1 | Thiên Kiếm là nút riêng, không chiếm 1 trong 4 ô? | **Có** |
| 2 | Phải đứng ngoài trời mới triệu hồi được Thiên Kiếm? | **Có**, để tạo kịch tính canh giờ |
| 3 | Được đổi kỹ năng giữa màn không? | Không; riêng màn 9–10 được đổi trong lúc nghỉ giữa đợt |
| 4 | Trong nhà mất bao nhiêu so với ngoài trời? | Trong nhà 12%, bán che 45% |
| 5 | Số chương và số bài của nội dung Tư tưởng Hồ Chí Minh? | Tạm theo 6 chương của giáo trình 2021; chờ nội dung của bạn |
| 6 | Hạn hoàn thành? (Nếu là đồ án môn học thì chọn bản rút gọn) | Cần bạn cho biết |
| 7 | Nền tảng: giữ cả PC và Android như hiện tại? | Giữ cả hai |
| 8 | Giữ không khí kinh dị hay chuyển hẳn sang hành động tu tiên? | Hành động tu tiên; màn đêm 3–7 vẫn u tối |
| 9 | Có làm phần sau khi phá đảo (Tháp Thí Luyện, Ác Mộng) không? | Để sau bản rút gọn |

---

## Phụ lục A — Mẫu nhập nội dung

Bạn gửi theo mẫu này (Excel hoặc CSV đều được). Nội dung trong ngoặc vuông là chỗ bạn điền; tài liệu này không tự soạn nội dung môn học.

**Bảng Bài học**

| chuong | bai_id | bai_ten | trang_so | noi_dung_trang | ghi_nho |
|:-:|---|---|:-:|---|---|
| 1 | ch1-b1 | [Tên bài] | 1 | [Đoạn nội dung khoảng 250 chữ] | [Ý cần nhớ] |
| 1 | ch1-b1 | [Tên bài] | 2 | [Đoạn tiếp theo] | [Ý cần nhớ] |

**Bảng Câu hỏi**

| cau_id | bai_id | loai | do_kho | cau_hoi | A | B | C | D | dap_an | giai_thich | nguon |
|---|---|---|:-:|---|---|---|---|---|:-:|---|---|
| ch1-b1-q01 | ch1-b1 | mot-dap-an | 1 | [Câu hỏi] | [..] | [..] | [..] | [..] | B | [Giải thích] | GT 2021, tr. [..] |
| ch1-b1-q02 | ch1-b1 | dung-sai | 1 | [Nhận định] | Đúng | Sai | | | A | [..] | GT 2021, tr. [..] |
| ch2-b3-q07 | ch2-b3 | sap-xep | 2 | [Xếp các mốc theo thứ tự] | [Mốc 1] | [Mốc 2] | [Mốc 3] | [Mốc 4] | C,A,D,B | [..] | GT 2021, tr. [..] |

Các giá trị `loai`: `mot-dap-an` · `nhieu-dap-an` · `dung-sai` · `sap-xep` · `noi-cap` · `dien-khuyet`.

Tôi sẽ viết công cụ nhập để chuyển các bảng trên thành `LessonData` và `QuestionBankData`, rồi chạy **Validate Content**.

---

## Phụ lục B — Thuật ngữ

| Thuật ngữ | Nghĩa trong game |
|---|---|
| **Tu Vi** | Điểm kinh nghiệm, kiếm chủ yếu từ học |
| **Cảnh giới / Tầng** | Cấp độ lớn (7 cảnh giới) và cấp nhỏ (5 tầng mỗi cảnh giới) |
| **Bình cảnh** | Tầng 5 đã đầy, phải Thi Đột Phá mới lên tiếp |
| **Đột phá** | Đậu kỳ thi cuối chương để lên cảnh giới mới |
| **Linh Lực** | Năng lượng để dùng kỹ năng |
| **Thể Lực** | Năng lượng để chạy nhanh và né (thanh Energy hiện có) |
| **Linh Thạch** | Điểm tích lũy từ học, dùng để mua đồ |
| **Đan dược / Phù chú** | Vật phẩm tiêu hao hồi phục hoặc tăng sức mạnh |
| **Pháp bảo** | Nâng cấp vĩnh viễn |
| **Cơ Duyên** | Buff tạm thời nhận được từ Linh Bia trong màn |
| **Khe Nứt** | Điểm quái xuất hiện |
| **Kiếm Ý** | Thanh tích lũy để triệu hồi Thiên Kiếm |
| **Thiên Hỏa / Dư Hỏa** | Mưa lửa của cự thú / vệt lửa còn lại sau đó |
| **Tinh anh / Phụ tố** | Quái mạnh / thuộc tính đặc biệt của quái mạnh |

---

## Phụ lục C — Nguồn tham khảo

**Giáo trình và pháp lý**
- [Giáo trình Tư tưởng Hồ Chí Minh (2021) — Thư viện ĐH Công nghiệp Hà Nội](https://lic.haui.edu.vn/vn/gioi-thieu-sach-moi/giao-trinh-tu-tuong-ho-chi-minh-2021/72764)
- [Điều 16 Luật An ninh mạng số 24/2018/QH14](https://thanhphohaiphong.gov.vn/dieu-16-luat-an-ninh-mang-so-24-2018-qh14.html)
- [Nghị định 147/2024/NĐ-CP — Bộ Khoa học và Công nghệ](https://mst.gov.vn/nghi-dinh-147-2024-nd-cp-quan-ly-chat-che-dich-vu-tro-choi-dien-tu-tren-mang-va-thong-tin-tren-internet-197241227124622733.htm)

**Tham khảo kỹ năng**
- Tu tiên và kiếm hiệp: *Phàm Nhân Tu Tiên* ([Thanh Trúc Phong Vân Kiếm](https://vidian.vn/chi-tiet/pham-nhan-tu-tien-thanh-truc-phong-van-kiem), [Đại Canh Kiếm Trận](https://ebookvie.com/dai-canh-kiem-tran-la-gi-su-hinh-thanh-va-uy-luc-cua-no/)), *Đấu Phá Thương Khung*, *Đấu La Đại Lục*, *Tru Tiên*, *Thế Giới Hoàn Mỹ*, *Tiên Nghịch*, *Thôn Phệ Tinh Không*, *Phong Vân* ([Vạn Kiếm Quy Tông — Thiên Kiếm Vô Danh](https://soha.vn/van-kiem-quy-tong.html)), *Phong Thần Diễn Nghĩa*, *Tây Du Ký*, Kim Dung (*Thiên Long Bát Bộ*, *Anh Hùng Xạ Điêu*), *Như Lai Thần Chưởng*.
- Anime: *Naruto*, *Kimetsu no Yaiba*, *Jujutsu Kaisen*, *Solo Leveling*, *One Piece*, *My Hero Academia*, *Fate*; hệ phản ứng nguyên tố tham khảo *Genshin Impact*.
