import bpy, pathlib, math, json
from mathutils import Vector, Matrix
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'task/models/candidates';OUT.mkdir(parents=True,exist_ok=True)
REF=pathlib.Path('D:/model-3d/Unity_Export/008_GrimReaper/008_GrimReaper.blend')
def append_mesh(path,height,x,selector=None):
    before=set(bpy.data.objects)
    before_images=set(bpy.data.images)
    if path.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        with bpy.data.libraries.load(str(path),link=False) as (src,dst):
            dst.objects=src.objects;dst.images=src.images
        for o in dst.objects:
            if o and o.name not in bpy.context.scene.objects:bpy.context.scene.collection.objects.link(o)
    objs=[o for o in bpy.data.objects if o not in before]
    images=[i for i in bpy.data.images if i not in before_images and i.type=='IMAGE']
    for im in images:
        if not im.packed_file:
            original=path.parent/pathlib.Path(im.filepath.replace('\\','/').replace('//','')).name
            if not original.exists() and 'colorMap' in im.name:original=path.parent/'colorMap.png'
            if not original.exists() and 'normal' in im.name:original=path.parent/'normalMap.png'
            if original.exists():im.filepath=str(original);im.reload()
    meshes=[o for o in objs if o.type=='MESH']
    mesh=max(meshes,key=lambda o:len(o.data.vertices)) if not selector else next(o for o in meshes if o.name.startswith(selector))
    arm=next((m.object for m in mesh.modifiers if m.type=='ARMATURE'),None)
    if arm:
        arm.data.pose_position='REST'
        if arm.animation_data:arm.animation_data_clear()
    bpy.context.view_layer.update()
    # Bake only the source rest geometry; controls/cameras are not part of the asset.
    deps=bpy.context.evaluated_depsgraph_get();data=bpy.data.meshes.new_from_object(mesh.evaluated_get(deps))
    matrix=mesh.matrix_world.copy()
    if 'bat_v5' in path.name:matrix=Matrix.Rotation(math.pi/2,4,'X')@matrix
    data.transform(matrix)
    for o in objs:bpy.data.objects.remove(o,do_unlink=True)
    g=bpy.data.objects.new('Candidate',data);bpy.context.scene.collection.objects.link(g)
    zmin=min(v.co.z for v in data.vertices);zmax=max(v.co.z for v in data.vertices)
    center=Vector(((min(v.co.x for v in data.vertices)+max(v.co.x for v in data.vertices))/2,(min(v.co.y for v in data.vertices)+max(v.co.y for v in data.vertices))/2,zmin))
    scale=height/(zmax-zmin)
    for v in data.vertices:v.co=(v.co-center)*scale
    g.location.x=x
    for p in data.polygons:p.use_smooth=True
    # Legacy Blender materials lose texture slots on import to Blender 5; restore their source images to Principled BSDF.
    for mat in data.materials:
        if not mat:continue
        mat.use_nodes=True;bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if bs is None:
            mat.node_tree.nodes.clear();bs=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled');output=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
        if not bs.inputs['Base Color'].is_linked:
            mat.node_tree.nodes.clear();bs=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled');output=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
        bs.inputs['Roughness'].default_value=.72
        images=[i for i in images if i.size[0]>32 and i.name not in ['Render Result','Viewer Node']]
        relevant=[i for i in images if any(k in i.name.lower() for k in ['diff','color','colour','albedo','base'])]
        if not relevant:relevant=[i for i in images if not any(k in i.name.lower() for k in ['normal','spec','glow','hdr','sky'])]
        if relevant and not bs.inputs['Base Color'].is_linked:
            im=max(relevant,key=lambda i:i.size[0]*i.size[1]);node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
        for i in images:
            if ('normal' in i.name.lower() or '_n.' in i.name.lower()) and not bs.inputs['Normal'].is_linked:
                node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=i;i.colorspace_settings.name='Non-Color';normal=mat.node_tree.nodes.new('ShaderNodeNormalMap');mat.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],bs.inputs['Normal']);break
        if 'golem_clean' in path.name:
            bs.inputs['Base Color'].default_value=(.07,.055,.04,1)
            for link in list(bs.inputs['Base Color'].links):mat.node_tree.links.remove(link)
            glow=next((i for i in images if 'glow' in i.name.lower() or 'emit' in i.name.lower()),None)
            if glow:
                node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=glow;mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=3
        output=next(n for n in mat.node_tree.nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output)
        for link in list(output.inputs['Surface'].links):mat.node_tree.links.remove(link)
        mat.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
    return g
def label(text,x):
    data=bpy.data.curves.new('Label','FONT');data.body=text;data.align_x='CENTER';data.size=.105;data.extrude=0
    g=bpy.data.objects.new('Label',data);bpy.context.scene.collection.objects.link(g);g.location=(x,-1,.03);g.rotation_euler=(math.pi/2,0,0)
    mat=bpy.data.materials.new('Label');mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.02,.025,.035,1);g.data.materials.append(mat)
def sheet(role,entries,height):
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=24
    scene.render.resolution_x=1600;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.15,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
    scene.view_settings.view_transform='AgX'
    for ix,(name,path,sel) in enumerate([('USER 008 Grim Reaper',REF,None)]+entries):
        # Isolate images/materials per append so legacy texture choices cannot cross-contaminate models.
        oldimages=set(bpy.data.images)
        x=(ix-len(entries)/2)*2.4
        g=append_mesh(path,1.5 if ix==0 else height,x,sel)
        label(name,x)
        for im in bpy.data.images:
            if im not in oldimages:im.name=role+'_'+str(ix)+'_'+im.name
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.01));ground=bpy.context.object
    mat=bpy.data.materials.new('Ground');mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.17,.18,.2,1);ground.data.materials.append(mat)
    for name,loc,energy,color,size in [('Key',(-4,-6,7),1600,(1,.88,.75),6),('Fill',(5,-3,4),1000,(.6,.75,1),5),('Rim',(1,3,6),1900,(.75,.8,1),5)]:
        l=bpy.data.objects.new(name,bpy.data.lights.new(name,'AREA'));scene.collection.objects.link(l);l.location=loc;l.data.energy=energy;l.data.color=color;l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
    cam=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(cam);cam.location=(0,-16,4);cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=10.7;scene.camera=cam
    scene.render.filepath=str(OUT/(role+'.png'));bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(role+'.blend')))
if __name__=='__main__':
    cfg=json.loads((ROOT/'task/models/candidate-config.json').read_text(encoding='utf-8'))
    for role,info in cfg.items():sheet(role,[(e['name'],ROOT/e['file'],e.get('mesh')) for e in info['entries']],info['height'])
