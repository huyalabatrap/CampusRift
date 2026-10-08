from b1007_runner import *
target=OUT/'retests';target.mkdir(exist_ok=True)
def focused(name,component,field,report,done,timeout=180):
    folder=target/name;folder.mkdir(exist_ok=True)
    if (folder/'row.json').exists():return
    if (folder/'invoked.json').exists():raise RuntimeError('Focused retest already invoked '+name)
    prepare('MainMenu' if name=='ARNavigationPlayTest' else 'SampleScene');stamp=time.time();save(folder/'invoked.json',{'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'field':field})
    args='var go=new GameObject("Focused failed items");go.SetActive(false);var t=go.AddComponent<'+component+'>();'+field+';go.SetActive(true);return true;'
    save(folder/'invocation-result.json',code(args));end=time.time()+timeout;p=ROOT/done
    while time.time()<end:
        if p.exists() and p.stat().st_mtime>=stamp and (name!='ShabanTraversal' or p.read_text(encoding='utf-8-sig').startswith('DONE')):break
        time.sleep(1)
    else:raise TimeoutError(name)
    source=ROOT/report;shutil.copy2(source,folder/('result'+source.suffix));data=json.loads(source.read_text(encoding='utf-8-sig')) if source.suffix=='.json' else p.read_text(encoding='utf-8-sig')
    status,failures=evaluate(data)
    if not isinstance(data,dict):status='FAIL' if ' FAIL:' in data else 'PASS'
    console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(folder/'console.json',console)
    if console.get('data'):status='FAIL'
    save(folder/'row.json',{'name':name,'status':status,'failures':failures,'summary':data});progress('Retest riêng1 lần '+name+' → '+status+'; evidence regression/retests/'+name);print(name+' '+status,flush=True)
if __name__=='__main__':
    jobs=[('LookSmoke','CampusRift.Controls.LookSmoke','t.GoldenBellOnly=true','task/look/smoke.json','task/look/smoke-DONE.txt',80),('BoostEnergy','CampusRift.Controls.BoostEnergyPlayTest','t.ExhaustionOnly=true','Artifacts/BoostEnergy/Validation.json','Artifacts/BoostEnergy/DONE.txt',80),('Level8to10PlayTest','CampusRift.Validation.Level8to10PlayTest','t.ResultScreenOnly=true','task/p19/regressions/levels/Level8to10PlayTest.json','task/p19/regressions/levels/smoke-DONE.txt',180),('ShabanTraversal','ShabanTraversalPlayTest','t.only="car seen parked at F13"','Temp/shaban-traversal-progress.txt','Temp/shaban-traversal-progress.txt',160)]
    # Initial navigation attempt stopped before entering AR: no passing item is repeated.
    jobs.append(('ARNavigationPlayTest','CampusRift.AR.ARNavigationPlayTest','t.name="Continue navigation from real Hub"','task/ar/m7/navigation.json','task/ar/m7/DONE.txt',240))
    selected=set(sys.argv[1:])
    for args in jobs:
        if not selected or args[0] in selected:focused(*args)
    stop()
