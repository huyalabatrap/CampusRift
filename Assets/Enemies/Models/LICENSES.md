# Giấy phép tài nguyên quái vật

| Model | Dùng cho | Tác giả | Giấy phép | Nguồn |
|---|---|---|---|---|
| Goblin | Tiểu Yêu (`TieuYeu.prefab`) | Quaternius | CC0 1.0 (Public Domain) | https://poly.pizza/m/OdCOFSmEhl |
| Green Spiky Blob | Độc Nhãn Xạ Thủ (`DocNhan.prefab`) | Quaternius | CC0 1.0 (Public Domain) | https://poly.pizza/m/IoWG5F9WUc |

Cả hai thuộc gói "Ultimate Monsters Bundle" của Quaternius (https://poly.pizza/bundle/Ultimate-Monsters-Bundle-5oyGWAmOB6).
CC0 không yêu cầu ghi công; vẫn ghi ở đây và trong màn danh sách thực hiện (P21-T04).

## Xử lý đã làm
- Tải `.glb` từ Poly Pizza, chuyển sang FBX bằng Blender 5.2 chạy nền (script gốc: `Tools/monsters_convert.py`).
- Đổi tên clip animation: `Attack`, `Death`, `Hit`, `Idle`, `Walk`/`Run`.
- Model **không** giữ vật liệu glTF: vật liệu URP dựng lại trong `Assets/Enemies/Materials/` (màu linear của glTF đổi sang sRGB). Tiểu Yêu được nhuộm màu vàng đất (hệ Thổ) bằng `_BaseColor`.
- File nguồn (`Source/*.fbx`, `*.json` thông tin, `*_Atlas.png`) giữ nguyên để có thể chuyển đổi lại.
