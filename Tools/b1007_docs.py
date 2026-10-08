from pathlib import Path
def append(name,text):
    p=Path('Docs')/name
    with p.open('a',encoding='utf-8') as f:f.write('\n'+text+'\n')
append('07-MAN-CHOI-MOI-TRUONG-UI.md','''## Mobile HUD — batch 1007 (07/10/2026)

Bố cục Job1 thay phần POLISH2 ở trên: joystick, Tương tác, Khóa, Tạm dừng và Đồ ở trái; Đánh, Nhảy, Né, BOOST và bốn chiêu theo vòng cung ở phải. Caption ngắn nằm trong nút; trạng thái bằng màu/vòng hồi chiêu. Giữ safe area, vùng trung tâm x32–64%, y20–85% và kẹp scale hiệu dụng, không ghi lại scale người chơi.

[ItemWheelUI](../Assets/CampusRiftUI/Runtime/ItemWheelUI.cs) thay hàng ô vật phẩm trên mobile. Chạm Đồ dưới250ms dùng lựa chọn nhanh; giữ từ250ms mở vòng, kéo/thả chọn và dùng; tâm hoặc ngoài vòng hủy. Vật phẩm hết, passive, cooldown hoặc không cần dùng bị xám theo cùng `PlayerItems.Availability` mà `Use` kiểm lại. Vòng không pause và không giành touch joystick. PC giữ B mở vòng bằng chuột; 1/2/3 vẫn dùng vật phẩm. Khi vòng mở, camera/click tấn công được chặn, cursor được phục hồi khi đóng.

AR dùng canvas safe area riêng. Các nhãn trong nút tròn nằm trong hình vuông nội tiếp và autosize. Linh Ấn là thanh mảnh; chỉ hiện chuỗi đang khớp tiền tố. Nút ? giữ hướng dẫn đầy đủ. Hướng dẫn Chủ động/An toàn và thao tác tay chỉ hiện5s đầu trận. Dragon objective thành toast mép trên3s, hiện lại khi giáp/điểm yếu/sẵn sàng đổi; Thiên Kiếm có vòng nhỏ cạnh rail. Tri Thức chỉ hiện bảng đáp án dưới cùng khi chữ phù văn thật sự bị cắt. Luyện Ấn là dải170px tại1/3 trên, nền bán trong suốt, hướng dẫn5s đầu.

[REPORT Job1](../task/batch-1007/REPORT-1-MOBILE-HUD.md) ghi thiết kế/ảnh; kết quả hồi quy và APK nằm ở REPORT Job7 khi hoàn tất. Ảnh posed Editor không chứng minh thao tác trên điện thoại.''')
append('05-AR-VA-DEEP-LEARNING.md','''## Các mode và công nghệ AR — batch 1007 (07/10/2026)

[ARModeCatalog](../Assets/ARRift/Runtime/ARModeCatalog.cs), [ARGameMode](../Assets/ARRift/Runtime/ARGameMode.cs) và [ARModeSession](../Assets/ARRift/Runtime/ARModeSession.cs) sở hữu lựa chọn trước placement, capability, lịch wave, seed, kết quả và chính sách persistence. Năm mode đều đã mở playable:

| Mode | Luật / thời lượng |
|---|---|
| Làm quen |3đợt3/4/6, bảo vệ Linh Trận,5chiêu mở sẵn |
| Thủ Trận |5đợt, Rift phụ/tường, tinh anh và rồng; Thiên Kiếm kết trận; limit360s |
| Truy Rift |180s; tìm portal, ngắm tâm và nối chuỗi chỉ định trong20s, không cần đầy Linh Ấn |
| Đấu Long |240s;3lớp giáp, combo điểm yếu và6hit; dọn ground support để Thiên Kiếm |
| Luyện Ấn |90s;6cử chỉ trên grid100BPM, Perfect≤90ms/Great≤200ms, nhịp tăng dần |

10Ải mở tuần tự:1–3 Làm quen,4/7/10 Thủ Trận,5/8 Truy Rift,6/9 Đấu Long. Daily luân phiên3mode bằng seed ngàyUTC. Thưởng5LT/sao và quiz dùng chung **trầnAR60LT/ngày riêng**, không Tu Vi; top20/mode lưu local. Dev/transient/KiểmẤn không lưu thưởng, score hoặc notebook. Vệt ấn Vàng/Lam mở tại tổng3/15sao, điểm tay chỉRAM/TTL250ms.

Giữ D1 world3D/model≥.6/100ms/một pose một intent, aim tâm màn hình, placement polygon và watchdog GóiA. [GestureSequenceMatcher](../Assets/ARRift/Runtime/GestureSequenceMatcher.cs) nhận intentD1, gap1.2s: V→Lên→Mở/Vạn Kiếm Quy Tông, Xuống→Nắm→Lên/Băng Thiên Lôi Ngục, Mở→Nắm/Thiên Thủ Hấp Tinh. Đầy100Ấn mới giữ tiền tố; fail/timeout fallback chiêuđầu, giữmeter. ThumbUp là Kim Chung Tráo, đỡ1đòn/hồi6s/perfect250ms. Chủđộng opt-in chỉSàn+capabilityDodge: đạn báo1.2s, né ngang/cúi25cm, slowclockAR.2×/.3s, khôngTime.timeScale toàn game.

Rift phụ chỉpolygon HorizontalUp/Vertical vàanchor riêng, cap3/reduced1. Quái entrance xong mới navigation ngang. Rồng dùng3prefabP12, bay cao hơncamera; Thiên Kiếm dùngP15, PointingUpD1+pitch>35° giữ1.5s với framefresh/geometry/epoch/watchdog. [ARHandMotion](../Assets/ARRift/Runtime/ARHandMotion.cs) bùcamera pose cho pinch/swipe, chặn static trướcD1. Nhấc chỉquáiStun/Freeze; Rigidbody chỉtrong lifecycle ném. Quiz3–4runes ngắm tâm+ClosedFist, nghỉ10s sauwaveclear thật; thưởngquaP08+capAR, buffwavekế vàsổôn.

Tech2 mặc định tắt giọng nói: JNI2tay cóD1/identityriêng,180ms ghép Mở+Mở/ThiênThủĐôi hoặcNắm+V/VạnKiếm khi100Ấn. Depth dùngproviderTrackableType.Depth/max10rayframe vàcacheocclusion, khôngmeshphòng/placementdepth. Probe poll250ms/2s, GPUcubemap chỉRAM/tiercao. VoskVN33.7MBzip/53.3MBunpack Apache2.0,3keywordngoại tuyến, micro riêng chỉcửasổKếtẤn; đúngtên buffuy lực1.3snapshot, PCMkhônglưu/upload. ClipAPI29+ chỉsauwarning+MediaProjectionconsent, foregroundservice, silent90s,24FPS/H264,MediaStoreMovies/CampusRift,khôngtựgửi.

XR Simulation khôngDepth và khôngchứng minhnativevoice/projection/recognition. Các phépđoFPS/identity/thermal/va chạm/quyền/gallery cầnmáythật. [Giaiđoạn3](../task/batch-1007/GIAI-DOAN-3-CHUAN-BI.md) chỉtài liệuGeospatial/co-op/cloudconsent; chưa kíchhoạtmạng/key/billing. Chi tiết nguồn vàgiới hạn: REPORT2–6 trongtask/batch-1007.''')
append('09-THUAT-NGU-VA-KHAI-NIEM.md','''## Thuật ngữ AR mới — batch 1007

| Thuật ngữ | Ý nghĩa trong source |
|---|---|
| Linh Ấn / Seal Energy |Meter0–100, hit+2/phảnứng+15/né+10; ngân sách tuyệt kỹ, khác Linh Lực |
| Kết Ấn / seal sequence |Chuỗi intentD1, gap1.2s; giữ tiền tố khi đầymeter, fallback chiêuđầu nếu hụt |
| Vạn Kiếm Quy Tông |V→Lên→Mở; SwordRain empowered quét disc |
| Băng Thiên Lôi Ngục |Xuống→Nắm→Lên; Freeze toàn sân+sét trễ1s |
| Thiên Thủ Hấp Tinh |Mở→Nắm; hút/gom tới6actor, damage theo sốgom |
| Kim Chung Tráo |ThumbUp; khiên1đòn/6s; perfectblock250ms phảnđạn |
| Chủ động |Opt-in Sàn+capabilityDodge; đạn vềcamera, né6DoF25cm |
| Kiếm Ý / Thiên Kiếm AR |Dọn groundqueue+bosslộ; PointingUp giữ1.5s/pitch>35° để kếtliễu |
| Rift phụ / Rift tường |Portalpolygon+anchor riêng; entrance xong quái vềdiscngang |
| Ấn Tri Thức |Quiz trong break10s, centreaim+ClosedFist; P08 vàcapAR60 dùng chung |
| Luyện Ấn |Mode90s/6pose theo nhịp100BPM; Perfect90ms/Great200ms |
| Vệt ấn |CosmeticVàng/Lam tại3/15sao, landmarkhiển thị chỉRAM |
| Thiên Thủ đôi |HaiOpenPalm ghéptrong180ms;1cost/CD,2visual vàdamage2× |
| Depth contact / Hidden |ProviderDepthray/max10frame vàcachevậtche; khôngroommesh |
| Voice keyword window |Voskngoại tuyến, chỉKếtẤn,3keyword; multiplier1.3snapshot |
| MediaProjection clip |QuyềnOSriêng từng lầnquay; video cóphòngthật, silent/max90s/gallery |

Tham chiếu runtime và giới hạn thiết bị ở [chương05](05-AR-VA-DEEP-LEARNING.md#các-mode-và-công-nghệ-ar--batch-1007-07102026). Không suy kết quả nhận dạng hoặc FPS máy thật từ screenshot/mock.''')
print('Updated Docs05/07/09')
