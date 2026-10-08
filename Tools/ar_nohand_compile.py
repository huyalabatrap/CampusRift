from ar_nohand import *
connect()
save('compile-after-fix.json',call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True}))
print(json.dumps(code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8')),ensure_ascii=True))
s=(OUT/'motion-before.cs').read_text(encoding='utf-8-sig').replace('motion.Process(f,camera', 'f.worldLandmarks=new float[63];motion.Process(f,camera')
save('motion-degenerate-after.json',code(s))
milestone('Mốc sửa nguồn: Cancel chỉ hủy động tác, không tạo BlocksStatic khi chưa có pinch/swipe; hiện tay và geometry/world được phân biệt, D1 tự giữ latch sau motion thật. Hai tay giữ rearm từ absence đã đạt khi tạo identity mới, wrist jump vẫn bắt buộc nhả. Thêm ready/hands/delegate/resultsHz/model/score/current+last reject/motion/nativeError trên [ARDiag] và KiểmẤn; lỗi native tự mở ô, giữ nguyên lỗi gốc qua recovery. Đã lưu kiểm hẹp degenerate trước/sau; thresholds/center aim/polygon/native/dependencies/manifest không đổi. Chuẩn bị mock duy nhất và 2 suite mỗi cái một lượt, chưa build.')
