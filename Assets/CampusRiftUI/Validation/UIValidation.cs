#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using CampusRift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class UIValidation
{
    public static void SetResolution(int width,int height)
    {
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);
        var group=sizesType.GetProperty("currentGroup",flags).GetValue(sizes);var groupType=group.GetType();
        int built=(int)groupType.GetMethod("GetBuiltinCount",flags).Invoke(group,null), custom=(int)groupType.GetMethod("GetCustomCount",flags).Invoke(group,null);
        int index=-1;var getSize=groupType.GetMethod("GetGameViewSize",flags);
        for(int i=0;i<built+custom;i++)
        {var s=getSize.Invoke(group,new object[]{i});var t=s.GetType();if((int)t.GetProperty("width",flags).GetValue(s)==width && (int)t.GetProperty("height",flags).GetValue(s)==height){index=i;break;}}
        if(index<0)
        {
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,flags,null,new object[]{Enum.ToObject(enumType,1),width,height,$"CR {width}x{height}"},null);
            groupType.GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});index=built+custom;
        }
        var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex",flags).SetValue(view,index);view.Focus();view.Repaint();
    }
    public static string AuditLayout()
    {
        Canvas.ForceUpdateCanvases();var issues=new List<string>();int count=0;
        foreach(var button in Object.FindObjectsByType<Selectable>())
        {
            if(!button.IsActive())continue;
            var r=button.transform as RectTransform;var corners=new Vector3[4];r.GetWorldCorners(corners);
            foreach(var corner in corners)if(corner.x<-.5f || corner.y<-.5f || corner.x>Screen.width+.5f || corner.y>Screen.height+.5f){issues.Add(button.name+" outside screen");break;}
            count++;
        }
        foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>())
        {
            text.ForceMeshUpdate();
            if(text.isTextTruncated)issues.Add(text.name+" truncated: "+text.text);
        }
        foreach(var slider in Object.FindObjectsByType<Slider>())
        {
            if(slider.fillRect!=null && slider.fillRect.rect.width>((RectTransform)slider.transform).rect.width+.5f)issues.Add(slider.name+" fill exceeds track");
            if(slider.handleRect!=null && slider.handleRect.rect.height>((RectTransform)slider.transform).rect.height+.5f)issues.Add(slider.name+" handle exceeds track height");
        }
        if(issues.Count>0)throw new Exception(string.Join("; ",issues));
        return $"{Screen.width}x{Screen.height}: {count} active controls within bounds; no truncated text.";
    }
}
#endif
