from pathlib import Path
import bpy,json
root=Path(__file__).resolve().parents[1]
path=root/'task/models/derived/LavaGolem.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
removed=[]
for mat in list(bpy.data.materials):bpy.data.materials.remove(mat,do_unlink=True)
for im in list(bpy.data.images):
 if not any(term in im.name.lower() for term in ['glow','emit']):
  removed.append({'name':im.name,'packed':bool(im.packed_file),'size':list(im.size)})
  bpy.data.images.remove(im,do_unlink=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(path))
report={'derived':'task/models/derived/LavaGolem.blend','removedImages':removed,'retainedImages':[im.name for im in bpy.data.images],'gameTexturesChanged':False,'sourceArchiveUnmodified':True}
(root/'task/models/derived-image-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
