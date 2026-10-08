import bpy, pathlib, json
root = pathlib.Path(__file__).resolve().parents[1]
base = root / 'Assets/Enemies/Models/P19'
out = base / 'LOD'
out.mkdir(exist_ok=True)
report=[]
for name in ['Ninja','Wizard','Armabee','Ghost']:
    for level,ratio in [(1,.5),(2,.25)]:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(base/(name+'.fbx')))
        collection=bpy.data.collections.new('COL_'+name+'_LOD'+str(level))
        bpy.context.scene.collection.children.link(collection)
        meshes=[o for o in bpy.data.objects if o.type=='MESH']
        before=after=unweighted=0
        for obj in meshes:
            before+=sum(len(p.vertices)-2 for p in obj.data.polygons)
            bpy.context.view_layer.objects.active=obj
            obj.select_set(True)
            mod=obj.modifiers.new('LOD_Collapse','DECIMATE');mod.ratio=ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
            after+=sum(len(p.vertices)-2 for p in obj.data.polygons)
            unweighted+=sum(1 for v in obj.data.vertices if not v.groups)
            for c in list(obj.users_collection):c.objects.unlink(obj)
            collection.objects.link(obj)
            obj.select_set(False)
        assert after<before and unweighted==0,(name,level,before,after,unweighted)
        bpy.ops.export_scene.fbx(filepath=str(out/(name+'_LOD'+str(level)+'.fbx')),use_selection=False,object_types={'MESH','ARMATURE'},apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
        report.append(dict(model=name,level=level,originalTriangles=before,triangles=after,unweighted=unweighted))
(root/'task/p19/lod-source-audit.json').write_text(json.dumps(report,indent=2))
