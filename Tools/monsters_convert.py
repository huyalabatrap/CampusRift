import bpy, sys, os, json
from mathutils import Vector

src_dir, out_dir = r"C:/Users/Admin/AppData/Local/Temp/monsters", sys.argv[sys.argv.index('--')+1]
names = sys.argv[sys.argv.index('--')+2:]
os.makedirs(out_dir, exist_ok=True)

RENAME = {'HitRecieve': 'Hit', 'HitReact': 'Hit', 'Bite_Front': 'Attack', 'Punch': 'Attack'}

for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(src_dir, name + ".glb"))
    # Clean clip names: "EnemyArmature|EnemyArmature|EnemyArmature|Attack" -> "Attack".
    for action in bpy.data.actions:
        short = action.name.split('|')[-1]
        action.name = RENAME.get(short, short)
    # Make sure every action is exported as its own take.
    for obj in bpy.context.scene.objects:
        if obj.animation_data:
            obj.animation_data.action = None
            for track in list(obj.animation_data.nla_tracks):
                obj.animation_data.nla_tracks.remove(track)
    mats = {}
    for m in bpy.data.materials:
        color = [1, 1, 1, 1]; texture = None
        if m.use_nodes:
            for node in m.node_tree.nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    color = list(node.inputs['Base Color'].default_value)
                    for link in m.node_tree.links:
                        if link.to_node == node and link.to_socket.name == 'Base Color' and link.from_node.type == 'TEX_IMAGE':
                            texture = link.from_node.image.name if link.from_node.image else 'linked'
        mats[m.name] = {'color': color, 'texture': texture}
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in meshes:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    info = {'height': mx.z - mn.z, 'width': mx.x - mn.x, 'depth': mx.y - mn.y, 'floor': mn.z, 'materials': mats,
            'actions': {a.name: [a.frame_range[0], a.frame_range[1]] for a in bpy.data.actions},
            'fps': bpy.context.scene.render.fps}
    json.dump(info, open(os.path.join(out_dir, name + ".json"), 'w'), indent=1)
    for img in bpy.data.images:
        if img.packed_file is not None:
            path = os.path.join(out_dir, name + '_' + img.name.replace(' ', '_').replace('.png', '') + '.png')
            open(path, 'wb').write(img.packed_file.data); print('TEXTURE', path, img.size[0], img.size[1])
    bpy.ops.export_scene.fbx(filepath=os.path.join(out_dir, name + ".fbx"), use_selection=False,
        object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        path_mode='STRIP', embed_textures=False, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL')
    print("CONVERTED", name, "height=%.2f" % info['height'], "actions=", list(info['actions']))
