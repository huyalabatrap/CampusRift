# Model cho Unity

Đã xử lý theo thứ tự 005, 008, 010, 012, 013, 025, 027. File GLB gốc và Untitled.blend được giữ nguyên.

## Dùng trong Unity

1. Import `GameReadyModels.unitypackage` bằng Assets > Import Package > Custom Package.
2. Chọn Tools > Game Ready Models > Build materials, controllers and prefabs. Bước này chọn material phù hợp Built-in hoặc URP của project hiện tại.
3. Kéo prefab trong `Assets/GameReadyModels/<model>/Generated/` vào scene.
4. Animator có float `Speed`: 0 = idle, 0.1–0.65 = walk, trên 0.65 = run. Các state còn lại có trigger cùng tên, chẳng hạn `Attack_Light`, `Attack_Heavy`, `Death_Forward`, `Taunt`, hoặc động tác riêng của từng model.

```csharp
animator.SetFloat("Speed", 0.4f);
animator.SetTrigger("Attack_Light");
// Có thể gọi trực tiếp bất kỳ state nào trong controller:
animator.CrossFade("Idle_Look", 0.15f);
```

Animation là in-place cho locomotion. Code gameplay di chuyển GameObject; `Apply Root Motion` tắt. Các động tác turn, chết, spawn/despawn có chuyển động ở xương Root nội bộ. Spawn/despawn là animation trồi lên/chìm xuống; hiệu ứng tan biến bằng shader chưa được thêm.

Các creature dùng rig **Generic** để giữ xương cánh hoa, hai đầu, áo choàng và đạo cụ. Chưa cấu hình Avatar Humanoid để retarget Mixamo. HDRP chưa có preset material trong bộ này.

## File Blender và FBX

- `GameReady_AllModels.blend`: tất cả model xếp theo số thứ tự, đang mở trong Blender.
- Mỗi thư mục `<số>_<tên>` có `.blend` riêng, `.fbx`, `Textures`, ảnh `Before.png`/`After.png`, `Previews` và `report.json`.
- Trong Blender, chọn scene của model. NLA Editor chứa 28 track, xếp liên tiếp theo thời gian. Trong Action Editor, chọn action bắt đầu bằng số model để chỉnh sửa từng clip. Tắt các NLA track khi chỉnh một action riêng.
- FBX xuất ở 30 fps, mét, Y-up / -Z-forward, không thêm leaf bone, tối đa 4 influences/vertex.
- BaseColor, normal map và UV được giữ. Các texture ORM được chuyển thêm sang MetallicSmoothness (metallic R, smoothness A) và Occlusion cho Unity.
- Kích thước game đặt tạm: Zombie 2m, Reaper 1.5m, Mummy 1.2m, Halo Demon 2.1m, Flower Alien 2.2m, Flower Crawler 2m, Twin Head Demon 2.8m. Có thể thay đổi scale prefab theo game.

## Kết quả

| Số | Model | Triangles Blender | Xương | Clip |
|---|---|---:|---:|---:|
| 005 | Zombie | 58.000 | 53 | 28 |
| 008 | Grim Reaper | 58.000 | 67 | 28 |
| 010 | Pumpkin Mummy | 57.999 | 54 | 28 |
| 012 | Halo Demon | 58.000 | 66 | 28 |
| 013 | Flower Alien | 58.000 | 58 | 28 |
| 025 | Flower Crawler | 58.000 | 68 | 28 |
| 027 | Twin Head Demon | 57.999 | 69 | 28 |

24 clip chung: idle/breathe/look/alert, đi trước/sau, chạy, strafe trái/phải, quay trái/phải, 3 attack, hit 4 hướng, stagger, stun, chết trước/sau, spawn/despawn, taunt. Bốn clip riêng/model: claw/bite/sniff; hover/scythe/lantern; pumpkin/wave/bounce; halo/curse/levitate/bow; petals/spore/slash hoặc slam/roar; roar từng đầu và double claw.

Đã kiểm tra ảnh trước/sau và khoảng 20.000–21.000 điểm bề mặt/model. Sai lệch lớn nhất trên điểm lấy mẫu: 0,074–0,338% chiều cao; đây là phép đo lấy mẫu một chiều, không phải bảo đảm sai số tối đa trên toàn bộ bề mặt. Texture normal gốc giữ các chi tiết nhỏ, nhưng giảm gần 2 triệu xuống 58.000 triangles vẫn làm mất một phần chi tiết hình học rất nhỏ.

Rig của 025/027 được giữ và bổ sung; các model khác có rig mới. Có sửa trọng số ở đường nối UV, bổ sung vùng cứng và kiểm tra một số tư thế chuyển động. Animation được tạo bằng keyframe thủ tục và IK hai xương cho chân; biên độ giảm khi skin bị kéo quá mạnh. Bộ này cần xem lại tính thẩm mỹ, va chạm đạo cụ/áo và nhịp chân trong gameplay trước khi phát hành; chưa phải bộ animation mocap hoặc animation được nghệ sĩ chỉnh từng frame.

`Unity_Validation.json` ghi kiểm tra trong project Unity riêng: đủ 7 model, 196 clips, số triangles trong ngân sách, không vertex thiếu skin weights, không quá 4 influences/vertex. Unity loại bỏ 4 triangles suy biến của model 013 nên model đó có 57.996 triangles sau import. Đây là kiểm tra cấu trúc import, không thay thế kiểm thử animation và hiệu năng trên thiết bị game.

## Tham khảo định dạng

- [Blender FBX exporter](https://docs.blender.org/manual/en/5.3/files/import_export/fbx_legacy.html)
- [Unity: nhập model và chọn loại animation](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/models/importing/importing-model-files)
