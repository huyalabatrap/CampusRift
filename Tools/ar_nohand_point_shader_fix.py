from ar_nohand import *
connect()
# The real log identifies Material(null) from the per-scan feature-point visual.
p=ROOT/'Assets/ARRift/Runtime/RiftPlacementService.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('Material dotMaterial;', 'Material dotMaterial;bool pointMaterialUnavailable;')
a='if(points==null)return;int index=0;if(dotMaterial==null){dotMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));dotMaterial.SetColor("_BaseColor",new Color(.65f,.65f,.65f,.22f));dotMaterial.SetFloat("_Surface",1);dotMaterial.SetInt("_SrcBlend",5);dotMaterial.SetInt("_DstBlend",10);dotMaterial.SetInt("_ZWrite",0);dotMaterial.renderQueue=3000;dotMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}'
b='''if(points==null)return;int index=0;
            if(dotMaterial==null)
            {
                // A Resources material retains the shader and its transparent variant in APKs.
                var template=Resources.Load<Material>("ARModes/FeaturePoints");
                if(template==null){if(!pointMaterialUnavailable){pointMaterialUnavailable=true;Debug.LogWarning("[ARDiag] Feature-point material missing; placement remains available");}return;}
                dotMaterial=new Material(template);
            }'''
assert a in s;s=s.replace(a,b,1);p.write_text(s,encoding='utf-8')
print(call('refresh_unity',{}))
asset='Assets/ARRift/Resources/ARModes/FeaturePoints.mat'
assert not (ROOT/asset).exists()
result=code('var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new System.Exception("Editor URP Unlit shader missing");var m=new Material(shader);m.SetColor("_BaseColor",new Color(.65f,.65f,.65f,.22f));m.SetFloat("_Surface",1);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",10);m.SetInt("_ZWrite",0);m.renderQueue=3000;m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");AssetDatabase.CreateAsset(m,"'+asset+'");AssetDatabase.SaveAssets();return new {shader=m.shader.name,queue=m.renderQueue,transparent=m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")};')
save('feature-point-material.json',result)
# Archive the verified first build before producing the final APK after the log-driven fix.
source=(OUT/'build').resolve();target=(OUT/'build-attempt1').resolve();workspace=OUT.resolve()
assert source.is_relative_to(workspace) and target.is_relative_to(workspace) and source.name=='build' and not target.exists()
shutil.move(str(source),str(target));save('added-assets.json',[asset,asset+'.meta'])
milestone('Log máy thật mới lấy phát hiện lỗi phụ đã chứng minh: RiftPlacementService.UpdatePoints tạo Material(Shader.Find URP/Unlit=null), spam ArgumentNullException mỗi scan; code đã có từ GóiA, lỗi đóng gói shader không phải native model. Sửa dùng Resources/ARModes/FeaturePoints.mat có shader/transparent variant được tham chiếu, thiếu asset thì ngừng visual optional và warning một lần. Không đổi polygon/placement/gameplay/D1; không rerun 2 suite. Build đã verify giữ nguyên nohand/build-attempt1; cần build cuối vì source đổi sau log. Editor vẫn Android/Edit/SampleScene và save đã phục hồi.')
print('Material retained, source fixed, first build archived.')
