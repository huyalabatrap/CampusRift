using UnityEngine;
namespace CampusRift.SkyBeast
{
    public enum SkyPath { Circle, FigureEight }
    public enum SkyAttack { None, Feather, Meteor }
    [System.Serializable] public sealed class SkyBeastPhase
    {
        public FireBreathProfile fire; public SkyAttack attack; public float furyMultiplier=1;
    }
    [CreateAssetMenu(menuName="Campus Rift/Sky Beast")]
    public sealed class SkyBeastDefinition : ScriptableObject
    {
        public string id,displayName;public GameObject prefab;
        public float scale=6,altitude=90,orbitRadius=105,cruiseSpeed=12,period=68;
        public string roarState="Air_Roar";public AudioClip[] roars=new AudioClip[0];public AudioClip rumble,wind;
        public Vector3 center=new Vector3(0,0,-5);public Color accent=Color.white;
        public float roarMin=20,roarMax=45;
        public string nameVi,nameEn;
        public SkyPath pathType;
        public int segments=1,level10Segments=1;
        public SkyBeastPhase[] phases=new SkyBeastPhase[0];
        public bool Valid=>prefab!=null&&scale>0&&altitude>=90&&orbitRadius>0&&period>0&&segments>0&&level10Segments==1&&phases.Length>0&&System.Array.TrueForAll(phases,p=>p!=null&&p.fire!=null&&p.furyMultiplier>=1);
        public static SkyBeastDefinition LoadCombat(string id)=>Resources.Load<SkyBeastDefinition>("P14/"+id);
    }
}
