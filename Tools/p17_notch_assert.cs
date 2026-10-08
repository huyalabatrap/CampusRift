var hud=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlsHUD>();
var safe=new Rect(100,45,Screen.width-200,Screen.height-90);hud.ApplySafeArea(safe);Canvas.ForceUpdateCanvases();
bool contained=true,overlap=false;var rects=new System.Collections.Generic.List<Rect>();var labels=new System.Collections.Generic.List<string>();var corners=new Vector3[4];
foreach(var zone in hud.Zones)
{
    if(zone.role==CampusRift.Controls.TouchRole.Look||zone.role==CampusRift.Controls.TouchRole.Cancel)continue;
    ((RectTransform)zone.transform).GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);var rect=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
    if(!safe.Contains(a)||!safe.Contains(b)){contained=false;labels.Add("outside "+zone.role+" "+rect);}
    for(int i=0;i<rects.Count;i++)if(rects[i].Overlaps(rect)){overlap=true;labels.Add("overlap "+zone.role+" with #"+i);}
    rects.Add(rect);
}
var result="MaxScale notch: contained="+contained+"; overlaps="+overlap+"; regions="+rects.Count+"\n"+string.Join("\n",labels);
System.IO.File.WriteAllText("task/p17/notch-fix-smoke.txt",result);hud.ApplySafeArea(Screen.safeArea);return result;
