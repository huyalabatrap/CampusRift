import json
from pathlib import Path

# UTF-8 authoring source. Never pipe Vietnamese source through an ANSI shell.
pages = [
('BFS: Tìm kiếm theo chiều rộng',
'BFS khám phá đồ thị theo từng lớp. Bắt đầu từ một đỉnh nguồn, đánh dấu đã thăm rồi đưa đỉnh đó vào hàng đợi. Mỗi bước, lấy đỉnh được đưa vào sớm nhất ra và thêm các đỉnh kề chưa được thăm. Đánh dấu ngay khi đưa vào hàng đợi để tránh thêm trùng khi đồ thị có chu trình.',
'A nối với B và C; B nối với D. Từ A, một thứ tự duyệt hợp lệ là A, B, C, D. B và C cách nguồn 1 cạnh; D cách nguồn 2 cạnh.',
'BFS dùng hàng đợi FIFO: vào trước, ra trước. BFS tìm số cạnh ít nhất trên đồ thị không trọng số; không đảm bảo chi phí nhỏ nhất khi trọng số các cạnh khác nhau.'),
('DFS và quay lui',
'DFS đi sâu theo một nhánh hết mức có thể, sau đó quay lui để khám phá các nhánh khác. Có thể dùng một ngăn xếp tường minh hoặc ngăn xếp lời gọi hàm để lưu đường đi hiện tại. Với đồ thị có chu trình, cần lưu các đỉnh đã thăm.',
'Với A → B → D và A → C, DFS có thể duyệt A, B, D, C. Thứ tự phụ thuộc cách xét các đỉnh kề; D được thăm trước không có nghĩa là D gần A hơn C.',
'DFS hữu ích để kiểm tra khả năng đi tới một đỉnh và khám phá cấu trúc đồ thị. DFS cơ bản không đảm bảo đường đi ngắn nhất.'),
('Tư duy về độ phức tạp',
'Độ phức tạp mô tả mức tăng tài nguyên khi kích thước đầu vào tăng. Công việc O(n) tăng tuyến tính; O(n²) tăng bậc hai. Khi xét tốc độ tăng tiệm cận, ta bỏ qua hệ số hằng, nhưng thời gian chạy thực tế còn phụ thuộc cách cài đặt.',
'Duyệt n phần tử một lần là O(n). Hai lượt duyệt độc lập vẫn là O(n). So sánh từng phần tử với mọi phần tử còn lại là O(n²).',
'Big O là giới hạn về tốc độ tăng, không phải đồng hồ đo thời gian. Hãy đếm công việc và nêu rõ cách biểu diễn dữ liệu cùng các giả định.'),
('Tìm kiếm nhị phân',
'Tìm kiếm nhị phân tìm một giá trị trong dữ liệu đã sắp xếp bằng cách liên tục so sánh với phần tử giữa. Nếu giá trị cần tìm nhỏ hơn, giữ nửa trái; nếu lớn hơn, giữ nửa phải. Dừng khi tìm thấy hoặc khi khoảng ứng viên rỗng.',
'Trong [2, 4, 7, 9, 12], tìm 9 bắt đầu bằng cách so sánh với 7 rồi tìm tiếp bên phải. Với hai biên đều được tính trong khoảng, cập nhật high = mid - 1 hoặc low = mid + 1 sau một phép so sánh không bằng.',
'Dữ liệu phải được sắp xếp theo cùng quy tắc so sánh. Mỗi vòng lặp phải làm khoảng tìm kiếm nhỏ lại.'),
('Chọn chiến lược trên đồ thị',
'Đồ thị mô hình hóa các đỉnh và những liên kết giữa chúng. Cạnh có hướng có chiều cụ thể; cạnh vô hướng cho phép đi cả hai chiều. Danh sách kề lưu các đỉnh kề; ma trận kề lưu một ô cho mỗi cặp đỉnh.',
'Dùng BFS để tìm số lần qua cửa ít nhất khi mỗi liên kết có chi phí như nhau. Dùng DFS để khám phá các phòng có thể đến được. Nếu chi phí khác nhau, cần thuật toán đường đi có trọng số phù hợp.',
'Chọn thuật toán dựa trên giả định của bài toán. Không áp dụng kết luận về đường đi ngắn nhất không trọng số cho các cạnh có chi phí khác nhau.')]

# prompt | four options | explanation; option positions match stable IDs 0..3.
questions = [
[
('BFS thường sử dụng cấu trúc dữ liệu nào?', ['Hàng đợi','Ngăn xếp','Đống (heap)','Mảng đã sắp xếp'], 'Hàng đợi xử lý theo thứ tự vào trước, ra trước, nhờ đó BFS duyệt từng lớp.'),
('BFS nên đánh dấu đỉnh kề là đã thăm vào lúc nào?', ['Ngay khi đưa vào hàng đợi','Chỉ khi thuật toán kết thúc','Không bao giờ','Sau khi duyệt mọi đỉnh kề của nó'], 'Đánh dấu ngay khi thêm vào hàng đợi giúp tránh thêm trùng và lặp lại công việc trên chu trình.'),
('BFS tối thiểu hóa đại lượng nào trên đồ thị không trọng số?', ['Số cạnh đi từ đỉnh nguồn','Tổng trọng số cạnh bất kỳ','Bộ nhớ trên mọi đồ thị','Số đỉnh của đồ thị'], 'Mỗi lớp BFS thêm một cạnh, nên lần phát hiện đầu tiên có số cạnh ít nhất.'),
('A nối với B và C; B nối với D. D thuộc lớp nào?', ['Lớp 2','Lớp 0','Lớp 1','Luôn là lớp 3'], 'A ở lớp 0. B và C ở lớp 1, nên D được tìm thấy qua B ở lớp 2.'),
('Vì sao cần tập hợp các đỉnh đã thăm?', ['Tránh khám phá lại các chu trình','Sắp xếp tất cả các đỉnh','Gán trọng số cho mọi cạnh','Đảo chiều mọi cạnh'], 'Tập đỉnh đã thăm ngăn việc duyệt lại đỉnh, kể cả trong đồ thị có chu trình.'),
('Điều gì xảy ra khi hàng đợi rỗng?', ['Đã xử lý mọi đỉnh có thể đến từ nguồn','Mọi đỉnh trong đồ thị đều liên thông','Thuật toán phải khởi động lại','Đồ thị không có cạnh'], 'BFS duyệt thành phần có thể đến từ nguồn; các đỉnh không liên thông vẫn có thể chưa được thăm.')
],[
('Có thể dùng cấu trúc nào để cài đặt DFS?', ['Ngăn xếp','Chỉ hàng đợi FIFO','Một biến đúng/sai','Chỉ tập hợp đã sắp xếp'], 'DFS hoạt động theo nguyên tắc vào sau, ra trước, thường thông qua lời gọi đệ quy.'),
('Khi gặp đường cụt, DFS làm gì?', ['Quay lui về điểm phân nhánh trước đó','Xóa đồ thị','Lặp lại mãi mãi','Nhảy đến đỉnh gần nhất'], 'Quay lui đưa thuật toán về điểm gần nhất còn nhánh chưa được khám phá.'),
('DFS cơ bản có đảm bảo đường đi ngắn nhất không?', ['Không','Luôn đảm bảo','Chỉ khi dùng đệ quy','Chỉ khi dùng ngăn xếp'], 'DFS ưu tiên đi sâu nên có thể tìm thấy một đường dài trước đường ngắn hơn.'),
('Vì sao DFS đệ quy có thể gây tràn ngăn xếp?', ['Nhánh đang duyệt có thể quá sâu','Đồ thị có tên','Mọi cạnh có trọng số bằng một','Hàng đợi rỗng'], 'Đệ quy quá sâu có thể vượt giới hạn ngăn xếp lời gọi; ngăn xếp tường minh tránh được giới hạn này.'),
('Với A → B → D và A → C, thứ tự nào có thể là DFS?', ['A B D C','D B A C','C A D B','B C A D'], 'Đi theo B trước sẽ khám phá D rồi mới quay về A để khám phá C.'),
('Điều gì ngăn DFS lặp vô hạn trên chu trình?', ['Theo dõi các đỉnh đã thăm','Đặt tên đỉnh theo bảng chữ cái','Chỉ dùng đệ quy','Đếm cạnh một lần'], 'Theo dõi đỉnh đã thăm giúp mỗi đỉnh đã khám phá không bị mở rộng lặp đi lặp lại.')
],[
('Duyệt toàn bộ n phần tử một lần có độ phức tạp nào?', ['O(n)','O(1)','O(log n)','O(n²)'], 'Một lượt duyệt thực hiện lượng công việc tỷ lệ với số phần tử.'),
('Hai lượt duyệt đầy đủ nối tiếp nhau có độ phức tạp nào?', ['O(n)','O(n²)','O(1)','O(log n)'], '2n vẫn tăng tuyến tính; Big O bỏ qua hệ số hằng 2.'),
('Hai vòng lặp lồng nhau, mỗi vòng chạy n lần, thường có độ phức tạp nào?', ['O(n²)','O(n)','O(1)','O(log n)'], 'Mỗi lần trong n lượt vòng ngoài thực hiện n lượt vòng trong: tổng cộng n × n.'),
('O(n) mô tả điều gì?', ['Tốc độ tăng theo kích thước đầu vào','Chính xác n mili giây','Luôn dùng bộ nhớ cố định','Ngôn ngữ lập trình'], 'Độ phức tạp mô tả mức tăng công việc, không phải thời gian chạy cố định.'),
('BFS dùng danh sách kề có độ phức tạp nào?', ['O(V + E)','O(1)','O(log V)','Luôn là O(V) với mọi đồ thị'], 'Mỗi đỉnh có thể đến được và các cạnh đi ra được xử lý một số lần hữu hạn.'),
('Đại lượng nào tăng nhanh hơn về mặt tiệm cận?', ['n²','n','log n','1'], 'Khi n đủ lớn, tăng trưởng bậc hai vượt tăng trưởng tuyến tính, logarit và hằng số.')
],[
('Tìm kiếm nhị phân chuẩn yêu cầu dữ liệu có tính chất gì?', ['Đã được sắp xếp','Có thứ tự ngẫu nhiên','Mọi phần tử bằng nhau','Là một đồ thị'], 'Dữ liệu đã sắp xếp cho phép loại bỏ một nửa dựa trên phép so sánh với phần tử giữa.'),
('Giá trị cần tìm lớn hơn phần tử giữa. Ta tìm tiếp ở đâu?', ['Nửa bên phải','Nửa bên trái','Luôn tìm cả hai nửa','Chỉ phần tử đầu tiên'], 'Trong dữ liệu sắp xếp tăng dần, mọi phần tử bên trái không lớn hơn phần tử giữa.'),
('Khoảng ứng viên thay đổi như thế nào?', ['Giảm còn khoảng một nửa','Tăng gấp đôi','Không đổi','Mỗi lần luôn giảm đúng một phần tử'], 'Mỗi phép so sánh loại bỏ khoảng một nửa số ứng viên còn lại.'),
('Tìm kiếm nhị phân trên mảng đã sắp xếp, truy cập trực tiếp, có độ phức tạp nào?', ['O(log n)','O(n²)','O(n!)','Luôn là O(n)'], 'Việc liên tục chia đôi cần số bước tăng theo logarit của n.'),
('Khi nào có thể kết luận tìm kiếm thất bại?', ['Khi khoảng ứng viên rỗng','Luôn sau phép so sánh đầu tiên','Chỉ sau khi sắp xếp lại','Không bao giờ'], 'Khoảng rỗng nghĩa là không còn vị trí nào có thể chứa giá trị cần tìm.'),
('Với biên bao gồm cả hai đầu và giá trị cần tìm > phần tử giữa, cập nhật nào làm khoảng nhỏ lại?', ['low = mid + 1','low = mid','high = mid + 1','low = 0'], 'Loại bỏ phần tử giữa đã kiểm tra đảm bảo thuật toán tiến lên và không bị mắc kẹt.')
],[
('Cạnh có hướng nghĩa là gì?', ['Một liên kết có chiều cụ thể','Liên kết luôn đi được cả hai chiều','Không có liên kết','Một đỉnh đã sắp xếp'], 'Cạnh từ A đến B không tự động tạo ra cạnh từ B về A.'),
('Để đi qua ít cửa nhất khi mỗi lần qua cửa có chi phí như nhau, nên chọn gì?', ['BFS','DFS cơ bản đảm bảo đường ngắn nhất','Sắp xếp theo bảng chữ cái','Đi ngẫu nhiên'], 'BFS khám phá các đỉnh theo số cạnh ít nhất, phù hợp khi mỗi lần đi qua có chi phí bằng nhau.'),
('Danh sách kề lưu thông tin gì?', ['Các đỉnh kề của mỗi đỉnh','Chỉ màu của đỉnh','Mọi đường đi có thể có','Chỉ đỉnh nguồn'], 'Mỗi đỉnh có một danh sách các đỉnh kề hoặc các cạnh đi ra.'),
('Ma trận kề của đồ thị có V đỉnh gồm bao nhiêu ô?', ['V²','V','log V','Luôn là 5'], 'Mỗi cặp đỉnh có thứ tự tương ứng với một ô, tổng cộng V × V ô.'),
('BFS có luôn tìm chi phí nhỏ nhất khi trọng số các cạnh khác nhau không?', ['Không','Có','Chỉ khi có tập đỉnh đã thăm','Chỉ khi dùng hàng đợi'], 'Đường ít cạnh nhất có thể đắt hơn một đường nhiều cạnh khi trọng số khác nhau.'),
('Điều gì nên quyết định việc chọn thuật toán?', ['Giả định bài toán và kết quả cần tìm','Tên hàm ngắn nhất','Luôn dùng đệ quy','Chỉ tên các đỉnh'], 'Tính đúng đắn phụ thuộc vào sự phù hợp với giả định như trọng số, hướng cạnh và kết quả cần có.')
]]
lessons=[]
for key,page,qs in zip(['bfs','dfs','complexity','binary','graphs'],pages,questions):
    title,body,example,takeaway=page
    lessons.append(dict(id=key,titleVN=title,pages=[dict(titleVN='GHI CHÉP THỰC ĐỊA',contentVN=body,exampleVN=example,takeawayVN=takeaway)],questions=[dict(promptVN=q[0],optionsVN=q[1],explanationVN=q[2]) for q in qs]))
Path('Assets/Localization/Editor/LearningVietnamese.json').write_text(json.dumps(dict(lessons=lessons),ensure_ascii=False,indent=2),encoding='utf-8')
