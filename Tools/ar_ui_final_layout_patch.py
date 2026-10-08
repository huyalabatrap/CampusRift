from ar_ui import *
p=ROOT/'Assets/ARRift/Runtime/ARBattleHUD.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('coverage.Add(spirit.rectTransform)','coverage.Add((RectTransform)spirit.transform.parent)')
s=s.replace('statusPanel,spirit.rectTransform','statusPanel,(RectTransform)spirit.transform.parent')
s=s.replace('spirit.gameObject.SetActive(fighting)','spirit.transform.parent.gameObject.SetActive(fighting)')
s=s.replace('Mathf.Lerp(680,650,','Mathf.Lerp(650,620,')
p.write_text(s,encoding='utf-8')
milestone('Duyệt ảnh trực tiếp tìm thêm 2 lỗi trình bày: các hàng ScrollRect đang neo giữa content nên có khoảng trống đầu; đã đổi neo hàng về đỉnh content để 6 công tắc đều thấy khi mở và chú thích cuộn tới được. Nền thanh Spirit còn hiện sau khi chỉ ẩn fill; đã ẩn/đếm toàn bộ track. Giảm điểm xuất phát animation nhãn rail sang trái để không giao icon. Source fix độc lập recognition; cần chụp lại views nguồn cuối trước build, không hồi quy. CanvasGroup thực tế là Unity fake-null ở GetComponent chứ không chỉ domain reload; đã sửa kiểm ==null explicit và lượt visual mới Console0.')
