using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using CampusRift.SkyBeast;
using CampusRift.Monsters;
namespace CampusRift.UI
{
    public sealed class FireWarningHUD:MonoBehaviour
    {
        FireBreathCycle cycle;Canvas canvas;RectTransform panel;TMP_Text title,label,doorText;Image shelterIcon,arrow;
        CanvasGroup warning;MonsterWarningUI pulse;Image[] edges;
        RectTransform doorMarker;Image markerArrow;TMP_Text markerDistance;
        readonly Texture2D[] edgeTextures=new Texture2D[4];
        readonly Sprite[] edgeSprites=new Sprite[4];
        public bool DoorMarkerVisible=>doorMarker!=null&&doorMarker.gameObject.activeSelf;
        public int DoorNode{get;private set;}=-1;
        public Vector3 DoorPosition{get;private set;}
        public float DoorDistance{get;private set;}
        float nextRoute;RoomGraph graph;
        ElevatorInteraction lift;
        NavMeshPath path;
        public static FireWarningHUD Attach(FireBreathCycle owner)
        {var hud=owner.gameObject.AddComponent<FireWarningHUD>();hud.cycle=owner;hud.Build();return hud;}
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=position;return r;}
        TMP_Text Text(Transform parent,string name,int size,Vector2 dimensions,Vector2 position)
        {var r=Rect(parent,name,dimensions,new Vector2(.5f,.5f),position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=ComicTheme.Font;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=ComicTheme.Paper;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.enableAutoSizing=true;t.fontSizeMin=18;t.fontSizeMax=size;ComicTheme.Text(t,true);return t;}
        Image Icon(Transform parent,string name,Vector2 size,Vector2 position,string id)
        {var r=Rect(parent,name,size,new Vector2(.5f,.5f),position);var im=r.gameObject.AddComponent<Image>();im.sprite=Resources.Load<Sprite>("P13/"+id);im.raycastTarget=false;return im;}
        void Build()
        {
            path=new NavMeshPath();
            var go=new GameObject("Fire Warning Comic Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=25;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
            panel=Rect(go.transform,"Shelter / Fire warning",new Vector2(540,180),new Vector2(.5f,1),new Vector2(0,-116));ComicTheme.Frame(panel.gameObject);
            shelterIcon=Icon(panel,"Shelter sprite",new Vector2(70,70),new Vector2(-211,30),"outdoor");
            title=Text(panel,"Countdown",30,new Vector2(385,54),new Vector2(38,53));
            label=Text(panel,"Shelter label",24,new Vector2(385,42),new Vector2(38,8));
            var row=Rect(panel,"Warning route",new Vector2(510,55),new Vector2(.5f,.5f),new Vector2(0,-52));warning=row.gameObject.AddComponent<CanvasGroup>();
            pulse=row.gameObject.AddComponent<MonsterWarningUI>();pulse.Indicator=warning;
            arrow=Icon(row,"Nearest entrance bearing",new Vector2(42,42),new Vector2(-178,0),"arrow");doorText=Text(row,"Entrance distance",24,new Vector2(340,44),new Vector2(38,0));
            doorMarker=Rect(go.transform,"Entrance screen / edge marker",new Vector2(112,128),new Vector2(.5f,.5f),Vector2.zero);ComicTheme.Frame(doorMarker.gameObject);
            Icon(doorMarker,"Entrance house",new Vector2(46,46),new Vector2(0,22),"indoor");
            markerDistance=Text(doorMarker,"Entrance metres",20,new Vector2(96,44),new Vector2(0,-22));
            markerArrow=Icon(doorMarker,"Entrance direction",new Vector2(54,54),new Vector2(0,72),"arrow");markerArrow.color=ComicTheme.Gold;
            doorMarker.gameObject.SetActive(false);
            var edgeRoot=new GameObject("Fire edges behind HUD",typeof(RectTransform),typeof(Canvas));edgeRoot.transform.SetParent(go.transform,false);
            var er=(RectTransform)edgeRoot.transform;er.anchorMin=Vector2.zero;er.anchorMax=Vector2.one;er.offsetMin=er.offsetMax=Vector2.zero;
            var edgeCanvas=edgeRoot.GetComponent<Canvas>();edgeCanvas.overrideSorting=true;edgeCanvas.sortingOrder=-1;
            edges=new Image[4];
            for(int i=0;i<4;i++)
            {
                var r=Rect(edgeRoot.transform,"Fire edge "+i,Vector2.zero,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;
                if(i==0){r.anchorMax=new Vector2(.025f,1);}else if(i==1){r.anchorMin=new Vector2(.975f,0);}else if(i==2){r.anchorMax=new Vector2(1,.025f);}else r.anchorMin=new Vector2(0,.975f);
                r.offsetMin=r.offsetMax=Vector2.zero;edges[i]=r.gameObject.AddComponent<Image>();edges[i].raycastTarget=false;
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Fire edge gradient",wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float t=i==0?x/31f:i==1?1-x/31f:i==2?y/31f:1-y/31f;texture.SetPixel(x,y,new Color(1,1,1,(1-t)*(1-t)));}
                texture.Apply();edgeTextures[i]=texture;edgeSprites[i]=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f);edges[i].sprite=edgeSprites[i];
            }
            panel.SetAsLastSibling();graph=ShelterGraphReference.Graph;
            if(cycle.Player!=null)lift=cycle.Player.GetComponent<ElevatorInteraction>();
        }
        public void FindDoor()
        {
            DoorNode=-1;DoorDistance=float.PositiveInfinity;if(graph==null||cycle.Player==null)return;
            Vector3 feet=cycle.Player.transform.position;
            // Only external ground-floor approaches; shortest reachable NavMesh path wins.
            // RoomGraph's FloorID starts at 1; use actual ground elevation rather than assuming zero.
            for(int i=0;i<graph.Nodes.Length;i++)
            {
                var n=graph.Nodes[i];if((n.Kind!=RoomNodeKind.Door&&n.Kind!=RoomNodeKind.Exit)||n.WorldPosition.y>1||n.RoomID.EndsWith("/inside"))continue;
                float straight=Vector3.Distance(feet,n.WorldPosition);if(straight>DoorDistance||straight>150)continue;
                if(!NavMesh.SamplePosition(n.WorldPosition,out var hit,2,NavMesh.AllAreas)||!NavMesh.CalculatePath(feet,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                var corners=path.corners;float distance=0;for(int j=1;j<corners.Length;j++)distance+=Vector3.Distance(corners[j-1],corners[j]);
                if(distance<DoorDistance){DoorDistance=distance;DoorNode=i;DoorPosition=hit.position;}
            }
        }
        void Update()
        {
            if(canvas==null||cycle==null)return;
            bool gameplay=UIStateManager.Instance==null||UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal;
            canvas.enabled=gameplay&&cycle.State!=FireBreathCycle.Phase.Disabled;
            if(!canvas.enabled)return;
            bool vi=LevelHUD.Vietnamese;var s=cycle.Player!=null?ShelterDetector.ForFire(cycle.Player.transform.position):Shelter.Outdoor;
            shelterIcon.sprite=Resources.Load<Sprite>("P13/"+(s==Shelter.Indoor?"indoor":s==Shelter.Partial?"partial":"outdoor"));
            label.text=s==Shelter.Indoor?(vi?"TRONG NHÀ · CHE CHẮN":"INDOOR · SHELTERED"):s==Shelter.Partial?(vi?"BÁN CHE · 45%":"PARTIAL · 45%"):vi?"NGOÀI TRỜI · NGUY HIỂM":"OUTDOOR · DANGER";
            title.text=cycle.IsFury?(vi?"LONG NỘ ":"DRAGON FURY ")+Mathf.CeilToInt(cycle.Remaining):cycle.State==FireBreathCycle.Phase.Warning?(vi?"THIÊN HỎA ":"FIRE STORM ")+Mathf.CeilToInt(cycle.Remaining)+"…":cycle.IsBreathing?(vi?"THIÊN HỎA · ĐANG PHUN":"FIRE STORM · ACTIVE"):cycle.State==FireBreathCycle.Phase.Afterfire?(vi?"DƯ HỎA ":"AFTERFIRE ")+Mathf.CeilToInt(cycle.Remaining):vi?"THIÊN HỎA · NGHỈ":"FIRE STORM · REST";
            bool warn=cycle.State==FireBreathCycle.Phase.Warning;warning.gameObject.SetActive(warn);
            doorMarker.gameObject.SetActive(warn&&s!=Shelter.Indoor&&DoorNode>=0&&DoorDistance>1);
            bool liftHint=lift!=null&&lift.CanInteract&&!Controls.CampusInput.Mobile;
            panel.sizeDelta=new Vector2(540,warn?180:125);panel.anchoredPosition=new Vector2(0,-(warn?116:89)-(liftHint?96:0));
            if(SkyBeastScheduler.Instance!=null)panel.anchoredPosition+=Vector2.down*(HeavenSwordUltimate.Instance!=null&&HeavenSwordUltimate.Instance.Visible?244:180);
            title.rectTransform.anchoredPosition=new Vector2(38,warn?53:28);label.rectTransform.anchoredPosition=new Vector2(38,warn?8:-15);shelterIcon.rectTransform.anchoredPosition=new Vector2(-211,warn?30:4);
            bool compact=Controls.CampusInput.Mobile;
            panel.anchorMin=panel.anchorMax=compact?new Vector2(0,1):new Vector2(.5f,1);
            panel.sizeDelta=compact?new Vector2(430,warn?150:110):panel.sizeDelta;
            if(compact)panel.anchoredPosition=new Vector2(355,-(warn?355:335));
            title.rectTransform.sizeDelta=new Vector2(compact?335:385,compact?44:54);title.fontSizeMax=compact?26:30;
            label.rectTransform.sizeDelta=new Vector2(compact?335:385,42);label.fontSizeMax=compact?20:24;
            shelterIcon.rectTransform.sizeDelta=Vector2.one*(compact?42:70);
            warning.GetComponent<RectTransform>().sizeDelta=new Vector2(compact?400:510,compact?42:55);
            warning.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,compact?-42:-52);
            arrow.rectTransform.sizeDelta=Vector2.one*(compact?26:42);arrow.rectTransform.anchoredPosition=new Vector2(compact?-150:-178,0);
            doorText.rectTransform.sizeDelta=new Vector2(compact?300:340,44);doorText.fontSizeMax=compact?20:24;
            if(compact){title.rectTransform.anchoredPosition=new Vector2(24,warn?44:24);label.rectTransform.anchoredPosition=new Vector2(24,warn?7:-18);shelterIcon.rectTransform.anchoredPosition=new Vector2(-176,warn?26:2);}
            if(warn)
            {
                if(Time.unscaledTime>=nextRoute){nextRoute=Time.unscaledTime+1;FindDoor();}
                arrow.gameObject.SetActive(DoorNode>=0&&s!=Shelter.Indoor);
                doorText.text=s==Shelter.Indoor?(vi?"ĐANG ĐƯỢC CHE":"SHELTERED"):DoorNode<0?(vi?"TÌM MÁI CHE":"FIND SHELTER"):DoorDistance<=1?(vi?"ĐANG Ở CỬA":"AT ENTRANCE"):(vi?"VÀO NHÀ · ":"ENTER BUILDING · ")+Mathf.CeilToInt(DoorDistance)+" m";
                if(DoorNode>=0&&Camera.main!=null){var d=Vector3.ProjectOnPlane(DoorPosition-cycle.Player.transform.position,Vector3.up);var f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up);arrow.rectTransform.localRotation=Quaternion.Euler(0,0,-Vector3.SignedAngle(f,d,Vector3.up));}
                if(DoorNode>=0&&DoorDistance>1&&Camera.main!=null&&s!=Shelter.Indoor)PositionDoorMarker(Camera.main);
            }
            float alpha=cycle.IsBreathing&&s!=Shelter.Indoor?.12f:0;
            foreach(var edge in edges)edge.color=new Color(1,.28f,.025f,alpha);
        }
        void PositionDoorMarker(Camera camera)
        {
            // The bounded area avoids the HP/objective panels and both PC/mobile skill controls.
            Vector3 view=camera.WorldToViewportPoint(DoorPosition+Vector3.up*1.4f);
            Vector2 fromCenter=new Vector2(view.x-.5f,view.y-.5f);
            if(view.z<=0){var d=DoorPosition-camera.transform.position;fromCenter=new Vector2(Vector3.Dot(camera.transform.right,d),Vector3.Dot(camera.transform.forward,d));}
            if(fromCenter.sqrMagnitude<.0001f)fromCenter=Vector2.down;
            Vector2 direction=fromCenter.normalized;
            bool mobile=Controls.CampusInput.Mobile;
            float minX=mobile?.29f:.14f,maxX=mobile?.56f:.86f,minY=mobile?.38f:.34f,maxY=mobile?.65f:.70f;
            bool onscreen=view.z>0&&view.x>minX&&view.x<maxX&&view.y>minY&&view.y<maxY-.08f;
            Vector2 viewport=onscreen?new Vector2(view.x,view.y)+Vector2.up*.08f:Vector2.one*.5f+direction*Mathf.Min(.34f/Mathf.Max(.001f,Mathf.Abs(direction.x)),.20f/Mathf.Max(.001f,Mathf.Abs(direction.y)));
            viewport.x=Mathf.Clamp(viewport.x,minX,maxX);viewport.y=Mathf.Clamp(viewport.y,minY,maxY);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,new Vector2(viewport.x*Screen.width,viewport.y*Screen.height),null,out var local);
            doorMarker.anchoredPosition=local;doorMarker.gameObject.SetActive(true);markerDistance.text=Mathf.CeilToInt(DoorDistance)+" M";
            if(onscreen)direction=Vector2.down;
            markerArrow.rectTransform.anchoredPosition=direction*72;
            markerArrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
        }
        void OnDestroy(){foreach(var sprite in edgeSprites)if(sprite!=null)Destroy(sprite);foreach(var texture in edgeTextures)if(texture!=null)Destroy(texture);}
    }
}
