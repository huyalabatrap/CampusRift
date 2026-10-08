from b1007_runner import *
name='ShabanHunt-stairs';folder=OUT/'runs'/name
assert not (folder/'row.json').exists()
ledger=json.loads((folder/'invoked.json').read_text(encoding='utf-8'));case=ledger['case'];p=ROOT/case['done']
stamp=(folder/'invoked.json').stat().st_mtime
while not (p.exists() and p.stat().st_mtime>=stamp and p.read_text(encoding='utf-8-sig').endswith('DONE\n')):time.sleep(1)
full=p.read_text(encoding='utf-8-sig');match=re.search(r'RESULT .*reacquiredAt=(-?[\d.]+).*',full)
assert match
shutil.copy2(p,folder/'DONE.txt');shutil.copy2(p,folder/'result.txt')
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(folder/'console.json',console)
status='PASS' if float(match[1])>=0 and not console.get('data') else 'FAIL'
save(folder/'row.json',dict(name=name,component=None,status=status,summary=full[:5000],resultLine=match[0],seconds=round(time.time()-stamp,1),collectedExistingInvocation=True))
for source in (folder/'historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(folder/'historical'))
progress(name+' → '+status+'; thu lượt đang chạy sau khi tạm dừng runner, không tái gọi suite.');print(status,match[0],flush=True)
stop()
