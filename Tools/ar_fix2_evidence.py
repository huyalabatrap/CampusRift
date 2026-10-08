from ar_fix2_local import *
save(out/'runtime-wiring.json',code(Path('task/ar/fix2-wiring.cs').read_text(encoding='utf-8')))
for w,h in [(2400,1080),(1600,720)]:
    code('UIValidation.SetResolution('+str(w)+','+str(h)+');return true;');time.sleep(.5)
    save(out/('layout-'+str(w)+'.json'),code(Path('task/ar/fix2-layout.cs').read_text(encoding='utf-8')))
# Resolution reduction also preserves the full camera rect (no viewport crop).
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().SessionPipeline.renderScale=.85f;return true;');time.sleep(.5)
save(out/'render-scale-085.json',code('var c=UnityEngine.Camera.main;return new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,pixelRect=c.pixelRect.ToString(),rect=c.rect.ToString(),renderScale=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().SessionPipeline.renderScale};'))
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().SessionPipeline.renderScale=1;return true;')
print('Wiring/layout/scale evidence saved')
