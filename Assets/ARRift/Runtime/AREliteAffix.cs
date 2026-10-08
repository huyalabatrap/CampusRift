using UnityEngine;
using TMPro;
using CampusRift.Combat;
using CampusRift.Enemies;
namespace CampusRift.AR
{
    // AR split uses SpawnAR and remains under the six-actor cap.
    public sealed class AREliteAffix : MonoBehaviour,IArmorShatterable
    {
        public AREliteKind Kind {get;private set;} public bool Child {get;private set;} public bool Leader {get;private set;}
        public bool MetalBroken {get;private set;} public float SpeedMultiplier=>Kind==AREliteKind.Haste?2:1;
        ARBattlefield field;EnemyInstance owner;Renderer[] model;bool[] originalOff;float visibleUntil;TMP_Text badge;
        public void Configure(ARBattlefield value,AREliteKind kind,bool child,bool leader)
        {
            Restore();field=value;owner=GetComponent<EnemyInstance>();Kind=kind;Child=child;Leader=leader;MetalBroken=false;visibleUntil=0;
            if(model==null){model=GetComponentsInChildren<SkinnedMeshRenderer>();originalOff=new bool[model.Length];for(int i=0;i<model.Length;i++)originalOff[i]=model[i].forceRenderingOff;}
            transform.localScale=Vector3.one*(child?.55f:leader?1.2f:1);
            if(child||leader)owner.Vitality.SetMaxHealth(owner.archetype.baseHealth*(child?.3f:2),true);
            if(badge==null){var go=new GameObject("AR elite name");go.transform.SetParent(transform,false);badge=go.AddComponent<TextMeshPro>();CampusRift.UI.ComicTheme.Text(badge);badge.margin=Vector4.zero;badge.overflowMode=TextOverflowModes.Overflow;badge.alignment=TextAlignmentOptions.Center;badge.fontSize=3;badge.rectTransform.sizeDelta=new Vector2(7,1);badge.color=CampusRift.UI.ComicTheme.Gold;}
            badge.gameObject.SetActive(kind!=AREliteKind.None||leader);
        }
        public bool Accept(ref DamageInfo hit)
        {
            if(!isActiveAndEnabled||Kind==AREliteKind.None)return true;
            if(Kind==AREliteKind.Invisible){if(hit.element==Element.Loi)visibleUntil=field.Clock+4;else if(field.Clock>=visibleUntil)return false;}
            if(Kind==AREliteKind.MetalArmor&&!MetalBroken)
            {if(hit.element==Element.Kim&&owner.Status!=null&&owner.Status.Has(StatusType.Stun)){MetalBroken=true;return true;}if(owner.Status!=null&&owner.Status.Has(StatusType.ArmorBreak))MetalBroken=true;else hit.amount=0;}
            return true;
        }
        public void BreakMetalBody(){MetalBroken=true;}
        public string Label(bool vn)
        {if(Leader)return vn?"ĐẦU ĐÀN":"PACK LEADER";string[] vi={"","GIÁP KIM","ẨN THÂN","PHÂN LIỆT","TỐC HÀNH"},en={"","METAL ARMOR","INVISIBLE","SPLIT","HASTE"};return vn?vi[(int)Kind]:en[(int)Kind];}
        void LateUpdate()
        {
            if(field==null||owner==null)return;bool hidden=Kind==AREliteKind.Invisible&&field.Clock>=visibleUntil&&owner.Alive;
            for(int i=0;i<model.Length;i++)if(model[i]!=null)model[i].forceRenderingOff=hidden||originalOff[i];
            if(badge!=null&&badge.gameObject.activeSelf){badge.text=Label(CampusRift.UI.LevelHUD.Vietnamese);badge.transform.localPosition=new Vector3(0,2.6f,0);var cam=field.placement.view;if(cam!=null)badge.transform.rotation=Quaternion.LookRotation(badge.transform.position-cam.transform.position);badge.enabled=!hidden&&owner.Alive;}
        }
        void Restore(){if(model!=null)for(int i=0;i<model.Length;i++)if(model[i]!=null)model[i].forceRenderingOff=originalOff[i];}
        void OnDisable(){Restore();if(badge!=null)badge.gameObject.SetActive(false);Kind=AREliteKind.None;}
    }
}
