"""Write the final Vietnamese report only after all work and handoff exist."""
from pathlib import Path
import json, collections, re
from b1007_inventory import cases
from b1007_supplementary import jobs

root=Path.cwd();base=root/'task/batch-1007';out=base/'regression'
ar=['ARGestureUnitTests','ARRiftPlayTest','ARNavigationPlayTest','ARGestureCheckMock']+['ARMode-'+x for x in ['training','defense','rift-hunt','dragon-duel','seal-practice']]
expected=[x['name'] for x in cases+jobs]+ar
rows={p.parent.name:json.loads(p.read_text(encoding='utf-8')) for p in (out/'runs').glob('*/row.json')}
assert all(x in rows for x in expected), [x for x in expected if x not in rows]
assert not any(not (p.parent/'row.json').exists() for p in (out/'runs').glob('*/invoked.json'))
verification=json.loads((base/'build/verification.json').read_text(encoding='utf-8'))
handoff=json.loads((base/'7-handoff.json').read_text(encoding='utf-8'))
restoration=json.loads((base/'7-restoration.json').read_text(encoding='utf-8'))
console=json.loads((base/'7-console-final.json').read_text(encoding='utf-8'))
assert all(x['matchesBackup'] for x in restoration['saveFiles'])
assert handoff['platform']=='Android' and not any(handoff[k] for k in ['playing','compiling','updating','dirty'])
assert handoff['scene']=='Assets/Scenes/SampleScene.unity'
assert handoff['xrLoader'] is None and console['success'] and console['data']==[]
assert verification['signature']['verified'] and verification['signature']['scheme']=='v2'
assert verification['development'] and verification['arm64'] and verification['gles3']
assert verification['gestureModel']['compression']==0
assert 'Succeeded' in (base/'build/DONE.txt').read_text(encoding='utf-8')
retests={p.parent.name:json.loads(p.read_text(encoding='utf-8')) for p in (out/'retests').glob('*/row.json')}
final={name:retests.get(name,rows[name])['status'] for name in expected}
counts=collections.Counter(final.values())
def cell(x):return str(x).replace('|','/').replace('\n',' ')[:220]
lines=['# Báo cáo Job 7 — Hồi quy / APK — 07/10/2026','',
       'Hoàn tất lượt hồi quy theo brief; mỗi suite chạy một lần, các mục FAIL chỉ retest riêng một lần. '+
       f'Tổng **{len(expected)} suite** ({len(cases+jobs)} game thường + {len(ar)} AR): '+', '.join(f'**{v} {k}**' for k,v in sorted(counts.items()))+'.', '',
       '**PlayerCombat, SkillSet1Edges, P12AudioEndingPlayTest, P17Smoke và UIPlayAcceptance vẫn ghi FAIL/chưa xác nhận.** Sau retest PlayerCombat đã sửa thiếu theo dõi transform tổ tiên/FitFrame; compile sạch và ảnh visual đúng bố cục, nhưng không chạy lần thứ ba. Focused expiry retest của SkillSet1Edges bị agent mở fixture kế tiếp quá sớm và ngắt trước khi có kết quả; không gọi retest lần2. Timing đã sửa theo thời điểm apply/chính xác2.5s nhưng chưa xác nhận. P12AudioEnding focused vẫn FAIL; sau đó sửa clock P21 không tính thời gian frame trước Begin, không retest lại. P17 opt-out retest PASS, caption text vẫn FAIL; sau đó sửa compact key English. UIPlay tiếp tục47PASS nhưng3layoutSettings mớiFAIL; đã sửa offsetY handle, chưa retest. Cần người dùng kiểm resize, hết Freeze/Chill, thời lượng reveal, caption mobile và slider settings trên máy thật.', '',
       'Đã tiếp quản bằng ledger và kết quả trên disk; không chạy lại các suite đã PASS. Bảy lỗi setup AR trước runtime được giữ ở `regression/setup-errors/`, không tính là lượt suite. Raw lượt đầu và retest ở `regression/runs/` / `regression/retests/`; `regression/Summary.json` giữ kết quả lượt đầu.', '',
       '## Bảng suite', '', '| Suite / harness thật | Lượt đầu | Retest mục FAIL | Kết quả cuối / bằng chứng |', '|---|---|---|---|']
for name in expected:
    r=rows[name];component=r.get('component') or next((x.get('component') for x in cases+jobs if x['name']==name),None)
    if not component:
        code=next((x.get('code') or '' for x in cases+jobs if x['name']==name),'')
        add=re.search(r'AddComponent<([^>]+)>',code)
        call=re.search(r'([\w.]+)\(',code)
        component=add.group(1) if add else call.group(1) if call else None
        if name=='ARGestureCheckMock':component='CampusRift.AR.ARGestureCheck (mock thủ công)'
        if name.startswith('ARMode-'):component='ARSessionBootstrap + mock gesture thủ công'
    display=name+(' — `'+component+'`' if component else '')
    path='retests/'+name+'/row.json' if name in retests else 'runs/'+name+'/row.json'
    summary=r.get('summary','')
    if isinstance(summary,dict):
        summary=f"{len(summary.get('passed',[]))} PASS / {len(summary.get('failed',[]))} FAIL" if 'passed' in summary else (f"{summary['visibleTexts']} chữ hiện; {len(summary['issues'])} lỗi" if 'visibleTexts' in summary else summary.get('summary',summary))
    lines.append('| '+display+' | '+r['status']+' · '+cell(summary)+' | '+retests.get(name,{}).get('status','—')+' | ['+final[name]+'](regression/'+path+') |')
lines += ['', '## Lỗi đã sửa và harness cập nhật', '',
    '- `Set1SkillRuntime.cs`: touch release của chiêu Instant dùng QuickCast, giữ luật cost/cooldown; LookSmoke/Kim Chung retest 2 mục PASS.',
    '- `MobileControlsHUD.cs`: theo dõi geometry canvas và cả localToWorldMatrix, chạy sau FitFrame để tránh nút văng khỏi màn hình khi resize. PlayerCombat: 65 PASS/1 FAIL ban đầu, retest 1 FAIL; ảnh sau sửa [7-mobile-resize-corrected.png](screens/7-mobile-resize-corrected.png) chỉ chứng minh bố cục visual.',
    '- `MonsterDoorInteraction.cs`, `CampusAutomaticDoor.cs`: xét phần capsule giao cánh cửa ở bậc, sửa kẹt cửa F13; ShabanTraversal retest mục fail PASS.',
    '- Harness `BoostEnergyPlayTest.cs`: nhận trạng thái cạn bằng vòng/màu và RECOVERING hiện hành; không còn yêu cầu caption cũ. Retest riêng PASS.',
    '- Harness `Level8to10PlayTest.cs`: đợi dawn/credits kết thúc rồi kiểm UIState.Victory và starMask7; chỉ mục màn kết quả retest PASS.',
    '- Harness `SkillSet1PlayTest.cs`: catalog/ranks21, wallet đủ21×3050, Fire Lotus mastery3×150% vẫn tổng450%, pool mưa50 nhưng baseline vẫn30 impact; fixture chờ FixedUpdate cho cast đầu. 268 PASS/12 FAIL ban đầu, chỉ12 mục retest PASS.',
    '- Harness `SkillSet1EdgePlayTest.cs`: đo expiry từ sự kiện apply thay vì giả định windup render, vẫn yêu cầu Freeze/Chill2.5s. Retest bị gián đoạn do lỗi điều phối agent, không được coi là PASS và không chạy lần2.',
    '- Harness `LocalizationPlayTest.cs`: cập nhật số câu của catalog hiện hành từ440 lên449, giữ kiểm dịch toàn bộ title/page/prompt/explanation và21lesson. Chỉ mục curriculum FAIL được retest, kết quả PASS;19mục PASS cũ không lặp.',
    '- Harness `EnemyAnimationPlayTest.cs`: kiểm frame strike theo attackImpactSeconds/clip.length (P19 là67.5%, clip cũ50%), giữ tolerance0.14; không dereference FootPlant trên Dực Yêu bay. Retest3strikeFAIL và tiếp tục7mục chưa chạy PASS; các mục PASS ban đầu không lặp.',
    '- Harness `ShabanBossPlayTest.cs`: theo REPORT-P19 T07/T08, boss7 phase2 có đúng2Ảnh Yêu; fixture saoL6 có summonersSeen/Killed và secondSummon, giữ bộ điều kiện pass/fail/completion. Chỉ2mục FAIL retest PASS.',
    '- `P12AudioEndingPlayTest.cs`: cập nhật wrapper P21 reveal lặp3.5s (thay P12ending4s), giữ cửa sổ -0.1/+0.3. Focused FAIL; timestamps gợi ý frame startup tính trước Begin. `P21StoryCinematic.cs` sửa dùng realtime tick bắt đầu tại Begin, tick cập nhật khi pause để không catch-up. Suite Cinematic/P21 riêng chạy sau sửa PASS gồm unskipped repeat>=3.3s/pause/restoration/dawn/credits/results; không thay FAIL P12 hoặc chạy focused lần2.',
    '- Harness `SkyBeastPresencePlayTest.cs`: primary lấy theo thứ tự Beasts của scheduler thay FindAny vốn không bảo đảm thứ tự khi L10 có2rồng; giữ expected023. Chỉ1mục FAIL retest riêng.',
    '- Collector ShelterAudit: `failed=40` là số node mismatch, acceptance cósẵn accuracy>=98%. Kết quả2086/2126=98.1185%, pass=true; sửa phân loại PASS, giữ40mismatch trong raw, không chạy lại suite và không thay threshold.',
    '- `MobileControlsHUD.cs` compact caption HƯ KHÔNG BÍCH→HƯ KHÔNG, gồm key VOID WALL trước localization. P17Smoke: opt-out dùng folderGuid mới thay foldercũ cókeep.txt, giữ assert khôngfile/directory; focused opt-outPASS/textFAIL trước khi thêmkeyEnglish. Không retest lần2.',
    '- `P17RelatedSmoke.cs`: sửa AudioSource trực tiếp đãcũ sang CurrentVoice/CurrentTrack của crossfade P21 và exactdusk/night/giao. Giữ6PASS capturepartial trướcexception; chỉ5mục music/language chưa chạy tiếp tụcPASS mộtlần.',
    '- `SquadIntegrationSmoke.cs`: fixture đi theo watchedEscapeDirection bằng CharacterController thay luônđithẳngright; window hữu hạn30s cho các lần thoát thực tế, không bơm exitUses hoặc bỏ yêu cầu AdaptedExitCount>0. Chỉ1mục FAIL retest PASS, Adaptive orders=1/signals9;4PASS không lặp.',
    '- `UIPlayValidation.cs`: fixture freshprofile cho Continue, Courses/Play quaHub, pagedCredits>=2/Nextđổi nội dung thayScrollRect cũ, gameplayreturnHub. Không lặp4PASS cũ;3FAIL cũ PASS và mọi mục bịchặn tiếp tục47PASS/3layoutFAIL. `SettingsUI.cs` bỏ sizeDelta.y+10 ở handle vì Slider.UpdateVisuals stretchY khiến38px vượt track28px. Giữ nguyên UIValidation audit, compile sạch, không retest lần2.',
    '- `ItemWheelUI.cs`: Quantity quick/slice row26→38px để chứa chữ ởTextSize130%, không giảmfont hoặc bỏaudit. P23Gameplay24PASS/1FAIL; chỉsize2mobile audit được retest1lần.',
    '- Harness `ARGestureUnitTests.cs` / `ARRiftPlayTest.cs`: ThumbUp là khiên, ILoveYou là unmapped; reset Linh Ấn fixture, đợi ReleaseReady thật (giữ D1 và giới hạn8s). Chỉ3 mục Thiên Thủ fail retest PASS.',
    '- Harness `ARNavigationPlayTest.cs`: đi qua Hub/MainMenu thật, chọn mode trước placement, xác nhận polygon bằng incenter; các mục fail/chưa chạy được tiếp tục đúng một lượt, không lặp mục PASS.', '',
    '## HUD và Docs', '',
    'Nút tròn AR dùng chữ nội tiếp/autosize. Linh Ấn thành thanh mảnh, chuỗi chỉ hiện theo tiền tố hoặc trợ giúp; hints đầu trận5s. Đấu Long dùng toast mép trên3s theo đổi trạng thái và vòng Kiếm Ý nhỏ cạnh rail. Tri Thức chỉ hiện bảng đáy khi rune cắt chữ. Luyện Ấn dùng dải1360×132 ở1/3 trên, nền bán trong suốt, hướng dẫn5s. Đồng bộ consent với ngoại lệ clip opt-in. Các file chính: ARUI, ARCombatHUD, ARSpaceHUD, ARHandsStudyHUD, ARKnowledgeSeal, ARSessionBootstrap.', '',
    'Đã cập nhật [Docs05](../../Docs/05-AR-VA-DEEP-LEARNING.md), [Docs07](../../Docs/07-MAN-CHOI-MOI-TRUONG-UI.md), [Docs09](../../Docs/09-THUAT-NGU-VA-KHAI-NIEM.md).', '',
    'P23BaseSkillRegression, AccessibilityPlayTest và P23GameplaySmoke là harness riêng được bổ sung vào inventory kế thừa. Alias P18Legacy/P18Reactions không gọi lặp cùng component. Các benchmark PC/native, bot thống kê50trial và ma trận ảnh review lịch sử không chạy theo TEST-POLICY; không gán PASS cho chúng. AR smoke mỗi mode chỉ một lượt ngắn/mock, không thay nghiệm thu camera/JNI trên Android.', '',
    '## APK', '',
    f"- [APK development](../../{verification['apk']}): **{verification['bytes']:,} byte** ({verification['bytes']/1024/1024:.2f} MiB).",
    f"- SHA256: `{verification['sha256']}`.",
    '- Build Succeeded trong 4 phút 48 giây; 0 lỗi, 73 cảnh báo. BuildPlayer thực sự chỉ được gọi một lần; lần queue trước đó bị compiler từ chối trước khi build vì kiểu enum orientation sai.',
    '- IL2CPP/ARM64, GLES3-only (manifest yêu cầu GLES3.1/0x30001), sensorLandscape (hai hướng ngang), debug keystore. Chữ ký Android Debug được apksigner xác nhận, scheme v2 PASS. Manifest debuggable; quyền Camera/Microphone/foreground MediaProjection và Activity/Service private có trong APK.',
    '- Model gesture STORED và SHA256 đúng model gốc;13file dữ liệu model Vosk STORED, mọi file Vosk so hash với StreamingAssets. README125byte dùng DEFLATED; VoiceBridge đọc asset qua stream để giải nén/copy sang filesDir. DEX có GestureBridge/onHands/setHandCount, VoiceBridge, ClipBridge/consent/service, Vosk Model/Recognizer, JNA và MediaPipe. lib/arm64-v8a có libil2cpp, libvosk, libjnidispatch, MediaPipe JNI.',
    '- Bằng chứng [build summary](build/build-summary.txt), [verification](build/verification.json), [manifest](build/AndroidManifest.txt), [badging](build/badging.txt), [chữ ký](build/apksigner.txt), [adb devices](build/adb-devices.txt).', '',
    '## Thiết bị và việc người dùng cần thử', '',
    ('Realme WGH6S8I7GIMBGQKR có kết nối; xem bằng chứng install/launch/logcat trong build/.' if verification['realmeConnected'] else 'adb không thấy Realme WGH6S8I7GIMBGQKR hoặc thiết bị nào. Chưa adb install/launch; chưa xác nhận Sảnh và màn chọn mode AR trên máy thật.'), '',
    '- Cài APK, mở Sảnh → chọn mode AR; xem camera/placement polygon, HUD và resize/nút khi xoay ngang, item wheel/touch đa ngón.',
    '- Chơi năm mode; thử ThumbUp/kết chuỗi, né25cm, Thiên Kiếm pitch/hold, nhấc/ném/quẹt, quiz/thưởng60LT và nhịp Luyện Ấn.',
    '- Tech2: hai tay/JNI khi giao nhau, Depth trên provider hỗ trợ, Vosk offline/quyền micro/ba keyword/+30%, MediaProjection cancel/stop/background/gallery/Android14+, FPS/nhiệt/RAM/probe. Những mục này chưa được chứng minh bằng mock hoặc PNG.', '',
    '## Bàn giao', '',
    'Unity **Android / Edit / SampleScene, scene không dirty, Console0 lỗi**, XR loader inactive. Save/settings/PlayerPrefs/InputSettings/EnterPlay/GameView/ProjectSettings phục hồi về snapshot gốc. Bằng chứng [restoration](7-restoration.json), [handoff](7-handoff.json), [Console](7-console-final.json).', '',
    'Backup trước sửa: `Backups/Regression-APK-pre-20261006/`; phần tiếp quản: `Backups/Regression-APK-pre-20261007-resume/`. Không sửa task/run-*.ps1 hoặc task/codex-accounts.json.']
(base/'REPORT-7-REGRESSION-APK.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
(out/'FinalSummary.json').write_text(json.dumps({'total':len(expected),'counts':dict(counts),'suites':final},ensure_ascii=False,indent=2),encoding='utf-8')
print('REPORT complete:',dict(counts))
