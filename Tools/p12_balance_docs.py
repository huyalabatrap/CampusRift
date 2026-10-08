"""Render measured balance evidence without changing gameplay targets or masking failures."""
import json
from pathlib import Path

root=Path('Artifacts/Levels')
audit=json.loads((root/'P12-Balance.json').read_text(encoding='utf-8-sig'))
rows={r['level']:r for r in audit['levels']}
realms=['Luyện Khí','Trúc Cơ','Kết Đan','Nguyên Anh','Hóa Thần','Luyện Hư','Độ Kiếp']
pars=[240,300,420,480,600,660,780,720,900,1080]
def table(levels):
    lines=['| Màn | Cảnh giới | Hạ / tổng | Thời gian thực | Par | Tỉ lệ | HP còn | Kết quả | ±30% |',
           '|---|---|---:|---:|---:|---:|---:|---|---|']
    for n in levels:
        r=rows.get(n)
        if not r:
            lines.append(f'| {n} | Chưa đo | — | — | {pars[n-1]}s | — | — | Chưa có dữ liệu | Chưa đạt |')
            continue
        lines.append(f"| {n} | {realms[r['realm']]} {r['tier']} | {r['kills']}/{r['total']} | {r['seconds']:.2f}s | {r['par']:.0f}s | {r['seconds']/r['par']*100:.1f}% | {r['healthLeft']:.1f}/{r['maxHealth']:.0f} | {'Thắng' if r['won'] else 'Không thắng'} | {'PASS' if r['durationTarget'] else 'FAIL'} |")
    return '\n'.join(lines)
def samples(levels):
    lines=['| Màn | Mẫu quái thường: HP, số hit combo và sát thương thực nhận |','|---|---|']
    for n in levels:
        if n in rows:lines.append(f"| {n} | "+'; '.join(rows[n]['normalSamples'])+' |')
    return '\n'.join(lines)
def bosses():
    p=root/'P12-BossBalance.json'
    if not p.exists():return 'Thời gian boss chưa có lượt đo riêng hợp lệ. Smoke DEV sát thương100000 không dùng làm cân bằng.'
    data=json.loads(p.read_text(encoding='utf-8-sig'))
    lines=['| Boss | Cảnh giới | Hạ boss | Thời gian gặp → chết | Mục tiêu | Kết quả |','|---|---|---|---:|---|---|']
    for r in data['levels']:
        lo,hi=(60,90) if r['level']==5 else (90,120)
        ok=r['won'] and lo<=r['bossSeconds']<=hi
        lines.append(f"| {r['level']} | {realms[r['realm']]} {r['tier']} | {'Có' if r['won'] else 'Không'} | {r['bossSeconds']:.2f}s | {lo}–{hi}s | {'PASS' if ok else 'FAIL'} |")
    return '\n'.join(lines)+'\n\nLượt riêng dùng cùng boss/chỉ số/AI ở sân, bỏ các đợt thường để đo boss; không thay thế bằng chứng thắng toàn màn.'
for name,levels in [('Balance-1-10.md',range(1,11)),('Balance-3-7.md',range(3,8))]:
    text='# Cân bằng P12 — số đo thực, chưa đạt toàn bộ tiêu chí\n\n'
    text+=f"Raw: `P12-Balance.json`, lần ghi UTC `{audit['at']}`; harness `Artifacts/P12/tests/P12BalancePlayTest.json`.\n\n"
    text+='Đúng cảnh giới/tầng đề nghị, chỉ số thường, crit tắt để so tuning; không hồi máu bằng vật phẩm, hồi sinh hay tăng chỉ số giữa trận. Kiếm và kỹ năng có chi phí Linh Lực/hồi chiêu thật; chạy/né bằng CharacterController, NavMesh chỉ lập đường, không warp trong trận. Đây là bot chiến thuật, không phải playtest người chơi. Các lượt lỗi fixture cũ giữ ở `balance-runs/`.\n\n'
    text+=table(levels)+'\n\n'
    text+='Mốc thời gian thiết kế và khoảng nghỉ15s giữ nguyên. Các màn thắng quá nhanh vẫn FAIL; lượt thua không chứng minh màn cân bằng hoặc không thể thắng bằng người chơi. Không dùng thời gian đứng chờ để đạt par. Cần quyết định thiết kế giữa quy mô các đợt hiện tại và mốc4–18phút.\n\n'
    text+=samples(levels)+'\n\n'
    text+='Mẫu dùng `ResolveHit` theo combo100/100/160% và `ApplyDamage` thật sau khi i-frame hết. Thiết Giáp Ngưu HP220×hệ số, phòng thủ20% là vai trò giáp nặng, tách khỏi mục tiêu2–3hit của quái thường. Boss/elite giữ HP900/1500/3450 theo đặc tả.\n\n'
    text+=bosses()+'\n'
    (root/name).write_text(text,encoding='utf-8')
