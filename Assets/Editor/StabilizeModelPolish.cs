#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CampusRift.Enemies;

public static class StabilizeModelPolish
{
    const string Folder="Assets/Enemies/Models/Stabilize";
    public static string Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Need Edit Mode");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        string batPath="Assets/Enemies/Prefabs/DucYeu.prefab";
        var bat=PrefabUtility.LoadPrefabContents(batPath);
        try{
            var visual=bat.transform.Find("Visual_VampireBat");
            visual.localScale=Vector3.one*.8f;
            bat.GetComponent<LODGroup>().RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(bat,batPath);
        }finally{PrefabUtility.UnloadPrefabContents(bat);}
        string path="Assets/Enemies/Prefabs/AnhYeu.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual=root.transform.Find("Visual_NightDemon");
            foreach(var child in visual.Cast<Transform>().Where(t=>new[]{"Torn shadow cloak","Shadow hood","Crimson face scarf","Shadow bone dagger","Shadow woven tunic","Crimson waist sash"}.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var bones=root.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Enemies/Animation/ReplacementP19/NightDemon_Idle_Breathe.anim");
            idle.SampleAnimation(visual.gameObject,0);
            foreach(string side in new[]{"L","R"})
            {
                var arm=bones["UpperArm."+side];var lower=bones["LowerArm."+side];
                var direction=root.transform.TransformDirection(new Vector3(side=="L"?-.13f:.13f,-1,.15f).normalized);
                arm.rotation=Quaternion.FromToRotation(lower.position-arm.position,direction)*arm.rotation;
                var hand=bones["Hand."+side];
                lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,root.transform.TransformDirection(new Vector3(0,-1,.28f).normalized))*lower.rotation;
                QuaternionCurves(idle,visual,arm);QuaternionCurves(idle,visual,lower);
            }
            EditorUtility.SetDirty(idle);idle.SampleAnimation(visual.gameObject,0);
            var cloth=Material("Shadow woven charcoal cloth",new Color(.12f,.15f,.19f),.08f);
            var wraps=Material("Shadow worn crimson wraps",new Color(.43f,.075f,.065f),.12f);
            var steel=Material("Shadow etched steel",new Color(.46f,.53f,.62f),.62f);
            var body=root.GetComponentsInChildren<SkinnedMeshRenderer>().First();
            body.sharedMaterial.SetFloat("_Metallic",.15f);body.sharedMaterial.SetFloat("_Smoothness",.3f);body.sharedMaterial.SetColor("_BaseColor",new Color(1.12f,1.06f,1.04f));EditorUtility.SetDirty(body.sharedMaterial);
            var cloak=new Mesh{name="STABILIZE torn cloak"};var verts=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var tris=new List<int>();
            int cols=9,rows=6;
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
            {
                float v=y/(float)(rows-1),u=x/(float)(cols-1);float width=Mathf.Lerp(.33f,.47f,v);
                float height=Mathf.Lerp(1.52f,.25f,v)+(y==rows-1?(x%2==0?.07f:-.11f):0);
                float z=-.22f-v*.14f+Mathf.Sin(u*Mathf.PI*6)*.027f;
                verts.Add(visual.InverseTransformPoint(root.transform.TransformPoint(new Vector3((u-.5f)*width*2,height,z))));uv.Add(new Vector2(u,v));
                weights.Add(new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-Mathf.Clamp01(v*1.5f),weight1=Mathf.Clamp01(v*1.5f)});
            }
            for(int y=0;y<rows-1;y++)for(int x=0;x<cols-1;x++){int a=y*cols+x;tris.AddRange(new[]{a,a+cols,a+1,a+1,a+cols,a+cols+1});}
            cloak.SetVertices(verts);cloak.SetUVs(0,uv);cloak.SetTriangles(tris,0);cloak.boneWeights=weights.ToArray();
            var cloakBones=new[]{bones["UpperChest"],bones["Hips"]};cloak.bindposes=cloakBones.Select(b=>b.worldToLocalMatrix*visual.localToWorldMatrix).ToArray();cloak.RecalculateNormals();cloak.RecalculateTangents();cloak.RecalculateBounds();
            var cloakGo=new GameObject("Torn shadow cloak");cloakGo.transform.SetParent(visual,false);var cloakSkin=cloakGo.AddComponent<SkinnedMeshRenderer>();
            cloakSkin.sharedMesh=SaveMesh(cloak,"TornCloak");cloakSkin.bones=cloakBones;cloakSkin.rootBone=visual;cloakSkin.sharedMaterial=cloth;cloakSkin.localBounds=cloakSkin.sharedMesh.bounds;cloakSkin.updateWhenOffscreen=false;
            var hood=Shell(root,visual,bones["Head"],new Vector3(0,1.66f,.11f),new Vector3(.18f,.21f,.16f),cloth,"Shadow hood",true);
            var scarf=Shell(root,visual,bones["Head"],new Vector3(0,1.52f,.18f),new Vector3(.16f,.075f,.15f),wraps,"Crimson face scarf",false);
            var knife=Knife(root,visual,bones["Hand.R"],steel,wraps);
            var tunic=Shell(root,visual,bones["UpperChest"],new Vector3(0,1.13f,.08f),new Vector3(.255f,.30f,.22f),cloth,"Shadow woven tunic",false);
            var sash=Shell(root,visual,bones["Hips"],new Vector3(0,.96f,.10f),new Vector3(.24f,.065f,.21f),wraps,"Crimson waist sash",false);
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=7;
            var group=root.GetComponent<LODGroup>();var lods=group.GetLODs();var details=new Renderer[]{cloakSkin,hood,scarf,knife,tunic,sash};
            for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null).Concat(details).ToArray();group.SetLODs(lods);group.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,path);
            AssetDatabase.SaveAssets();return "Bat visual scale0.8; Shadow skinned cloak/hood/scarf/knife + lowered idle arms; gameplay root/capsule unchanged";
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void QuaternionCurves(AnimationClip clip,Transform visual,Transform bone)
    {
        var q=bone.localRotation;string path=AnimationUtility.CalculateTransformPath(bone,visual);
        foreach(var pair in new[]{new KeyValuePair<string,float>("x",q.x),new KeyValuePair<string,float>("y",q.y),new KeyValuePair<string,float>("z",q.z),new KeyValuePair<string,float>("w",q.w)})
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+pair.Key),AnimationCurve.Constant(0,clip.length,pair.Value));
    }
    static Material Material(string name,Color tint,float smooth)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Campus Rift/Enemy PBR Dissolve")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",name.Contains("steel")?.65f:0);m.SetColor("_EdgeColor",new Color(.7f,.1f,.9f));
        // Small woven variation authored in Unity; no edits to supplied FBX or source UV maps.
        string texPath=Folder+"/"+name+".png";var tex=new Texture2D(128,128,TextureFormat.RGB24,false);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){float grain=.8f+.14f*Mathf.PerlinNoise(x*.2f,y*.2f)+(((x+y)%4==0)?.07f:0);tex.SetPixel(x,y,new Color(grain,grain,grain));}tex.Apply();File.WriteAllBytes(texPath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texPath);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));EditorUtility.SetDirty(m);return m;
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
    static SkinnedMeshRenderer Shell(GameObject root,Transform visual,Transform bone,Vector3 center,Vector3 radius,Material mat,string name,bool hood)
    {
        var mesh=new Mesh{name=name};var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();int rings=6,segments=14;
        // Hood is open at the face (+Z); scarf forms a short cloth band around jaw.
        for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++)
        {
            float v=y/(float)rings,u=x/(float)segments;float a=(hood?35+290*u:360*u)*Mathf.Deg2Rad;
            float ry=hood?Mathf.Lerp(-.45f,1,v):Mathf.Lerp(-1,1,v);float r=hood?Mathf.Sqrt(Mathf.Max(.07f,1-ry*ry)):.92f;
            var point=center+new Vector3(Mathf.Sin(a)*radius.x*r,ry*radius.y,Mathf.Cos(a)*radius.z*r);
            vs.Add(visual.InverseTransformPoint(root.transform.TransformPoint(point)));uv.Add(new Vector2(u,v));
        }
        for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=y*(segments+1)+x;ts.AddRange(new[]{a,a+1,a+segments+1,a+1,a+segments+2,a+segments+1});}
        mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);return Skin(mesh,visual,bone,mat,name);
    }
    static SkinnedMeshRenderer Knife(GameObject root,Transform visual,Transform hand,Material steel,Material wraps)
    {
        var mesh=new Mesh{name="Shadow dagger"};var points=new[]{new Vector3(-.035f,0,0),new Vector3(.035f,0,0),new Vector3(.028f,-.25f,0),new Vector3(0,-.36f,0),new Vector3(-.028f,-.25f,0),new Vector3(0,-.17f,.025f)};
        // Dagger descends from the palm; skin binding makes every attack/death clip carry it.
        var h=root.transform.InverseTransformPoint(hand.position);mesh.vertices=points.Select(v=>visual.InverseTransformPoint(root.transform.TransformPoint(h+v+Vector3.forward*.055f))).ToArray();mesh.uv=points.Select(v=>new Vector2(v.x*10+.5f,-v.y*2.6f)).ToArray();mesh.triangles=new[]{0,1,5,1,2,5,2,3,5,3,4,5,4,0,5};
        return Skin(mesh,visual,hand,steel,"Shadow bone dagger");
    }
    static SkinnedMeshRenderer Skin(Mesh mesh,Transform visual,Transform bone,Material mat,string name)
    {
        mesh.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},mesh.vertexCount).ToArray();mesh.bindposes=new[]{bone.worldToLocalMatrix*visual.localToWorldMatrix};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var go=new GameObject(name);go.transform.SetParent(visual,false);var r=go.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=SaveMesh(mesh,name);r.bones=new[]{bone};r.rootBone=visual;r.sharedMaterial=mat;r.localBounds=r.sharedMesh.bounds;return r;
    }
}
#endif
