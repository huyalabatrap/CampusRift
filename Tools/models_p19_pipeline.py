"""Adapt the user's original Blender rig/pose/IK/export pipeline; never modifies its source files."""
import bpy, pathlib, sys, json, math, importlib.util, hashlib
from mathutils import Vector, Matrix, Quaternion
ROOT=pathlib.Path(__file__).resolve().parents[1]
def module(name,file):
    spec=importlib.util.spec_from_file_location(name,file);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
geometry=module('user_geometry','D:/model-3d/pipeline/process_models.py')
animation=module('user_animation','D:/model-3d/pipeline/animate_export.py')
cfg=json.loads((ROOT/'task/models/selected-config.json').read_text(encoding='utf-8'))
ident=sys.argv[sys.argv.index('--')+1]
info=cfg[ident];name=info['model'];height=info['height'];folder=ROOT/'Assets/Enemies/Models/ReplacementP19'/name
folder.mkdir(parents=True,exist_ok=True);tex=folder/'Textures';tex.mkdir(exist_ok=True)
path=ROOT/info['file'];bpy.ops.wm.open_mainfile(filepath=str(path))
scene=bpy.context.scene
meshes=[o for o in bpy.data.objects if o.type=='MESH']
source=next(o for o in meshes if o.name==info['mesh']) if info.get('mesh') else max(meshes,key=lambda o:len(o.data.vertices))
rig=next((m.object for m in source.modifiers if m.type=='ARMATURE'),None)
if rig is None:rig=next((o for o in bpy.data.objects if o.type=='ARMATURE'),None)
source_rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
for source_rig in source_rigs:
    source_rig.data.pose_position='REST';source_rig.animation_data_clear()
    for b in source_rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
conversion=Matrix.Rotation(math.radians(info.get('rotate_x',0)),4,'X')
data=bpy.data.meshes.new_from_object(source.evaluated_get(bpy.context.evaluated_depsgraph_get()));data.transform(conversion@source.matrix_world)
minimum=Vector((min(v.co.x for v in data.vertices),min(v.co.y for v in data.vertices),min(v.co.z for v in data.vertices)))
maximum=Vector((max(v.co.x for v in data.vertices),max(v.co.y for v in data.vertices),max(v.co.z for v in data.vertices)))
center=Vector(((minimum.x+maximum.x)/2,(minimum.y+maximum.y)/2,minimum.z));scale=height/(maximum.z-minimum.z)
for v in data.vertices:v.co=(v.co-center)*scale
source_bones={b.name:{'head':(conversion@source_rig.matrix_world@b.head_local-center)*scale,'tail':(conversion@source_rig.matrix_world@b.tail_local-center)*scale} for source_rig in source_rigs for b in source_rig.data.bones}
# Restore and save source color/normal/spec/glow images from legacy Blender texture slots.
images=list(bpy.data.images)
for im in images:
    if not im.packed_file:
        p=path.parent/pathlib.Path(im.filepath.replace('\\','/').replace('//','')).name
        if not p.exists() and 'colorMap' in im.name:p=path.parent/'colorMap.png'
        if not p.exists() and 'normal' in im.name:p=path.parent/'normalMap.png'
        if p.exists():im.filepath=str(p);im.reload()
textures={}
for role,terms in [('BaseColor',['diff','color','colour','albedo']),('Normal',['normal','_n.']),('Specular',['spec']),('Emission',['glow','emit'])]:
    matches=[im for im in images if im.size[0]>32 and any(t in im.name.lower() for t in terms)]
    if ident=='hoa-linh' and role!='Emission':matches=[]
    override=info.get('textures',{}).get(role)
    if override:matches=[bpy.data.images.load(str(ROOT/override),check_existing=False)]
    if matches:
        im=max(matches,key=lambda im:im.size[0]*im.size[1]);im.filepath_raw=str(tex/(name+'_'+role+'.png'));im.file_format='PNG';im.save();textures[role]=name+'_'+role+'.png'
if ident=='hoa-linh':
    # The clean archive still carries an unused packed diffuse datablock. Do not carry
    # unknown legacy pixels into the editable derivative or FBX material references.
    for mat in list(bpy.data.materials):bpy.data.materials.remove(mat,do_unlink=True)
    for im in list(bpy.data.images):
        if not any(term in im.name.lower() for term in ['glow','emit']):bpy.data.images.remove(im,do_unlink=True)
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
collection=bpy.data.collections.new('COL_'+name);scene.collection.children.link(collection)
mesh=bpy.data.objects.new(name+'_Mesh',data);collection.objects.link(mesh);mesh['height_m']=height
for p in data.polygons:p.use_smooth=True
original_triangles=geometry.count_tri(data)
if original_triangles>58000:
    geometry.select([mesh]);mod=mesh.modifiers.new('LOD0_58k','DECIMATE');mod.ratio=58000/original_triangles;bpy.ops.object.modifier_apply(modifier=mod.name)
arm=bpy.data.objects.new(name+'_Rig',bpy.data.armatures.new(name+'_Skeleton'));collection.objects.link(arm)
geometry.CURRENT=ident;geometry.HEIGHTS[ident]=height;geometry.NAMES[ident]=name
geometry.select([arm]);bpy.ops.object.mode_set(mode='EDIT')
def add(n,h,t,parent=None,deform=True):return geometry.bone(arm,n,h,t,parent,deform)
def point(key,default):
    mapping=info.get('joints',{}).get(key)
    if mapping:
        if isinstance(mapping,list):return tuple(mapping)
        return tuple(source_bones[mapping]['head']/height)
    return default
hip=point('hip',(0,0,.48));spine=point('spine',(0,0,.59));chest=point('chest',(0,0,.73));neck=point('neck',(0,0,.83));head=point('head',(0,-.03,.90))
add('Root',(0,0,0),(0,0,.12),deform=False);add('Hips',hip,spine,'Root');add('Spine',spine,chest,'Hips');mid=tuple((chest[i]+neck[i])/2 for i in range(3));add('Chest',chest,mid,'Spine');add('UpperChest',mid,neck,'Chest');add('Neck',neck,head,'UpperChest');add('Head',head,(head[0],head[1],.99),'Neck')
for side,sg in [('L',1),('R',-1)]:
    upper=point('thigh'+side,(sg*.075,0,.48));knee=point('shin'+side,(sg*.085,-.02,.26));foot=point('foot'+side,(sg*.09,0,.065));toe=point('toe'+side,(sg*.09,-.12,.03))
    shoulder=point('arm'+side,(sg*.15,0,.76));elbow=point('fore'+side,(sg*.24,0,.60));wrist=point('hand'+side,(sg*.28,-.015,.45));palm=point('palm'+side,(sg*.30,-.025,.39))
    add('UpperLeg.'+side,upper,knee,'Hips');add('LowerLeg.'+side,knee,foot,'UpperLeg.'+side);add('Foot.'+side,foot,toe,'LowerLeg.'+side);add('Toes.'+side,toe,(toe[0],toe[1]-.04,toe[2]),'Foot.'+side)
    add('Shoulder.'+side,mid,shoulder,'UpperChest');add('UpperArm.'+side,shoulder,elbow,'Shoulder.'+side);add('LowerArm.'+side,elbow,wrist,'UpperArm.'+side);add('Hand.'+side,wrist,palm,'LowerArm.'+side)
    if ident=='duc-yeu':
        add('Wing.'+side,(sg*.09,0,.62),(sg*.5,0,.63),'UpperChest');add('WingTip.'+side,(sg*.5,0,.63),(sg*1.0,.03,.55),'Wing.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
geometry.select([arm,mesh]);bpy.context.view_layer.objects.active=arm
try:bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except RuntimeError:pass
weights=geometry.clean_weights(mesh,arm,ident)
# Use geometric regions for bats: membranes follow two broad wing segments, head and torso remain rigid.
if ident=='duc-yeu':
    for v in mesh.data.vertices:
        x,y,z=v.co/height;side='L' if x>0 else 'R';a=abs(x)
        wing=max(0,min(1,(a-.08)/.16));tip=max(0,min(1,(a-.4)/.55))
        influences={'Head' if z>.70 else 'UpperChest':1-wing,'Wing.'+side:wing*(1-tip),'WingTip.'+side:wing*tip}
        for g in list(v.groups):mesh.vertex_groups[g.group].remove([v.index])
        for nm,w in influences.items():
            if w>0:mesh.vertex_groups[nm].add([v.index],w,'REPLACE')
mesh.parent=arm;mesh.matrix_parent_inverse=Matrix.Identity(4)
rest=[v.co.copy() for v in mesh.data.vertices];edges=[(e.vertices[0],e.vertices[1],(rest[e.vertices[0]]-rest[e.vertices[1]]).length) for e in mesh.data.edges]
animation.NAMES[ident]=name
specials={'anh-yeu':[('Teleport_Vanish',36,False),('Shadow_Strike',48,False),('Conceal',90,True),('Taunt_Shadow',90,False)],'trieu-hon-su':[('Summon_Cast',60,False),('Heal_Channel',90,True),('Shield_Cast',60,False),('Ritual_Bow',90,False)],'duc-yeu':[('Hover',30,True),('Fly',24,True),('Dive',48,False),('Fireball_Cast',60,False)],'hoa-linh':[('Fire_Idle',90,True),('Fire_Cast',60,False),('Ember_Run',30,True),('Fire_Burst',60,False)]}
clips=[];timeline=1;arm.animation_data_create();scene.render.fps=30
def pose(clip,t,amplitude=1):
    # Two-bone IK and the base movement/recoil/death poses are from the user's animate_export.py.
    # The source excludes root turns from its amplitude multiplier. Limit the actual root
    # quaternion separately for the pitched bat so an idle turn becomes a small wing bank.
    animation.pose(arm,mesh,ident,clip,t,amplitude)
    if ident=='duc-yeu' and clip.startswith('Turn'):
        pb=arm.pose.bones['Root'];pb.rotation_quaternion=Quaternion((1,0,0,0)).slerp(pb.rotation_quaternion,.12)
    beat=animation.pulse(t);sine=math.sin(t*math.tau)
    if info.get('arms_drop'):
        for side,sg in [('L',1),('R',-1)]:animation.rotate(arm,'UpperArm.'+side,(0,1,0),sg*math.radians(info['arms_drop']))
    if clip in ['Summon_Cast','Heal_Channel','Shield_Cast','Fireball_Cast','Fire_Cast','Fire_Burst']:
        for side,sg in [('L',1),('R',-1)]:
            animation.rotate(arm,'UpperArm.'+side,(1,0,0),math.radians(-42*beat)*amplitude);animation.rotate(arm,'LowerArm.'+side,(1,0,0),math.radians(-25*beat)*amplitude)
        animation.rotate(arm,'Chest',(1,0,0),math.radians(-7*beat)*amplitude)
    if clip in ['Teleport_Vanish','Conceal','Shadow_Strike']:
        animation.rotate(arm,'Chest',(1,0,0),math.radians(-18*beat)*amplitude);animation.rotate(arm,'UpperArm.R',(1,0,0),math.radians(-42*beat)*amplitude)
    if ident=='duc-yeu':
        # Every living bat clip flaps, so the existing generic driver retains aerial movement animation.
        if not clip.startswith('Death'):
            for side,sg in [('L',1),('R',-1)]:
                angle=-22*sg*math.sin(t*math.tau)*(1.2 if clip in ['Fly','Run_Forward'] else 1)
                if clip=='Dive':angle=sg*(30*beat-14)
                animation.rotate(arm,'Wing.'+side,(0,1,0),math.radians(angle)*amplitude)
                animation.rotate(arm,'WingTip.'+side,(0,1,0),math.radians(angle*.45)*amplitude)
    bpy.context.view_layer.update()
for clip,length,loop in animation.CORE+specials[ident]:
    arm.animation_data.action=None
    for tr in arm.animation_data.nla_tracks:tr.mute=True
    amp=1;quality=[]
    for attempt in range(5):
        quality=[]
        for t in [.25,.5,.75]:pose(clip,t,amp);quality.append(animation.skin_quality(mesh,rest,edges))
        if max(q['edge_over_2x_percent'] for q in quality)<.5:break
        amp*=.65
    act=bpy.data.actions.new(name+'_'+clip);act.use_fake_user=True;arm.animation_data.action=act
    for f in list(range(1,length+1,2))+([length] if length%2==0 else []):
        pose(clip,(f-1)/(length-1),amp)
        for pb in arm.pose.bones:pb.keyframe_insert('location',frame=f,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=f,group=pb.name)
    for slot in act.slots:
        for layer in act.layers:
            for strip in layer.strips:
                bag=strip.channelbag(slot)
                if bag:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points:kp.interpolation='LINEAR'
    arm.animation_data.action=None;track=arm.animation_data.nla_tracks.new();track.name=act.name;strip=track.strips.new(act.name,timeline,act);strip.action_frame_start=1;strip.action_frame_end=length;strip.extrapolation='NOTHING';strip.blend_type='REPLACE';strip.action_slot=act.slots[0]
    clips.append({'name':name+'_'+clip,'frames':length,'fps':30,'loop':loop,'amplitude':amp,'quality':quality});timeline+=length+4
for tr in arm.animation_data.nla_tracks:tr.mute=False
arm.animation_data.action=None
for pb in arm.pose.bones:pb.matrix_basis=Matrix.Identity(4)
scene.frame_start=1;scene.frame_end=timeline;bpy.context.view_layer.update()
def export(path,animated):
    geometry.select([arm,mesh])
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,use_armature_deform_only=False,bake_anim=animated,bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='AUTO',embed_textures=False)
export(folder/(name+'.fbx'),True)
derived=ROOT/'task/models/derived';derived.mkdir(exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(derived/(name+'.blend')))
lods=[geometry.count_tri(mesh.data)]
original_data=mesh.data
for level,budget,ratio in [(1,15000,.5),(2,5000,.25)]:
    mesh.data=original_data.copy();geometry.select([mesh]);mod=mesh.modifiers.new('Decimate_LOD'+str(level),'DECIMATE');mod.ratio=min(ratio,budget/geometry.count_tri(mesh.data));bpy.ops.object.modifier_apply(modifier=mod.name)
    geometry.clean_weights(mesh,arm,ident);count=geometry.count_tri(mesh.data);lods.append(count);assert count<=budget
    export(folder/(name+'_LOD'+str(level)+'.fbx'),False)
    mesh.data=original_data
report={'id':ident,'model':name,'source':info['file'],'height':height,'originalTriangles':original_triangles,'lodTriangles':lods,'bones':len(arm.data.bones),'weights':weights,'clips':clips,'textures':textures,'pipeline':'Adapted process_models.py clean_weights/bone + animate_export.py CORE/pose/IK/skin_quality, generic FBX30fps, no external animations.'}
(ROOT/'task/models'/ (name+'-pipeline.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'model':name,'lods':lods,'clips':len(clips),'textures':textures}),flush=True)
