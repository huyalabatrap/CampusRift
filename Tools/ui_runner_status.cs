var runner=UnityEngine.Object.FindAnyObjectByType<UIPlayValidation>();
if(runner==null)return "runner not found";
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var r=(UIPlayValidation.Report)typeof(UIPlayValidation).GetField("report",flags).GetValue(runner);
return new {active=runner.isActiveAndEnabled,passed=r.passed,failed=r.failed,captures=r.captures,screen=new[]{UnityEngine.Screen.width,UnityEngine.Screen.height},time=UnityEngine.Time.unscaledTime};
