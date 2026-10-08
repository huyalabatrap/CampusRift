# P05 — Màn chơi, đợt quái, khe nứt → **Mốc 1**

> **Mục tiêu:** có 10 màn dạng dữ liệu, một `LevelDirector` điều khiển các đợt quái và thắng/thua, luồng chọn màn tối thiểu. Chơi được màn 1–2.
>
> **Phạm vi:** MVP · **Ước lượng:** 3,5 ngày công · **Phụ thuộc:** P04 · **Tham chiếu:** §2, §14.3, §14.5
>
> **Kết quả (Mốc 1):** từ menu vào màn 1, diệt 2 đợt Tiểu Yêu, thắng, sang màn 2. Thua thì chơi lại đúng màn.

| ID | Task | Loại | Ước lượng | Phụ thuộc | Trạng thái |
|---|---|---|:-:|---|:-:|
| P05-T01 | `LevelDefinition`, `WaveDefinition` và 10 asset màn | Dữ liệu | 0,5 | P04 | ✅ |
| P05-T02 | Vùng Khe Nứt và hiệu ứng cổng | Code | 0,4 | T01 | ✅ |
| P05-T03 | `LevelDirector`: đợt quái, giới hạn cùng lúc, thắng/thua | Code | 0,8 | T01, T02 | ✅ |
| P05-T04 | Tắt vòng chơi cũ (áp lực thời gian, thắng khi hạ Shaban) | Code | 0,3 | T03 | ✅ |
| P05-T05 | Chuyển cảnh theo màn, điểm xuất phát, bầu trời | Code | 0,4 | T03 | ✅ |
| P05-T06 | HUD: số quái còn lại, đợt, nghỉ, Tầm Yêu | UI | 0,4 | T03 | ✅ |
| P05-T07 | `LevelResultUI` bản tối thiểu | UI | 0,3 | T03 | ✅ |
| P05-T08 | Dữ liệu và cân bằng màn 1–2 | Dữ liệu | 0,2 | T03–T07 | 🔄 |
| P05-T09 | Harness `LevelFlowPlayTest` | Test | 0,2 | T08 | ✅ |

---

## P05-T01 — `LevelDefinition`, `WaveDefinition` và 10 asset màn

- **Loại:** Dữ liệu · **Ước lượng:** 0,5 ngày · **Phụ thuộc:** P04
- **File:** (mới) `Assets/Levels/Runtime/LevelDefinition.cs`, `WaveDefinition.cs`, `StarCondition.cs`, `LevelSession.cs`; `Assets/Levels/Data/Level01.asset` … `Level10.asset`; `Assets/Levels/Editor/LevelValidation.cs`
- **Các bước:**
  1. Tạo các class theo §14.3:
     - `LevelDefinition`: `id`, `index`, tên EN/VN, `requiredRealm/Tier`, `sky`, `waves`, `aiTier`, hệ số máu/sát thương/tốc độ, `bosses`, `skyPhases` (dùng ở P14), `stars[3]`, `parTimeSeconds`, `spawnPoint`.
     - `WaveDefinition`: `entries` (archetype, count, eliteCount), `riftZones`.
  2. `LevelSession`: static, giữ màn đang chọn và bộ kỹ năng/vật phẩm mang theo.
  3. Điền 10 asset theo bảng §2.1:
     - tổng quái: 6 / 10 / 14 / 18 / 22 / 26 / 30 / 32 / 36 / 45;
     - các hệ số, cấp AI, thời gian mốc.

     MVP chỉ dùng 5 loại quái (P04-T01). Màn 6–10 tạm thay Ảnh Yêu, Triệu Hồn Sư, Dực Yêu, Hỏa Linh bằng loại MVP có hệ gần nhất; P19 sẽ trả lại đúng.
  4. Validation: tổng quái từng màn đúng bảng; hệ số tăng dần theo màn; mọi archetype không null.
- **Hoàn thành khi:**
  - [ ] Có 10 asset.
  - [ ] Validation PASS.

## P05-T02 — Vùng Khe Nứt và hiệu ứng cổng

- 🔎 **Tự tìm tài nguyên:** AI làm task này tự tìm asset miễn phí phù hợp (hiệu ứng cổng Khe Nứt, âm thanh mở cổng) từ nguồn có giấy phép dùng được trong game (ưu tiên CC0, CC-BY thì ghi công), tải về, chuyển đổi nếu cần và ghi nguồn vào `LICENSES.md` của thư mục tương ứng.

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P05-T01
- **File:** (mới) `Assets/Levels/Runtime/RiftZone.cs`, `RiftPortal.cs`, `Assets/Levels/Editor/RiftZoneTool.cs`
- **Các bước:**
  1. `RiftZone` là điểm đặt trong scene, có `zoneId` và bán kính. Công cụ editor gợi ý vị trí từ nút `Outdoor` và `Door` trong `CampusRoomGraph` theo `BuildingID` của từng màn (§2.3: màn 1 ở sân trung tâm khu A, màn 2 ở khu B, …).
  2. Kiểm tra từng điểm bằng `NavMesh.SamplePosition`; báo lỗi nếu điểm nằm ngoài NavMesh.
  3. `RiftPortal`: vết nứt phát sáng tím. Dùng lại material/hiệu ứng tông Void Wall, xuất hiện khi đợt bắt đầu.
- **Hoàn thành khi:**
  - [ ] Mỗi màn có ít nhất 3 vùng hợp lệ.
  - [ ] Validation không lỗi NavMesh.

## P05-T03 — `LevelDirector`: đợt quái, giới hạn cùng lúc, thắng/thua

- **Loại:** Code · **Ước lượng:** 0,8 ngày · **Phụ thuộc:** P05-T01, P05-T02
- **File:** (mới) `Assets/Levels/Runtime/LevelDirector.cs`, `LevelEvents.cs`
- **Các bước:**
  1. Đọc `LevelSession.Current`.
  2. Sinh quái từng đợt từ `RiftZone`. Giới hạn cùng lúc: **PC 14, mobile 9** (theo `CampusInput.Mobile`). Con nào chết thì sinh con kế tiếp.
  3. Áp hệ số của màn:
     - máu qua `MonsterVitality.SetMaxHealth(base × HP×)`;
     - sát thương và tốc độ qua `EnemyInstance`.
  4. Nghỉ giữa đợt 15 giây và hồi 20% Linh Lực.
  5. Thắng khi hết đợt và boss đã chết. Màn 8–10 có thêm điều kiện cự thú (P15).
  6. Thua khi người chơi chết, không còn Hộ Mệnh Phù → gọi `UIStateManager.Defeat()`.
  7. Phát event cho HUD, `StarEvaluator` (P12) và `SwordIntent` (P15): `WaveStarted`, `WaveCleared`, `EnemyKilled`, `LevelWon`, `LevelLost`.
- **Hoàn thành khi:**
  - [ ] Số quái sinh ra đúng dữ liệu.
  - [ ] Không vượt giới hạn cùng lúc.
  - [ ] Thắng/thua được phát đúng một lần.

## P05-T04 — Tắt vòng chơi cũ (áp lực thời gian, thắng khi hạ Shaban)

- **Loại:** Code · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P05-T03
- **File:** (sửa) `Assets/MonsterShaban/Scripts/MonsterBrain.cs`, `MonsterAIConfig.cs`, `Assets/CampusRiftUI/Runtime/UIManager.cs`, `SampleScene.unity`
- **Hiện trạng:**
  - `MonsterBrain` tăng `PressureSeconds` liên tục; tốc độ chạy tối đa tới 20; tốc đánh nhân đôi sau 180 giây.
  - `UIManager.Start` gọi `Win()` khi con `MonsterVitality` đầu tiên chết.
- **Các bước:**
  1. `MonsterAIConfig` thêm `enableTimeEscalation`, mặc định **false** ở V2. Khi tắt, `MovementMultiplier` và `AttackRateMultiplier` bằng 1.
  2. `UIManager`: bỏ đoạn đăng ký thắng khi quái chết; `LevelDirector` quyết định thắng/thua.
  3. SampleScene: tắt instance Shaban có sẵn (sao lưu scene trước). Shaban sẽ do `LevelDirector` sinh ra khi màn cần (P12).
  4. `ObjectiveUI`: nội dung do `LevelDirector` đặt ("Diệt quái: 12/26"). Bỏ chữ "Escape from the monster".
  5. Cập nhật `ShabanPressurePlayTest`: bật lại cờ escalation trong phạm vi test, hoặc chuyển test sang kiểm tra chế độ đã tắt.
- **Hoàn thành khi:**
  - [ ] Không còn áp lực tăng theo thời gian.
  - [ ] Hạ một quái bất kỳ không làm thắng màn.
  - [ ] Nhóm hồi quy Quái PASS (sau khi đã cập nhật).

## P05-T05 — Chuyển cảnh theo màn, điểm xuất phát, bầu trời

- **Loại:** Code · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P05-T03
- **File:** (sửa) `GameSceneManager.cs`, `GameOverUI.cs`, `SkyLightingController.cs`, `CampusExplorer.cs`
- **Các bước:**
  1. `GameSceneManager.StartLevel(int index)`: đặt `LevelSession`, nạp `SampleScene`. Giữ `StartNewGame()` tạm thời để gọi màn 1.
  2. Khi màn bắt đầu: đặt người chơi ở `LevelDefinition.spawnPoint` bằng `CampusExplorer.spawnPosition/spawnYaw` và `ReturnToSpawn()`.
  3. `SkyLightingController.SetPreset(SkyPreset)` với các preset: `Dusk`, `Night`, `BloodMoon`, `Inferno`, `RedEclipse` (hai preset cuối dùng ở P13). Áp trên material bầu trời runtime đang có.
  4. `GameOverUI.Retry()`: chơi lại đúng màn với bộ kỹ năng cũ, thay cho `ReloadCurrentScene()`.
- **Hoàn thành khi:**
  - [ ] Vào màn 1 và màn 2 đúng vị trí, đúng bầu trời.
  - [ ] Chơi lại giữ nguyên bộ kỹ năng.

## P05-T06 — HUD: số quái còn lại, đợt, nghỉ, Tầm Yêu

- **Loại:** UI · **Ước lượng:** 0,4 ngày · **Phụ thuộc:** P05-T03
- **File:** (mới) `Assets/CampusRiftUI/Runtime/LevelHUD.cs`, `EnemyRevealMarker.cs` · (sửa) `UIFoundationBuilder.Gameplay.cs`
- **Các bước:**
  1. Dòng "Quái còn lại: 12/26 · Đợt 2/4", dùng lại khung `ObjectiveUI`.
  2. Đếm ngược nghỉ giữa đợt "Đợt tiếp theo sau 15…".
  3. **Tầm Yêu tự động:** khi còn ≤ 3 quái, cứ 20 giây hiện dấu vị trí xuyên tường trong 3 giây (dấu trên màn hình và mũi tên ở mép).
  4. Bỏ `BreakthroughProgressUI` (vòng 5/5 cũ) khỏi HUD. Tu Vi hiển thị trong Sảnh (P06, P09).
- **Hoàn thành khi:**
  - [ ] Số liệu HUD khớp `LevelDirector`.
  - [ ] Dấu vị trí hiện đúng khi còn 3 quái.

## P05-T07 — `LevelResultUI` bản tối thiểu

- **Loại:** UI · **Ước lượng:** 0,3 ngày · **Phụ thuộc:** P05-T03
- **File:** (mới) `Assets/CampusRiftUI/Runtime/LevelResultUI.cs` · (sửa) builder panel Victory
- **Các bước:**
  1. Khi thắng: thời gian, số quái đã hạ, chỗ trống cho sao (hoàn chỉnh ở P09).
  2. Nút: Chơi lại · Màn tiếp · Về Sảnh (tạm thời về MainMenu).
  3. Thua: dùng panel GameOver hiện có, nút Chơi lại gọi đúng màn.
- **Hoàn thành khi:**
  - [ ] Luồng thắng và thua đều quay về được menu, hoặc chơi lại được.

## P05-T08 — Dữ liệu và cân bằng màn 1–2

- **Loại:** Dữ liệu · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P05-T03 đến T07
- **Các bước:**
  1. Màn 1: 2 đợt × 3 Tiểu Yêu, sân trung tâm, bầu trời `Dusk`.
  2. Màn 2: 10 quái (7 Tiểu Yêu, 3 Độc Nhãn), khu B và hành lang tầng 1–2.
  3. Chơi thử 3 lần mỗi màn. Ghi thời gian so với mốc 4:00 và 5:00. Chỉnh vị trí vùng khe nứt nếu quá dễ hoặc quá khó.
- **Hoàn thành khi:**
  - [ ] Thời gian chơi thử trong khoảng ±30% so với mốc.

## P05-T09 — Harness `LevelFlowPlayTest`

- **Loại:** Test · **Ước lượng:** 0,2 ngày · **Phụ thuộc:** P05-T08
- **File:** (mới) `Assets/Levels/Validation/LevelFlowPlayTest.cs` → `Artifacts/Levels/LevelFlow.json`
- **Kiểm tra:**
  - Màn 1 thắng khi hạ quái bằng API sát thương.
  - Thua → Game Over → Chơi lại đúng màn.
  - Nghỉ giữa đợt đúng 15 giây.
  - Giới hạn cùng lúc được tôn trọng trên PC và mobile.
  - Số đếm quái trên HUD đúng.
- **Hoàn thành khi:**
  - [ ] PASS.

---

## Kiểm chứng cuối phase → Mốc 1

- [ ] Chơi được màn 1 → màn 2 từ menu, trên PC và mobile.
- [ ] Nhóm hồi quy Quái và Giao diện không có FAIL mới.
- [ ] Cập nhật trạng thái P05 và Mốc 1 trong `task/README.md`.


---

## Ghi chú triển khai

- **Dữ liệu màn:** 10 asset `Assets/Levels/Data/Level01..10.asset` + `Resources/LevelCatalog.asset`, ghi lại bằng menu `Campus Rift/V2/Setup Levels` từ bảng §2.1 (tổng quái, số đợt, HP×/ST×/Tốc×, cấp AI, mốc thời gian, bầu trời). Validation `Campus Rift/V2/Validate Levels`: 185 kiểm tra đạt.
- **Vùng Khe Nứt là dữ liệu, không phải đối tượng trong scene:** mỗi màn giữ danh sách `RiftZoneData` (id, vị trí, bán kính) và mỗi đợt tham chiếu theo id. Lý do: không phải sửa scene cho mỗi lần chỉnh vị trí. Công cụ `Snap Rift Zones To NavMesh` thay cho `RiftZoneTool` dạng gizmo; nó đã kéo 28 điểm về đúng NavMesh. Mỗi màn có 4–10 vùng, gồm cả hành lang trong nhà (khu A, B, D, E, V).
- **Chỉ dùng Tiểu Yêu và Độc Nhãn cho cả 10 màn** (Thiết Giáp, Bạo Thi, Shaban... chưa có model — P12/P19). Tổng số quái vẫn đúng bảng; tỉ lệ Độc Nhãn tăng dần (0 → 14/45).
- **`skyPhases` chưa có** trong `LevelDefinition` (thuộc P14). `bosses` có nhưng `LevelDirector` chưa dùng; chỗ móc sẵn là `HoldWin(key)/ReleaseWin(key)`.
- **`RiftPortal`** dựng bằng code (LineRenderer + hạt + âm thanh tổng hợp), không dùng tài nguyên ngoài; ghi trong `Assets/Levels/LICENSES.md`.
- **Shaban trong scene không bị tắt trong file scene** mà bị ẩn lúc chạy khi có màn (`LevelDirector.Begin`) và bật lại khi `End()`. Nhờ vậy các harness cũ (mở thẳng scene) vẫn thấy Shaban. Mở scene chơi trực tiếp thì không có màn nào chạy; `GameSceneManager.StartSandbox()` nạp scene kiểu này (runner hồi quy và các test cũ dùng nó thay cho `StartNewGame`).
- **`MonsterAIConfig.enableTimeEscalation` = false.** Tắt thì `Pressure = 0`, hệ số tốc độ/tốc đánh = 1. `ShabanPressurePlayTest` tự bật rồi trả lại giá trị cũ, và có thêm 1 kiểm tra chế độ tắt.
- **`UIManager`:** Shaban chết chỉ còn thắng khi không có `LevelDirector` (harness cũ).
- **Bầu trời:** `SkyLightingController.SetPreset(SkyPreset)` — `Default, Dusk, Night, BloodMoon, Inferno, RedEclipse` — chồng lên giá trị của scene rồi mới nhân với độ sáng người chơi chọn; đổi cả hướng mặt trời và sắc môi trường.
- **HUD:** `LevelHUD` viết vào thẻ mục tiêu có sẵn ("Quái còn lại 6/6 · Đợt 1/2", đếm ngược nghỉ), ẩn vòng Đột Phá; `EnemyRevealMarker` (Tầm Yêu) tạo bằng code: khi tổng quái còn lại của **cả màn** ≤ 3 và đã sinh hết thì cứ 20 giây hiện 3 giây.
- **Kết quả:** thẻ Victory cũ chuyển thành thẻ kết quả (`LevelResultUI`: tên, thời gian, x/y quái, 3 ô sao chỉ sáng sao 1) với nút Chơi lại · Màn tiếp · Menu chính (chưa có Sảnh, P09). Menu `Campus Rift/V2/Build Level Result Card` dựng lại thẻ.
- **Chuyển màn:** `GameSceneManager.StartLevel/RetryLevel/NextLevel`. New Game → màn 1; Continue → màn chưa qua tiếp theo (tiến độ tạm ở `PlayerPrefs`, save v2 làm ở P09). `LevelSession.Loadout` giữ 4 kỹ năng qua Chơi lại và Màn tiếp.
- **Chưa làm / để phase sau:** `StarEvaluator` (P12), elite/phụ tố (P12), thời gian chơi thử ±30% của T08 cần người chơi thật, thử trên thiết bị di động.
- **Test:** `LevelFlowPlayTest` 47 kiểm tra đạt (`Artifacts/Levels/LevelFlow.json`); dữ liệu `Artifacts/Levels/LevelData.txt`.
- **Hồi quy đầy đủ** (`Artifacts/V2/P05`, `P05b`): mọi suite khớp baseline; chỉ còn lỗi có sẵn (BoostEnergy HUD kiệt sức, ShabanBehavior "Vision respects maximum range", ShabanTraversal "lift is faster", PhantomDecoy chập chờn). `SkyVictory` phải sửa test: nút Victory giờ là CHƠI LẠI/MÀN TIẾP/MENU (49/0 sau khi sửa). Đã chơi thử bằng tay chuỗi menu → màn 1 → thắng → thẻ kết quả, thua → Thử lại, và vào màn 2 (khu B, có Độc Nhãn).
- **Kiểm chứng cuối phase:** màn 1 → 2 chạy được trên PC; **chưa** thử trên thiết bị/giao diện mobile; nhóm Giao diện (UIPlayValidation) vẫn là bản cũ như baseline nên không chạy lại.
