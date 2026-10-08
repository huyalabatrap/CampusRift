# P14 — Cự thú bầu trời

> **03/10/2026: ✅ smoke T01–T08.** Xem [REPORT-P14](p14/REPORT-P14.md); kiểm chứng gọn theo TEST-POLICY, chưa đo cân bằng/Android thật.

> **Mục tiêu:** cự thú bay quanh campus, phun Thiên Hỏa đồng bộ với `FireBreathCycle`, có thanh máu theo khúc, có đòn riêng của Chu Tước và Hỏa Long Vương, và màn 10 có nhiều cự thú cùng lúc.
>
> **Phạm vi:** MVP · **Ước lượng:** 4 ngày công · **Phụ thuộc:** P13 · **Tham chiếu:** §4.1, §2.3 (màn 8–10)
>
> **Kết quả:** màn 8–10 có cự thú hoạt động đúng lịch. Việc hạ cự thú bằng Thiên Kiếm làm ở P15.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P14-T01 | Model cự thú (MVP: 1 model, 3 biến thể) | Asset | 0,8 | — | ✅ smoke |
| P14-T02 | `SkyBeastDefinition` và 3 asset | Dữ liệu | 0,2 | T01 | ✅ smoke |
| P14-T03 | `SkyBeastController`: bay, gầm, tư thế phun | Code | 0,8 | T02, P13 | ✅ smoke |
| P14-T04 | Thanh máu cự thú chia khúc | UI | 0,3 | T03 | ✅ smoke |
| P14-T05 | Chu Tước: Lông Vũ Hỏa | Code | 0,4 | T03 | ✅ smoke |
| P14-T06 | Hỏa Long Vương: mưa thiên thạch và Long Nộ | Code | 0,6 | T03 | ✅ smoke |
| P14-T07 | Lịch nhiều cự thú cho màn 10 | Code | 0,4 | T03, T05, T06 | ✅ smoke |
| P14-T08 | Harness `SkyBeastPlayTest` | Test | 0,5 | T03–T07 | ✅ smoke |

---

## P14-T01 — Model cự thú (MVP: 1 model, 3 biến thể)

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (model rồng/chim lửa biết bay có animation bay/gầm/phun) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Asset · **Ước lượng:** 0,8 ngày · **Phụ thuộc:** —
- **File:** (mới) `Assets/SkyBeast/Models/…`, `Assets/SkyBeast/Models/LICENSES.md`, prefab `XichHoaGiao.prefab`, `ChuTuoc.prefab`, `HoaLongVuong.prefab`
- **Các bước:**
  1. Tìm model rồng hoặc chim biết bay, có animation bay, gầm, phun. Giấy phép CC0 hoặc CC-BY (Quaternius, Poly Pizza, …).
  2. GLB → Blender headless → FBX. Nếu không có animation bay: dùng vỗ cánh bằng code (xoay xương cánh theo hàm sin) cộng dao động thân.
  3. **Bản MVP:** 1 model làm 3 biến thể:
     - Giao dài khoảng 120 m, màu đỏ;
     - Chu Tước: cánh to, sải khoảng 150 m, màu cam vàng;
     - Long Vương: khoảng 200 m, thêm 9 sừng, màu đỏ đen.

     Model riêng cho từng con để P21.
  4. Material phát sáng (emission) để nhìn rõ trên bầu trời đêm; LOD đơn giản.
- **Hoàn thành khi:**
  - [x] Có 3 prefab.
  - [x] Nhìn rõ từ mặt đất.
  - [x] Có LICENSES đầy đủ.

## P14-T02 — `SkyBeastDefinition` và 3 asset

- **Loại:** Dữ liệu · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P14-T01
- **File:** (mới) `Assets/SkyBeast/Runtime/SkyBeastDefinition.cs`; `Assets/SkyBeast/Data/xich-hoa-giao.asset`, `chu-tuoc.asset`, `hoa-long-vuong.asset`
- **Các bước:** trường dữ liệu gồm:
  - `id`, tên EN/VN, prefab;
  - `altitude` (90 / 100 / 120 m), `pathType` (Circle / FigureEight), `pathRadius`, `speed`;
  - `segments` (số nhát Thiên Kiếm);
  - danh sách pha, mỗi pha gồm: `FireBreathProfile`, đòn phụ, hệ số cuồng nộ.
- **Hoàn thành khi:**
  - [x] Validation: mọi trường hợp lệ; số khúc khớp màn (8: 1; 9: 2; 10: mỗi con 1).

## P14-T03 — `SkyBeastController`: bay, gầm, tư thế phun

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh gầm, vỗ cánh, phun lửa) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,8 ngày · **Phụ thuộc:** P14-T02, P13
- **File:** (mới) `Assets/SkyBeast/Runtime/SkyBeastController.cs`, `SkyBeastVitality.cs`
- **Các bước:**
  1. Tâm và bán kính đường bay: tính từ phạm vi các nút trong `CampusRoomGraph` (tâm campus).
  2. Bay theo đường tròn hoặc hình số 8; nghiêng mình khi lượn; luôn nằm trong `far clip plane` của camera (350 m). Nếu cần thì tăng far clip và dùng sương mù để che.
  3. Đồng bộ với `FireBreathCycle`:
     - `WarningStarted` → bay vọt lên, há miệng, sáng dần ở họng;
     - `BreathStarted` → tư thế phun, luồng lửa hướng xuống campus;
     - xong thì quay lại lượn.
  4. `SkyBeastVitality`: máu theo khúc, **chỉ** nhận sát thương từ Thiên Kiếm. Mọi đòn khác bỏ qua. Không có collider với người chơi (layer `SkyBeast`).
  5. Gầm khi vào màn và khi chuyển pha.
- **Hoàn thành khi:**
  - [x] Bay mượt, không ra khỏi tầm nhìn.
  - [x] Tư thế phun khớp đúng thời điểm Thiên Hỏa.

## P14-T04 — Thanh máu cự thú chia khúc

- **Loại:** UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P14-T03
- **File:** (mới) `Assets/CampusRiftUI/Runtime/SkyBeastBarUI.cs`
- **Các bước:**
  1. Trên cùng màn hình: tên cự thú; thanh chia khúc (1 khúc = 1 nhát); nhãn pha.
  2. Màn 10: hiện thanh cho tất cả cự thú đang có trên trời (tối đa 2 cùng lúc).
  3. Chừa chỗ ngay dưới cho thanh Kiếm Ý (P15).
- **Hoàn thành khi:**
  - [x] Thanh hiển thị đúng số khúc và đúng pha.

## P14-T05 — Chu Tước: Lông Vũ Hỏa

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng lông vũ lửa rơi) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P14-T03
- **File:** (mới) `Assets/SkyBeast/Runtime/Attacks/FeatherBarrage.cs`
- **Các bước:**
  1. Trong thời gian nghỉ giữa hai lần phun, cứ 8 giây: **nếu người chơi đang ở ngoài trời**, thả 3 lông lửa. Mỗi lông có vòng báo 1,2 giây tại vị trí dự đoán của người chơi.
  2. Mỗi lông gây 12% máu đề nghị và để lại một vệt Dư Hỏa nhỏ.
  3. Pha 2 hoặc cuồng nộ: thả 5 lông.
- **Hoàn thành khi:**
  - [x] Không thả lông khi người chơi đang trong nhà.
  - [x] Né được nhờ vòng báo.

## P14-T06 — Hỏa Long Vương: mưa thiên thạch và Long Nộ

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (âm thanh và hiệu ứng thiên thạch, lửa liên tục) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P14-T03
- **File:** (mới) `Assets/SkyBeast/Runtime/Attacks/MeteorShower.cs`, `DragonFury.cs`
- **Các bước:**
  1. **Mưa thiên thạch:** mỗi 15 giây, 6 điểm rơi có vòng báo 1,5 giây. Chỉ rơi vào điểm ngoài trời (mái nhà cũng được, nhưng không xuyên vào trong). Sát thương 15% máu đề nghị, bán kính 3 m.
  2. **Long Nộ:** khi đợt 3 bị dọn sạch, trước nhát kiếm cuối, gọi `FireBreathCycle` chế độ Long Nộ (12 giây liên tục).
     - Báo trước 5 giây: "LONG NỘ — HÃY TRÚ ẨN!".
     - Sau khi Long Nộ kết thúc mới cho phép triệu hồi Thiên Kiếm.
  3. Thả Hỏa Linh xuống sân: để P19 (MVP không có Hỏa Linh).
- **Hoàn thành khi:**
  - [x] Thiên thạch không trúng người chơi đang ở trong nhà.
  - [x] Long Nộ ép người chơi phải trú ẩn; ngoài trời không có phòng hộ thì gần như chắc chết.

## P14-T07 — Lịch nhiều cự thú cho màn 10

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P14-T03, T05, T06
- **File:** (mới) `Assets/SkyBeast/Runtime/SkyBeastScheduler.cs`
- **Các bước:**

| Giai đoạn | Trên trời | Thiên Hỏa | Kết thúc |
|---|---|---|---|
| 1 | Giao + Chu Tước | Thay nhau phun: mỗi con 40 giây, lệch nhau 20 giây → **có một lần phun mỗi 20 giây**. Không bao giờ hai con cùng phun | Nhát 1 diệt Giao |
| 2 | Chu Tước (cuồng nộ) | Mỗi 28 giây, Lông Vũ Hỏa dày hơn | Nhát 2 diệt Chu Tước |
| 3 | Hỏa Long Vương bay xuống thấp hơn | Mỗi 25 giây, báo trước 4 giây; mưa thiên thạch | Long Nộ → nhát 3 |

  Màn 8 và 9 dùng cùng scheduler, với cấu hình 1 cự thú.
- **Hoàn thành khi:**
  - [x] Đúng thứ tự giai đoạn.
  - [x] Không có hai lần phun trùng nhau.

## P14-T08 — Harness `SkyBeastPlayTest`

- **Loại:** Test · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P14-T03 đến T07
- **File:** (mới) `Assets/SkyBeast/Validation/SkyBeastPlayTest.cs` → `Artifacts/SkyBeast/SkyBeast.json`
- **Kiểm tra:**
  - Đường bay nằm trong giới hạn và đúng độ cao.
  - Lịch phun theo từng màn và từng pha.
  - Lông Vũ Hỏa chỉ thả khi người chơi ở ngoài trời.
  - Thiên thạch không trúng người chơi trong nhà.
  - Long Nộ đúng sát thương.
  - Cự thú bỏ qua mọi đòn không phải Thiên Kiếm.
- **Hoàn thành khi:**
  - [x] PASS.

---

## Kiểm chứng cuối phase

- [x] Lệnh DEV vào màn 8, 9, 10: cự thú xuất hiện, phun đúng lịch, đòn phụ đúng.
- [x] Cập nhật trạng thái P14 trong `task/README.md`.
