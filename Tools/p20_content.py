"""Append source-grounded P20 questions to authoring; regenerate CSVs. Existing IDs/text are preserved."""
from pathlib import Path
import subprocess,json,csv,re
root=Path(__file__).resolve().parents[1]
rows=[
('ch1-b1-p20-multi','ch1-b1','nhieu-dap-an','2','Theo khái niệm, tư tưởng Hồ Chí Minh kế thừa và tiếp thu những giá trị nào?','Các giá trị truyền thống tốt đẹp của dân tộc','Tinh hoa văn hóa nhân loại','Chỉ các giá trị kinh tế','Chỉ kinh nghiệm quân sự','A,B','Khái niệm nêu sự kế thừa các giá trị truyền thống tốt đẹp của dân tộc và tiếp thu tinh hoa văn hóa nhân loại.','GT 2021, tr. 12-13'),
('ch1-b1-p20-match','ch1-b1','noi-cap','2','Nối đại hội với mốc năm tương ứng.','Đại hội VII','Đại hội XI','Năm 1991','Năm 2011','a:1,b:2','Đại hội VII diễn ra năm 1991; Đại hội XI năm 2011.','GT 2021, tr. 12, 16-17'),
('ch1-b1-p20-fill','ch1-b1','dien-khuyet','1','Tư tưởng Hồ Chí Minh là một hệ thống quan điểm ___ về những vấn đề cơ bản của cách mạng Việt Nam.','toàn diện và sâu sắc','rời rạc và ngẫu nhiên','chỉ mang tính địa phương','chỉ mang tính kỹ thuật','A','Khái niệm xác định đó là hệ thống quan điểm toàn diện và sâu sắc.','GT 2021, tr. 12'),
('ch1-b2-p20-multi','ch1-b2','nhieu-dap-an','2','Hệ thống quan điểm của Hồ Chí Minh được phản ánh ở những nguồn nào sau đây?','Bài nói và bài viết','Hoạt động cách mạng','Cuộc sống hằng ngày','Chỉ trong sách giáo khoa','A,B,C','Giáo trình nêu bài nói, bài viết, hoạt động cách mạng và cuộc sống hằng ngày của Người.','GT 2021, tr. 19'),
('ch1-b2-p20-match','ch1-b2','noi-cap','2','Nối thuật ngữ với nội dung tương ứng.','Di sản tư tưởng','Hiện thực hóa','Toàn bộ những quan điểm của Hồ Chí Minh trong di sản của Người','Quá trình hệ thống quan điểm vận động trong thực tiễn','a:1,b:2','Đối tượng nghiên cứu gồm toàn bộ những quan điểm trong di sản và quá trình hiện thực hóa trong thực tiễn.','GT 2021, tr. 19-20'),
('ch1-b2-p20-fill','ch1-b2','dien-khuyet','1','Hồ Chí Minh học nằm trong ngành ___.','Khoa học chính trị','Khoa học tự nhiên','Kỹ thuật','Nghệ thuật','A','Môn học là nội dung của chuyên ngành Hồ Chí Minh học, nằm trong ngành Khoa học chính trị.','GT 2021, tr. 19'),
('ch1-b3-p20-multi','ch1-b3','nhieu-dap-an','2','Chọn các nguyên tắc phương pháp luận nghiên cứu tư tưởng Hồ Chí Minh.','Thống nhất lý luận và thực tiễn','Quan điểm lịch sử - cụ thể','Quan điểm toàn diện và hệ thống','Tách rời lý luận khỏi thực tiễn','A,B,C','Ba lựa chọn đầu thuộc các nguyên tắc trong giáo trình; lý luận và thực tiễn phải thống nhất.','GT 2021, tr. 21-25'),
('ch1-b3-p20-match','ch1-b3','noi-cap','2','Nối phương pháp với cách nghiên cứu tương ứng.','Phương pháp lôgíc','Phương pháp lịch sử','Tìm bản chất vốn có và khái quát thành lý luận','Theo trình tự thời gian, từ phát sinh, phát triển đến hệ quả','a:1,b:2','Phương pháp lôgíc khái quát bản chất; phương pháp lịch sử nghiên cứu quá trình theo trình tự thời gian.','GT 2021, tr. 25-26'),
('ch1-b3-p20-fill','ch1-b3','dien-khuyet','1','Hồ Chí Minh ví lý luận như ___ chỉ phương hướng cho công việc thực tế.','cái kim chỉ nam','cái hòm đựng sách','cái đích để bắn','cái mũ để trang trí','A','Lý luận như cái kim chỉ nam, chỉ phương hướng cho công việc thực tế.','GT 2021, tr. 21')]
p=root/'Content/src/ch1.txt';text=p.read_text(encoding='utf-8')
for r in rows:
    if '#q '+r[0]+' | ' not in text:text+='\n#q '+' | '.join(r)+'\n'
p.write_text(text,encoding='utf-8')
subprocess.run(['python',str(root/'Tools/content_build.py')],cwd=root,check=True)
cards=[]
for r in csv.DictReader((root/'Content/lessons.csv').open(encoding='utf-8-sig',newline='')):
    if r['ghi_nho']:
        cards.append({'id':r['bai_id']+'/card/'+r['trang_so'],'bai_id':r['bai_id'],'mat_truoc':r['tieu_de_trang'],'mat_sau':r['ghi_nho'],'nguon':'; '.join(re.findall(r'\((?:Giáo trình|GT)\s*2021,\s*tr\.[^)]+\)',r['noi_dung_trang']))})
with (root/'Content/flashcards.csv').open('w',encoding='utf-8',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(cards[0]));w.writeheader();w.writerows(cards)
(root/'task/p20/content-provenance.json').write_text(json.dumps({'newQuestionIds':[r[0] for r in rows],'source':'Content/src/ch1.txt, existing supplied textbook pages 12–26','cards':len(cards),'missingCardSource':[c['id'] for c in cards if not c['nguon']],'status':'awaiting instructor review'},ensure_ascii=False,indent=2),encoding='utf-8')
