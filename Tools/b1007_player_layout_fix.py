from pathlib import Path
import shutil
p=Path('Assets/Combat/Validation/PlayerCombatPlayTest.cs');b=Path('Backups/Regression-APK-pre-20261006')/p
b.parent.mkdir(parents=True,exist_ok=True)
if not b.exists():shutil.copy2(p,b)
s=p.read_text(encoding='utf-8-sig')
s=s.replace('bool running;','[NonSerialized] public bool LayoutOnly;\n        bool running;',1).replace('var run = Run();','var run = LayoutOnly ? FailedLayout() : Run();',1)
start=s.index('                Canvas.ForceUpdateCanvases();',s.index('// No two buttons overlap'))
end=s.index('\n            }',start)
block=s[start:end]
s=s[:start]+'                CheckTouchLayout(hud, size);'+s[end:]
method='''        IEnumerator FailedLayout()
        {
            // Reproduce a resize from the previous Game View, then check only the
            // original failed 1920x1080 item. All combat and other sizes stay skipped.
            UIValidation.SetResolution(1600, 1200); yield return Frames(4);
            Mode(ControlMode.Mobile); yield return Frames(4);
            var hud = FindAnyObjectByType<MobileControlsHUD>(); hud.ShowAim();
            var size = new Vector2Int(1920, 1080);
            UIValidation.SetResolution(size.x, size.y); yield return Frames(4);
            CheckTouchLayout(hud, size);
        }
        void CheckTouchLayout(MobileControlsHUD hud, Vector2Int size)
        {
BLOCK
        }

'''.replace('BLOCK',block)
s=s.replace('        static Rect ScreenRect(RectTransform rt)',method+'        static Rect ScreenRect(RectTransform rt)',1)
p.write_text(s,encoding='utf-8')
with Path('task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Mốc PlayerCombat: diagnostic resize1600×1200→1920×1080 thấy Đánh/Né y âm và nút phải vượt2047px sau4frame. MobileControlsHUD theo dõi scale/rect Canvas ở LateUpdate để đặt lại khi geometry đổi sau Update. Harness LayoutOnly chỉ tái dùng đúng assertion1920 đãFAIL, các mụcPASS không lặp; file Combat đã backup.\n')
