from ar_fix3_local import *
if (out/'placement-timing.json').exists(): (out/'placement-initial-sparse.json').write_bytes((out/'placement-timing.json').read_bytes())
for angle in range(150,216,5):
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.rotation=UnityEngine.Quaternion.Euler(45,'+str(angle)+',0);return true;')
    time.sleep(.15)
code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.rotation=UnityEngine.Quaternion.Euler(40,180,0);return true;')
time.sleep(2)
save(out/'placement-rescanned.json',code(Path('task/ar/fix3-placement-inspect.cs').read_text()))
print((out/'placement-rescanned.json').read_text(encoding='utf-8'))
