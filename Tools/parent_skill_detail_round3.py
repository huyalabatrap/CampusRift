from pathlib import Path
p=Path('Assets/CampusRiftUI/Runtime/HubUI.cs');s=p.read_text(encoding='utf-8-sig')
start=s.index('            var detail=kit.Panel(content, "Detail Back"')
end=s.index('        static string CastName',start)
part=s[start:end]
part=part.replace('kit.Text(content,','kit.Text(detail,').replace('kit.Image(content,','kit.Image(detail,')
for a,b in [(690,30),(700,40),(960,300),(1024,364)]:part=part.replace(', '+str(a)+',',', '+str(b)+',')
s=s[:start]+part+s[end:];p.write_text(s,encoding='utf-8')
print('Skill details now live inside their rounded info panel')
