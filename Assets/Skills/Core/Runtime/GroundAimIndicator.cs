using UnityEngine;
using CampusRift.Combat;
using CampusRift.Controls;
namespace CampusRift.Skills
{
    public sealed class GroundAimIndicator : MonoBehaviour
    {
        SkillVfxPool pool;
        SkillVfxPool.Node indicator;
        readonly GiantHandTargeting targeting=new GiantHandTargeting();
        public Vector3 Point {get;private set;}
        public Vector3 Direction {get;private set;}
        public bool Valid {get;private set;}
        public void Show(bool cone,float radius,Color color)
        {
            Hide();pool=GetComponent<SkillVfxPool>();
            if(pool!=null)indicator=pool.Spawn(cone?SkillVfxKind.Cone:SkillVfxKind.Ring,transform.position,color,3600,radius);
        }
        public bool Evaluate(Camera camera,CampusInput input,float range,bool cone)
        {
            Vector2 viewport=input!=null&&CampusInput.Mobile?new Vector2(.5f+input.SkillDrag.x*.36f,.43f+input.SkillDrag.y*.32f):new Vector2(.5f,.5f);
            Ray ray=camera!=null?camera.ViewportPointToRay(viewport):new Ray(transform.position+Vector3.up*1.5f,transform.forward+Vector3.down*.3f);
            RaycastHit hit;
            Vector3 flat=Vector3.ProjectOnPlane(ray.direction,Vector3.up).normalized;if(flat.sqrMagnitude<.01f)flat=transform.forward;
            Vector3 p=transform.position+flat*Mathf.Min(8,range);
            if(targeting.Cast(ray.origin,ray.direction,range+12,transform,out hit))
            {
                if(hit.normal.y>.5f)p=hit.point;
                else p=hit.point-ray.direction*.35f;
            }
            p=transform.position+Vector3.ClampMagnitude(p-transform.position,range);
            Valid=targeting.Cast(p+Vector3.up*3,Vector3.down,6,transform,out hit)&&hit.normal.y>.5f;
            if(Valid)p=hit.point;
            Point=p;Direction=Vector3.ProjectOnPlane(Point-transform.position,Vector3.up).normalized;if(Direction.sqrMagnitude<.01f)Direction=flat;
            if(Valid&&!cone)Valid=CombatLine.Clear(transform.position+Vector3.up,Point+Vector3.up*.2f,transform);
            if(indicator!=null){indicator.Position=cone?transform.position:Point;indicator.End=indicator.Position+Direction;indicator.Progress=1;}
            return Valid;
        }
        public void SetExternal(Vector3 point,bool valid){Point=point;Valid=valid;if(indicator!=null){indicator.Position=point;indicator.Opacity=valid?1:0;}}
        public void Hide(){if(pool!=null)pool.Release(indicator);indicator=null;}
        void OnDisable(){Hide();}
    }
}
