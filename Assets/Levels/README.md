# Levels

10 màn dạng dữ liệu (`LevelDefinition`, `WaveDefinition`), `LevelDirector`, vùng Khe Nứt, sự kiện màn, sao.

- Namespace: `CampusRift.Levels`
- Task: `task/P05`, `task/P12`, `task/P16`

## Thành phần (P05)

| File | Việc |
|---|---|
| `Runtime/LevelDefinition.cs` | Dữ liệu một màn: đợt quái, hệ số HP/ST/Tốc, cấp AI, bầu trời, điểm xuất phát, vùng Khe Nứt (`RiftZoneData`), điều kiện sao |
| `Runtime/LevelCatalog.cs` | 10 màn theo thứ tự, nằm trong `Resources/LevelCatalog.asset` |
| `Runtime/LevelSession.cs` | Màn đang chọn, số lần thử, bộ kỹ năng mang theo, tiến độ (`PlayerPrefs` `CampusRift.Levels.Completed.v1`, tạm cho đến save v2 ở P09) |
| `Runtime/LevelDirector.cs` | Sinh quái theo đợt (PC 14, mobile 9 cùng lúc), nghỉ 15 s, thắng/thua đúng một lần; `HoldWin/ReleaseWin` cho boss (P12) và cự thú (P15) |
| `Runtime/LevelBootstrap.cs` | Tự bắt đầu màn khi scene chơi nạp xong và `LevelSession.Current != null` |
| `Runtime/RiftPortal.cs` | Vết nứt tím (tạo bằng code, có pool và âm thanh tổng hợp) |
| `Runtime/LevelEvents.cs` | `LevelStarted, WaveStarted, WaveCleared, EnemyKilled, LevelWon, LevelLost` |
| `Editor/LevelSetup.cs` | Menu `Campus Rift/V2/Setup Levels`: ghi lại 10 asset từ bảng §2.1 |
| `Editor/LevelValidation.cs` | `Validate Levels` và `Snap Rift Zones To NavMesh` |
| `Validation/LevelFlowPlayTest.cs` | Harness Play Mode (47 kiểm tra) |

Mở scene chơi trực tiếp (không qua menu) thì **không** có màn nào chạy: scene giữ hành vi cũ, dùng cho các harness QA.
`GameSceneManager.StartSandbox()` nạp scene chơi kiểu này.
