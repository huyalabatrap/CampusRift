import bpy, json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = []
for relative in [
    'Assets/GameReadyModels/027_TwinHeadDemon/027_TwinHeadDemon.fbx',
    'Assets/Enemies/Models/LOD/027_TwinHeadDemon_LOD1.fbx',
]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root / relative), use_anim=False)
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        import bmesh
        bm = bmesh.new(); bm.from_mesh(obj.data)
        before = dict(vertices=len(bm.verts), faces=len(bm.faces), boundary=sum(e.is_boundary for e in bm.edges))
        sizes = []
        for distance in [0.0001, 0.001, 0.003, 0.005, 0.01]:
            copy = bm.copy()
            bmesh.ops.remove_doubles(copy, verts=list(copy.verts), dist=distance)
            sizes.append(dict(distance=distance, vertices=len(copy.verts), faces=len(copy.faces), boundary=sum(e.is_boundary for e in copy.edges)))
            copy.free()
        rows.append(dict(file=relative, before=before, welded=sizes))
        bm.free()
path = root / 'Artifacts/P12/fix1/Mesh-probe.json'
path.parent.mkdir(parents=True, exist_ok=True)
path.write_text(json.dumps(rows, indent=2))
print(json.dumps(rows))
