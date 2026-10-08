using System.Collections.Generic;
using UnityEngine;
using CampusRift.Skills;
namespace CampusRift.Enemies
{
    // A fixed pool of ground warnings, each with a broad ink silhouette and an HDR core.
    public sealed class EnemyTelegraph : MonoBehaviour
    {
        static readonly List<EnemyTelegraph> nodes=new List<EnemyTelegraph>(32);
        LineRenderer ink,core,pattern;float until,born;Color hue;int points;readonly Vector3[] vertices=new Vector3[70];
        public bool Live {get;private set;}
        public float Radius {get;private set;}
        public static int Exhaustions {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){nodes.Clear();Exhaustions=0;}
        public static void Warm()
        {
            if(nodes.Count>0)return;
            var material=Resources.Load<Material>("EnemyVfx/P12Telegraph");
            for(int i=0;i<32;i++){
                var go=new GameObject("Enemy Telegraph "+i);var n=go.AddComponent<EnemyTelegraph>();
                n.ink=n.Line("Ink",material,.36f);n.core=n.Line("Core",material,.12f);n.pattern=n.Line("Crossed danger pattern",material,.09f);nodes.Add(n);go.SetActive(false);}
        }
        LineRenderer Line(string name,Material material,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var l=go.AddComponent<LineRenderer>();
            l.sharedMaterial=material;l.useWorldSpace=false;l.widthMultiplier=width;l.numCornerVertices=4;l.numCapVertices=4;l.sortingOrder=name=="Core"?1:0;
            l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;return l;
        }
        public static EnemyTelegraph Show(Vector3 at,Vector3 forward,float radius,float seconds,DangerShape shape=DangerShape.Circle,float extent=120,Color color=default)
        {
            Warm();foreach(var n in nodes)if(!n.Live){n.Begin(at,forward,radius,seconds,shape,extent,color==default?new Color(2.4f,.26f,.08f):color);return n;}
            Exhaustions++;return null;
        }
        public static EnemyTelegraph ShowHalo(Vector3 head,Vector3 towards,float seconds)
        {
            var n=Show(head,towards,.6f,seconds,color:new Color(2.4f,.45f,1.2f));
            if(n!=null&&towards.sqrMagnitude>.001f)n.transform.rotation=Quaternion.LookRotation(towards.normalized)*Quaternion.Euler(90,0,0);
            return n;
        }
        void Begin(Vector3 at,Vector3 forward,float radius,float seconds,DangerShape shape,float extent,Color color)
        {
            transform.position=at+Vector3.up*.075f;forward=Vector3.ProjectOnPlane(forward,Vector3.up);
            transform.rotation=forward.sqrMagnitude>.001f?Quaternion.LookRotation(forward):Quaternion.identity;
            Radius=radius;born=Time.time;until=born+seconds;hue=color;Live=true;gameObject.SetActive(true);
            points=0;
            if(shape==DangerShape.Capsule){vertices[points++]=new Vector3(-radius,0,0);vertices[points++]=new Vector3(-radius,0,extent);vertices[points++]=new Vector3(0,0,extent+radius);vertices[points++]=new Vector3(radius,0,extent);vertices[points++]=new Vector3(radius,0,0);vertices[points++]=vertices[0];}
            else {
                if(shape==DangerShape.Cone)vertices[points++]=Vector3.zero;
                int segments=shape==DangerShape.Circle?64:32;float angle=shape==DangerShape.Circle?360:extent;
                for(int i=0;i<=segments;i++){float a=(-angle*.5f+angle*i/segments)*Mathf.Deg2Rad;vertices[points++]=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius;}
                if(shape==DangerShape.Cone)vertices[points++]=Vector3.zero;
            }
            ink.positionCount=core.positionCount=points;for(int i=0;i<points;i++){ink.SetPosition(i,vertices[i]);core.SetPosition(i,vertices[i]+Vector3.up*.005f);}
            float z=shape==DangerShape.Circle?0:shape==DangerShape.Capsule?extent*.5f:radius*.5f;
            float arm=radius*(shape==DangerShape.Cone?.2f:.45f);
            pattern.positionCount=5;pattern.SetPositions(new[]{new Vector3(-arm,.02f,z-arm),new Vector3(arm,.02f,z+arm),new Vector3(0,.02f,z),new Vector3(arm,.02f,z-arm),new Vector3(-arm,.02f,z+arm)});
            Apply();
        }
        void Apply(){bool accessible=UI.Accessibility.ColorBlind;float pulse=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes==true?1:.7f+.3f*Mathf.Sin((Time.time-born)*23);ink.startColor=ink.endColor=new Color(.055f,.008f,.025f,.96f);core.startColor=core.endColor=(accessible?new Color(1,.72f,.08f):hue)*pulse;pattern.enabled=accessible;pattern.startColor=pattern.endColor=Color.white;}
        void Update(){if(!Live)return;Apply();if(Time.time>=until)Hide();}
        public void Hide(){if(this==null)return;Live=false;gameObject.SetActive(false);}
        public static void Clear(){foreach(var n in nodes)if(n!=null)n.Hide();}
    }
}
