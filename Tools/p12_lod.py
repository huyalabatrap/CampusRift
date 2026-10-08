"""Blender headless: skin-preserving LODs from supplied FBX, never changes source."""
import bpy, json, math, sys, time
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / 'Artifacts' / 'P12'
out.mkdir(parents=True, exist_ok=True)
report_path = out / 'LOD-Blender.json'
rows = json.loads(report_path.read_text()) if report_path.exists() else []
for family, destination in [('GameReadyModels', 'Enemies'), ('GameReadyDragons', 'SkyBeast')]:
    for source in sorted((root / 'Assets' / family).glob('*/*.fbx')):
        for level, target in [(1, 15000), (2, 5000)]:
            dest = root / 'Assets' / destination / 'Models' / 'LOD' / (source.stem + f'_LOD{level}.fbx')
            if dest.exists() and any(r['file'] == str(dest.relative_to(root)) and r['passed'] for r in rows):
                continue
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False)
            meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
            rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
            total = sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
            row = dict(id=source.stem[:3], level=level, source=str(source.relative_to(root)),
                       file=str(dest.relative_to(root)), source_triangles=total, target=target, meshes=[])
            for rig in rigs:
                rig.data.pose_position = 'REST'
                rig.animation_data_clear()
            for obj in meshes:
                bpy.context.view_layer.objects.active = obj
                obj.select_set(True)
                modifier = obj.modifiers.new('P12_SkinLOD', 'DECIMATE')
                modifier.ratio = min(1.0, target / total)
                modifier.use_collapse_triangulate = True
                bpy.ops.object.modifier_apply(modifier=modifier.name)
                # Collapse interpolates vertex groups. Limit after interpolation to FBX/Android budget.
                bpy.ops.object.vertex_group_limit_total(limit=4)
                bpy.ops.object.vertex_group_normalize_all(lock_active=False)
                missing = sum(1 for v in obj.data.vertices if not any(g.weight > 0 for g in v.groups))
                influences = max((sum(g.weight > 0 for g in v.groups) for v in obj.data.vertices), default=0)
                triangles = sum(len(p.vertices)-2 for p in obj.data.polygons)
                row['meshes'].append(dict(name=obj.name, triangles=triangles, vertices=len(obj.data.vertices),
                    unweighted=missing, max_influences=influences, uv_layers=len(obj.data.uv_layers),
                    armature=any(m.type == 'ARMATURE' for m in obj.modifiers)))
                obj.select_set(False)
            row['triangles'] = sum(m['triangles'] for m in row['meshes'])
            row['bones'] = sum(len(r.data.bones) for r in rigs)
            row['passed'] = (abs(row['triangles']-target) < target*.05 and row['bones'] > 30 and
                all(m['unweighted']==0 and m['max_influences']<=4 and m['uv_layers']>0 and m['armature'] for m in row['meshes']))
            if not row['passed']:
                raise RuntimeError('LOD audit failed: ' + json.dumps(row))
            bpy.ops.object.select_all(action='SELECT')
            dest.parent.mkdir(parents=True, exist_ok=True)
            bpy.ops.export_scene.fbx(filepath=str(dest), use_selection=True, object_types={'MESH','ARMATURE'},
                add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y',
                apply_unit_scale=True, mesh_smooth_type='FACE', use_mesh_modifiers=True)
            rows = [r for r in rows if r['file'] != row['file']] + [row]
            report_path.write_text(json.dumps(rows, indent=2), encoding='utf-8')
            print('P12_LOD', row['id'], level, row['triangles'], 'PASS', flush=True)
print('P12_LOD_DONE', len(rows), flush=True)
