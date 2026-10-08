from ar_nohand import *
connect();milestone('Mốc20:40 chỉ xác nhận restoration Editor của build1, chưa đóng job: còn device-ready và build2 cho sửa shader từ log thật. Chưa viết REPORT. First reinstall thất bại không có reason; đã hỏi trạng thái sử dụng/unlock Realme để tránh tranh thao tác; không tự lặp install. Build2/verify nguồn cuối vẫn tiếp tục độc lập.')
# Verification itself is read-only; leave device installation/navigation to the coordinated step.
p=ROOT/'Tools/ar_nohand_verify.py';s=p.read_text(encoding='utf-8-sig').replace('if connected:\n', 'if connected and "--install" in sys.argv:\n');p.write_text(s,encoding='utf-8')
print(subprocess.check_output([str(ADB),'-s','WGH6S8I7GIMBGQKR','shell','dumpsys','power'],text=True,encoding='utf-8',errors='replace').split('mWakefulness=')[-1][:120])
