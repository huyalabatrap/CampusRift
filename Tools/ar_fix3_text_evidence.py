from ar_fix3_local import *
save(out/'focused-text-final.json',code(Path('task/ar/fix3-focused-text.cs').read_text(encoding='utf-8')))
assert json.loads((out/'focused-text-final.json').read_text(encoding='utf-8'))['issues']==0
progress('C text hẹp đạt\n- Status cần rect40px (preferredHeight33,3px), baseliney2; kiểmhẹp đúng3labelđổi0issue. Fullauditraw1issue giữ nguyên. Sẽ đồng bộ rect vào source ởEdit sauharness, không sửacombat.')
print('Focused text: 0 issues')
