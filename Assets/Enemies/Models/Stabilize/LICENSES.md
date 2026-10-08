# STABILIZE · phụ kiện Ảnh Yêu

Mesh cloak rách, hood, khăn che mặt, dao, tunic và đai lưng; texture vải128×128 được tạo trực tiếp bằng Unity trong `Assets/Editor/StabilizeModelPolish.cs`, ngày04/10/2026. Các mesh/texture mới này được phát hành CC0 1.0; không có tài nguyên tải thêm. Material dùng shader của project, không đổi giấy phép shader/runtime.

Phụ kiện bind vào rig Night Demon của Morgan Strauss (nubux). Night Demon và clip idle dẫn xuất vẫn giữ attribution/CC-BY3.0 tại [LICENSES nguồn](../ReplacementP19/LICENSES.md). STABILIZE hạ tay trong clip idle, thêm phụ kiện weighted, điều chỉnh material. Không sửa FBX hay bản tải nguồn. Bat chỉ giảm visual scale1→0,8; nguồn rubberduck/Yughues CC0 giữ nguyên. Không thay Mage CC-BY-SA3.0, Golem hoặc7model người dùng.

Tái tạo trong Edit Mode: `StabilizeModelPolish.Install()`; thao tác idempotent cho prefab và mesh, không nhân scale mỗi lần chạy. Backup trước sửa ở `Backups/STABILIZE-pre-20261004`. Smoke và ảnh: `task/stabilize/REPORT-STABILIZE.md`.
