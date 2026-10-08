from pathlib import Path
import json,sys
import unity_mcp as m
m.initialize()
def code(value):
    result=m.call('execute_code',{'action':'execute','code':value})['result']
    data=result.get('structuredContent') or json.loads(result['content'][0]['text'])
    print(json.dumps(data,ensure_ascii=True))
    return data
name=sys.argv[1]
if name.startswith('capture-'):
    filenames={'capture-hands':'6-two-hands.png','capture-options':'6-voice-switch-depth-gate.png','capture-warning':'6-clip-privacy-warning.png','capture-clip':'6-record-clip-button.png'}
    code('Canvas.ForceUpdateCanvases();foreach(var g in UnityEngine.Object.FindObjectsByType<CampusRift.AR.ARHandGraphic>(FindObjectsSortMode.None))g.SetVerticesDirty();foreach(var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))t.ForceMeshUpdate();Canvas.ForceUpdateCanvases();CampusRift.Controls.LookCapture.Image("task/batch-1007/screens/'+filenames[name]+'");return "Photo saved";')
elif name=='hands':
    code('var t=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARTechHUD>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;t.Open(false);((RectTransform)t.GetType().GetField("warning",f).GetValue(t)).gameObject.SetActive(false);((RectTransform)t.GetType().GetField("dual",f).GetValue(t)).gameObject.SetActive(true);Canvas.ForceUpdateCanvases();return "Twin hand identity panel visible";')
elif name=='options':
    code('var t=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARTechHUD>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;t.Open(true);((RectTransform)t.GetType().GetField("dual",f).GetValue(t)).gameObject.SetActive(false);((RectTransform)t.GetType().GetField("warning",f).GetValue(t)).gameObject.SetActive(false);var s=t.GetComponent<CampusRift.AR.ARTechSettings>();s.Voice=false;s.DepthCollision=true;Canvas.ForceUpdateCanvases();return "Voice OFF and depth unavailable shown";')
elif name=='warning':
    code('var t=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARTechHUD>();t.Open(false);var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;((RectTransform)t.GetType().GetField("warning",f).GetValue(t)).gameObject.SetActive(true);((RectTransform)t.GetType().GetField("dual",f).GetValue(t)).gameObject.SetActive(false);Canvas.ForceUpdateCanvases();return "Room privacy warning posed, no recording request";')
elif name=='clip':
    code('var t=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARTechHUD>();t.Open(false);var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;((RectTransform)t.GetType().GetField("warning",f).GetValue(t)).gameObject.SetActive(false);((RectTransform)t.GetType().GetField("dual",f).GetValue(t)).gameObject.SetActive(true);((TMPro.TMP_Text)t.GetType().GetField("recordState",f).GetValue(t)).text="DỪNG\\nCLIP";foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))if(text.text!=null&&text.text.StartsWith("ẢNH BỐ CỤC EDITOR"))text.text="ẢNH BỐ CỤC EDITOR · TRẠNG THÁI DỪNG ĐƯỢC ĐẶT TRỰC TIẾP";return "Stop button posed; no MediaProjection or recording";')
else:raise SystemExit('Unknown photo name')
