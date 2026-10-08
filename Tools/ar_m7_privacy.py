"""Small consent/aggregate-only check, isolated from user telemetry."""
from ar_session import *
script='''
var manager=CampusRift.UI.SettingsManager.Instance;
var original=manager.Current.Copy();
var telemetry=CampusRift.Progression.LocalTelemetry.Instance;
int before=telemetry.WrittenRows;
CampusRift.Progression.LocalTelemetry.TestFolder="task/ar/m7/telemetry";
var settings=original.Copy();settings.LocalTelemetryEnabled=false;manager.Apply(settings,false);
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var host=new UnityEngine.GameObject("AR consent check");
var counter=host.AddComponent<CampusRift.AR.ARGestureTelemetry>();
var count=typeof(CampusRift.AR.ARGestureTelemetry).GetMethod("Count",flags);
var flush=typeof(CampusRift.AR.ARGestureTelemetry).GetMethod("Flush",flags);
count.Invoke(counter,new object[]{"recognized:Open_Palm"});flush.Invoke(counter,null);
bool off=telemetry.WrittenRows==before;
UnityEngine.Object.DestroyImmediate(host);
settings.LocalTelemetryEnabled=true;manager.Apply(settings,false);
host=new UnityEngine.GameObject("AR consent check enabled");counter=host.AddComponent<CampusRift.AR.ARGestureTelemetry>();
count.Invoke(counter,new object[]{"recognized:Victory"});count.Invoke(counter,new object[]{"fired:Victory"});flush.Invoke(counter,null);
bool on=telemetry.WrittenRows==before+1;
UnityEngine.Object.DestroyImmediate(host);
manager.Apply(original,false);CampusRift.Progression.LocalTelemetry.TestFolder=null;
return new {optOutWritesNothing=off,optInWritesOneAggregate=on};
'''
save('task/ar/m7/privacy.json',code(script))
console('task/ar/m7/privacy-console.json')
