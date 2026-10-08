using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // A single rigidly weighted accent mesh follows the existing skeleton on every LOD.
    public sealed class SkyBeastIdentity:MonoBehaviour
    {
        public int HornCount {get;private set;}
        public int AccentTriangles=>mesh!=null?mesh.triangles.Length/3:0;
        public SkinnedMeshRenderer Accent {get;private set;}
        Mesh mesh;readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> triangles=new List<int>();
        readonly List<Color> colors=new List<Color>();readonly List<BoneWeight> weights=new List<BoneWeight>();readonly List<Transform> bones=new List<Transform>();
        public void Build(SkyBeastController beast)
        {
            var id=beast.definition.id;bool king=id=="020"||id=="hoa-long-vuong",bird=id=="026"||id=="chu-tuoc";
            Color hot=king?new Color(1,.20f,.025f):bird?new Color(1,.42f,.045f):new Color(1,.73f,.14f);
            var all=GetComponentsInChildren<Transform>(true);var map=new Dictionary<string,Transform>();foreach(var t in all)if(!map.ContainsKey(t.name))map[t.name]=t;
            if(king&&map.TryGetValue("Skull",out var skull))
            {
                for(int i=0;i<9;i++){float a=(i-4)*.32f;Spike(skull,new Vector3(Mathf.Sin(a)*.38f,.20f,-.10f),new Vector3(Mathf.Sin(a)*.32f,.55f+(.18f*(4-Mathf.Abs(i-4))/4),-.24f),.09f,hot);HornCount++;}
                foreach(var pair in map)if(pair.Key.StartsWith("Neck_")||pair.Key.StartsWith("Spine_"))Spike(pair.Value,new Vector3(0,.20f,0),new Vector3(0,.48f,-.12f),.09f,hot*.8f);
            }
            else if(bird)
            {
                foreach(var pair in map)if(pair.Key.StartsWith("WingFinger")&&pair.Key.Contains("_03."))
                {float side=pair.Key.EndsWith(".L")?-1:1;Spike(pair.Value,Vector3.zero,new Vector3(side*.50f,.32f,-.75f),.14f,hot);}
                if(map.TryGetValue("Socket_TailTip",out var tail))for(int i=-2;i<=2;i++)Spike(tail,Vector3.zero,new Vector3(i*.32f,.18f,-1.5f-Mathf.Abs(i)*.10f),.12f,hot);
            }
            else
            {
                foreach(var pair in map)if(pair.Key.StartsWith("Spine_")||pair.Key.StartsWith("Tail_"))
                    for(int side=-1;side<=1;side+=2)Spike(pair.Value,new Vector3(side*.16f,.13f,0),new Vector3(side*.06f,.22f,-.16f),.09f,hot);
            }
            var go=new GameObject(king?"Nine horn fire crown / mane":bird?"Burning wing quills / five tail plumes":"Luminous scale ridges");go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            mesh=new Mesh{name=go.name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.boneWeights=weights.ToArray();var poses=new Matrix4x4[bones.Count];for(int i=0;i<bones.Count;i++)poses[i]=bones[i].worldToLocalMatrix*go.transform.localToWorldMatrix;mesh.bindposes=poses;mesh.RecalculateNormals();mesh.RecalculateBounds();
            Accent=go.AddComponent<SkinnedMeshRenderer>();Accent.sharedMesh=mesh;Accent.bones=bones.ToArray();Accent.rootBone=transform;var bounds=mesh.bounds;bounds.Expand(4);Accent.localBounds=bounds;Accent.sharedMaterial=Resources.Load<Material>("P21/IdentityGlow");Accent.shadowCastingMode=ShadowCastingMode.Off;
            // Preserve texture detail; use a multiply tint rather than replacing authored materials.
            foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>())if(r!=Accent){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor("_BaseColor",king?new Color(.74f,.40f,.35f):bird?new Color(.55f,.20f,.14f):new Color(.95f,.42f,.27f));if(king)block.SetColor("_EmissionColor",new Color(.025f,.0015f,.001f));r.SetPropertyBlock(block);}
            var lod=GetComponentInChildren<LODGroup>();if(lod!=null){var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++){var rs=new List<Renderer>(levels[i].renderers);rs.Add(Accent);levels[i].renderers=rs.ToArray();}lod.SetLODs(levels);}
        }
        void Spike(Transform bone,Vector3 offset,Vector3 direction,float radius,Color tint)
        {
            int bi=bones.IndexOf(bone);if(bi<0){bi=bones.Count;bones.Add(bone);}int start=vertices.Count;
            Vector3 origin=transform.InverseTransformPoint(bone.position)+offset;Vector3 tip=origin+direction;
            Vector3 axis=direction.normalized,right=Vector3.Cross(axis,Vector3.forward).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;Vector3 up=Vector3.Cross(axis,right);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Add(origin+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius,bi,tint*.35f);}
            Add(tip,bi,tint);for(int i=0;i<6;i++){triangles.Add(start+i);triangles.Add(start+(i+1)%6);triangles.Add(start+6);}
        }
        void Add(Vector3 p,int bone,Color color){color.a=1;vertices.Add(p);colors.Add(color);weights.Add(new BoneWeight{boneIndex0=bone,weight0=1});}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
