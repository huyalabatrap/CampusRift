from pathlib import Path
p=Path('Assets/ARRift/Runtime/ARSkillCaster.cs');s=p.read_text(encoding='utf-8');s=s.replace('clonedHand.maximumHeight*=field.Scale;','clonedHand.maximumHeight=Mathf.Min(clonedHand.maximumHeight,3.5f)*field.Scale;');p.write_text(s,encoding='utf-8')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Soi ảnh phát hiện healthbar thường còn kích thước/offset gốc trong AR và hiệu ứng overhead quá cao: sửa riêng nhánh AR (EnemyHealthBars, hand height, rain height). Raw harness23/2 giữ nguyên; camera fixture khác M5. Rain visual và damage nay cùng tâm nằm trọn trong disc để tránh dồn hit ra mép; tiếp kiểm hẹp2combo, không chạy lại full harness.\n')
print('followup applied')
