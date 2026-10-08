from b1007_ar import setup
from b1007_retest import focused
from b1007_runner import *
folder=OUT/'retests/ARRiftPlayTest';folder.mkdir(parents=True,exist_ok=True)
if not (folder/'row.json').exists():
    if (folder/'invoked.json').exists():raise RuntimeError('ARRift failed-item retest already invoked')
    setup();stamp=time.time();save(folder/'invoked.json',{'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'onlySkill':0,'items':3})
    code('var go=new GameObject("Only failed Giant Hand items");go.SetActive(false);var h=go.AddComponent<CampusRift.AR.ARRiftPlayTest>();h.OnlySkill=0;go.SetActive(true);return true;')
    p=ROOT/'task/ar/goiA/AR-DONE.txt';deadline=time.time()+100
    while time.time()<deadline:
        if p.exists() and p.stat().st_mtime>=stamp:break
        time.sleep(1)
    else:raise TimeoutError('Focused AR Giant Hand checks')
    data=json.loads((ROOT/'task/ar/goiA/ar-results.json').read_text(encoding='utf-8-sig'));save(folder/'result.json',data);status,failed=evaluate(data)
    console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(folder/'console.json',console)
    if console.get('data'):status='FAIL'
    save(folder/'row.json',{'name':'ARRiftPlayTest','status':status,'failures':failed,'summary':data});progress('Retest riêng1 lần ARRift3mụcThiênThủ → '+status+'; evidence regression/retests/ARRiftPlayTest');print('ARRiftPlayTest '+status,flush=True)
focused('ARNavigationPlayTest','CampusRift.AR.ARNavigationPlayTest','t.name="Continue navigation from real Hub"','task/ar/m7/navigation.json','task/ar/m7/DONE.txt',240)
stop()
