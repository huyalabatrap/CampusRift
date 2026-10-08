using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    // Consent belongs to this scene visit. The loader cannot open the camera before it.
    [DefaultExecutionOrder(-1100)]
    public sealed class ARSessionBootstrap : MonoBehaviour
    {
        public bool Accepted { get; private set; }
        public int Step { get; private set; }
        RectTransform canvas; TMP_Text title,body; Button next; Behaviour[] gated;
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        void Awake()
        {
            gated=new Behaviour[]{FindAnyObjectByType<GestureRecognizerBridge>(),FindAnyObjectByType<FrameSampler>(),FindAnyObjectByType<RiftPlacementService>()};
            foreach(var b in gated)if(b!=null)b.enabled=false;
        }
        void Start()
        {
            canvas=ARUI.Canvas(transform,"AR safety and camera permission");canvas.GetComponentInParent<Canvas>().sortingOrder=200;
            var backdrop=ARUI.Rect(canvas.parent,"Consent backdrop",0,0,0,0);
            backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.sizeDelta=Vector2.zero;backdrop.SetAsFirstSibling();
            var shade=backdrop.gameObject.AddComponent<Image>();shade.color=new Color(.025f,.04f,.08f,.97f);shade.raycastTarget=true;
            var panel=ARUI.Panel(canvas,"Safety",0,0,1250,700);
            title=ARUI.Text(panel,"",0,240,1120,110,42);title.color=ComicTheme.Gold;
            body=ARUI.Text(panel,"",0,20,1120,350,30);
            next=ARUI.Button(panel,L("TIẾP TỤC","CONTINUE"),240,-235,410,Continue);
            ARUI.Button(panel,L("VỀ SẢNH","BACK TO HUB"),-240,-235,410,Exit);
            Refresh();
        }
        public void Continue()
        {
            if(Step==0){Step=1;Refresh();return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                next.interactable=false;
                var callbacks=new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted+=_=>Accept();
                callbacks.PermissionDenied+=_=>Denied();
                callbacks.PermissionDeniedAndDontAskAgain+=_=>Denied();
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera,callbacks);return;
            }
#endif
            Accept();
        }
        void Denied(){if(this==null)return;next.interactable=true;body.text=L("Chưa có quyền camera. Hãy cho phép Camera trong Cài đặt ứng dụng rồi thử lại, hoặc trở về Sảnh.","Camera permission is off. Enable Camera in app settings and try again, or return to the Hub.");}
        void Accept(){if(this==null)return;Accepted=true;canvas.GetComponentInParent<Canvas>().gameObject.SetActive(false);foreach(var b in gated)if(b!=null)b.enabled=true;}
        void Refresh(){title.text=Step==0?L("CHƠI AR AN TOÀN","PLAY AR SAFELY"):L("CAMERA & QUYỀN RIÊNG TƯ","CAMERA & PRIVACY");body.text=Step==0?L("Chú ý xung quanh, không đi lại khi đang chiến đấu.\n\nChơi ngồi ở chế độ Bàn. Giữ điện thoại chắc; giơ tay còn lại trước camera sau, cách máy 25–55 cm (bắt đầu 40 cm), lòng hoặc mu tay đều được. Đưa ngón tay cầm máy khỏi ống kính. Ngắm bằng tâm màn hình; xoay máy nhẹ để chọn mục tiêu.","Watch your surroundings. Stay still during battle.\n\nSit down in Table mode. Hold the phone securely and raise your other hand 25–55 cm from the rear camera (start at 40 cm), palm or back of hand. Keep holding fingers out of the lens. Aim with screen center."):L("Camera dùng để đặt chiến trường và nhận cử chỉ tay.\n\nHình ảnh và điểm bàn tay được xử lý trên máy, không gửi đi. Không lưu hình ảnh khi nhận cử chỉ; chỉ lưu clip cục bộ nếu bạn bật Quay clip và đồng ý trong hộp thoại Android. Điểm tay chỉ giữ tạm trong RAM. Chỉ mở camera sau khi bạn đồng ý.","The camera places the battlefield and recognizes hand gestures.\n\nImages and hand landmarks are processed on this device, never uploaded. Gesture recognition saves no images; a local clip is saved only if you enable Record Clip and approve the Android dialog. Hand landmarks stay briefly in RAM. The camera opens only after your permission.");next.GetComponentInChildren<TMP_Text>().text=Step==0?L("TIẾP TỤC","CONTINUE"):L("CHO PHÉP CAMERA","ALLOW CAMERA");}
        public static void Exit(){ARSceneNavigation.Exit();}
    }
}
