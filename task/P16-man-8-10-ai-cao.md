# P16 — Màn 8–10 và AI T3–T4 → **Mốc 4**

> **Mục tiêu:** hoàn thiện 3 màn cuối, AI cấp cao (phối hợp đòn, phục kích, chặn cửa khi Thiên Hỏa), đổi kỹ năng giữa đợt ở màn 9–10, sao ★★★ màn 8–10, và chơi thử toàn bộ 10 màn.
>
> **Phạm vi:** MVP · **Ước lượng:** 3 ngày công · **Phụ thuộc:** P12, P15 · **Tham chiếu:** §2.3, §3.3
>
> **Kết quả (Mốc 4):** chơi được màn 1 → 10.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P16-T01 | AI T3: đòn lệch nhịp, phục kích ở cửa, nghe tiếng động | Code | 0,6 | P12-T07 | ✅ smoke |
| P16-T02 | AI T4 (MVP): chặn cửa khi Thiên Hỏa sắp phun | Code | 0,5 | T01, P13 | ✅ smoke |
| P16-T03 | Dữ liệu và cân bằng màn 8–10 | Dữ liệu | 0,5 | P15 | ✅ smoke; chưa đo thực chiến |
| P16-T04 | Đổi kỹ năng trong lúc nghỉ giữa đợt (màn 9–10) | UI | 0,3 | P09-T04 | ✅ smoke |
| P16-T05 | Điều kiện ★★★ màn 8–10 | Code | 0,2 | P12-T10 | ✅ smoke |
| P16-T06 | Lệnh DEV và chơi thử toàn bộ 10 màn | Công cụ | 0,6 | T01–T05 | ✅ smoke |
| P16-T07 | Harness `Level8to10PlayTest` | Test | 0,3 | T06 | ✅ 87/0 |

---

## P16-T01 — AI T3: đòn lệch nhịp, phục kích ở cửa, nghe tiếng động

- **Loại:** Code · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P12-T07
- **File:** (sửa) `EnemyDirector.cs`, `MinionBrain.cs`, `AITierProfile` T3
- **Các bước:**
  1. **Đòn lệch nhịp:** `EnemyDirector` xếp 2–3 quái tấn công lệch nhau 0,3–0,5 giây, để người chơi phải né liên tiếp.
  2. **Phục kích:** quái rảnh chờ gần các nút `Door` nằm trên hướng người chơi dự kiến đi qua (dùng `RoomGraph`).
  3. **Nghe tiếng động:** quái đăng ký `SoundEventBus.Emitted` (đã có trong `PlayerSoundEmitter.cs`). Khi nghe tiếng chạy nhanh hoặc tiếng kỹ năng trong tầm, quái chuyển sang truy đuổi.
- **Hoàn thành khi:**
  - [x] Ở T3, số đòn trúng cùng lúc vẫn nằm trong giới hạn vé (smoke cấp vé/đòn lệch nhịp).
  - [x] Có phục kích tại cửa.

## P16-T02 — AI T4 (MVP): chặn cửa khi Thiên Hỏa sắp phun

- **Loại:** Code · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P16-T01, P13
- **Các bước:**
  1. Khi `WarningStarted` và người chơi đang ở ngoài trời: tối đa 3 quái chạy tới các lối vào gần người chơi nhất (nút `Door` tầng trệt). Mỗi lối vào có tối đa 1 quái chặn.
  2. Bảo đảm luôn có đường thoát: không chặn quá 50% số lối vào trong bán kính 40 m quanh người chơi.
  3. Quái chặn cửa vẫn dính lửa nếu đứng ngoài trời (P13-T08).
  4. Các phần "thích nghi theo kỹ năng" và né 60% để P19.
- **Hoàn thành khi:**
  - [x] Người chơi luôn còn ít nhất một lối vào không bị chặn (luật và smoke 3/8 lối).
  - [x] Quái chặn cửa tạo được áp lực thật (AI chạy/giữ cửa, guard và chịu damage Thiên Hỏa; độ khó chưa đo thực chiến).

Luật MVP: đếm Door/Exit `/approach` tầng trệt ngoài trời/bán che, nối với shelter, có đường NavMesh hoàn chỉnh, trong 40 m; gộp nút trùng trong 3 m. Giữ lối gần nhất trống, mỗi lối khác tối đa một quái, số chặn ≤ `min(3, floor(n/2), n−1)`. Quái rảnh khác nhường vùng 4 m quanh lối giữ trống. Thích nghi và né 60% để P19; MVP né 40%.

## P16-T03 — Dữ liệu và cân bằng màn 8–10

- **Loại:** Dữ liệu · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P15
- **Các bước:**
  1. Đợt quái:
     - màn 8: 32 quái, 1 đợt;
     - màn 9: 2 đợt, 16 + 20;
     - màn 10: 3 đợt, 13 + 15 + 17.

     Bản MVP dùng Bạo Thi làm quái hệ Hỏa, thay cho Hỏa Linh.
  2. Gắn `SkyBeastScheduler`, `FireBreathProfile`, bầu trời `Inferno`/`RedEclipse`.
  3. Chơi thử ở đúng cảnh giới đề nghị. So với §16 và §4.3:
     - thời lượng màn 12 / 15 / 18 phút ±30%;
     - Thiên Hỏa lấy khoảng 56% / 64% / 73% máu nếu đứng ngoài trời.
- **Hoàn thành khi:**
  - [ ] Cả 3 màn nằm trong khoảng mục tiêu — **chưa đo thực chiến**, theo [TEST-POLICY](TEST-POLICY.md).
  - [x] Ghi chú lưu vào `Artifacts/Levels/Balance-8-10.md`.

## P16-T04 — Đổi kỹ năng trong lúc nghỉ giữa đợt (màn 9–10)

- **Loại:** UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P09-T04
- **Các bước:**
  1. Trong 15 giây nghỉ ở màn 9–10: hiện nút "Đổi kỹ năng". Mở panel thu gọn của màn Chuẩn Bị (chỉ 4 ô kỹ năng).
  2. Mở panel thì **tạm dừng** đồng hồ nghỉ; đóng thì chạy tiếp.
  3. Kỹ năng vừa đổi vào bắt đầu ở trạng thái sẵn sàng; kỹ năng bị gỡ ra giữ nguyên hồi chiêu.
- **Hoàn thành khi:**
  - [x] Đổi được kỹ năng giữa đợt (PC/mobile, giữ/chạy tiếp đồng hồ màn và Thiên Hỏa, ready/cooldown, TextAudit 0).
  - [x] Màn 1–8 không có nút này.

## P16-T05 — Điều kiện ★★★ màn 8–10

- **Loại:** Code · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P12-T10
- **Các bước:**

| Màn | Điều kiện | Loại điều kiện |
|:-:|---|---|
| 8 | Không trúng Thiên Hỏa lần nào khi ở ngoài trời | `NoOutdoorFireHits` |
| 9 | Triệu hồi Thiên Kiếm trong 10 giây sau khi Kiếm Ý đầy, cả 2 lần | `SwordWithin` |
| 10 | Ít nhất 3 lần Tương Sinh Liên Hoàn | `ChainCount` |

- **Hoàn thành khi:**
  - [x] Mỗi điều kiện có test đạt và test không đạt; sự kiện thật trong lượt màn 8–10 đều ghi sao thứ ba.

## P16-T06 — Lệnh DEV và chơi thử toàn bộ 10 màn

- **Loại:** Công cụ · **Ước lượng:** 0,6 ngày · **Phụ thuộc:** P16-T01 đến T05
- **File:** (mới) `Assets/Levels/Editor/V2DevCommands.cs` (chỉ biên dịch trong Editor)
- **Các bước:**
  1. Các lệnh trong menu `Campus Rift/V2/DEV - …`:
     - Đặt cảnh giới/tầng;
     - Cộng Linh Thạch;
     - Mở mọi màn;
     - Hạ hết đợt hiện tại;
     - Đầy Kiếm Ý;
     - Bật/tắt Thiên Hỏa;
     - Hồi đầy máu.
  2. Chơi thử liên tục màn 1 → 10, mỗi màn ở đúng cảnh giới đề nghị. Ghi lỗi và cảm nhận vào `Artifacts/V2/Playthrough-MVP.md`.
- **Hoàn thành khi:**
  - [x] Hoàn thành cả 10 màn không bị kẹt (một lượt DEV smoke, đúng cảnh giới đề nghị).
  - [x] Lỗi phát hiện đã được đưa thành task hoặc đã sửa; raw và nguyên nhân tại `p16/PROGRESS.md`.

## P16-T07 — Harness `Level8to10PlayTest`

- **Loại:** Test · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P16-T06
- **File:** (mới) `Assets/Levels/Validation/Level8to10PlayTest.cs`
- **Kiểm tra (dạng smoke, có kịch bản):**
  - Mỗi màn: đợt quái → Kiếm Ý → Thiên Kiếm → chuyển pha → thắng.
  - Màn 10: 3 cự thú theo đúng thứ tự; có Long Nộ trước nhát cuối.
  - Luật chặn cửa của AI không chặn quá 50% lối vào.
- **Hoàn thành khi:**
  - [x] PASS — `Level8to10PlayTest` 87/0; polish trực tiếp 12/0.

---

## Kiểm chứng cuối phase → Mốc 4

- [x] **Mốc 4 ✅ smoke:** chơi được 1→10 bằng DEV; HeavenSword 41/0, SkyBeast phiên Play sạch 58/0.
- [ ] Toàn bộ harness V2 và bộ hồi quy PASS — không chạy theo [TEST-POLICY](TEST-POLICY.md); không dùng smoke để xác nhận full regression/cân bằng.
- [x] Cập nhật trạng thái P16 và Mốc 4 trong `task/README.md`.
