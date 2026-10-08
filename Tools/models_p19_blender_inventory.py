import bpy, pathlib, json, sys
root=pathlib.Path(__file__).resolve().parents[1]
rows=[]
for path in (root/'task/models/source').rglob('*.blend'):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    row={'file':str(path.relative_to(root)), 'meshes':[], 'rigs':[], 'images':[]}
    for o in bpy.data.objects:
        if o.type=='MESH':row['meshes'].append({'name':o.name,'verts':len(o.data.vertices),'tris':sum(len(p.vertices)-2 for p in o.data.polygons),'bounds':[list(o.matrix_world@__import__('mathutils').Vector(v)) for v in o.bound_box],'materials':[{'name':m.name,'nodes':[{'type':n.type,'image':n.image.name if n.type=='TEX_IMAGE' and n.image else None} for n in m.node_tree.nodes] if m.node_tree else []} for m in o.data.materials if m], 'groups':[g.name for g in o.vertex_groups]})
        if o.type=='ARMATURE':row['rigs'].append({'name':o.name,'bones':[{'name':b.name,'head':list(o.matrix_world@b.head_local),'tail':list(o.matrix_world@b.tail_local)} for b in o.data.bones]})
    for im in bpy.data.images:row['images'].append({'name':im.name,'filepath':im.filepath,'size':list(im.size),'packed':bool(im.packed_file)})
    rows.append(row)
    print(path.name,[(x['name'],x['tris']) for x in row['meshes']],flush=True)
(root/'task/models/blender-source-inventory.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
