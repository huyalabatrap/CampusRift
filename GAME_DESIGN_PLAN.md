# Campus Rift — Kế hoạch phát triển gameplay

> Tài liệu thiết kế & triển khai. Viết ngày 2026-09-28, dựa trên đọc code/config hiện tại (chưa kèm số liệu playtest).
> Mọi con số tuning dưới đây là **giá trị khởi điểm đề xuất**, cần kiểm chứng bằng Play Mode harness và playtest người thật.

---

## Mục lục

1. [Tầm nhìn & trụ cột thiết kế](#1-tầm-nhìn--trụ-cột-thiết-kế)
2. [Hiện trạng & các vấn đề cần giải quyết](#2-hiện-trạng--các-vấn-đề-cần-giải-quyết)
3. [Vòng lặp mục tiêu](#3-vòng-lặp-mục-tiêu)
4. [Giai đoạn 0 — Sửa nhanh](#4-giai-đoạn-0--sửa-nhanh-2-3-ngày)
5. [Giai đoạn 1 — Khép vòng chơi một lượt](#5-giai-đoạn-1--khép-vòng-chơi-một-lượt-2-tuần)
6. [Giai đoạn 2 — Hợp nhất Học & Chơi](#6-giai-đoạn-2--hợp-nhất-học--chơi-2-3-tuần)
7. [Giai đoạn 3 — Đa dạng nội dung](#7-giai-đoạn-3--đa-dạng-nội-dung-4-6-tuần)
8. [Giai đoạn 4 — Giữ chân dài hạn & LiveOps](#8-giai-đoạn-4--giữ-chân-dài-hạn--liveops)
9. [Cốt truyện](#9-cốt-truyện)
10. [Kinh tế & tiến trình](#10-kinh-tế--tiến-trình)
11. [Telemetry & chỉ số thành công](#11-telemetry--chỉ-số-thành-công)
12. [Kiểm thử](#12-kiểm-thử)
13. [Rủi ro](#13-rủi-ro)
14. [Các quyết định cần chốt](#14-các-quyết-định-cần-chốt)

**Phần II — Tham khảo thị trường & nâng cấp** *(bổ sung 2026-09-28)*

15. [Tham khảo: các game hot 2025–2026](#15-tham-khảo-các-game-hot-20252026)
16. [Xu hướng rút ra](#16-xu-hướng-rút-ra)
17. [Đề xuất nâng cấp chi tiết (U-01 → U-18)](#17-đề-xuất-nâng-cấp-chi-tiết-u-01--u-18)
18. [Ma trận ưu tiên & lộ trình tổng hợp](#18-ma-trận-ưu-tiên--lộ-trình-tổng-hợp)
19. [Định vị, giá, phát hành & tuân thủ](#19-định-vị-giá-phát-hành--tuân-thủ)
20. [Nguồn tham khảo](#20-nguồn-tham-khảo)

---

## 1. Tầm nhìn & trụ cột thiết kế

**Một câu:** *Roguelite kinh dị–học thuật — mỗi đêm Rift là một lượt sinh tồn 8–15 phút trong khuôn viên trường, nơi trả lời đúng là vũ khí và trả lời sai là tiếng động đánh thức quái vật.*

| Trụ cột | Ý nghĩa | Kiểm tra một tính năng mới bằng câu hỏi |
|---|---|---|
| **Tri thức có rủi ro** | Học không phải menu tách rời; kiến thức được dùng *dưới áp lực* trong thế giới game | "Tính năng này có làm việc học ảnh hưởng tới sống/chết không?" |
| **Bị săn, không bị giết vô lý** | Shaban thông minh nhưng công bằng: mọi mối đe dọa đều được báo trước | "Người chơi có hiểu vì sao mình chết không?" |
| **Mỗi đêm một khác** | Ngẫu nhiên có kiểm soát: bố cục mục tiêu, sự kiện, bộ kỹ năng | "Lượt thứ 20 có khác lượt thứ 2 không?" |
| **Tiến bộ thật** | Người chơi mạnh lên *vì* họ hiểu bài hơn, không vì grind | "Phần thưởng này đến từ kỹ năng/hiểu biết hay chỉ thời gian?" |

---

## 2. Hiện trạng & các vấn đề cần giải quyết

### 2.1 Vòng chơi hiện tại

```
MainMenu ──► SampleScene (1 map) ──► sống sót trước Shaban
   │              │                       │
   │              ├─ Sprint/Energy         ├─ Thắng: MonsterVitality = 0 (500 HP)
   │              ├─ Flashlight            └─ Thua: PlayerMonsterHealth = 0 (4–5 đòn × 25)
   │              ├─ Void Wall (20 charge)
   │              ├─ Phantom Decoy (cd 18s)
   │              └─ Giant Hand (khóa đến 5 breakthrough; 60 dmg, cd 24s, stun 5s)
   │
   └─ COURSES (menu, đóng băng game) ──► lesson ──► quiz ──► stat bonus / skill
```

### 2.2 Vấn đề (theo mức độ)

| ID | Mức | Vấn đề | Bằng chứng |
|---|---|---|---|
| P-01 | 🔴 | Objective hiển thị "Escape from the monster." nhưng không có lối thoát; thắng duy nhất = giết Shaban. `ObjectiveUI.SetObjective` không được gọi ở đâu. | `Assets/CampusRiftUI/Runtime/ObjectiveUI.cs`, `UIFoundationBuilder.Gameplay.cs:34`, `UIManager.cs:21` |
| P-02 | 🔴 | Người mới **không thể thắng**: chưa có Giant Hand thì không có nguồn damage, và game không nói điều đó. | `LearningSkillGate.Allows` |
| P-03 | 🔴 | Áp lực tăng không trần hiệu quả: chase `5.5 + 0.1/5s`, trần `MaxChaseSpeed = 20`. Player run 10 (11 khi max bonus) → sau ~275s Shaban nhanh hơn sprint. Attack rate ×2 ở 180s. Giết Shaban tối thiểu 9 hit × cd 24s ≈ **192s** nếu không trượt. Cửa sổ thắng rất hẹp và vô hình. | `MonsterBrain.cs:20-31, 99-102`, `MonsterAIConfig.asset` |
| P-04 | 🟠 | Courses mở được giữa Gameplay → thành nút pause khẩn cấp; tri thức không gắn với thế giới. | `UIStateManager.OpenCourse` cho phép từ `Gameplay` |
| P-05 | 🟠 | Nội dung cạn nhanh: 1 course, 5 lesson, 30 câu; survival gate tổng 60s. Retake không có giá trị. Speed cap +10% đạt ngay sau 5 bài. | `LearningEngine.Award`, `LearningCatalog.asset` |
| P-06 | 🟠 | Không có biến thể giữa các lượt (spawn, mục tiêu, sự kiện đều cố định). | — |
| P-07 | 🟡 | Void Wall 20 charge/lượt, 12s, 75 HP (3 đòn) → spam tường làm mất tension. | `VoidWallConfig.asset` |
| P-08 | 🟡 | Kết thúc lượt không có kết quả/điểm/phần thưởng → không có động lực "thêm ván nữa". | `GameOverUI.cs`, Victory panel |
| P-09 | 🟡 | AI nghe/đoán (hearing, belief particles, search) rất mạnh nhưng người chơi thiếu động từ stealth (trốn, khom, đánh lạc hướng bằng tiếng động) → phần lớn chiều sâu AI không được "chơi". | `MonsterHearing`, `MonsterBelief`, `PlayerSoundEmitter` |

### 2.3 Tài sản có thể tái sử dụng (không viết lại)

- `SoundEventBus.Publish(SoundEvent)` — kênh âm thanh ẩn danh; mọi nguồn tiếng động mới (terminal báo động, đồ ném) chỉ cần publish.
- `RoomGraph` / `RoomNode` (có `RoomID`, `BuildingID`, `FloorID`, `Kind` gồm `Zone`, `Exit`...) — dùng để chọn vị trí spawn mục tiêu, phân chương theo tòa nhà.
- `CampusInteractable` + `ContextInteraction` — nền tảng cho terminal, tủ trốn, cửa phòng an toàn.
- `MonsterWarningUI`, `ObjectiveUI`, `BreakthroughProgressUI`, Victory/GameOver panel, SkyVictory.
- `LearningEngine` (thuần logic, có store/versioning), `IQuestionGrader`, `LocalizationService`.
- Pattern Play Mode harness ghi `Artifacts/...` + `Temp/*-progress.txt`.

---

## 3. Vòng lặp mục tiêu

### 3.1 Vòng lặp lõi (trong một lượt, 8–15 phút)

```
Vào đêm Rift (seed + sự kiện đêm + loadout)
   │
   ▼
Khám phá ──► Tìm Mảnh Tri thức ──► Trả lời câu hỏi tại Terminal
   ▲               │                       │
   │               │              đúng ────┼──── sai
   │               │               │               │
   │               │        +Mảnh, +energy   Báo động (SoundEvent) +khóa terminal
   │               │               │               │
   │               └──── Pha áp lực tăng ◄─────────┘
   │                          │
   └──── Trốn / đánh lạc hướng / chiến đấu ◄── Shaban săn
                              │
               Đủ mảnh ──► CHỌN: Thoát (sân thượng) hoặc Phong ấn (boss)
                              │
                              ▼
                 Màn kết quả: hạng, Insight, ôn tập đến hạn
```

### 3.2 Vòng lặp meta (qua nhiều lượt & nhiều ngày)

```
Kết quả lượt ──► Insight + dữ liệu đúng/sai
      │
      ├──► Courses (MainMenu / Phòng an toàn): học bài mới → mở skill
      ├──► Ôn tập đến hạn (spaced repetition) → nâng cấp Thành thạo → nâng cấp skill
      ├──► Chọn loadout, tăng "Học kỳ" (độ khó)
      └──► Thử thách ngày / thành tựu / lore
```

---

## 4. Giai đoạn 0 — Sửa nhanh (2–3 ngày)

Mục tiêu: loại bỏ những chỗ gây hiểu lầm trước khi làm tính năng lớn.

| Task | Việc cụ thể | File | Tiêu chí hoàn thành |
|---|---|---|---|
| 0.1 | Objective phản ánh đúng điều kiện thắng hiện tại: nếu Giant Hand khóa → "Sống sót và học để mở Giant Hand (x/5)"; nếu đã mở → "Phong ấn Shaban (HP còn lại)". Gọi `SetObjective` từ `UIManager`/một `ObjectiveDirector` tối giản. Localize EN/VN. | `ObjectiveUI.cs`, `UIManager.cs`, `LocalizationCatalog` | Text đổi đúng khi unlock skill và khi Shaban mất máu |
| 0.2 | Trần áp lực: thêm `ChaseSpeedCap` = `playerRunSpeed − 0.8` (≈ 9.2) thay vì 20; hoặc tạm đặt `MaxChaseSpeed: 9` trong asset. | `MonsterAIConfig.asset` | Harness pressure: ở 600s, chase < run speed của player |
| 0.3 | Void Wall `charges: 20 → 6`, `lifetime: 12 → 9`. | `VoidWallConfig.asset` | `VoidWallPlayTest` cập nhật expectation, pass |
| 0.4 | Toast lần đầu vào gameplay khi skill còn khóa: "Giant Hand bị phong ấn — hoàn thành 5 bài ở COURSES để mở". | `GiantHandHUD` / `SkillSlotUI` | Hiện một lần mỗi phiên |
| 0.5 | Màn Game Over hiện thời gian sống sót lượt này + tổng survival hướng tới bài kế tiếp ("còn 12s để mở DFS"). | `GameOverUI`, `LearningService` | Số liệu khớp `Progress.survivalSeconds` |

---

## 5. Giai đoạn 1 — Khép vòng chơi một lượt (≈2 tuần)

### 5.1 `RunDirector` — bộ não của một lượt

**File mới:** `Assets/Gameplay/Runtime/RunDirector.cs` (namespace `CampusRift.Gameplay`).

Trách nhiệm:
- Tạo `RunContext` khi scene gameplay load: `seed`, danh sách `RunModifier`, loadout, số mảnh yêu cầu.
- Spawn Mảnh Tri thức, quản lý pha áp lực, mở lối thoát, phát sự kiện cho UI.
- Là nguồn duy nhất gọi `ObjectiveUI.SetObjective`.

```csharp
public sealed class RunContext
{
    public int Seed;
    public int FragmentsRequired;      // mặc định 5
    public int FragmentsCollected;
    public int Phase;                  // 0..3
    public List<RunModifier> Modifiers;
    public RunStats Stats;
}
public event Action<int> PhaseChanged;
public event Action<int,int> FragmentsChanged;   // collected, required
public event Action ExitOpened;
```

**Config:** `RunConfig` (ScriptableObject) chứa số mảnh, khoảng cách tối thiểu, bảng pha, phần thưởng.

### 5.2 Mảnh Tri thức & vị trí spawn

- Ứng viên = các `RoomNode` có `Kind == Zone` trong `RoomGraph`.
- Luật chọn (deterministic theo `seed`):
  - Cách spawn player ≥ **25 m** (đường đi, không phải chim bay nếu có thể dùng `RoomPathfinder` cost).
  - Hai mảnh cách nhau ≥ **20 m**.
  - Trải ít nhất **2 tầng** khác nhau (`FloorID`) → buộc dùng cầu thang/thang máy, tận dụng AI thang máy của Shaban.
  - Không đặt trong phòng an toàn.
- Mỗi mảnh = một **Terminal** (bảng đen/màn hình) — prefab `KnowledgeTerminal : CampusInteractable`.
- Mảnh tỏa ánh sáng nhẹ + âm thanh vo ve nhỏ trong bán kính 8 m để dễ tìm; HUD compass hiện hướng mảnh gần nhất **chỉ khi** đứng yên 2s (khuyến khích dừng lại quan sát → tạo tension).

### 5.3 Pha áp lực (thay cho tăng tốc vô hạn)

Pressure gắn với **tiến độ mục tiêu**, không chỉ thời gian. Trong mỗi pha vẫn có leo thang mềm theo thời gian để chống "cắm trại".

| Pha | Kích hoạt | Chase speed | Attack rate | Hành vi thêm |
|---|---|---|---|---|
| 0 — Tĩnh lặng | Bắt đầu | 5.5 | ×1.0 | Patrol chậm, `HearingThreshold` 0.12 |
| 1 — Cảnh giác | Nhặt mảnh thứ 1 | 6.5 | ×1.2 | Search radius +20% |
| 2 — Săn lùng | Mảnh thứ 3 | 7.5 | ×1.5 | Mỗi 45s nhận "linh cảm": `MonsterBelief` được một quan sát nhiễu (±10 m) về vị trí player |
| 3 — Rift mở | Đủ mảnh | 8.5 | ×1.7 | Linh cảm mỗi 20s (±6 m); đèn campus nhấp nháy |

- Leo thang mềm trong pha: `+0.05` speed mỗi 15s, trần = speed pha + 0.5. **Không bao giờ vượt `playerRunSpeed − 0.5`.**
- Mỗi lần lên pha: `MonsterWarningUI` pulse + tiếng gầm toàn map + dòng objective đổi → người chơi luôn biết.
- Triển khai: thêm `MonsterBrain.SetPressureProfile(PhaseProfile)` thay cho tính `MovementMultiplier` từ `PressureSeconds`. Giữ `PressureSeconds` cho telemetry.

### 5.4 Hai cách thắng

**A. Thoát (Escape)** — con đường mặc định, không cần skill:
- Đủ mảnh → cửa sân thượng / thang máy lên mái mở (`RoomNode.Kind == Exit`), đánh dấu trên HUD.
- Đứng trong vùng thoát **8s** (kênh "Mở Rift") — Shaban được thông báo vị trí ngay khi bắt đầu kênh → cao trào cuối.
- Kết thúc bằng Victory panel đổi text: "YOU ESCAPED" / "ĐÃ THOÁT KHỎI RIFT"; dùng lại SkyVictory.

**B. Phong ấn (Seal)** — con đường khó, thưởng ×1.5 Insight:
- Chỉ khả dụng ở pha 3 (đủ mảnh) — mảnh "nạp" Giant Hand.
- Shaban HP `500 → 300`; Giant Hand damage 60 → cần 5 hit. Ở pha 3, cd Giant Hand giảm còn 16s → tối thiểu ~64s chiến đấu.
- Hoặc: 3 "Dấu Phong ấn" đặt ở 3 góc map, phải dụ Shaban vào vùng dấu rồi dùng Giant Hand (chiến thuật hơn, dùng Phantom Decoy để dụ). → *Chọn một trong hai sau playtest.*

### 5.5 Phòng an toàn

- 2–3 phòng/map (`SafeRoom` component trên một `RoomNode`), cửa đóng được bằng tương tác.
- Cửa phòng an toàn giữ Shaban **30s** (Shaban đập cửa, có âm thanh + rung), sau đó vỡ; mỗi cửa dùng **1 lần/lượt**.
- **COURSES chỉ mở được từ MainMenu hoặc trong phòng an toàn** (sửa `UIStateManager.OpenCourse`: bỏ `Gameplay` khỏi danh sách; thêm cổng từ `SafeRoom`). Pause menu không còn nút Courses khi đang trong lượt (hoặc hiện nhưng khóa kèm giải thích).
- Học trong phòng an toàn vẫn đóng băng thời gian (giữ luật hiện tại) → phòng an toàn là "trạm nghỉ" có giới hạn số lần.

### 5.6 Kết quả lượt & điểm

**File mới:** `RunStats.cs`, `RunResultUI.cs` (tái dùng card style Victory/GameOver).

```csharp
public sealed class RunStats
{
    public float Duration;
    public int FragmentsCollected, CorrectAnswers, WrongAnswers;
    public int TimesSpotted;          // số lần MonsterBrain chuyển sang Chase
    public float DamageTaken;
    public int SkillsUsed, VoidWallsUsed, DecoysUsed, SealHits;
    public RunOutcome Outcome;        // Escaped, Sealed, Died, Quit
}
```

**Điểm & hạng:**
```
score = 100×fragments + 40×correct − 30×wrong − 20×spotted − 0.5×damageTaken
      + (Escaped ? 300 : 0) + (Sealed ? 500 : 0) + max(0, 600 − duration) × 0.5
S ≥ 1100 · A ≥ 850 · B ≥ 600 · C còn lại
```
Hiển thị: hạng, breakdown, Insight nhận, **"Bài cần ôn"** (lesson có câu sai trong lượt) với nút mở thẳng review.

### 5.7 Lượt hướng dẫn (Onboarding)

Lượt đầu tiên của save mới (`Progress` rỗng) chạy `TutorialRun`:
1. Map giới hạn 1 tầng, 2 mảnh, Shaban bắt đầu ở pha 0 và bị "xích" (patrol cố định) 60s đầu.
2. Terminal đầu tiên dùng câu hỏi từ BFS lesson kèm giải thích ngay.
3. Dạy lần lượt: sprint → trốn/khom (khi có ở GĐ3) → Void Wall → Phantom → Thoát.
4. Kết thúc → mở COURSES với gợi ý "Học BFS để có sức mạnh mới".

### 5.8 Tiêu chí hoàn thành Giai đoạn 1

- [ ] Người chơi mới (chưa học bài nào) **có thể thắng** bằng đường Thoát.
- [ ] Seed khác → vị trí mảnh khác, vẫn thỏa luật khoảng cách/tầng (harness chạy 100 seed).
- [ ] Không pha nào cho Shaban nhanh hơn player sprint.
- [ ] Mọi kết thúc lượt đều ra `RunResultUI` với số liệu đúng.
- [ ] Không mở được Courses giữa lượt ngoài phòng an toàn.
- [ ] Toàn bộ text mới có EN/VN.

---

## 6. Giai đoạn 2 — Hợp nhất Học & Chơi (≈2–3 tuần)

### 6.1 Knowledge Terminal (câu hỏi trong thế giới game)

**Luồng:**
1. Người chơi tương tác Terminal → UI state mới `UIState.Terminal`.
2. `Time.timeScale = 0.25` (không đóng băng hẳn) — Shaban vẫn di chuyển chậm, âm thanh bước chân vẫn nghe → tension.
3. Hiện **1 câu** + đồng hồ **20s thời gian thực** (`unscaledTime`).
4. **Đúng:** nhận mảnh, +25 energy, +1 Void Wall charge; âm thanh nhỏ.
5. **Sai hoặc hết giờ:** `SoundEventBus.Publish(new SoundEvent(pos, 4f, ...))` — to hơn HeavyLanding (3), nhỏ hơn Giant Hand (5); terminal khóa 25s; hiện giải thích đáp án ngắn (học từ sai lầm).
6. Rời terminal giữa chừng (bấm Back) = tính như sai nhưng không báo động? → **Không**: rời giữa chừng được, không phạt, nhưng câu hỏi đổi. Tránh trường hợp người chơi bị kẹt khi Shaban tới.

**Chọn câu hỏi** (`TerminalQuestionPicker`):
1. Ưu tiên câu thuộc lesson **đến hạn ôn tập** (xem 6.2).
2. Sau đó lesson đã mastered bất kỳ.
3. Nếu chưa mastered lesson nào: lesson đầu tiên của course mặc định (tag `difficulty ≤ 1`).
4. Không lặp câu trong cùng lượt; tránh câu vừa trả lời đúng ở lượt trước nếu còn lựa chọn.

**Mở rộng Engine** (không phá save cũ):
```csharp
// LearningEngine
public QuestionData PickTerminalQuestion(System.Random rng, ISet<string> usedThisRun);
public void RecordRecall(string lessonId, string questionId, bool correct, DateTime utcNow);
```

### 6.2 Thành thạo & ôn tập giãn cách (Spaced Repetition)

| Cấp | Điều kiện | Thưởng (áp lên skill của course) |
|---|---|---|
| 0 Chưa học | — | — |
| 1 Đồng | PASS lần đầu (luật hiện tại) | Breakthrough + stat như hiện tại |
| 2 Bạc | Ôn tập đạt sau ≥ **1 ngày** kể từ Đồng | Skill liên quan: cooldown −8% |
| 3 Vàng | Ôn tập đạt sau ≥ **3 ngày** kể từ Bạc | Hiệu ứng phụ (vd. Giant Hand stun +1s) |
| 4 Kim cương | Ôn tập đạt sau ≥ **7 ngày** kể từ Vàng | Cosmetic viền skill + Insight ×1.1 cho lượt chứa câu của lesson này |

- **Ôn tập** = quiz ngắn 3 câu (đạt ≥ 2/3) trong Courses **hoặc** tích lũy 3 câu đúng về lesson đó tại Terminal trong các lượt chơi → việc ôn diễn ra tự nhiên khi chơi.
- Trượt ôn tập: **không tụt cấp**, chỉ đặt lại hẹn ôn sau 1 ngày (tránh cảm giác bị phạt).
- Màn hình Courses có tab **"Đến hạn ôn"** + badge số lượng trên MainMenu.

**Thay đổi save (v1 → v2):**
```csharp
// LessonProgress thêm:
public int mastery;               // 0..4
public long masteredAtUtcTicks;   // lần lên cấp gần nhất
public long nextReviewUtcTicks;
public int recallCorrect, recallWrong;
```
- `JsonLearningStore`: thêm migration `version 1 → 2` (lesson `rewarded == true` → `mastery = 1`, `nextReview = now + 1 ngày`). Giữ `.bak` của bản v1 riêng (`learning-v1.migrated.bak`).
- Thời gian dùng **UTC** và chống chỉnh đồng hồ lùi: nếu `now < lastSeenUtc` thì dùng `lastSeenUtc`.

### 6.3 Cây kỹ năng theo môn & Loadout

**Nhánh đề xuất** (mỗi course = một nhánh, 3 tier skill mỗi nhánh):

| Course | Chủ đề nhánh | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|---|
| Algorithms (có) | Kiểm soát | Giant Hand Seal | Void Wall+ (tường nối nhau) | "Pathfinder": hiện đường đi ngắn nhất tới mảnh 5s |
| Vật lý | Di chuyển | Dash ngắn (loudness thấp) | Nhảy cao / leo | Giảm quán tính: đổi hướng sprint không mất tốc |
| Hóa học | Tiêu hao | Bom khói (chặn tầm nhìn 6s) | Bẫy dính (làm chậm 50%) | Chất phát sáng đánh dấu Shaban 20s |
| Văn–Sử | Đánh lừa | Phantom Decoy (có sẵn, chuyển vào gate) | Giọng vọng (tạo tiếng động từ xa) | "Ký ức": xem lại đường Shaban đi 10s qua |

- Tận dụng `CourseData.skillTiers` + `LearningSkillGate.bindings` hiện có.
- **Loadout:** trước lượt chọn **3 slot** từ các skill đã mở (MainMenu → PLAY → màn chuẩn bị). Lưu `LoadoutData` trong save. Skill không nằm trong loadout bị tắt trong lượt.
- Void Wall và Sprint là "cơ bản", không chiếm slot.

### 6.4 Dạng câu hỏi mới

Thêm qua `IQuestionGrader` + renderer tương ứng; type key trong `QuestionBankData`:

| Type key | Mô tả | Dùng tốt cho |
|---|---|---|
| `multi-choice` | Chọn nhiều đáp án | Khái niệm |
| `ordering` | Kéo thả sắp thứ tự | Thứ tự duyệt BFS/DFS, bước thuật toán |
| `numeric` | Nhập số (có sai số) | Độ phức tạp, tính toán |
| `code-trace` | Đọc đoạn code, chọn output | Lập trình |
| `graph-pick` | Chạm vào node trên đồ thị | BFS/DFS, shortest path |

**Puzzle trong thế giới game** (dùng `ordering`/`graph-pick`): "Mạch cửa" — sắp đúng thứ tự BFS để mở dãy cửa khóa tắt đường. Đặt 0–1 puzzle/lượt, thay thế một terminal.

### 6.5 Tiêu chí hoàn thành Giai đoạn 2

- [ ] Terminal hoạt động PC + mobile; timeScale 0.25 không làm hỏng energy/AI/elevator.
- [ ] Trả lời sai phát SoundEvent và Shaban điều tra đúng vị trí (harness đo thời gian Shaban tới terminal).
- [ ] Migration save v1 → v2 không mất tiến độ (test với save mẫu từ `Artifacts/Learning/Original*`).
- [ ] Lên cấp Bạc sau 1 ngày (test bằng đồng hồ giả lập — `ILearningClock`).
- [ ] Loadout lưu và áp dụng đúng qua scene reload.

---

## 7. Giai đoạn 3 — Đa dạng nội dung (≈4–6 tuần)

### 7.1 Động từ stealth cho người chơi

| Tính năng | Chi tiết | Tích hợp AI |
|---|---|---|
| **Khom (Crouch)** | Tốc độ 3, loudness bước chân 0.15 (Walk hiện tại 0.35), chiều cao collider giảm → nấp sau bàn | Thêm `PlayerSoundType.Crouch`; `MonsterPerception` giảm khoảng cách phát hiện 30% khi player khom trong vùng tối |
| **Chỗ trốn** | Tủ locker, gầm bàn giảng (`HideSpot : CampusInteractable`) | Nếu Shaban **thấy** player chui vào → kéo ra (1 đòn chắc chắn). Nếu không thấy → `MonsterSearch` thêm hide spot vào candidate với trọng số thấp; kiểm tra ngẫu nhiên. Mỗi hide spot bị kiểm tra sẽ "nhớ" (`NegativeEvidence`) |
| **Đồ ném** | Phấn / lon (nhặt trong map, tối đa 3) — tạo `SoundEvent` loudness 2.5 tại điểm rơi | Dùng thẳng `SoundEventBus` — AI đã xử lý tiếng động ẩn danh |
| **Tắt đèn** | Công tắc phòng → phòng tối, giảm tầm nhìn Shaban trong phòng | `MonsterPerception` đọc `LightZone` |

### 7.2 Sự kiện đêm (`RunModifier`)

ScriptableObject `RunModifier` với `Apply(RunContext)` / `Revert()`. Mỗi lượt random 1 (từ lượt thứ 3 trở đi).

| Sự kiện | Hiệu ứng | Thưởng |
|---|---|---|
| Mất điện | Tắt đèn chung, chỉ còn đèn pin; pin đèn giới hạn | Insight ×1.2 |
| Phong tỏa | Thang máy tắt, chỉ cầu thang | ×1.1 |
| Mưa giông | Bước chân nhỏ hơn 40%, sấm định kỳ che tiếng động | ×1.0 |
| Đêm thi | Câu hỏi độ khó +1, thời gian trả lời 15s | ×1.3 |
| Tiếng vọng | Shaban nghe tốt hơn 50% | ×1.25 |
| Sương mù | Tầm nhìn cả hai bên giảm 50% | ×1.15 |

### 7.3 "Học kỳ" — độ khó tùy chọn (kiểu Heat của Hades)

- Mở sau khi Thoát lần đầu. Học kỳ 1 → 10, mỗi cấp thêm một luật vĩnh viễn cho lượt đó (cộng dồn):
  1. +1 mảnh yêu cầu · 2. Terminal khóa lâu hơn · 3. Pha 1 bắt đầu ngay · 4. Không phòng an toàn thứ 3 · 5. Luôn có 1 sự kiện đêm · 6. Void Wall 4 charge · 7. Shaban đi thang máy nhanh hơn · 8. 2 sự kiện đêm · 9. HP player −20% · 10. Quái thứ hai (7.4).
- Mỗi cấp +10% Insight, có huy hiệu riêng trên hồ sơ.

### 7.4 Quái mới (tái dùng module AI)

| Quái | Cơ chế | Module dùng lại | Buộc người chơi |
|---|---|---|---|
| **Kẻ Nghe (The Listener)** | Mù, nghe cực tốt (threshold 0.05), chạy nhanh khi nghe rõ | `MonsterHearing`, `MonsterBelief`, `MonsterSearch`; tắt `MonsterPerception` vision | Khom, dùng đồ ném |
| **Bóng Gương** | Chỉ di chuyển khi **không** nằm trong view camera người chơi | `MonsterNavigation`, `CampusNavGraph` | Quản lý góc nhìn, đi lùi |
| **Giám Thị** | Xuất hiện ở terminal sau 2 lần trả lời sai liên tiếp, tuần tra khu vực đó 60s | `MonsterBrain` config riêng | Trả lời cẩn thận |

Mỗi chương (7.5) có một quái chính; Học kỳ 10 cho phép ghép hai quái.

### 7.5 Chương theo tòa nhà

- Tách campus theo `RoomNode.BuildingID` thành các chương, mỗi chương gắn 1 course và 1 quái:
  - **Chương 1 — Tòa CNTT:** Algorithms, Shaban (hiện có).
  - **Chương 2 — Thư viện:** Văn–Sử, Kẻ Nghe (thư viện = im lặng, cực hợp).
  - **Chương 3 — Phòng Lab:** Hóa học, Bóng Gương (kính, gương, phản chiếu).
  - **Chương 4 — Sân vận động/Khu thể chất:** Vật lý, quái di chuyển nhanh.
  - **Chương cuối — Trung tâm Rift:** tất cả môn, boss nhiều pha.
- Mở chương kế: Thoát chương trước **và** đạt Đồng ≥ 3 lesson của course chương đó.
- Có thể dùng scene additive theo tòa nhà để giảm tải mobile.

### 7.6 Tiêu chí hoàn thành Giai đoạn 3

- [ ] Crouch + hide spot + đồ ném có harness riêng (Shaban phản ứng đúng với tiếng ném, kiểm tra hide spot).
- [ ] 6 sự kiện đêm, mỗi cái có test apply/revert không rò trạng thái qua lượt sau.
- [ ] Ít nhất 1 quái mới + 1 chương mới + 1 course mới (5 lesson / ≥ 30 câu, EN/VN).

---

## 8. Giai đoạn 4 — Giữ chân dài hạn & LiveOps

| Tính năng | Chi tiết | Phụ thuộc |
|---|---|---|
| **Thử thách ngày** | Seed = ngày UTC; cùng map, cùng sự kiện, cùng loadout cố định cho mọi người; 1 lần tính điểm/ngày | Offline được; bảng xếp hạng cần backend |
| **Bảng xếp hạng** | Theo điểm thử thách ngày, theo tuần, theo Học kỳ cao nhất | Unity Gaming Services Leaderboards hoặc PlayFab |
| **Thành tựu** | ~40 thành tựu: "Thoát không bị phát hiện", "Thắng không dùng Void Wall", "10 câu đúng liên tiếp", "Kim cương toàn course Algorithms"... | Local trước, sau đó Google Play Games / Steam |
| **Chuỗi ôn tập** | Đếm ngày liên tiếp có ôn tập; **không phạt** khi mất chuỗi, có 1 "ngày nghỉ" miễn phí/tuần | 6.2 |
| **Course cộng đồng** | Import JSON/CSV câu hỏi → tạo `CourseData` runtime (`ScriptableObject.CreateInstance`), chạy `LearningContentValidation` ở runtime; chia sẻ bằng mã. Giáo viên tạo đề cho lớp mình | Schema JSON công khai; kiểm duyệt nếu có chia sẻ online |
| **Sự kiện mùa** | Mùa thi (tháng 6, 12): course chủ đề + modifier đặc biệt + cosmetic giới hạn thời gian | Remote config |
| **Cloud save** | Đồng bộ `learning-v2.json` | UGS Cloud Save; xung đột: lấy bản có `breakthroughs` cao hơn, merge mastery theo max |

**Nguyên tắc đạo đức:** không dark pattern (không thông báo gây lo lắng, không mất tiến độ khi bỏ ngày, không pay-to-win). Game học tập cần giữ uy tín với phụ huynh/giáo viên.

---

## 9. Cốt truyện

**Giả thuyết:** Đêm trước kỳ thi cuối, một thí nghiệm của CLB Khoa học Máy tính mở ra "Rift" — vết nứt nơi những kiến thức bị lãng quên hóa thành quái vật. Shaban là hiện thân của "sự quên": nó săn những ai còn nhớ. Chỉ tri thức được *hiểu thật* mới phong ấn được nó.

- **Kể chuyện gián tiếp:** ghi chú, tin nhắn điện thoại, băng ghi âm của giảng viên rải trong map (nhặt được, lưu vĩnh viễn trong "Nhật ký Rift"). Mỗi lượt 1–2 mảnh lore ngẫu nhiên.
- **Mở lore theo học:** một số ghi chú chỉ đọc được khi lesson liên quan đạt Bạc ("ghi chú viết bằng mã — cần hiểu DFS để giải").
- **Nhân vật:** 3–4 bạn học mắc kẹt; nhiệm vụ phụ hộ tống (NPC đi theo, tạo tiếng ồn) → giải cứu mở hội thoại và cosmetic.
- **Nhiều kết thúc:** Thoát (kết thường) · Phong ấn (kết đúng) · Phong ấn + cứu đủ bạn + Kim cương 1 course (kết thật — hé lộ nguồn gốc Rift, mở chương cuối).

---

## 10. Kinh tế & tiến trình

### 10.1 Tiền tệ

| Tên | Kiếm từ | Tiêu vào |
|---|---|---|
| **Insight** | Mỗi lượt: `score / 10` × hệ số sự kiện × hệ số Học kỳ | Cosmetic, mở rộng slot loadout (3 → 4), lore chưa nhặt, skin đèn pin |
| **Breakthrough** (có sẵn) | PASS lần đầu | Stat + skill theo tier (giữ nguyên luật) |
| **Mastery** | Ôn tập giãn cách | Nâng cấp skill (6.2) |

**Không** dùng Insight mua sức mạnh chiến đấu trực tiếp → sức mạnh luôn đến từ học.

### 10.2 Nhịp tiến trình mục tiêu

| Mốc | Thời điểm mong muốn | Người chơi đạt được |
|---|---|---|
| Lượt đầu tiên | 0–10 phút | Tutorial, hiểu terminal, thoát được |
| Mở Giant Hand | 45–90 phút (buổi đầu) | 5 lesson Algorithms |
| Phong ấn Shaban lần đầu | Ngày 1–2 | Đường thắng thứ hai |
| Bạc đầu tiên | Ngày 2 | Lý do quay lại ngày hôm sau |
| Chương 2 | Ngày 3–5 | Course + quái + map mới |
| Học kỳ 5 | Tuần 2–3 | Người chơi nòng cốt |
| Kết thật | Tuần 3–6 | Hoàn thành cốt truyện |

### 10.3 Stat cap
Giữ cap hiện tại (`speed +10%, HP +100%, energy +50%`) nhưng phân bổ theo **toàn bộ** các course (ví dụ mỗi lesson +1% HP thay vì +5%) để tiến trình trải đều khi có 4–5 course.

---

## 11. Telemetry & chỉ số thành công

**Giai đoạn đầu:** ghi local `Application.persistentDataPath/telemetry/*.jsonl` (có thể tắt trong Settings). Sau này gửi UGS Analytics nếu người chơi đồng ý.

| Sự kiện | Trường |
|---|---|
| `run_start` | seed, chapter, semester, modifiers, loadout |
| `run_end` | outcome, duration, stats (RunStats), score, grade |
| `terminal_answer` | lessonId, questionId, correct, responseMs, phase |
| `player_death` | vị trí, phase, nguồn damage, skill còn sẵn |
| `lesson_pass` / `review_pass` | lessonId, attempt, percent, mastery |
| `session` | độ dài, số lượt |

**KPI mục tiêu (sau soft launch):**
- D1 ≥ 35%, D7 ≥ 12%, D30 ≥ 5%.
- Thời lượng phiên trung vị 15–25 phút; 2+ lượt/phiên.
- ≥ 60% người chơi hoàn thành lượt tutorial; ≥ 40% mở Giant Hand trong ngày 1.
- Tỷ lệ đúng tại terminal 60–80% (thấp hơn → đề quá khó; cao hơn → quá dễ, nâng difficulty).
- **Heatmap vị trí chết** để phát hiện chỗ Shaban bất công (chết mà không có cảnh báo).

---

## 12. Kiểm thử

Theo pattern hiện có (harness Play Mode ghi `Artifacts/<Feature>/*.json`, `Temp/*-progress.txt` kết thúc bằng `DONE`; gọi `UIStateManager.EnterScene(true)` và `Application.runInBackground = true` khi chạy qua MCP).

| Harness mới | Kiểm tra |
|---|---|
| `RunDirectorPlayTest` | 100 seed: số mảnh, khoảng cách, phân bố tầng; không mảnh nào ở phòng an toàn / ngoài NavMesh |
| `PhasePressurePlayTest` | Chuyển pha đúng theo số mảnh; chase speed luôn < player run speed; warning UI bật |
| `TerminalPlayTest` | Đúng → mảnh/energy/charge; sai → SoundEvent + Shaban tới trong ≤ X giây; timeScale khôi phục đúng khi rời; mobile touch |
| `EscapePlayTest` | Kênh thoát 8s, bị đánh gián đoạn, Victory → RunResult |
| `SafeRoomPlayTest` | Cửa giữ 30s, Courses chỉ mở trong phòng, dùng 1 lần |
| `LearningMigrationTest` | v1 → v2, backup, mastery khởi tạo đúng |
| `SpacedRepetitionTest` | `ILearningClock` giả lập: Đồng → Bạc → Vàng → Kim cương; đồng hồ lùi |
| `StealthPlayTest` | Crouch loudness, hide spot bị thấy/không thấy, đồ ném dụ Shaban |
| `RunModifierPlayTest` | Apply/revert từng modifier, không rò sang lượt sau |

Cập nhật harness cũ bị ảnh hưởng: `ShabanPressurePlayTest` (pressure theo pha), `VoidWallPlayTest` (charge), `LearningPlayTest` (Courses không mở từ Gameplay).

**Playtest người thật:** sau GĐ1 và sau GĐ2, mỗi lần 5–8 người (gồm học sinh/sinh viên thật), quan sát không hướng dẫn; ghi lại: lần đầu hiểu mục tiêu sau bao lâu, chết lần đầu vì sao, có muốn chơi lại ngay không.

---

## 13. Rủi ro

| Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|
| Terminal chậm thời gian (0.25) làm lỗi AI/thang máy/energy vốn giả định timeScale 0 hoặc 1 | Cao | Test riêng elevator + Shaban riding trong Terminal state; nếu rủi ro, dùng timeScale 0 + giới hạn thời gian thật |
| Người chơi thấy học là "việc vặt" chen ngang chơi | Cao | Câu hỏi ngắn, có thời gian, có phần thưởng tức thời; không bắt buộc học lesson mới để thắng đường Thoát |
| Nội dung học tốn công viết (mỗi course ≥ 30 câu × 2 ngôn ngữ) | Cao | Công cụ import CSV; course cộng đồng; ưu tiên chất lượng câu hơn số lượng |
| Randomize mảnh tạo lượt quá khó/dễ | Trung bình | Ràng buộc khoảng cách + kiểm tra độ dài đường đi tổng; lưu seed "xấu" từ telemetry để loại |
| Save migration làm mất tiến độ | Cao | Backup riêng trước migrate; test với save thật; không xóa v1 |
| Mobile: map lớn + nhiều quái → tụt FPS | Trung bình | Chương = scene additive; profiler trên thiết bị thật trước GĐ3 |
| Stealth làm Shaban "ngu" đi (trốn quá an toàn) | Trung bình | Hide spot có giới hạn dùng; Shaban kiểm tra ngẫu nhiên; bị thấy khi chui vào = bị lôi ra |

---

## 14. Các quyết định cần chốt

1. **Đường Phong ấn:** giảm HP Shaban (300 HP / 5 hit) hay cơ chế "Dấu Phong ấn" dụ quái vào vùng? (5.4)
2. **Terminal:** timeScale 0.25 (căng thẳng hơn, rủi ro kỹ thuật) hay 0 + giới hạn thời gian thật (an toàn)? (6.1)
3. **Courses giữa lượt:** chỉ phòng an toàn (đề xuất) hay bỏ hẳn khỏi gameplay?
4. **Độ dài lượt mục tiêu:** 8–10 phút (hợp mobile) hay 12–15 phút (hợp PC)?
5. **Backend:** có dùng dịch vụ online (leaderboard, cloud save) không, và dùng nhà cung cấp nào?
6. **Đối tượng chính:** học sinh THPT, sinh viên CNTT, hay người chơi kinh dị nói chung? — quyết định độ khó nội dung và thứ tự course.
7. **Co-op:** có đưa multiplayer 2–4 người vào lộ trình không (U-07)? Đây là quyết định kiến trúc — nếu có, cần bỏ giả định "một player" ngay từ Giai đoạn 1.
8. **Chế độ Lớp học (U-12):** có nhắm tới giáo viên/trường học không? Nếu có, cần mức kinh dị "nhẹ" và công cụ tạo đề.
9. **Nền tảng ưu tiên:** Mobile Việt Nam trước (F2P, phiên ngắn) hay PC/Steam trước (giá rẻ, streamer)? — ảnh hưởng thứ tự U-13/U-15/U-16.

---

## Phụ lục A — Tóm tắt thay đổi code theo giai đoạn

| Giai đoạn | File mới | File sửa |
|---|---|---|
| 0 | — | `ObjectiveUI`, `UIManager`, `MonsterAIConfig.asset`, `VoidWallConfig.asset`, `GameOverUI`, `LocalizationCatalog` |
| 1 | `Gameplay/Runtime/RunDirector`, `RunContext`, `RunConfig`, `RunStats`, `RunResultUI`, `KnowledgeFragment`, `SafeRoom`, `EscapeZone`, `TutorialRun` | `MonsterBrain` (pressure profile), `UIStateManager` (Course gate), Victory/GameOver panel, `UIFoundationBuilder.*` |
| 2 | `KnowledgeTerminal`, `TerminalUI`, `TerminalQuestionPicker`, `ILearningClock`, `LoadoutData`, `LoadoutUI`, các `IQuestionGrader` mới | `LearningEngine`, `LearningProgress` (v2), `JsonLearningStore` (migration), `LearningUI` (tab ôn tập), `UIStateManager` (`UIState.Terminal`), `LearningSkillGate` |
| 3 | `HideSpot`, `Throwable`, `LightZone`, `RunModifier` + 6 asset, quái mới (config + prefab), scene chương | `CampusExplorer` (crouch), `PlayerSoundEmitter` (Crouch type), `MonsterPerception`, `MonsterSearch` |
| 4 | `DailyChallenge`, `AchievementService`, `CourseImporter`, `TelemetryService`, cloud save adapter | `ILearningStore` implementations, MainMenu UI |

---
---

# PHẦN II — Tham khảo thị trường & đề xuất nâng cấp

> Bổ sung 2026-09-28 sau khi khảo sát các game đang hot (Steam, Roblox, mobile, ed-tech). Số liệu thị trường lấy từ báo chí/wiki/blog ngành (xem [mục 20](#20-nguồn-tham-khảo)); một số con số (đặc biệt về Duolingo, Roblox CCU) đến từ nguồn thứ cấp — dùng để định hướng, **không** dùng làm cam kết KPI.

## 15. Tham khảo: các game hot 2025–2026

### 15.1 Kinh dị & sinh tồn

| Game | Nền tảng | Số liệu nổi bật | Cơ chế cốt lõi | Bài học cho Campus Rift |
|---|---|---|---|---|
| **DOORS** | Roblox | Thuộc nhóm horror top đầu Roblox 2026 | Đi qua từng phòng đánh số; mỗi thực thể có **dấu hiệu riêng** (đèn nhấp nháy, tiếng hét) và **cách khắc chế riêng** (Rush → trốn tủ; Ambush quay lại nhiều lần → phải ra/vào tủ) | Mối đe dọa phải **đọc được và học được**. Tạo "bộ sưu tập thực thể Rift" có tín hiệu + cách khắc chế |
| **Dandy's World** | Roblox | Cùng 99 Nights & Forsaken đạt >400k CCU/ngày cộng lại | Tìm 4–20 **máy**, rút Ichor bằng **skill check QTE** — trượt thì **báo động quái**; xong hết máy → **Panic 30s chạy tới thang máy**; 40+ nhân vật với 6 chỉ số + kỹ năng; trinket; shop Ichor; tầng mất điện, sàn băng | **Gần như trùng ý tưởng Terminal** (GĐ2) → xác nhận hướng đi đúng. Bổ sung: pha chạy thoát có đồng hồ, roster nhân vật, trinket, modifier tầng |
| **99 Nights in the Forest** | Roblox | ~26 tỷ lượt truy cập (04/2026); tăng trưởng bền, không "viral rồi chết" | Ngày thu thập – đêm sống sót; nâng cấp trại; **tìm trẻ em mất tích**; chọn class; thua = tập hợp lại, đổi class, thử lại | Mục tiêu vĩ mô dài hạn (99 đêm) + nhiệm vụ giải cứu + chọn class tạo lý do chơi lại |
| **R.E.P.O.** | PC (2025) | 230k CCU cuối tuần ra mắt; giá ~$8 | Map ngẫu nhiên; khuân đồ có giá trị ra điểm rút — **va đập làm mất giá trị**; shop nâng cấp giữa màn; tối đa 6 người; vật lý | "Giá trị bị rủi ro trong lúc mang về" → tạo căng thẳng; giá rẻ; hỗn loạn co-op sinh khoảnh khắc hài |
| **Lethal Company / PEAK** | PC | Lethal: 271k CCU, 1 dev. PEAK: 1 triệu bản/6 ngày, 5 triệu < 1 tháng, làm trong game jam 4 tuần, < $5 | "Friendslop": co-op, voice chat theo khoảng cách, mỗi ván tạo ra clip | Co-op + clip-ability là động cơ lan truyền mạnh nhất 2023–2026; phạm vi nhỏ vẫn thắng lớn |
| **Phasmophobia** | PC/Console | ~22 triệu bản; thuộc top best-seller | Thu thập bằng chứng → **suy luận** loại ma; thiết bị theo tier; nhận diện giọng nói; **độ khó tùy chỉnh có hệ số thưởng** | Học = *áp dụng kiến thức để suy luận*, không chỉ trắc nghiệm. Độ khó tùy chỉnh ↔ hệ "Học kỳ" |
| **Blue Prince** | PC/Console (2025) | Game được đánh giá cao nhất 2025 | Dinh thự reset mỗi ngày; mở cửa → **chọn 1 trong 3 phòng**; puzzle cài trong phòng (bảng phi tiêu = bài toán); **tiến trình nằm trong đầu người chơi** | Kiến thức người chơi chính là tiến trình → "aha moment". Draft phòng 1-trong-3 = biến thể ngẫu nhiên có quyết định |
| **Alien: Isolation** | PC/Console | Tham chiếu kinh điển về AI kinh dị | "Hai bộ não": director biết vị trí player, chỉ **gợi ý hướng**; **menace gauge** đo độ căng, đủ cao thì cho quái rút "hậu trường" | AI Shaban rất mạnh nhưng **thiếu nhịp** — cần director giữ căng thẳng ở mức vừa, có lúc thở |
| **Dead by Daylight** | Đa nền tảng | Chuẩn mực asymmetric horror | Vòng chạy quanh chướng ngại (loop), **pallet giới hạn** vs **cửa sổ vô hạn** | Rượt đuổi cần địa hình cho người chơi thể hiện kỹ năng, và tài nguyên cạn dần |
| **Poppy Playtime** | PC/Mobile | Chương 1: >10 triệu lượt cài Google Play | Phát hành **theo chương**; chương 1 miễn phí trên mobile; nhân vật phản diện kiểu mascot | Shaban có thể là mascot nhận diện; mô hình chương hợp với nhóm nhỏ |

### 15.2 Roguelite & tiến trình

| Game | Bài học |
|---|---|
| **Hades / Hades II** | Cốt truyện tiến lên *sau mỗi lần chết*; hub lớn dần như một nơi có thật; meta-currency đơn giản, dễ hiểu |
| **Rogue Legacy 2** | Meta-progression **hiện hình**: mỗi nâng cấp xây thêm một phần lâu đài → người chơi *thấy* mình tiến bộ |
| **Brotato / DRG: Survivor** | Meta-upgrade mở rộng *khả năng build*, không chỉ tăng chỉ số |

### 15.3 Học tập & thói quen

| Sản phẩm | Số liệu (nguồn thứ cấp) | Cơ chế | Bài học |
|---|---|---|---|
| **Duolingo** | ~47.7 triệu DAU (2025); >10 triệu người có streak ≥ 365 ngày | Streak, XP, league tuần, nhắc nhở đúng lúc, streak freeze | Thói quen hằng ngày là trục giữ chân; cần *mềm* (freeze) để không gây áp lực độc hại |
| **Gimkit / Blooket / Kahoot** | Top 3 công cụ ôn bài trong lớp học Mỹ; Blooket 25+ chế độ | **Trả lời đúng → kiếm tiền trong game → tiêu vào power-up**; giáo viên tạo bộ câu hỏi (kể cả bằng AI) | Vòng "đúng → tài nguyên → lợi thế" đã được kiểm chứng trong lớp học. Chế độ giáo viên là kênh phân phối riêng |

### 15.4 Thị trường Việt Nam

- ~58.5 triệu người chơi; mobile chiếm ưu thế; Free Fire MAX đứng đầu cả lượt tải và doanh thu.
- 2026: lượt tải nhiều thể loại giảm theo năm trong khi doanh thu tăng → cạnh tranh bằng **chất lượng & giữ chân**, không phải bằng lượt cài.
- Nhu cầu ôn thi (THPT, tiếng Anh, tin học) rất lớn — khoảng trống cho game giáo dục có *gameplay thật*.

---

## 16. Xu hướng rút ra

| # | Xu hướng | Bằng chứng | Áp dụng cho Campus Rift |
|---|---|---|---|
| T1 | **Co-op "friendslop" & clip-ability** | Lethal Company, R.E.P.O., PEAK | Thiết kế để mỗi ván có khoảnh khắc đáng quay clip (trả lời sai → jumpscare). Co-op là nâng cấp lớn nhất về lan truyền (U-07) |
| T2 | **Mối đe dọa đọc được** | DOORS, DbD | Mỗi thực thể có tín hiệu nghe/nhìn + cách khắc chế rõ ràng (U-03) |
| T3 | **Kỹ năng dưới áp lực, thất bại gây ồn** | Dandy's World | Terminal nhiều bước + skill check + báo động (U-02) |
| T4 | **Kiến thức là tiến trình** | Blue Prince, Phasmophobia | Puzzle thế giới mà đáp án nằm trong bài học; suy luận thực thể từ bằng chứng (U-08) |
| T5 | **Roster nhân vật có chỉ số** | Dandy's World, 99 Nights | Nhân vật sinh viên theo ngành, trinket (U-05, U-06) |
| T6 | **Director AI giữ nhịp** | Alien: Isolation | Menace gauge cho Shaban (U-01) |
| T7 | **Giá rẻ / F2P có đạo đức + nội dung do người dùng** | PEAK, R.E.P.O., Gimkit | Giá thấp trên PC, chương 1 miễn phí trên mobile, công cụ tạo đề cho giáo viên (U-12, U-16) |
| T8 | **Phiên ngắn + thói quen hằng ngày** | Duolingo, mobile VN | Ca trực đêm hằng ngày, streak mềm, league tuần (U-13) |

---

## 17. Đề xuất nâng cấp chi tiết (U-01 → U-18)

Mỗi nâng cấp ghi: **Cảm hứng · Thiết kế · Số liệu khởi điểm · Triển khai trong code · Ưu tiên/Công sức** (S ≤ 3 ngày, M ≤ 2 tuần, L ≤ 6 tuần, XL > 6 tuần).

### U-01 — Rift Director & Menace Gauge
- **Cảm hứng:** Alien: Isolation.
- **Thiết kế:** Một lớp đạo diễn nằm *trên* `MonsterBrain`, không thay thế nó. Director đo **Menace** (0–1) = mức căng thẳng người chơi đang chịu, và điều phối Shaban:
  - Menace cao kéo dài → ra lệnh **"hậu trường"**: Shaban rút về khu xa người chơi một thời gian, người chơi được thở, đi tìm mảnh.
  - Menace thấp kéo dài → **gợi ý hướng** (không phải vị trí chính xác) cho Shaban về khu vực người chơi.
  - Giới hạn số "đỉnh căng thẳng" mỗi pha để tránh mệt mỏi.
- **Số liệu khởi điểm:**
  - Menace tăng: +0.25/s khi Shaban nhìn thấy player; +0.1/s khi cách < 15 m không LOS; +0.05/s khi nghe được bước chân Shaban. Giảm: −0.04/s còn lại.
  - Menace ≥ 0.85 liên tục 20s → hậu trường 30–45s (không săn trừ khi player chạm mặt trực tiếp trong < 6 m).
  - Menace ≤ 0.2 liên tục 60s → gửi quan sát nhiễu ±12 m qua `MonsterBelief.ObserveSighting`.
  - Tối đa 3 đỉnh/pha; sau đỉnh thứ 3, cooldown hậu trường tăng lên 60s.
- **Triển khai:** `RiftDirector` trong `Assets/Gameplay/Runtime/` (cạnh `RunDirector`). Cần thêm vào `MonsterBrain` một API `SetBackstage(Vector3 retreatZone, float seconds)` và cờ `Backstage` được `Decide()` ưu tiên. Vị trí hậu trường chọn từ `RoomGraph` (xa nhất theo cost đường đi từ player, khác tầng nếu có). Log menace vào telemetry để vẽ đồ thị nhịp căng thẳng mỗi lượt.
- **Ưu tiên/Công sức:** P0 · M. Thay thế một phần "linh cảm" trong 5.3 — dùng chung một kênh gợi ý.

### U-02 — Terminal nhiều bước + skill check (mở rộng 6.1)
- **Cảm hứng:** Dandy's World (máy + QTE), R.E.P.O. (giá trị bị rủi ro).
- **Thiết kế:**
  - Mỗi Terminal cần **3 câu đúng** để đầy 100%; tiến độ **được giữ** khi rời đi → người chơi có thể bỏ chạy giữa chừng rồi quay lại.
  - Giữa các câu có **vòng "Tập trung"** (QTE nhẹ, 1 lần bấm đúng vùng): trúng vùng vàng → câu tiếp theo +5s thời gian; trượt → tiếng động nhỏ (loudness 1.5).
  - **Chuỗi đúng liên tiếp** trong lượt nhân Insight: ×1.0 → ×1.5 (tối đa ở chuỗi 10).
  - Trả lời đúng trong < 5s = **"Vàng"**: +1 charge kỹ năng tùy chọn.
  - Sau khi đầy mảnh cuối → **Panic Rift 45s** (thay cho kênh 8s ở 5.4): cửa thoát mở, Shaban biết vị trí, người chơi phải chạy tới lối thoát.
- **Số liệu:** câu sai = loudness 4; hết thời gian = loudness 4; QTE trượt = 1.5; khóa terminal 25s sau câu sai.
- **Triển khai:** `KnowledgeTerminal` giữ `int progress` (0–3); `TerminalUI` thêm `FocusRing` (UI Image fill + vùng ngẫu nhiên). Dùng `SoundEventBus.Publish`. Phần Panic nằm trong `RunDirector` (Phase 3 + timer), dùng lại `MonsterWarningUI`.
- **Ưu tiên/Công sức:** P0 · M (gộp vào GĐ2).

### U-03 — Bộ sưu tập thực thể Rift có tín hiệu & khắc chế
- **Cảm hứng:** DOORS (Rush/Ambush/Screech/Seek), Dandy's World (Twisteds).
- **Thiết kế:** Ngoài Shaban (kẻ săn chính), mỗi lượt có 1–3 **sự kiện thực thể** ngắn, xuất hiện theo lịch của Director. Mỗi thực thể có tín hiệu báo trước **≥ 2 giây**, cả nghe lẫn nhìn:

| Thực thể | Tín hiệu | Hành vi | Khắc chế | Liên hệ học tập |
|---|---|---|---|---|
| **Kiểm Tra Đột Xuất** | Chuông trường reo, loa phát "Kiểm tra!" | Câu hỏi hiện ngay trên HUD, 10s | Trả lời đúng → không sao; sai/không trả lời → Shaban biết vị trí | Ôn tập bắt buộc, bất ngờ |
| **Kẻ Lướt** | Đèn hành lang chập chờn 3s, tiếng gió rít | Lao dọc hành lang, xóa đèn | Vào phòng/trốn tủ trước khi nó đi qua | — |
| **Cô Giám Thị** | Tiếng thì thầm "suỵt" trong phòng tối | Xuất hiện sau lưng trong phòng tối | Quay lại chiếu đèn pin vào trong 2s | Khuyến khích dùng đèn, bật công tắc |
| **Bóng Bài Cũ** | Chữ trên bảng tự viết lại | Câu hỏi của terminal gần đó bị xáo đáp án | Trả lời dựa trên hiểu bài, không nhớ vị trí đáp án | Chống học vẹt |
| **Người Gác Thư Viện** (chương 2) | Tiếng lật sách | Săn theo tiếng động lớn | Đi khom, không chạy | — |

  - **Sổ tay Rift:** lần đầu gặp mỗi thực thể sẽ mở một trang (ảnh, tín hiệu, cách khắc chế, lore). Đây vừa là collection vừa là hướng dẫn.
- **Triển khai:** `RiftEntity` (abstract ScriptableObject + MonoBehaviour runtime) với `Telegraph()`, `Execute()`, `Resolve()`; `RiftEntityScheduler` trong Director quyết định khi nào (không trong 20s sau khi Shaban vừa đánh trúng; không chồng hai thực thể). Âm thanh dùng `MonsterAudio` pattern; đèn dùng `LightZone` (U-04/7.1).
- **Ưu tiên/Công sức:** P1 · M cho 2 thực thể đầu (Kiểm Tra Đột Xuất, Kẻ Lướt), sau đó S cho mỗi thực thể mới.

### U-04 — Thiết kế rượt đuổi (địa hình & tài nguyên)
- **Cảm hứng:** Dead by Daylight (loop, pallet, window).
- **Thiết kế:**
  - **Bàn/cửa sổ vượt được** (vô hạn): player vượt 0.6s; Shaban vượt 1.1s → tạo khoảng cách nếu chơi khéo.
  - **Kệ sách/tủ đẩy đổ** (mỗi cái 1 lần): làm choáng Shaban 2s nếu trúng, chặn đường vĩnh viễn → tài nguyên cạn dần, giống pallet.
  - **Vòng chạy** trong giảng đường lớn: đặt 2–3 cột + bàn để tạo loop an toàn tương đối, mỗi loop "mòn" dần (sau 3 vòng Shaban đổi hướng chặn đầu — tận dụng `InterceptionPlanner` có sẵn).
  - **Void Wall** trở thành "pallet cao cấp" (6 charge, xem GĐ0).
- **Triển khai:** `Vaultable` & `Topplable : CampusInteractable`; NavMeshLink cho Shaban vượt (cost cao hơn); cập nhật `MonsterBarrierTactics` để xử lý kệ đổ như chướng ngại cố định. Harness: `ChaseTerrainPlayTest` đo khoảng cách trước/sau khi vượt.
- **Ưu tiên/Công sức:** P1 · M.

### U-05 — Roster nhân vật sinh viên
- **Cảm hứng:** Dandy's World (Toons 6 chỉ số), 99 Nights (class).
- **Thiết kế:** 6 chỉ số (1–5 sao): **Máu · Tốc độ · Thể lực · Lén lút · Tư duy** (thời gian trả lời) · **Tập trung** (vùng QTE rộng). Mỗi nhân vật có 1 nội tại + 1 kỹ năng chủ động (không trùng skill học được).

| Nhân vật (ngành) | Nổi bật | Nội tại | Chủ động | Mở khóa |
|---|---|---|---|---|
| **Linh** — CNTT (hiện có, SchoolGirl) | Cân bằng | Terminal +3s | "Debug": gợi ý loại bỏ 1 đáp án sai (1 lần/lượt) | Mặc định |
| **Minh** — Thể thao | Tốc độ, Thể lực | Hồi thể lực +30% | Nước rút 4s không tốn năng lượng | Mặc định |
| **An** — Y khoa | Máu | Băng gạc hồi gấp đôi | Tự hồi 30 máu (cd 90s) | 300 Insight |
| **Vy** — Mỹ thuật | Lén lút | Bước chân nhỏ hơn 25% | Vẽ "bóng giả" trên tường thu hút Shaban | 500 Insight + Đồng 3 lesson |
| **Khoa** — Báo chí | Tập trung | Chụp ảnh Shaban → đánh dấu 5s | Đèn flash làm choáng 1.5s (cd 45s) | 700 Insight |
| **Hà** — Hóa học | Tư duy | Nhặt được nhiều vật phẩm hơn | Tạo bom khói (2 lần/lượt) | Hoàn thành course Hóa |

  - Cân bằng: tổng sao mỗi nhân vật = 18; không ai 5 sao ở cả Máu lẫn Tốc độ.
  - Skin theo mùa/sự kiện (chỉ thẩm mỹ).
- **Triển khai:** `StudentData` (ScriptableObject): stats, `passive`, `active` prefab, model/avatar. Áp stat qua cùng cơ chế với `LearningPlayerBridge` (base × hệ số, không cộng dồn khi reload). Cần thêm model nhân vật — dùng pipeline Blender/Poly Pizza hiện có hoặc biến thể màu của SchoolGirl cho MVP.
- **Ưu tiên/Công sức:** P2 · L (nặng về asset).

### U-06 — Trinket "Đồ dùng học tập"
- **Cảm hứng:** Dandy's World (trinket), Hades (keepsake).
- **Thiết kế:** 2 slot trinket/lượt; nhận từ thành tựu, cuối chương, sự kiện.

| Trinket | Hiệu ứng |
|---|---|
| Bút highlight | Được phép sai 1 câu mà không báo động (1 lần/lượt) |
| Máy tính cầm tay | Câu dạng `numeric` +5s |
| Tai nghe cũ | Nghe tiếng bước Shaban xa hơn 30%, hiển thị chỉ báo hướng |
| Giày thể thao | +5% tốc độ đi bộ |
| Bình nước | Hồi năng lượng +15% |
| Thẻ thư viện | Phòng an toàn giữ cửa thêm 10s |
| Sổ tay ghi chép | Sau mỗi câu sai, lượt sau câu đó xuất hiện kèm gợi ý |

- **Triển khai:** `TrinketData` + `IRunModifierHook` (tái dùng interface của `RunModifier` 7.2). Không cho stack cùng loại.
- **Ưu tiên/Công sức:** P2 · S.

### U-07 — Co-op 2–4 người
- **Cảm hứng:** Lethal Company, R.E.P.O., PEAK, Dandy's World, 99 Nights.
- **Thiết kế:**
  - Cùng đêm Rift, 2–4 sinh viên; Shaban săn *người dễ nhất* (tận dụng `MonsterTargetAssessment`).
  - **Học đồng đội:** terminal "song song" — 2 người cùng đứng tại terminal thì mỗi người thấy một nửa dữ kiện, phải nói cho nhau nghe để trả lời.
  - **Hồi sinh:** người bị hạ ở trạng thái "ngất" 45s; đồng đội hồi sinh bằng cách trả lời đúng 1 câu tại chỗ (có báo động nếu sai).
  - **Voice chat theo khoảng cách:** tiếng nói lớn cũng là `SoundEvent` (loudness nhỏ) — Shaban nghe được, giống Lethal Company.
  - Số mảnh yêu cầu ×(1 + 0.5 × (n−1)); Shaban +10% tốc độ mỗi người thêm.
- **Triển khai (lưu ý kiến trúc):**
  - Code hiện giả định **một** player ở nhiều nơi: `MonsterPerception.player` (một `CampusExplorer`), `FindAnyObjectByType<CampusExplorer>` trong `MonsterPerception`, `GameplayHUD`, `CampusAutomaticDoor`, `CampusElevator`. Cần chuyển sang `PlayerRegistry` (danh sách player) **ngay từ GĐ1** dù chưa làm mạng — chi phí thấp bây giờ, rất cao về sau.
  - Mạng: Netcode for GameObjects + Unity Relay/Lobby (hệ sinh thái Unity 6) hoặc Fish-Net/Photon Fusion. AI Shaban chạy **host-authoritative**; client chỉ nhận vị trí/animation.
  - Voice: Vivox (UGS) hoặc Photon Voice.
  - `Time.timeScale` hiện dùng để pause/Course/Terminal — **không dùng được trong co-op**; phải chuyển sang pause cục bộ UI (chỉ khóa input) và Terminal không làm chậm thời gian.
- **Ưu tiên/Công sức:** P2 (chiến lược) · XL. Làm sau khi vòng chơi solo đã chứng minh vui (GĐ5). Chỉ `PlayerRegistry` là P0 · S.

### U-08 — Kiến thức là tiến trình: puzzle thế giới & suy luận
- **Cảm hứng:** Blue Prince, Phasmophobia, Outer Wilds.
- **Thiết kế:**
  - **Puzzle môi trường** có đáp án nằm trong bài học, không có trong save:
    - Tủ khóa số: mã = thứ tự BFS trên sơ đồ dán ở bảng tin.
    - Thang máy khẩn cấp: tầng đúng = kết quả tìm kiếm nhị phân dựa trên gợi ý "cao hơn/thấp hơn" từ loa.
    - Cửa phòng máy chủ: độ phức tạp của đoạn code trên màn hình (O(n), O(n log n)...).
    - Mở được → phòng bí mật có lore, trinket, hoặc lối tắt.
  - **Suy luận thực thể:** mỗi đêm Shaban mang một **"dạng Rift"** ngẫu nhiên (Lắng nghe / Săn mồi / Kiên nhẫn / Hỗn loạn) với điểm mạnh-yếu khác nhau. Người chơi thu **bằng chứng** (vết cào, dấu chân phát sáng, cách phản ứng với đèn...) rồi xác định dạng tại terminal trung tâm. Đúng → mở kỹ năng khắc chế và Insight ×1.3; sai → không phạt.
  - **Draft phòng an toàn:** mỗi khi vào phòng an toàn, chọn 1 trong 3 "tiện ích" (hồi máu / thêm charge / bản đồ vị trí mảnh), kiểu Blue Prince.
- **Triển khai:** `EnvironmentPuzzle : CampusInteractable` dùng chung `IQuestionGrader` (dạng `ordering`/`numeric`). `RiftVariant` ScriptableObject áp chỉnh tham số `MonsterAIConfig` runtime (clone config, không sửa asset). Puzzle **sinh tham số theo seed** (sơ đồ BFS khác nhau mỗi lượt) để không tra mạng được.
- **Ưu tiên/Công sức:** P1 · M (puzzle) + M (suy luận).

### U-09 — Chế độ "99 đêm": chiến dịch dài
- **Cảm hứng:** 99 Nights in the Forest.
- **Thiết kế:** Chế độ tùy chọn sau khi xong chương 1: một **chiến dịch 30 đêm** liên tục (save giữa các đêm). Ban ngày ở ký túc xá: học, chế tạo, nâng cấp phòng. Ban đêm: một lượt Rift khó dần. Mục tiêu phụ: tìm 5 bạn học mất tích qua các đêm. Chết = mất chiến dịch nhưng giữ tri thức/Insight (roguelite).
- **Triển khai:** `CampaignState` lưu riêng (`campaign-v1.json`), dùng lại `RunDirector` với `RunConfig` theo số đêm.
- **Ưu tiên/Công sức:** P3 · L.

### U-10 — Clip-ability & công cụ cho streamer
- **Cảm hứng:** PEAK ("mỗi ván đều ra clip"), Lethal Company.
- **Thiết kế:**
  - **Death cam:** khi chết, phát lại 4s cuối từ góc nhìn Shaban (ghi ring buffer vị trí/animation, không quay video).
  - **Ảnh jumpscare tự động** + **thẻ kết quả** chia sẻ (ảnh PNG có hạng, câu hỏi khiến bạn chết: *"Chết vì không biết BFS dùng queue"*) — tự nhiên thành meme và quảng bá nội dung học.
  - **Photo mode** đơn giản.
  - **Chế độ streamer:** ẩn tên người chơi, tắt nhạc có bản quyền (nếu có).
  - (Sau này) Tích hợp Twitch/YouTube chat bình chọn sự kiện đêm.
- **Triển khai:** `ReplayBuffer` (vị trí + state 10 Hz, 6s) cho Shaban & player; `ShareCardRenderer` dùng RenderTexture → PNG vào `persistentDataPath/Captures`.
- **Ưu tiên/Công sức:** P1 · M. Chi phí vừa, tác động marketing lớn.

### U-11 — Hub ký túc xá lớn dần
- **Cảm hứng:** Hades (hub sống), Rogue Legacy 2 (meta hiện hình).
- **Thiết kế:** Thay MainMenu phẳng bằng **phòng ký túc xá 3D nhỏ**:
  - Kệ sách tự đầy thêm sách mỗi lesson đạt Đồng; sách viền bạc/vàng/kim cương theo mastery.
  - Bảng ghim: ảnh jumpscare, ảnh thực thể trong Sổ tay Rift, mảnh lore đã nhặt.
  - Tủ quần áo: đổi nhân vật/skin; bàn học: Courses; cửa: vào đêm Rift.
  - Bạn học đã giải cứu xuất hiện trong phòng, có hội thoại mới sau mỗi lượt (kiểu Hades).
- **Triển khai:** scene `Dorm.unity` nhẹ (tái dùng asset campus), UI hiện tại chuyển thành panel mở từ vật thể. Giữ MainMenu 2D cho thiết bị yếu (Settings: "Menu đơn giản").
- **Ưu tiên/Công sức:** P2 · L.

### U-12 — Chế độ Lớp học (giáo viên)
- **Cảm hứng:** Gimkit, Blooket, Kahoot.
- **Thiết kế:**
  - Giáo viên nhập bộ câu hỏi (CSV/JSON/Google Sheets export) → tạo **mã lớp**.
  - Học sinh nhập mã → chơi các lượt Rift dùng câu hỏi của lớp; mức kinh dị **"Nhẹ"** (không jumpscare, Shaban dạng hoạt hình, không máu).
  - Bảng kết quả cho giáo viên: tỷ lệ đúng từng câu, câu nhiều người sai nhất, thời gian trả lời.
  - Chế độ "Trình chiếu": máy chiếu hiện bảng xếp hạng lớp theo thời gian thực.
- **Triển khai:** GĐ đầu **offline**: xuất/nhập file bộ câu hỏi + file kết quả (không cần backend). GĐ sau: backend nhỏ (UGS Cloud Code / Firebase) cho mã lớp. Dùng lại `CourseImporter` (GĐ4) và `LearningContentValidation` ở runtime. Bộ câu hỏi do AI tạo phải qua **giáo viên duyệt** trước khi dùng.
- **Ưu tiên/Công sức:** P2 · L. Kênh phân phối rất khác biệt so với đối thủ horror.

### U-13 — Vòng thói quen hằng ngày (mở rộng mục 8)
- **Cảm hứng:** Duolingo.
- **Thiết kế:**
  - **Ca trực đêm:** 1 lượt/ngày theo seed chung (= Thử thách ngày) + các bài ôn đến hạn → hoàn thành cả hai = +1 streak.
  - **Streak freeze:** tặng 1 cái/tuần, mua thêm bằng Insight (tối đa giữ 2).
  - **League tuần** theo Insight (nhóm 30 người, lên/xuống hạng) — cần backend; bản offline dùng "kỷ lục cá nhân tuần".
  - **Thông báo cục bộ** trên mobile, tối đa 1/ngày, giọng điệu vui/nhân vật Shaban ("Shaban đang đói... có bài cần ôn"), tắt được dễ dàng.
- **Triển khai:** Unity Mobile Notifications (local); `StreakService` dựa trên `ILearningClock` (6.2).
- **Ưu tiên/Công sức:** P1 · S (offline) / M (league).

### U-14 — Mức kinh dị & trợ năng
- **Cảm hứng:** yêu cầu thị trường học sinh; DOORS có nhiều tín hiệu âm thanh → cần tương đương hình ảnh.
- **Thiết kế:**
  - Thanh **Mức kinh dị**: Nhẹ / Vừa / Mạnh (tắt jumpscare, giảm máu/âm thanh gắt, đổi model Shaban).
  - **Chỉ báo âm thanh trực quan:** vòng cung chỉ hướng bước chân Shaban, chuông, tiếng hét thực thể — bắt buộc vì nhiều cơ chế dựa vào âm thanh (hearing). Giúp người khiếm thính và người chơi tắt tiếng trên mobile.
  - Phụ đề mọi thoại/lore; chế độ mù màu cho HUD; tùy chỉnh thời gian trả lời (×1.5, ×2) cho người cần hỗ trợ đọc.
- **Triển khai:** mở rộng `SettingsManager`; `SoundIndicatorHUD` lắng nghe `SoundEventBus` + `MonsterAudio`.
- **Ưu tiên/Công sức:** P1 · M.

### U-15 — Phát hành theo chương & Shaban là mascot
- **Cảm hứng:** Poppy Playtime.
- **Thiết kế:** Chương 1 (Tòa CNTT) là sản phẩm hoàn chỉnh; mỗi chương mới = 1 tòa nhà + 1 course + 1 quái + 1 đoạn cốt truyện. Thiết kế Shaban có **hình bóng dễ nhận ra** (dùng làm icon, sticker, thumbnail). Mỗi lần ra chương là một đợt truyền thông.
- **Ưu tiên/Công sức:** P2 · (lịch phát hành, không phải một task code).

### U-16 — Kinh tế có đạo đức
- **Thiết kế:** Không bán sức mạnh, **không bán đáp án/gợi ý**, không loot box. Chỉ bán: mở khóa chương (mobile), cosmetic, ủng hộ (supporter pack). Mọi thứ ảnh hưởng gameplay kiếm được bằng chơi/học. Lý do: giữ uy tín với phụ huynh/giáo viên, và tuân thủ quy định (mục 19.4).
- **Ưu tiên/Công sức:** P1 · quyết định kinh doanh.

### U-17 — Tối ưu mobile cho thị trường Việt Nam
- **Thiết kế & mục tiêu kỹ thuật:**
  - Lượt chơi mobile 5–8 phút (RunConfig "mobile": 3 mảnh, map 2 tầng).
  - 30 FPS ổn định trên Android tầm trung (RAM 4 GB); dung lượng cài đặt ban đầu < 300 MB, chương sau tải thêm (Addressables).
  - Chơi offline hoàn toàn (trừ league/lớp học).
  - Kiểm thử trên thiết bị thật (hiện mới test Touchscreen trong Editor — xem `LEARNING_VALIDATION_REPORT.md`).
- **Triển khai:** Profiler trên thiết bị; giảm `BeliefParticles` (320 → 160) và `BeliefRaysPerTick` trên mobile nếu cần — tham số đã có trong `MonsterAIConfig`, tạo biến thể config theo nền tảng.
- **Ưu tiên/Công sức:** P1 · M.

### U-18 — Lộ trình nội dung học
- **Thiết kế:** Ưu tiên course theo quy mô người học tại Việt Nam:

| Thứ tự | Course | Lý do | Nhánh kỹ năng |
|---|---|---|---|
| 1 | Algorithms (có) | Đã có, hợp sinh viên CNTT | Kiểm soát |
| 2 | **Tiếng Anh — từ vựng & ngữ pháp THPT** | Tệp người học lớn nhất; câu hỏi ngắn hợp terminal | Đánh lừa (Văn–Ngữ) |
| 3 | Toán THPT (hàm số, xác suất, dãy số) | Ôn thi | Di chuyển/Vật lý |
| 4 | Vật lý THPT | Ôn thi | Di chuyển |
| 5 | Hóa học THPT | Ôn thi | Tiêu hao |
| 6 | Course cộng đồng/giáo viên | Nội dung vô hạn | — |

  - **Quy trình sản xuất:** soạn khung lesson → AI hỗ trợ tạo bản nháp câu hỏi + giải thích → **người có chuyên môn duyệt** → song ngữ → `Validate Content` → thử nghiệm với 5–10 học sinh → điều chỉnh độ khó theo tỷ lệ đúng (mục tiêu 60–80%).
  - Mỗi course tối thiểu 5 lesson × 12 câu (để terminal ít lặp), tag độ khó 1–3.
- **Ưu tiên/Công sức:** P1 · L (liên tục).

---

## 18. Ma trận ưu tiên & lộ trình tổng hợp

### 18.1 Ma trận tác động / công sức

| Nâng cấp | Giữ chân | Lan truyền | Học tập | Công sức | Ưu tiên |
|---|:-:|:-:|:-:|:-:|:-:|
| U-01 Director & Menace | ●●● | ●● | ○ | M | **P0** |
| U-02 Terminal nhiều bước + Panic | ●●● | ●●● | ●●● | M | **P0** |
| U-07a `PlayerRegistry` (chuẩn bị co-op) | ○ | ○ | ○ | S | **P0** |
| U-03 Thực thể có tín hiệu | ●●● | ●●● | ●● | M | P1 |
| U-04 Địa hình rượt đuổi | ●● | ●● | ○ | M | P1 |
| U-08 Puzzle kiến thức & suy luận | ●●● | ●● | ●●● | M+M | P1 |
| U-10 Clip & thẻ chia sẻ | ● | ●●● | ● | M | P1 |
| U-13 Thói quen hằng ngày | ●●● | ● | ●●● | S–M | P1 |
| U-14 Mức kinh dị & trợ năng | ●● | ● | ●● | M | P1 |
| U-17 Tối ưu mobile | ●● | ● | ○ | M | P1 |
| U-18 Course Tiếng Anh | ●● | ●● | ●●● | L | P1 |
| U-05 Roster nhân vật | ●●● | ●● | ○ | L | P2 |
| U-06 Trinket | ●● | ○ | ● | S | P2 |
| U-11 Hub ký túc xá | ●● | ● | ●● | L | P2 |
| U-12 Chế độ Lớp học | ●● | ●●● | ●●● | L | P2 |
| U-07b Co-op đầy đủ | ●●● | ●●● | ●● | XL | P2 (chiến lược) |
| U-09 Chiến dịch 30 đêm | ●● | ● | ● | L | P3 |

### 18.2 Lộ trình sau khi gộp

| Giai đoạn | Nội dung (Phần I + Phần II) | Mốc kiểm chứng |
|---|---|---|
| **0 — Sửa nhanh** | Mục 4 + U-07a `PlayerRegistry` | Không còn chỗ gây hiểu lầm; code sẵn sàng cho nhiều player |
| **1 — Vòng chơi một lượt** | Mục 5 + **U-01 Director** | Playtest 5–8 người: ≥ 70% muốn "chơi thêm ván" |
| **2 — Học & Chơi** | Mục 6 + **U-02** + U-13 (offline) + U-14 | Tỷ lệ đúng terminal 60–80%; không ai kêu "học là việc vặt" |
| **3 — Đa dạng** | Mục 7 + U-03 + U-04 + U-08 + U-06 | Lượt thứ 10 vẫn có điều mới |
| **3.5 — Vertical slice / Demo** | U-10 + U-17 + polish âm thanh/VFX + 1 chương hoàn chỉnh | Demo 20–30 phút cho Steam Next Fest / bản thử mobile |
| **4 — Giữ chân dài hạn** | Mục 8 + U-05 + U-11 + U-18 (Tiếng Anh) + U-12 (offline) | Soft launch: đo D1/D7/D30 |
| **5 — Co-op** | U-07b | Beta kín với nhóm bạn 2–4 người |
| **6 — Mở rộng** | U-09, chương 2–4, U-12 online, league | Theo số liệu soft launch |

### 18.3 Định nghĩa Vertical Slice (demo)
- Chương 1 hoàn chỉnh: 1 map, Shaban + 2 thực thể, 3 terminal nhiều bước, 1 puzzle kiến thức, Panic thoát, phòng an toàn.
- Course Algorithms 5 lesson (có sẵn) + Tiếng Anh 2 lesson mẫu.
- Màn kết quả + thẻ chia sẻ + death cam.
- Mức kinh dị, chỉ báo âm thanh, EN/VN.
- Chạy 30 FPS trên 1 máy Android tầm trung đã chọn.

---

## 19. Định vị, giá, phát hành & tuân thủ

### 19.1 Định vị
> *"DOORS gặp Duolingo trong khuôn viên trường lúc nửa đêm: trả lời đúng để sống sót, trả lời sai là tiếng chuông đánh thức Shaban."*

| Phân khúc | Nền tảng | Thông điệp | Kênh |
|---|---|---|---|
| A — Học sinh/sinh viên VN ôn thi | Mobile | "Ôn thi mà tim đập thình thịch" | TikTok, YouTube Shorts, Facebook group ôn thi |
| B — Người chơi horror/indie toàn cầu | PC (Steam) | "Kinh dị có AI săn mồi thông minh + câu đố tri thức" | Streamer, Steam Next Fest, Reddit |
| C — Giáo viên | PC/Web/Mobile | "Biến buổi ôn bài thành game" | Cộng đồng giáo viên, hội thảo ed-tech |

### 19.2 Giá (tham chiếu PEAK < $5, R.E.P.O. ~$8)
- **PC:** $5.99–7.99 (giá khu vực VN thấp hơn); demo miễn phí.
- **Mobile:** Chương 1 miễn phí; mở toàn bộ chương hiện có ~49–99k VND (mua một lần); cosmetic.
- **Lớp học:** miễn phí cho giáo viên ở bản offline; bản online (mã lớp, dashboard) cân nhắc gói trường học sau.

### 19.3 Chiến lược ra mắt
1. **Trước demo:** đăng trang Steam sớm để gom wishlist; đăng clip ngắn hằng tuần (trả lời sai → jumpscare, Shaban đi thang máy đuổi theo — tận dụng điểm mạnh AI).
2. **Demo:** Steam Next Fest với Vertical Slice; bộ tài liệu cho streamer (key, mô tả cơ chế, chế độ streamer).
3. **Mobile soft launch** tại VN (1 tỉnh/nhóm trường thử nghiệm hoặc beta mở) → đo KPI mục 11.
4. **Early Access PC** sau demo nếu wishlist đạt mục tiêu; cập nhật theo chương (U-15).

### 19.4 Tuân thủ tại Việt Nam (cần xác minh pháp lý trước khi phát hành)
- **Nghị định 147/2024/NĐ-CP** (hiệu lực 25/12/2024) quản lý trò chơi điện tử trên mạng, phân loại G1–G4 và độ tuổi 18+/16+/12+/00+. Game tải về chơi **không tương tác** giữa người chơi hay với máy chủ thuộc nhóm **G4**; khi thêm league, co-op, lớp học online, thanh toán trong game thì nhóm phân loại và nghĩa vụ cấp phép thay đổi (G1/G2/G3).
- Nhà phát hành tự phân loại độ tuổi; nội dung kinh dị/bạo lực ảnh hưởng mức tuổi. **Mức kinh dị "Nhẹ" (U-14)** giúp hướng tới 12+ cho thị trường học sinh — cần kiểm tra tiêu chí cụ thể.
- Có quy định xử phạt mới về game từ 01/07/2026 và quy định giới hạn thời gian chơi cho người dưới 18 tuổi — cần tư vấn pháp lý trước khi bật tính năng online.
- Dữ liệu học sinh (Chế độ Lớp học): thu thập tối thiểu, không lưu tên thật nếu không cần, có đồng ý của phụ huynh/nhà trường.

---

## 20. Nguồn tham khảo

**Kinh dị / Roblox**
- [DOORS — Beginner's Guide 2026 (BloxQuiz)](https://www.bloxquiz.gg/guides/doors-beginners-guide) · [DOORS Monsters Guide 2026](https://gamelandinsider.com/doors-monsters-guide) · [DOORS Wiki — List of Entities](https://doors-game.fandom.com/wiki/List_of_Entities)
- [Dandy's World — gameplay guide 2026 (The Experiment)](https://md-eksperiment.org/en/post/20260309-dandys-world-gameplay-mastery-toon-tiers-floor-strategies-ichor-maximization-guide) · [Dandy's World Wiki — Floors](https://dandys-world-robloxhorror.fandom.com/wiki/Floors)
- [99 Nights in the Forest — ComicBook.com](https://comicbook.com/gaming/news/roblox-survival-horror-title-is-quietly-becoming-one-of-the-games-biggest-hits/) · [Best Horror Games on Roblox 2026 (Endsights)](https://endsights.com/best-horror-roblox-games) · [List of Roblox games (Wikipedia)](https://en.wikipedia.org/wiki/List_of_Roblox_games)

**Co-op / PC**
- [R.E.P.O. (Wikipedia)](https://en.wikipedia.org/wiki/R.E.P.O.) · [Best Co-op Horror Games 2026 (FEEDERS)](https://www.playfeeders.com/blog/best-co-op-horror-games-2026/) · [Games like Lethal Company 2026 (Summer Engine)](https://www.summerengine.com/blog/games-like-lethal-company)
- [PEAK — 5 triệu bản (PCGamesN)](https://www.pcgamesn.com/peak/major-sales-milestone) · [PEAK — 2 triệu bản với < $200k (Game Developer)](https://www.gamedeveloper.com/production/how-co-op-climbing-hit-peak-achieved-2-million-sales-for-less-than-200-000-) · [PEAK dev interview (GamesRadar+)](https://www.gamesradar.com/games/co-op/peaks-success-was-a-surprise-because-the-co-op-romp-didnt-launch-with-much-of-what-we-thought-was-required-to-make-a-hit-but-lead-wants-to-keep-its-game-jam-like-spirit-for-future-updates/)
- [Phasmophobia (Wikipedia)](https://en.wikipedia.org/wiki/Phasmophobia_(video_game))
- [Blue Prince review (PCGamesN)](https://www.pcgamesn.com/blue-prince/review) · [A puzzle designer on Blue Prince (mssv)](https://mssv.net/2025/04/07/a-puzzle-designer-on-blue-prince-a-roguelike-puzzle-masterpiece/)
- [The Perfect Organism: AI of Alien: Isolation (Game Developer)](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation) · [Revisiting the AI of Alien: Isolation (AI and Games)](https://www.aiandgames.com/p/revisiting-alien-isolation)
- [Dead by Daylight — Looping guide (EIP Gaming)](https://eip.gg/dbd/guides/how-to-loop/)
- [Poppy Playtime Chapter 1 (AppBrain)](https://www.appbrain.com/app/poppy-playtime-chapter-1/com.MOBGames.PoppyMobileChap1)
- [Best roguelikes 2026 (GamesRadar+)](https://www.gamesradar.com/best-roguelikes-roguelites/) · [Roguelites with the best progression (Game Rant)](https://gamerant.com/roguelite-games-with-best-progression-systems/)
- [Weekly Top Trending on Steam, 14–20/09/2026 (GameGrin)](https://www.gamegrin.com/news/weekly-top-trending-games-on-steam-14th20th-of-september-2026) · [Best indie horror 2026 (Pocket Tactics)](https://www.pockettactics.com/indie-horror-games)

**Học tập**
- [Duolingo gamification explained (StriveCloud)](https://www.strivecloud.io/duolingo-gamification-explained) · [Duolingo User Statistics 2026 (Gitnux)](https://gitnux.org/duolingo-user-statistics/)
- [Kahoot vs Gimkit vs Blooket (Northern Review, 2026)](https://northernreview.org/2026/05/13/kahoot-vs-gimkit-vs-blooket/) · [Blooket vs Gimkit vs Kahoot 2026 (TriviaMaker)](https://triviamaker.com/blooket-vs-gimkit-vs-kahoot/)

**Thị trường Việt Nam & pháp lý**
- [Vietnam Gaming & Esports 2026 (Digital in Asia)](https://digitalinasia.com/reports/vietnam-gaming-2026/) · [2026 SEA Mobile Game Marketing Report (Mobidictum)](https://mobidictum.com/2026-southeast-asia-mobile-game-marketing-report/) · [Mobile games in Vietnam (MAF)](https://maf.ad/en/blog/mobile-games-in-vietnam/)
- [Nghị định 147/2024/NĐ-CP (Bộ KH&CN)](https://mst.gov.vn/nghi-dinh-147-2024-nd-cp-quan-ly-chat-che-dich-vu-tro-choi-dien-tu-tren-mang-va-thong-tin-tren-internet-197241227124622733.htm) · [Phân loại game theo độ tuổi (LuatVietnam)](https://luatvietnam.vn/tin-van-ban-moi/tro-choi-dien-tu-tren-mang-duoc-phan-loai-theo-4-do-tuoi-tu-25-12-2024-186-99866-article.html) · [Quy định xử phạt liên quan đến game từ 01/7/2026 (Thư Viện Pháp Luật)](https://thuvienphapluat.vn/chinh-sach-phap-luat-moi/vn/ho-tro-phap-luat/chinh-sach-moi/116285/tong-hop-quy-dinh-xu-phat-lien-quan-den-game-tu-01-7-2026)
