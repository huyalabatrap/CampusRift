# P22 cosmetics — nguồn và cách thay

Bốn biến thể Thanh Ngọc, Bình Minh, Lăng Vân, Dạ Mộng do code P22 tạo bằng tint material quần áo/phụ kiện của rig SchoolGirl hiện có; không thêm chỉ số, không bán bằng tiền thật. MaterialPropertyBlock theo renderer/material index, không sửa material nguồn, mesh, bones hay clip.

PlayerPreview.prefab là bản visual-only của SchoolGirl Visual trong Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab, giữ Animator/controller/LOD của người dùng. Model, texture, rig và animation nền giữ giấy phép/quyền sử dụng của tài nguyên người dùng; không tái gán CC0. Không tải asset ngoài: brief PROMPT-P22 ưu tiên material/màu trên rig hiện có, nên dùng trực tiếp nguồn có sẵn và tạo biến thể bằng code.

Đổi màu trong PlayerCostume.Catalog; khóa mở bằng achievement id. Mặc định phục hồi BaseColor của shared material. Preview và gameplay cùng PlayerCostume.Apply. Rollback: bỏ module P22 và khôi phục các thư mục trong task/p22/BACKUP.txt. Không sửa scene/NavMesh.
