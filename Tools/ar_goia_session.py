import sys,json,time,pathlib
import unity_mcp as u
OUT=pathlib.Path('task/ar/goiA')
def connect(): u.initialize()
def code(s):
    r=u.call('execute_code',{'action':'execute','code':s,'safety_checks':False})
    obj=r.get('result',{}).get('structuredContent')
    if obj is None:obj=json.loads(r['result']['content'][0]['text'])
    if not obj.get('success'):raise RuntimeError(obj)
    return obj.get('data',{}).get('result')
def save(name,r):(OUT/name).write_text(json.dumps(r,indent=2,ensure_ascii=False),encoding='utf-8')
if __name__=='__main__':
    connect()
    if sys.argv[1]=='accept':
        print(code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;'))
    elif sys.argv[1]=='place':
        code('var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>();h.SetHelp(false);h.SetMenu(false);var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.InputBlocked=true;var camera=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();camera.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);camera.transform.rotation=UnityEngine.Quaternion.Euler(40,180,0);return true;')
        print(code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();return new {p.ReticleValid,p.HitType,p.Message,root=p.Root!=null};'))
    elif sys.argv[1]=='state':
        print(json.dumps(code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();var f=p.GetComponent<CampusRift.AR.ARBattlefield>();var c=p.GetComponent<CampusRift.AR.ARSkillCaster>();return new {p.ReticleValid,p.HitType,p.Message,root=p.Root!=null,p.Adjusting,f.NavigationReady,f.Paused,c.AimValid,c.Fired,check=p.GetComponent<CampusRift.AR.ARGestureCheck>()!=null};')))
