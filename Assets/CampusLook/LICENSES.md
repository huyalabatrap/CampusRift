# Campus LOOK — nguồn texture

Tất cả bộ texture/HDRI dưới đây tải trực tiếp từ **Poly Haven**, giấy phép
[CC0 1.0](https://polyhaven.com/license). Được phép dùng, sửa và phân phối
trong game. Ngày tải: 02/10/2026. Bản nguồn, JSON API, URL từng map, MD5 và
SHA256 lưu tại `task/look/source/manifest.json`; script `Tools/look_textures.py`.

| Bộ CC0 | Dùng |
|---|---|
| [plastered_wall_04](https://polyhaven.com/a/plastered_wall_04) | Tường sơn ngà/vàng phai; biến thể màu từ ảnh thật |
| [concrete_floor_worn_001](https://polyhaven.com/a/concrete_floor_worn_001) | Bê tông, sàn corridor/cầu thang cũ |
| [brick_pavement](https://polyhaven.com/a/brick_pavement) | Gạch đỏ sân |
| [rusty_painted_metal](https://polyhaven.com/a/rusty_painted_metal) | Cửa/khung sắt, lan can tróc, thép xanh bạc |
| [rusty_corrugated_iron](https://polyhaven.com/a/rusty_corrugated_iron) | Tôn cũ mái |
| [wood_planks_grey](https://polyhaven.com/a/wood_planks_grey) | Cửa/bàn gỗ, thân cây |
| [sparse_grass](https://polyhaven.com/a/sparse_grass) | Cỏ đất khuôn viên |
| [brown_mud_03](https://polyhaven.com/a/brown_mud_03) | Đất bồn cây |
| [kloppenheim_06_puresky](https://polyhaven.com/a/kloppenheim_06_puresky) | Bầu trời chụp thực tế, bản tonemapped JPG |

PBR 1K: BaseColor sRGB, OpenGL Normal, ARM (R=AO, G=roughness, B=metallic).
Sky 2K. Nhập Android ASTC 6×6/mipmap, không readable. World-space shader,
macro-noise, mưa/rêu và notice cũ không thương hiệu là code gốc cho project;
không thêm mesh/decal object. Giữ UV và mesh FBX gốc. Quái/rồng P12 và ảnh UI
comic dùng nguyên nguồn/giấy phép của các phần đó.

`dusty_window_base.png` và `dusty_window_arm.png`: texture bụi kính tổng hợp
bằng noise xác định cho project, không dùng ảnh bên thứ ba. Kính giữ bề mặt
opaque của nguồn gốc; chỉ thay màu/texture/specular. 12 material dùng chung.
