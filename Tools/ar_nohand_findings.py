from ar_nohand import *
connect()
# Prove invalid world geometry keeps the old motion blocker latched without a motion.
s=(OUT/'motion-before.cs').read_text(encoding='utf-8-sig').replace('motion.Process(f,camera', 'f.worldLandmarks=new float[63];motion.Process(f,camera')
save('motion-degenerate-before.json',code(s))
milestone('Điều tra theo thứ tự: native mặc định 1 tay, nhánh single giữ contract GóiA; setHandCount chỉ đổi khi bật 2tay và bridge rảnh. APK dev2 đã kiểm STORED/model hash/JNI signatures/ARM64 libs/manifest: nohand/old-apk/verification.json. Vosk/clip không mở khi mặc định tắt; chưa có log thiết bị để khẳng định nạp native runtime. Root code đã chứng minh: ARHandMotion.Cancel đặt BlocksStatic=true kể cả startup/world-degenerate; D1 dynamic-motion lập requireRelease dù không có pinch/swipe. nohand/motion-before.json và motion-degenerate-before.json. GóiA cho world thiếu/hỏng Neutral, Job5 biến thành chặn tĩnh. Matcher/mapping/pause/quality đọc code không thấy lock vĩnh viễn ở mặc định Làm quen; sẽ ghi đủ đúng/sai và dòng trong report cuối. Đang sửa false blocker và thêm chẩn đoán dev, giữ .6/3frame/100ms/nhả thật.')
