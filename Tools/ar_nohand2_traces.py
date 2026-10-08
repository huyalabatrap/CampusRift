from ar_nohand2 import *
connect()
data=code((OUT/'state-traces.cs').read_text(encoding='utf-8'))
save('state-traces.json',data)
assert data['assertions']=='PASS'
milestone('Đã sửa !allowed chỉ reset candidate, lifecycle chỉ khóa tư thế đã tiêu thụ; epoch khi startup không tạo khóa; ResetEnergy không tạo reject giả. None/không map hợp lệ trong khung ≥200ms/≥3frame nhả khóa, đổi nhãn held theo stableSwitch .2s và ≥3frame; giữ .6/3frame/.1s/lockout/aim. Motion chỉ block Pinching/Swiping đang armed, chỉ hoàn tất thật đặt latch dynamic; cancel/gap không latch. Trace C# thực chạy PASS tại state-traces.json: suspend→allowed có landmark và epoch mới, Open_Palm .7 fire222/200/167/100ms ở9/10/12/20Hz, held không lặp; None nhả; reject/switch; dynamic cancel/complete; invalid release. Unit chỉ đổi expected held_then_none từ1 sang2 theo yêu cầu mới, chưa invoked suite. Diagnostics ghi nguồn khóa/held/geometryReason/fingers/norm/world/convert/infer/queue; thêm log lý do GPU fallback, không đổi model/native contract/thresholds.')
print(json.dumps({'assertions':data['assertions'],'cadence':[(r['hz'],round(r['fireAfterMs'])) for r in data['traces'] if 'hz' in r]}))
