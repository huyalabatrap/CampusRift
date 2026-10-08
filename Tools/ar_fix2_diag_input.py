from ar_fix2_local import *
def triple():
    for i in range(3):
        code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState {position=new UnityEngine.Vector2(UnityEngine.Screen.safeArea.xMax-10,UnityEngine.Screen.safeArea.yMax-10)}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));return true;');time.sleep(.08)
        code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState {position=new UnityEngine.Vector2(UnityEngine.Screen.safeArea.xMax-10,UnityEngine.Screen.safeArea.yMax-10)});return true;');time.sleep(.08)
triple();on=code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Visible;')
triple();off=not code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Visible;')
save(out/'diagnostics-input.json',dict(tripleClickShows=on,tripleClickHides=off))
print(on,off)
