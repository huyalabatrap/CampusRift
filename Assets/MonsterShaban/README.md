# Monster_Shaban

Đã đặt **một** instance trong `Assets/Scenes/SampleScene.unity` và lưu prefab tại `Assets/MonsterShaban/Monster_Shaban.prefab`.

## Sử dụng

- Chạy scene hiện tại. Quái bắt đầu tuần tra ở sân, gần `(8, 0.06, -4)`.
- **F3** bật/tắt bảng debug và gizmos của quái; chọn component `MonsterDebug` để xem dữ liệu trong Inspector.
- Chỉnh thông số tại `MonsterAIConfig.asset`: Patrol 2.5, Investigate 3.8, Chase 6.2 m/s; Vision 30 m / 100°; Attack 1.6 m, cooldown 1.8 s, damage 25, windup 0.35 s.
- Player nhận damage qua `PlayerMonsterHealth`. Khi hết 100 HP, gọi chức năng về spawn sẵn có và có 3 giây miễn thương.

## Model và audio

Sử dụng trực tiếp FBX và MP3 được cung cấp. Chỉ sửa thiết lập import/rig, loop, material tương thích URP, scale và vị trí model trong prefab. Generic avatar lấy `mixamorig_Hips` làm motion node; tắt apply root motion. Model/capsule cao khoảng 1.75 m để phù hợp cửa và cầu thang của campus. Không có animation attack riêng trong controller hiện tại; attack dùng logic placeholder có windup và kiểm tra lại tầm nhìn khi gây damage.

Audio dùng bản MP3 gốc, import mono/streaming, `spatialBlend = 1`, logarithmic rolloff 2–45 m, loop; thay đổi intensity theo state và chuyển mượt. Occlusion được kiểm tra mỗi 0.25 s, giảm volume còn 72% và low-pass về 1,200 Hz khi có vật cản. Vị trí listener chỉ phục vụ âm thanh, không được truyền sang AI.

## AI và navigation

`Idle → Patrol → Investigate / Chase → Attack → Search → ReturnToPatrol → Patrol`.

- Vision 8 Hz; quyết định 4 Hz; cập nhật đường tối đa mỗi 0.3 s; hearing theo event; tính tuyến cấp cao tối đa mỗi 0.75 s.
- Chỉ perception đọc vị trí Player để kiểm tra distance/FOV/raycast. Sau khi nhìn thấy mới cung cấp snapshot vị trí, velocity, grounded và gravity cho brain/prediction.
- Khi mất tầm nhìn, AI dùng evidence trong memory. Debug không cập nhật khoảng cách tới Player đang bị khuất.
- Search kiểm tra vị trí evidence trước (nếu còn đáng đi), sau đó chọn mục tiêu từ **belief** (xem mục "Săn đuổi nhiều tầng"). Không có belief thì dùng lại các cửa/cầu thang lân cận như trước.
- NavMesh bake từ PhysicsColliders. Cửa chuyển động được loại khỏi bake; quái yêu cầu mở cửa thật và chờ cánh cửa mở trước khi đi tiếp. Chỗ NavMesh không phủ được (vế thang quá dốc, cửa mở xuống vế thang thấp hơn) được nối bằng `NavMeshLink` (xem "NavMeshLink cho chỗ NavMesh không nối được").
- Room Graph có 2,126 node tĩnh (gồm 277 điểm quan sát trong phòng lớn), nối bằng các đoạn NavMesh hoàn chỉnh. Graph giải quyết các tuyến dài xuyên tòa nhà/tầng; không cung cấp thông tin về vị trí Player bị khuất. Không có teleport, không tạo quái khác, squad hoặc boss.
- Prediction chỉ dùng snapshot nhìn thấy, giới hạn 10 m. Khi airborne, mô phỏng quỹ đạo ngắn với gravity, kiểm tra va chạm và mặt NavMesh ở điểm đáp; thiếu độ tin cậy thì dùng evidence cũ.

## Săn đuổi nhiều tầng (belief + thang máy)

Trước đây, khi Player đi thang máy, quái mất dấu vì: Room Graph/NavMesh không có thang, perception không nhận biết thang, search chỉ quét ~12–26 m quanh vị trí cuối (gần như cùng tầng) rồi `Forget()` sau 18 s. Quái còn có thể đi xuyên cửa thang đóng vào giếng thang trống ở tầng trệt.

**Belief — "Player có thể đang ở đâu?"** (`MonsterBelief`): particle filter 320 giả thuyết chạy trên topology tĩnh (`CampusNavGraph`: Room Graph + 102 sảnh thang). Mỗi giả thuyết di chuyển như người: chạy theo hướng đã thấy rồi đi bộ, dừng trong phòng, lên/xuống cầu thang, đi thang máy, tránh xa quái. Chỉ cảm nhận của chính quái thay đổi belief:

- Thấy Player: thu về vị trí/hướng/tốc độ quan sát. Nghe tiếng: kéo belief về nguồn âm.
- Nhìn thấy một chỗ mà không có Player: giả thuyết ở đó bị loại (negative evidence, tối đa 40 ray/tick, 5 Hz).
- Bị bác bỏ hoàn toàn: gieo lại từ bằng chứng thật gần nhất, tránh chỗ vừa nhìn.

**Thang máy** (`MonsterElevatorAwareness`) — quái chỉ biết những gì một người đứng đó biết:

- Thấy Player bước vào cabin / mất dấu ở cửa cabin đang mở → belief chuyển sang "đang trong cabin".
- Đọc **bảng số tầng** trên cửa thang khi đứng ở sảnh cùng tầng, trong 20 m, nhìn thấy được mặt bảng: biết cabin đang ở tầng nào, đang lên/xuống. Quái nhớ những lần chính nó gọi thang/đi thang (tầng nó gọi và tầng nó bấm), nên chỉ những chuyển động còn lại của cabin mới là bằng chứng Player liên quan.
- Nghe **chuông báo tới tầng** (≤16 m, giảm khi bị che).
- Khi cabin chưa dừng, quái đứng nhìn bảng số tới khi dừng rồi tới đúng tầng đó (cầu thang, hoặc thang máy nếu nhanh hơn); nếu tới muộn, bảng số vẫn cho biết cabin đang ở đâu. Thấy cabin mở mà trống → giả thuyết "trong cabin" chuyển thành "đã ra ở tầng khác".
- Nghi Player trốn trong cabin đóng cửa ngay trước mặt → bấm nút gọi thang để cửa mở và nhìn vào.

**Chọn mục tiêu** (`MonsterSearch`): khối xác suất quanh node (7 m) + khối theo khu vực (~24 m cùng tầng), chia cho thời gian di chuyển thực (Dijkstra trên graph, gồm cầu thang; cạnh thang máy chỉ được tính khi đi thang nhanh hơn), có hysteresis để không đổi mục tiêu vô cớ. Tới nơi thì nhìn về phía còn nhiều giả thuyết. Mục tiêu không tới được bị tạm bỏ 25 s. Chạy (≈90% tốc độ đuổi) khi manh mối còn nóng hoặc mục tiêu chắc chắn (vd. vừa thấy cabin dừng ở F11); ngược lại đi tìm tốc độ search.

**Vòng đời**: `SearchDuration` (60 s) tính từ bằng chứng thật **mới nhất** (thấy, nghe, sự kiện thang), không từ lúc bắt đầu search. Hết hạn thì `Forget()` bộ nhớ ngắn hạn nhưng vẫn **roam** các khu vực khả nghi với tốc độ tuần tra `RoamDuration` (60 s), sau đó mới về tuyến tuần tra cố định.

**Nhìn xuyên kính**: ~700 collider kính/cửa kính trong suốt với tầm nhìn (≤ `GlassVisionDistance` 22 m) nhưng vẫn chặn âm thanh và đòn đánh — quái thấy qua cửa sổ nhưng phải đi vòng qua cửa. Tầm nhìn thử thêm điểm đầu khi ngực bị che thấp.

**Điều hướng bền vững** (`MonsterNavigation`): dùng đường partial để tới điểm gần nhất thay vì đứng im; Player ở chỗ không tới được (gờ, nóc xe) thì tới điểm đứng gần nhất (`MoveNear`); bám route Room Graph ổn định khi mục tiêu dịch chuyển nhẹ; phát hiện kẹt (1.6 s không tiến) → tính lại đường → né sang bên → tạm bỏ đích 10 s. Cửa không mở sau 4 s cũng được xử lý. `ElevatorShaftGuard` carve NavMesh trong giếng thang khi cabin không ở đó; `CampusElevator` không đóng cửa khi quái đang ở ngưỡng cửa/trong cabin, trừ khi chính quái đang đi thang (xem mục dưới).

**Phản ứng với cái liếc**: vision chạy 8 Hz, brain quyết định 4 Hz. Player lướt qua tầm nhìn giữa hai lần quyết định (vd. lúc quái đang quay người) trước đây bị bỏ qua; giờ được xử lý như một lần thấy vừa mất dấu (đuổi theo điểm vừa thấy rồi chuyển sang săn bằng belief). Tiếng động đầu tiên nghe được trong 3 s sau khi mất dấu cho biết hướng chạy ("thấy ở đây, nghe ở kia"), nên belief và mục tiêu tìm kiếm đi theo hướng đó thay vì tỏa đều mọi phía.

**Chặn đầu**: `InterceptionPlanner` (đã có) giờ được Brain dùng khi Player chạy nhanh hơn quái và ở xa > 7 m, chỉ khi quái tới điểm chặn trước với biên an toàn.

**Debug**: F3 hiện ý định (vd. `Hunting: LiftDisplay -> watch lift X display until it stops`), tóm tắt belief, sự kiện thang gần nhất, trạng thái điều hướng. Gizmos vẽ từng giả thuyết (đỏ: di chuyển, tím: trốn, cam: trong cabin), route graph và mục tiêu. `MonsterDebug.logReasoning` ghi các thay đổi ý định/suy luận thang vào Console.

**Tham số mới** trong `MonsterAIConfig`: nhóm *Belief*, *Hunt*, *Elevators*, *Vision through glazing*, *Navigation recovery* (đều có tooltip). Nhóm *Elevators* có thêm `RideLifts`, `LiftAdvantageSeconds` (4 s), `LiftMaxWait` (35 s), `LiftBoardSpeed` (2.2 m/s). Kiểm chứng: `ShabanHunterValidation.Belief/Lifts/NavigationRobustness` (Play Mode), harness `ShabanHuntScenarioTest` (kịch bản `elevator`, `elevator-return`, `stairs`, `room`, `sprint`) và `ShabanTraversalPlayTest` (link + thang/cầu thang).

Sau khi sửa kiến trúc, dùng `Campus Rift > Shaban > Rebake Campus Navigation`, rồi `Build Room Graph`, cuối cùng `Audit Campus Routes`. Unity có thể trả về đường partial từ [`NavMesh.CalculatePath`](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/ai/navmesh/calculatepath); code kiểm tra trạng thái và chuyển sang các chặng Room Graph đã xác nhận.

## Đi thang máy khi nhanh hơn cầu thang

`MonsterNavigation` so sánh thời gian tới đích bằng hai cách mỗi khi đích lệch ≥ 2.8 m theo chiều cao (tối đa 1 lần / 2.5 s):

- **Đi bộ (cầu thang)**: đường NavMesh thật tới đích — chiều dài / tốc độ hiện tại, cộng thời gian phanh-tăng tốc ở mỗi khúc cua (cầu thang gấp khúc quay đầu 180° hai lần mỗi tầng; đây là phần tốn thời gian nhất khi chạy nhanh). Không có đường NavMesh trực tiếp thì dùng Room Graph.
- **Thang máy** (từng thang trong ~70 m đi bộ): đi tới sảnh + **thời gian chờ cabin** + thời gian chạy của chính thang đó (đóng/mở cửa, tăng tốc, tốc độ tối đa, phanh — `CampusNavGraph.RideSeconds`) + bước vào/ra + đi từ sảnh tới đích.
- Thời gian chờ lấy từ những gì quái **đã thấy**: bảng số tầng đọc được trong 30 s gần nhất (cabin đứng ngay tầng này → chỉ chờ mở cửa; ở tầng khác → thời gian cabin chạy tới; đang chạy → hết chuyến rồi mới tới). Chưa thấy thì dùng thời gian chờ trung bình của thang đó. Không đọc vị trí cabin "miễn phí".
- Chọn thang khi nhanh hơn cầu thang ít nhất `LiftAdvantageSeconds` (4 s). Đang chờ ở sảnh thì mỗi giây đọc lại bảng số và tính lại; cầu thang trở nên nhanh hơn, hoặc chờ quá `LiftMaxWait` (35 s) → bỏ thang, đi bộ.

Chuyến đi là chuyển động vật lý thật, **không teleport**: đi tới sảnh → bấm gọi (bấm lại nếu cửa đóng/cabin đi mất) → khi cửa mở, bước qua ngưỡng tới giữa cabin (`LiftBoardSpeed` 2.2 m/s, cửa được giữ mở vì quái đứng ở ngưỡng) → bấm tầng đích; cửa đóng, cabin chở quái đi theo đúng độ dịch chuyển của cabin → ra ở tầng đích (hoặc ở tầng mà đích vừa chuyển tới, hoặc khi cabin dừng hẳn không đi tiếp) → bước ra sảnh, gắn lại NavMeshAgent. Trong cabin quái vẫn nhìn/nghe và đánh được nếu Player ở trong tầm. Đích đổi về cùng tầng khi đang chờ → bỏ chuyến. Chuyến của chính quái không bị coi là bằng chứng về Player (awareness nhớ tầng nó gọi/bấm), nhưng cabin dừng ở tầng khác mà quái không bấm thì vẫn là dấu hiệu có người gọi ở đó.

`MonsterSearch` dùng cùng mô hình: chi phí di chuyển tới mục tiêu có tính cạnh thang máy khi thang nhanh hơn.

Debug (F3): dòng `Nav:` hiện kế hoạch (`lift X F1->F11: Riding (est. 24s)`) hoặc lần so sánh gần nhất (`lift X F1->F11 42s vs stairs 33s: stairs`); `logReasoning` ghi các thay đổi vào Console; chọn quái để thấy gizmo sảnh đi/đến.

## NavMeshLink cho chỗ NavMesh không nối được

`Campus Rift > Shaban > Repair Navigation Gaps (links + door leaves)` (`ShabanNavigationRepair`):

- 20 cánh cửa phụ chưa bị loại khỏi bake (cánh cửa đóng thành "tường" trong NavMesh) → thêm `NavMeshModifier` (ignoreFromBuild). Gốc lỗi đã sửa trong `CampusDoorSetup`/`CampusTraversalRepair` để cửa tạo sau này cũng được loại.
- Cầu thang lõi khối C (F1→F3) dốc hơn độ dốc tối đa của agent (48°) nên NavMesh không phủ các vế thang → 4 `NavMeshLink` hai chiều nối chiếu nghỉ.
- Cửa phía tây A7N (F8, F9) mở ra vế thang thấp hơn ~1.05 m → 2 `NavMeshLink`.

Quái tự đi qua link (agent `autoTraverseOffMeshLink = false`): leo với 60% tốc độ (1.6–3.2 m/s), mở cửa nằm trên đường link và chờ cửa mở, rồi tính lại đường từ đầu kia (tránh quay ngược lại link do vận tốc cũ).

## Điểm quan sát trong phòng lớn

`Build Room Graph` thêm các node `Observation` (277 node): lấy mẫu mặt sàn NavMesh trong nhà, chỗ nào không nhìn thấy node nào trong 9 m (cùng tầng, kính/cửa tự động coi là trong suốt) thì đặt điểm quan sát ở chỗ xa nhất trước, cho tới khi phủ hết những chỗ nối được với graph. Node trong phòng có nhãn `<cửa>/room_n` và được tính như "bên trong phòng" (giả thuyết trốn trong phòng lớn không còn bị loại chỉ vì quái nhìn từ cửa). Node phải nối được với graph bằng đường đi thật (≤ 40 m). Graph: 2,126 node, 1 thành phần liên thông.

## Tích hợp movement

Player hiện tại là `CampusExplorer`: đi/chạy, sprint, nhảy và thang máy. Walk/Run/Sprint/Jump/Landing/HeavyLanding đã được nối tự động qua `PlayerSoundEmitter`.

Project hiện chưa có implementation grapple/swing/wall-run/air-dash/combat/GiantHand. Khi thêm các kỹ năng, gọi các hook tại thời điểm kích hoạt thật:

```csharp
var sound = GetComponent<CampusRift.Monsters.PlayerSoundEmitter>();
sound.Grapple();
sound.AirDash();
sound.Combat();
sound.GiantHandSkill();
```

Prediction đang đọc velocity từ CharacterController và gravity/grounded từ CampusExplorer. Nếu controller kỹ năng sau này dùng Rigidbody hoặc di chuyển Transform trực tiếp, cần nối telemetry tương ứng trong MonsterPerception.

## Kiểm chứng

- `Validation/BehaviorPlayMode.json`: **19 pass, 0 fail**, gồm FOV, distance, occlusion, memory, search/return, hearing, attack damage/cooldown/chặn qua tường, landing prediction và audio.
- `Validation/Hunter/*.json` (`ShabanHunterValidation`, Play Mode): **66/66** — Memory 8, Motion 7, Pursuit 5, ProbabilitySearch 7, Graph 5, Interception 6, Belief 9, Lifts 11, Navigation 8 (gồm 5 kiểm tra chọn thang/cầu thang: cabin chờ sẵn → đi thang; cabin đỗ ở tầng cao → cầu thang; lên 1 tầng → cầu thang; đích về cùng tầng → hủy chuyến; đọc được bảng số tầng từ sảnh).
- `Validation/TraversalPlayMode.txt` (`ShabanTraversalPlayTest`): **10/10 tuyến** — cầu thang dốc khối C lên/xuống qua 4 link, cửa A7N phía tây hai chiều, và các tình huống thang/cầu thang (chi tiết: `Validation/Hunter/11-LiftsLinksObservation.md`). Không có frame nào nhanh bất thường (không teleport), agent luôn gắn lại NavMesh, cửa thang đóng lại sau khi quái ra.
- `Validation/NavigationPlayMode.txt`: **3 tuyến pass** trong Play Mode: sân → tầng 3 E → tầng 6 X → tầng 2 A; không có lần overlap với cửa đang đóng. Test chạy thời gian 3x; displacement theo frame trong báo cáo chịu ảnh hưởng thời gian frame Editor.
- `Validation/NavigationAudit.json`: **1,010/1,012 điểm có tuyến**, 0 điểm tách khỏi graph (508 tuyến dùng Room Graph). Console cuối cùng không có error/warning.
- Kịch bản săn đuổi nhiều tầng: `Validation/Hunter/10-MultiFloorScenarios.md`.
- Đo tham khảo trong Editor: tuyến graph 11 chặng trung bình khoảng 0.68 ms sau warm-up (5 lần); đây không phải benchmark trên mọi máy.

### Các điểm navigation còn hạn chế

- Audit (`Validation/NavigationAudit.json`): **1,010/1,012** điểm có tuyến, **0** điểm bị tách khỏi graph. Hai điểm còn lại là phía ngoài `A7N_Door_E_28` và `A7N_Door_E_32`: cửa mở ra khoảng không ở tầng 8–9 (không có sàn/ban công bên ngoài) — không phải lỗ hổng NavMesh; muốn đi qua được cần bổ sung kiến trúc.
- Đã sửa: phía cầu thang `A7N_Door_W_28/W_32` (NavMeshLink), `Block_X_Stair_East_GndExit` và hai phía `I_F01_M2_Doors 1` (cánh cửa phụ trước đây không được loại khỏi bake), cầu thang lõi khối C tầng 1–3 (NavMeshLink).
- Link được leo chậm hơn chạy (1.6–3.2 m/s). Ước lượng thời gian đi bộ dùng đường NavMesh + chi phí quay đầu ở khúc cua, sai số đo được ~5–10%.

Scene trước thay đổi: `Backups/SampleScene-before-Shaban.unity`; bản sao trước các đợt nâng cấp săn đuổi: `Backups/ShabanMultiFloorHunt-20260927`, `Backups/ShabanLiftsNavFix-20260927`. Ngoài thư mục MonsterShaban và các dữ liệu scene/navigation, thay đổi script có sẵn gồm: hook mở cửa cho quái (`CampusAutomaticDoor.cs`), danh sách vật cản giữ cửa thang (`CampusElevator.cs`), và gắn `NavMeshModifier` loại cánh cửa khỏi bake trong hai script Editor dựng cửa (`CampusDoorSetup.cs`, `CampusTraversalRepair.cs`).
