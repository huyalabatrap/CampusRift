# Ba rồng cho Unity

| Số | Model | Triangles | Xương Blender | Animation |
|---|---|---:|---:|---:|
| 020 | Silver Cloud Dragon | 58.000 | 71 | 40 |
| 023 | Azure Serpent Dragon | 58.000 | 85 | 40 |
| 026 | Lava Wing Dragon | 58.000 | 105 | 46 |

126 clip mới ở 30 fps. File gốc và gói 7 model trước được giữ nguyên.

## Import và chạy

1. Unity: Assets > Import Package > Custom Package > `Dragons_Flight.unitypackage`.
2. Chọn Tools > Game Ready Dragons > Build materials, controllers and prefabs để áp dụng material Built-in/URP của project.
3. Kéo prefab trong `Assets/GameReadyDragons/<model>/Generated/` vào scene.
4. Rig Generic; Apply Root Motion tắt. Dùng code gameplay điều khiển vị trí/hướng của prefab. Animation có chuyển động lên/xuống và nghiêng trong xương Root; tránh cộng thêm cùng độ cao hai lần khi cất/hạ cánh.

```csharp
animator.SetFloat("Speed", 0.4f); // 0: hover, 0.1–0.65: cruise, >0.65: fast
animator.SetTrigger("Bank_Left"); // trạng thái lặp: giữ đến khi đổi state
animator.SetTrigger("Glide");
animator.SetTrigger("Takeoff_Start"); // tự nối Lift -> ToCruise -> Fly_Cruise
animator.SetTrigger("Land_Touchdown"); // tự nối Settle -> Idle_Ground (026)
animator.SetTrigger("Spell_Start"); // tự nối Spell_Loop; kết thúc bằng Spell_End
animator.SetTrigger("Dive_Start"); // tự nối Dive_Loop; thoát bằng Dive_Recover
animator.CrossFade("Hover_Idle", .15f); // thoát trạng thái loop bất kỳ
```

Tất cả clip bổ sung có trigger cùng tên state (không có tiền tố số). Các loop đặc biệt giữ trạng thái đến khi gameplay chuyển state. Air_DeathStart nối Air_DeathFall; gameplay gọi Air_DeathImpact khi chạm đất. Ground_Death/Air_DeathImpact giữ tư thế cuối. Controller có mọi state để chỉnh tiếp theo cơ chế game.

020 và 023 là rồng không cánh, bay huyền ảo bằng sóng thân/đuôi. Sau Land_Settle, controller trở lại Hover_Idle; nếu muốn rồng không cánh đứng yên trên mặt đất, giữ frame cuối Land_Settle bằng controller của game. 026 có thêm idle/walk/run mặt đất; gọi trigger Walk_Ground/Run_Ground để dùng các clip này.

## File chỉnh sửa

- `GameReady_AllModels_10.blend`: 10 model xếp theo số; mỗi model có scene riêng.
- `020_SilverCloudDragon`, `023_AzureSerpentDragon`, `026_LavaWingDragon`: FBX, Blender, texture, Before/After, Previews và report.
- `Dragons_Animation_List.csv`: toàn bộ clip và trạng thái lặp.
- `Dragons_Research_Design.md`: nguồn tham khảo, thiết kế riêng từng rồng và giới hạn kiểm tra.
- `Dragons_Unity_Validation.json`: kết quả import thực tế trong Unity.

FBX dùng mét, Y-up, -Z-forward; 4 influences/vertex, không leaf bones. Texture gốc giữ nguyên; ORM chuyển sang MetallicSmoothness/Occlusion cho Unity. Các socket Mouth/Rider/TailTip là xương gắn VFX, không kèm hệ hạt. Chiều cao rest tạm đặt 020: 3m, 023: 3,5m, 026: 4m; có thể đổi scale prefab.

Animation được dựng bằng keyframe procedural theo hình dáng từng model, chưa được animator chỉnh tay từng frame. Đã kiểm tra các tư thế mẫu, giãn cạnh và đầu/cuối các loop; chưa đảm bảo mọi trường hợp phối trộn hay va chạm trong game. Hai rồng không cánh giữ dáng thân cuộn của mesh gốc. Sai số bề mặt đo từ mẫu không phải bảo đảm tuyệt đối giữ mọi chi tiết nhỏ.
