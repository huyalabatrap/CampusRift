# P11 — Phản ứng và combo

> **Mục tiêu:** kỹ năng kết hợp với nhau tạo phản ứng, và có Tương Sinh Liên Hoàn. Đây chính là phần "phối hợp combo mạnh nhất theo từng màn".
>
> **Phạm vi:** MVP · **Ước lượng:** 1,5 ngày công · **Phụ thuộc:** P10 · **Tham chiếu:** §7.2, §7.5
>
> **Kết quả:** 5 phản ứng MVP và Tương Sinh Liên Hoàn chạy, có phản hồi rõ ràng trên màn hình.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P11-T01 | `ReactionResolver` với 5 phản ứng MVP | Code | 0,6 | P10 | ✅ |
| P11-T02 | Tương Sinh Liên Hoàn và vòng báo trên HUD | Code+UI | 0,4 | P10 | ✅ |
| P11-T03 | Phản hồi khi phản ứng xảy ra (chữ, âm, rung) | UI | 0,2 | T01 | ✅ |
| P11-T04 | Harness `ReactionPlayTest` | Test | 0,3 | T01–T03 | ✅ |

---

## P11-T01 — `ReactionResolver` với 5 phản ứng MVP

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P10
- **File:** (mới) `Assets/Combat/Runtime/ReactionResolver.cs`, `Assets/Combat/Data/Reactions.asset`
- **Các bước:**
  1. Chen vào đường sát thương: trước khi `ApplyDamage`, xét hệ của đòn đang đánh và trạng thái đang có trên mục tiêu.
  2. Các phản ứng MVP:

| Phản ứng | Điều kiện | Hiệu ứng |
|---|---|---|
| **Băng Lôi Liệt** | Freeze + đòn Lôi | Đòn Lôi +150%, nổ lan 3 m; tiêu hao Freeze |
| **Điện Lưu** | Chill hoặc Wet + đòn Lôi | Shock lan sang quái trong 5 m |
| **Bạo Viêm** | Burn + đòn Hỏa | Nổ thêm 80% Công, bán kính 4 m |
| **Tụ Sát** | Đang bị Hắc Động hút (`Pulled`) + đòn vùng | +40% sát thương |
| **Phá Giáp** | Stun + đòn Kim | ArmorBreak 8 giây (−30% phòng thủ); phá được phụ tố Kim Thân (P19) |

  3. Mỗi mục tiêu có hồi chiêu nội bộ 1 giây cho mỗi loại phản ứng, để tránh vòng lặp nổ dây chuyền.
  4. Sát thương phản ứng dùng nguồn `Reaction`.
  5. Phát event `ReactionTriggered(type, target)`, dùng cho sao và thống kê.
- **Hoàn thành khi:**
  - [x] Mỗi phản ứng chỉ kích hoạt khi đủ điều kiện, đúng số liệu.
  - [x] Không có vòng lặp vô hạn.

## P11-T02 — Tương Sinh Liên Hoàn và vòng báo trên HUD

- **Loại:** Code+UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P10
- **File:** (mới) `Assets/Combat/Runtime/GenerationChainTracker.cs`, `Assets/CampusRiftUI/Runtime/GenerationChainUI.cs`
- **Các bước:**
  1. Ghi lại hệ của các lần dùng kỹ năng (không tính đánh thường) trong cửa sổ 6 giây.
  2. Nếu 3 lần liên tiếp theo đúng vòng tương sinh (`ElementChart.Generates`, ví dụ Mộc → Hỏa → Thổ): kỹ năng thứ 3 +30% sát thương, hồi 20 Linh Lực, phát event `ChainCompleted`.
  3. HUD: vòng nhỏ "Tương Sinh ●●○" cạnh thanh kỹ năng, sáng lên khi hoàn thành.
- **Hoàn thành khi:**
  - [x] Đúng thứ tự thì kích hoạt; sai thứ tự hoặc quá 6 giây thì reset.

## P11-T03 — Phản hồi khi phản ứng xảy ra (chữ, âm, rung)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh riêng cho từng phản ứng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** UI · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P11-T01
- **Các bước:**
  1. `DamageNumberPool` hiện nhãn tên phản ứng (ví dụ "BĂNG LÔI LIỆT!"), màu pha giữa hai hệ.
  2. Mỗi phản ứng có SFX riêng; rung máy quay nhẹ.
  3. Lần đầu người chơi gặp một phản ứng: hiện thẻ gợi ý 3 giây ("Đóng băng + Lôi = Băng Lôi Liệt").
- **Hoàn thành khi:**
  - [x] Phản ứng dễ nhận ra ngay cả khi đông quái.

## P11-T04 — Harness `ReactionPlayTest`

- **Loại:** Test · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P11-T01 đến T03
- **File:** (mới) `Assets/Combat/Validation/ReactionPlayTest.cs`
- **Kiểm tra:**
  - Từng phản ứng: đúng điều kiện, đúng số liệu.
  - Hồi chiêu nội bộ 1 giây.
  - Tương Sinh: đúng thứ tự, sai thứ tự, quá thời gian.
- **Hoàn thành khi:**
  - [x] PASS.

---

## Kiểm chứng cuối phase

- [x] Trong màn: Hàn Băng rồi Thần Kiếm Ngự Lôi ra Băng Lôi Liệt; Hắc Động rồi Phật Nộ Hỏa Liên ra Tụ Sát.
- [x] Cập nhật trạng thái P11 trong `task/README.md`.

Hoàn tất 02/10/2026: [báo cáo, số đo và ảnh kiểm chứng](p11/REPORT-P11.md).
