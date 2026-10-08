from pathlib import Path
p=Path('Assets/Combat/Runtime/DamageNumberPool.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('existing.text.SetText("{0:0}",Mathf.Ceil(existing.amount));','existing.text.text=UI.Accessibility.Symbol(existing.element)+" "+Mathf.CeilToInt(existing.amount);');p.write_text(s,encoding='utf-8')
print('damage merge symbol preserved')
