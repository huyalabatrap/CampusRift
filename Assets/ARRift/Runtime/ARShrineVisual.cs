using UnityEngine;
using UnityEngine.Rendering;
using CampusRift.Monsters;

namespace CampusRift.AR
{
    public sealed class ARShrineVisual : MonoBehaviour
    {
        PlayerMonsterHealth health;
        ARBattlefield field;
        readonly Transform[] shards = new Transform[6];
        readonly LineRenderer[] cracks = new LineRenderer[6];
        readonly Mesh[] meshes = new Mesh[6];
        Material stone, ink, gold, violet, light;
        Transform halo;
        float struckAt = -100, brokenAt = -1;
        static readonly Color Gold = new Color(1,.66f,.06f), Violet = new Color(.53f,.16f,1);
        public bool Broken => brokenAt >= 0;
        public void Initialize(PlayerMonsterHealth target, ARBattlefield battlefield)
        {
            health=target;field=battlefield;
            stone=Material(new Color(.15f,.13f,.23f),0);ink=Material(new Color(.015f,.008f,.03f),0);
            gold=Material(Gold,.8f);violet=Material(Violet,1.1f);light=Material(new Color(1,.93f,.63f),2);
            gold.SetFloat("_Metallic",.7f);gold.SetFloat("_Smoothness",.85f);violet.SetFloat("_Metallic",.6f);violet.SetFloat("_Smoothness",.85f);
            Disc("Ink pedestal",.70f,.09f,.08f,ink);
            Disc("Round carved stone",.65f,.09f,.13f,stone);
            Disc("Gold pedestal lip",.58f,.025f,.235f,gold);
            Disc("Violet inset",.51f,.02f,.265f,violet);
            var rune=ARRune.Create(transform,.66f);rune.transform.localPosition=Vector3.up*.285f;
            halo=ARRune.Create(transform,.56f).transform;halo.localPosition=Vector3.up*.42f;
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
                var vertices=new[]{new Vector3(0,1.65f,0),new Vector3(Mathf.Cos(a)*.34f,.84f,Mathf.Sin(a)*.34f),new Vector3(Mathf.Cos(b)*.34f,.84f,Mathf.Sin(b)*.34f),new Vector3(0,.4f,0)};
                var mesh=new Mesh{name="Runic crystal facet"};
                // Separate triangle vertices retain crisp comic facets.
                int[] indices={0,2,1,1,2,3,0,1,3,0,3,2};var flat=new Vector3[12];for(int j=0;j<12;j++)flat[j]=vertices[indices[j]];
                mesh.vertices=flat;mesh.triangles=new[]{0,1,2,3,4,5,6,7,8,9,10,11};mesh.RecalculateNormals();meshes[i]=mesh;
                var go=new GameObject("Rune crystal shard "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);shards[i]=go.transform;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=i%2==0?gold:violet;
                var edge=Line(go.transform,"Ink crystal edges",ink,.018f,new[]{vertices[0],vertices[1],vertices[3],vertices[2],vertices[0]});
                cracks[i]=Line(go.transform,"Luminous fracture",light,.024f,new[]{Vector3.Lerp(vertices[0],vertices[1],.3f),Vector3.Lerp(vertices[0],vertices[1],.63f)+Vector3.up*.04f,vertices[1],Vector3.Lerp(vertices[1],vertices[3],.7f)});
                cracks[i].enabled=false;
            }
            health.Damaged.AddListener(Hit);health.Defeated.AddListener(Break);
        }
        Material Material(Color color,float emission)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.35f);
            if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}return m;
        }
        void Disc(string label,float radius,float halfHeight,float y,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=label;Destroy(go.GetComponent<Collider>());go.transform.SetParent(transform,false);
            go.transform.localPosition=Vector3.up*y;go.transform.localScale=new Vector3(radius*2,halfHeight,radius*2);go.GetComponent<Renderer>().sharedMaterial=material;
        }
        LineRenderer Line(Transform parent,string label,Material material,float width,Vector3[] points)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=material;
            l.widthMultiplier=width*field.Scale;l.positionCount=points.Length;l.SetPositions(points);l.shadowCastingMode=ShadowCastingMode.Off;return l;
        }
        void Hit(float amount){struckAt=field.Clock;}
        void Break(){brokenAt=field.Clock;}
        void Update()
        {
            if(health==null)return;
            if(!health.IsDead&&Broken)brokenAt=-1;
            float now=field.Clock,flash=Mathf.Clamp01(1-(now-struckAt)/.35f),damage=1-health.CurrentHealth/health.maxHealth;
            gold.SetColor("_EmissionColor",Gold*(.8f+flash*2));violet.SetColor("_EmissionColor",Violet*(1.1f+flash*2));
            halo.localScale=Vector3.one*(Broken?Mathf.Max(0,1-(now-brokenAt)*2):1+flash*.15f);
            for(int i=0;i<shards.Length;i++)
            {
                float a=(i+.5f)*Mathf.PI/3;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float t=Broken?Mathf.Clamp01((now-brokenAt)/.8f):0;
                shards[i].localPosition=Broken?dir*(t*.6f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.55f-t*.1f):dir*(flash*.07f+damage*.025f)+Vector3.up*(Mathf.Sin(now*2)*.035f);
                shards[i].localRotation=Broken?Quaternion.Euler(t*55,i*17*t,t*65):Quaternion.identity;
                shards[i].localScale=Vector3.one*(Broken?Mathf.Lerp(1,.26f,t):1);
                cracks[i].enabled=!Broken&&(flash>0||damage>(i+1)/8f);
            }
        }
        void OnDestroy()
        {
            if(health!=null){health.Damaged.RemoveListener(Hit);health.Defeated.RemoveListener(Break);}
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            foreach(var m in new[]{stone,ink,gold,violet,light})if(m!=null)Destroy(m);
        }
    }
}
