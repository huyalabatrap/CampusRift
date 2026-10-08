from ar_nohand2 import *
connect();call('refresh_unity',{})
save('startup-deadline.json',code((OUT/'startup-deadline.cs').read_text(encoding='utf-8')))
save('startup-console.json',call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True}))
assert not json.loads((OUT/'startup-console.json').read_text())['data']
milestone('Phát hiện/sửa rẻ backend sau suite: ARRecoveryGate.Watchdog1s trước đây áp cả !Ready lúc graph/delegate đang tạo, có thể ép GPU→CPU sớm. Log cũ PID21238 dòng46464 lúc20:50:58.676 ghi delegate=pending/recovering=True trước readyCPU; phù hợp startup watchdog nhưng không đủ chứng minh từng lần delegate đổi. Nay khởi tạo riêng5s; inference đã submit vẫn1s, retry lỗinative/recovery2s không đổi. Focused10case C# deadline PASS + compileConsole0; D1/motion/geometry từ hai suite không đổi, không rerun hai suite. Build1 hoàn tất trước sửa này được giữ build-attempt1 cùng APK; cần finalbuild2 và chỉ verify/install finalhash. Evidence startup-deadline.json/startup-console.json.')
# Previous build completed; preserve its ledger before opening a new final-build ledger.
for path in (OUT/'build').iterdir():
    if path.is_file():
        assert (OUT/'build-attempt1/build'/path.name).exists()
        path.unlink()
print('Startup deadline focused checks PASS; ready for final build')
