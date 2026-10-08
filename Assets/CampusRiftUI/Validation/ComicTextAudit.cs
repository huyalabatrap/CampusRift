#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.UI
{
    public static class ComicTextAudit
    {
        [Serializable] public sealed class Entry { public string path,text,reason; public Rect rect; public Vector3 boundsMin,boundsMax; public float fontSize; }
        [Serializable] public sealed class ScreenReport { public string screen,language; public int width,height,visibleTexts,partiallyVisibleScrollTexts; public List<Entry> issues=new List<Entry>(); }
        public static ScreenReport Scan(string screen)
        {
            Canvas.ForceUpdateCanvases();
            var report=new ScreenReport {screen=screen,width=Screen.width,height=Screen.height,language=LevelHUD.Vietnamese?"VI":"EN"};
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>())
            {
                if(!Visible(text))continue;
                text.ForceMeshUpdate();report.visibleTexts++;
                Rect rect=text.rectTransform.rect;Bounds bounds=text.textBounds;
                bool outside=bounds.min.x<rect.xMin-.75f||bounds.max.x>rect.xMax+.75f||bounds.min.y<rect.yMin-.75f||bounds.max.y>rect.yMax+.75f;
                string parentIssue=ParentClip(text,bounds,report);
                if(text.isTextTruncated||outside||parentIssue.Length>0)
                    report.issues.Add(new Entry {path=PathOf(text.transform),text=text.text,rect=rect,boundsMin=bounds.min,boundsMax=bounds.max,fontSize=text.fontSize,reason=(text.isTextTruncated?"isTextTruncated ":"")+(outside?"textBounds outside rect ":"")+parentIssue});
            }
            return report;
        }
        static string ParentClip(TMP_Text text,Bounds bounds,ScreenReport report)
        {
            var glyphCorners=new[]{new Vector3(bounds.min.x,bounds.min.y),new Vector3(bounds.min.x,bounds.max.y),new Vector3(bounds.max.x,bounds.min.y),new Vector3(bounds.max.x,bounds.max.y)};
            var world=glyphCorners.Select(p=>text.transform.TransformPoint(p)).ToArray();
            for(var parent=text.transform.parent;parent!=null;parent=parent.parent)
            {
                var r=parent as RectTransform;if(r==null)continue;
                var mask=parent.GetComponent<Mask>();var rectMask=parent.GetComponent<RectMask2D>();var image=parent.GetComponent<Image>();
                bool masked=(mask!=null&&mask.isActiveAndEnabled)||(rectMask!=null&&rectMask.isActiveAndEnabled);
                bool framed=image!=null&&image.enabled&&image.sprite!=null&&image.sprite.name.StartsWith("round-")&&image.sprite.name!="round-mask";
                if(!masked&&!framed)continue;
                Rect visible=r.rect;float radius=masked&&image!=null&&image.sprite==ComicTheme.Sprite("round-mask")?ComicTheme.CornerRadius:0;
                if(framed){visible=Rect.MinMaxRect(visible.xMin+8,visible.yMin+14,visible.xMax-14,visible.yMax-8);radius=8;}
                if(rectMask!=null){var pad=rectMask.padding;visible=Rect.MinMaxRect(visible.xMin+pad.x,visible.yMin+pad.y,visible.xMax-pad.z,visible.yMax-pad.w);}
                var points=world.Select(p=>r.InverseTransformPoint(p)).ToArray();
                // Hanging key labels sit entirely below an unmasked slot frame. A sprite cannot
                // clip pixels outside its own rect; its border can obscure intersecting glyphs.
                if(framed&&!masked&&(points.All(p=>p.y<r.rect.yMin)||points.All(p=>p.y>r.rect.yMax)||points.All(p=>p.x<r.rect.xMin)||points.All(p=>p.x>r.rect.xMax)))continue;
                if(points.All(p=>Inside(visible,p,radius)))continue;
                // Scroll rows legitimately cross the viewport while scrolling. They are counted explicitly,
                // but their own card frames are still checked. Non-scroll masks are always checked.
                if(masked&&parent.GetComponent<ScrollRect>()!=null)
                {report.partiallyVisibleScrollTexts++;continue;}
                return "textBounds clipped by parent "+parent.name+" ("+(framed?"sprite visible fill":"mask")+")";
            }
            return "";
        }
        static bool Inside(Rect rect,Vector3 point,float radius)
        {
            if(point.x<rect.xMin-.75f||point.x>rect.xMax+.75f||point.y<rect.yMin-.75f||point.y>rect.yMax+.75f)return false;
            radius=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f);
            float x=Mathf.Clamp(point.x,rect.xMin+radius,rect.xMax-radius),y=Mathf.Clamp(point.y,rect.yMin+radius,rect.yMax-radius);
            return (new Vector2(point.x-x,point.y-y)).sqrMagnitude<=(radius+.75f)*(radius+.75f);
        }
        static string PathOf(Transform transform)
        {var names=new List<string>();for(var t=transform;t!=null;t=t.parent)names.Add(t.name);names.Reverse();return string.Join("/",names);}
        static bool Visible(TMP_Text text)
        {
            if(!text.isActiveAndEnabled||string.IsNullOrWhiteSpace(text.text)||text.color.a<.01f||text.GetComponentInParent<Canvas>()==null)return false;
            if(text.GetComponentsInParent<CanvasGroup>().Any(group=>group.alpha<.01f))return false;
            var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
            Rect screen=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
            if(!screen.Overlaps(new Rect(0,0,Screen.width,Screen.height)))return false;
            foreach(var mask in text.GetComponentsInParent<RectMask2D>())
            {
                if(!mask.isActiveAndEnabled)continue;
                ((RectTransform)mask.transform).GetWorldCorners(corners);
                if(!screen.Overlaps(Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y)))return false;
            }
            return true;
        }
        public static void Save(ScreenReport report,string path)
        {Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));}
    }
}
#endif
