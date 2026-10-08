using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using CampusRift.UI;
namespace CampusRift.AR
{
    public static class ARUI
    {
        public static RectTransform Canvas(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.transform.SetParent(parent,false);go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
            if(EventSystem.current==null)new GameObject("AR UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var content=Rect(go.transform,"AR safe area",0,0,1920,1080);
            go.AddComponent<ARScreenLayout>().Initialize(content);
            return content;
        }
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        public static TMP_Text Text(Transform parent,string value,float x,float y,float w,float h,float size=30)
        {var r=Rect(parent,"AR text",x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();ComicTheme.Text(t);t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=Mathf.Min(size,Mathf.Max(12,size*.72f));return t;}
        public static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h)
        {var r=Rect(parent,name,x,y,w,h);ComicTheme.Frame(r.gameObject);return r;}
        public static Button Button(Transform parent,string value,float x,float y,float w,Action action)
        {var r=Rect(parent,value,x,y,w,82);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=ComicTheme.Frame(r.gameObject,"button-blue",true);b.onClick.AddListener(()=>action());Text(r,value,-5,2,w-42,68,24);return b;}
        public static Button Round(Transform parent,string name,string value,float x,float y,float diameter,Action action)
        {var r=Rect(parent,name,x,y,diameter,diameter);var image=r.gameObject.AddComponent<Image>();image.sprite=ComicTheme.Sprite("round-mask");image.color=new Color(.12f,.06f,.23f,.94f);var rim=Rect(r,"Gold rim",0,0,diameter,diameter).gameObject.AddComponent<ARArcGraphic>();rim.Amount=1;rim.color=ComicTheme.Gold;rim.raycastTarget=false;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());if(!string.IsNullOrEmpty(value)){var text=Text(r,value,0,0,diameter*.68f,diameter*.68f,diameter>100?23:22);text.margin=Vector4.zero;text.enableAutoSizing=true;text.fontSizeMax=diameter>100?23:22;text.fontSizeMin=11;}return b;}
    }
}
