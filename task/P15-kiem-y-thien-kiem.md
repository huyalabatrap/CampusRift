# P15 — Kiếm Ý và Thiên Kiếm

> **Mục tiêu:** diệt sạch quái của đợt thì thanh Kiếm Ý đầy; người chơi ra ngoài trời triệu hồi Thiên Kiếm; có cảnh diễn Vạn Kiếm Quy Tông; cự thú mất khúc máu, chuyển pha hoặc chết.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P14 · **Tham chiếu:** §5, quyết định 1–2 trong `task/README.md`
>
> **Kết quả:** hoàn thành được màn 8 và 9. Màn 10 hoàn thiện ở P16.
>
> **03/10/2026 · ✅ smoke:** T01–T08 đã triển khai; HeavenSword41/41, SkyBeast58/58, FireBreath42/42. Checkbox dưới đây xác nhận bằng smoke/DEV theo [TEST-POLICY](TEST-POLICY.md), chưa xác nhận cân bằng thực chiến hoặc thiết bị thật. [Báo cáo](p15/REPORT-P15.md).

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P15-T01 | Thanh Kiếm Ý | Code+UI | 0,4 | P14 | ✅ smoke |
| P15-T02 | Nút Thiên Kiếm và các trạng thái | Code+UI | 0,4 | T01 | ✅ smoke |
| P15-T03 | Luật triệu hồi: ngoài trời, niệm chú, bị ngắt | Code | 0,5 | T02 | ✅ smoke |
| P15-T04 | Cảnh diễn Vạn Kiếm Quy Tông | Asset+Code | 1 | T03 | ✅ smoke |
| P15-T05 | Chuyển pha và chiến thắng | Code | 0,4 | T04 | ✅ smoke |
| P15-T06 | Âm thanh Thiên Kiếm | Asset | 0,2 | T04 | ✅ smoke |
| P15-T07 | Nâng cấp Thiên Kiếm theo cảnh giới | Code | 0,2 | T03 | ✅ smoke |
| P15-T08 | Harness `HeavenSwordPlayTest` | Test | 0,4 | T01–T07 | ✅ smoke |

---

## P15-T01 — Thanh Kiếm Ý

- **Loại:** Code+UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P14
- **File:** (mới) `Assets/SkyBeast/Runtime/SwordIntent.cs`, `Assets/CampusRiftUI/Runtime/SwordIntentUI.cs`
- **Các bước:**
  1. Lắng nghe `EnemyDied` và cộng trọng số: quái thường 1; Thiết Giáp/Triệu Hồn Sư 2; tinh anh 4.
  2. Kiếm Ý = trọng số đã hạ ÷ tổng trọng số của đợt hiện tại.
  3. Quái có `countsForSwordIntent = false` (do Triệu Hồn Sư gọi ra, do cự thú thả xuống) không tính. Chúng **tự tan** khi Kiếm Ý đạt 100%.
  4. **Chỉ đạt 100% khi toàn bộ quái của đợt đã chết.** Không vật phẩm nào làm đầy sớm hơn.
  5. Không giảm theo thời gian. Thanh nằm ngay dưới thanh máu cự thú.
- **Hoàn thành khi:**
  - [x] Còn 1 quái thì chưa đầy.
  - [x] Hạ con cuối thì đầy đúng 100%.

## P15-T02 — Nút Thiên Kiếm và các trạng thái

- **Loại:** Code+UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P15-T01
- **File:** (sửa) `CampusInput.cs` (dùng action `Ultimate` đã thêm ở P02), ô Thiên Kiếm trên HUD (P02-T06), `MobileControlsHUD.cs` (thêm `TouchRole.Ultimate`)
- **Các bước:**
  1. Nút chỉ xuất hiện ở màn 8–10 và khi đã đạt Hóa Thần.
  2. Các trạng thái:
     - `Charging` (thanh Kiếm Ý đang tăng);
     - `Ready` (sáng rực, kèm tiếng kiếm ngân);
     - `NeedOutdoor` ("Ra ngoài trời để triệu hồi");
     - `Blocked` (đang Long Nộ). Lúc đang Thiên Hỏa vẫn là `Ready` nhưng kèm cảnh báo "Sẽ bị ngắt nếu không có hộ thể";
     - `Channeling`.
  3. PC: phím V. Mobile: nút lớn phía trên cụm nút kỹ năng.
- **Hoàn thành khi:**
  - [x] Trạng thái hiển thị đúng trong mọi tình huống.

## P15-T03 — Luật triệu hồi: ngoài trời, niệm chú, bị ngắt

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P15-T02
- **File:** (mới) `Assets/SkyBeast/Runtime/HeavenSwordUltimate.cs`
- **Các bước:**
  1. Điều kiện: Kiếm Ý 100%, `ShelterDetector.Current == Outdoor`, không đang Long Nộ.
  2. Niệm chú 2,5 giây, đứng yên; dưới chân hiện vòng kiếm trận vàng. Kiếm Tâm Đan rút ngắn 40%.
  3. Bị một nhịp Thiên Hỏa trúng thì ngắt, **Kiếm Ý vẫn giữ 100%**. Ngoại lệ: có Kim Chung Tráo, hoặc Kiếm Tâm Đan (miễn ngắt 1 lần).
  4. Niệm xong thì chạy cảnh diễn (T04), rồi trừ một khúc máu cự thú.
- **Hoàn thành khi:**
  - [x] Đứng trong nhà thì không triệu hồi được.
  - [x] Bị ngắt không mất Kiếm Ý.
  - [x] Vật phẩm và kỹ năng tác động đúng.

## P15-T04 — Cảnh diễn Vạn Kiếm Quy Tông

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (mesh kiếm, hiệu ứng hạt, âm thanh cảnh diễn) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset+Code · **Ước lượng:** 1 ngày · **Phụ thuộc:** P15-T03
- **File:** (mới) `Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs`, `GiantSwordVisual.cs`; material kiếm vàng phát sáng
- **Các bước (§5.3):**
  1. Máy quay: kéo lên cao và nhìn về phía cự thú. Làm bằng code (hoặc Timeline — package đã có), chạy theo thời gian unscaled.
  2. Hàng nghìn phi kiếm nhỏ bay lên từ khắp sân: GPU instancing hoặc hạt, dùng mesh phi kiếm của P03.
  3. Phi kiếm tụ thành **thanh kiếm vàng khoảng 150 m** (mesh sinh bằng code, theo cách làm của `GiantHandVisual`).
  4. Kiếm lao xuyên cự thú: chậm hình cục bộ (animator cự thú chậm lại, hạt chậm lại; **không** đổi `Time.timeScale`), sóng xung kích.
  5. Trong lúc diễn: tạm dừng `FireBreathCycle` và `SkyBeastScheduler`; khóa input người chơi.
  6. Lần đầu dài 6 giây. Các lần sau 3 giây, bỏ qua được. Cờ "đã xem" lưu trong hồ sơ.
  7. Mobile: giảm số phi kiếm còn khoảng 40%.
- **Hoàn thành khi:**
  - [x] Cảnh chạy mượt; bỏ qua được.
  - [x] Sau cảnh, gameplay tiếp tục đúng trạng thái.

## P15-T05 — Chuyển pha và chiến thắng

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P15-T04
- **Các bước:**
  1. **Chưa phải nhát cuối:** cự thú gầm và chuyển sang pha kế (profile phun mới), nghỉ 15 giây, đợt quái kế xuất hiện, Kiếm Ý về 0.
  2. **Nhát diệt một cự thú ở màn 10:** con đó rơi và tan thành tro, lịch chuyển giai đoạn (P14-T07).
  3. **Nhát cuối:**
     - cự thú rơi và tan;
     - bầu trời chuyển sang bình minh qua `SkyLightingController`;
     - `LevelDirector.Win()` → màn Kết Quả.
- **Hoàn thành khi:**
  - [x] Màn 8 xong sau 1 nhát.
  - [x] Màn 9 xong sau 2 nhát, qua đúng các pha.

## P15-T06 — Âm thanh Thiên Kiếm

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh kiếm ngân, chuỗi âm thanh triệu hồi, nhạc chiến thắng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P15-T04
- **Các bước:**
  1. Chuỗi 7 bước giống cấu hình của Giant Hand: cast → charge → rift → descent → impact → aftershock → dissipate. Dùng lại clip có sẵn trong `GiantHandConfig`, hoặc thêm clip mới có giấy phép phù hợp.
  2. Thêm tiếng kiếm ngân khi Kiếm Ý đầy, và nhạc chiến thắng.
- **Hoàn thành khi:**
  - [x] Âm thanh khớp từng bước của cảnh diễn.

## P15-T07 — Nâng cấp Thiên Kiếm theo cảnh giới

- **Loại:** Code · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P15-T03
- **Các bước (§5.5):**

| Cảnh giới | Niệm chú | Thêm |
|---|:-:|---|
| Hóa Thần | 2,5 giây | — |
| Luyện Hư | 2,0 giây | Sau khi chém: "Kiếm Ý hộ thể" 10 giây, −50% lửa (qua `BuffSystem`) |
| Độ Kiếp | 1,5 giây | Cảnh diễn luôn dùng bản ngắn |

- **Hoàn thành khi:**
  - [x] Thời gian niệm đổi đúng theo cảnh giới.

## P15-T08 — Harness `HeavenSwordPlayTest`

- **Loại:** Test · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P15-T01 đến T07
- **File:** (mới) `Assets/SkyBeast/Validation/HeavenSwordPlayTest.cs` → `Artifacts/SkyBeast/HeavenSword.json`
- **Kiểm tra:**
  - Kiếm Ý chỉ đạt 100% khi đợt chết hết; quái triệu hồi không tính và tự tan.
  - Trong nhà không triệu hồi được.
  - Bị ngắt thì vẫn giữ Kiếm Ý; Kiếm Tâm Đan và Kim Chung Tráo tác động đúng.
  - Cự thú mất đúng 1 khúc máu.
  - Chuyển pha; chiến thắng; bầu trời về bình minh.
  - Nhận nút trên PC và mobile.
- **Hoàn thành khi:**
  - [x] PASS.

---

## Kiểm chứng cuối phase

- [ ] Chơi hết màn 8 và màn 9 ở đúng cảnh giới đề nghị.
- [x] Cập nhật trạng thái P15 trong `task/README.md`.

DEV smoke đã hoàn thành màn 8/Hóa Thần và màn 9/Luyện Hư bằng đợt quái thật, spawn tăng tốc/DEV damage/AI hold/HP10000. Mục chơi thực chiến giữ chưa tick; cân bằng chưa đo theo TEST-POLICY.
