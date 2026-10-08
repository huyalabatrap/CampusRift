"""Collect an already invoked suite; never invokes its component or assertions."""
from b1007_runner import *

name = sys.argv[1]
folder = OUT / 'runs' / name
assert not (folder / 'row.json').exists()
ledger = json.loads((folder / 'invoked.json').read_text(encoding='utf-8'))
case = ledger['case']
stamp = (folder / 'invoked.json').stat().st_mtime
p = ROOT / case['done']
deadline = stamp + case['timeout']
while time.time() < deadline:
    if p.exists() and p.stat().st_mtime >= stamp and (not case['contains'] or case['contains'] in p.read_text(encoding='utf-8-sig')):
        break
    time.sleep(1)
else:
    raise TimeoutError('Existing invocation has no completion: ' + name)
full = p.read_text(encoding='utf-8-sig')
shutil.copy2(p, folder / ('DONE' + p.suffix))
report = ROOT / case['report']
shutil.copy2(report, folder / ('result' + report.suffix))
if name.startswith('ShabanHunt-'):
    match = re.search(r'RESULT .*reacquiredAt=(-?[\d.]+).*', full)
    assert match
    status, failures = ('PASS' if float(match[1]) >= 0 else 'FAIL'), []
else:
    status, failures = evaluate(json.loads(report.read_text(encoding='utf-8-sig')))
console = call('read_console', {'action':'get','types':['error'],'count':100,'include_stacktrace':True})
save(folder/'console.json', console)
if console.get('data'):
    status = 'FAIL'
row = dict(name=name, component=case['component'], status=status, failures=failures,
           summary=full[:5000], report=(folder/('result'+report.suffix)).relative_to(ROOT).as_posix(),
           seconds=round(time.time()-stamp,1), collectedExistingInvocation=True)
save(folder/'row.json', row)
for source in (folder/'historical').rglob('*'):
    if source.is_file():
        shutil.copy2(source, ROOT/source.relative_to(folder/'historical'))
progress(name+' → '+status+'; thu invocation đang chạy, không gọi lại suite.')
stop()
print(name+' '+status, flush=True)
