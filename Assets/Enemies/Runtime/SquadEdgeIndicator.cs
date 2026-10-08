using UnityEngine;
using CampusRift.UI;
namespace CampusRift.Enemies
{
    // A small compass warning for wings leaving view. IMGUI draws only a handful of markers;
    // no temporary Canvas/GameObjects/text are created in gameplay. Uses the existing white texture.
    public sealed class SquadEdgeIndicator:MonoBehaviour
    {
        Camera view;EnemyDirector director;
        void Awake(){director=GetComponent<EnemyDirector>();}
        void OnGUI()
        {
            if(Event.current.type!=EventType.Repaint||Time.timeScale<=0||director.Squad==null||!director.Squad.Enabled||
                UIStateManager.Instance!=null&&UIStateManager.Instance.State!=UIState.Gameplay)return;
            if(view==null)view=Camera.main;if(view==null)return;
            int shown=0;var old=GUI.color;
            for(int i=0;i<director.Squad.Members.Count;i++)
            {
                var a=director.Squad.Members[i];
                if(shown>=4)break;
                if(!director.Squad.TryGet(a.enemy,out _)||a.role==SquadRole.Chaser||a.role==SquadRole.Support||a.role==SquadRole.Ranged)continue;
                Vector3 p=view.WorldToViewportPoint(a.enemy.transform.position+Vector3.up);
                if(p.z>0&&p.x>.04f&&p.x<.96f&&p.y>.08f&&p.y<.9f)continue;
                Vector2 dir=new Vector2(p.x-.5f,.5f-p.y);if(p.z<0)dir=-dir;
                if(dir.sqrMagnitude<.01f)dir=Vector2.right;
                float scale=Mathf.Min((Screen.width*.5f-30)/Mathf.Max(.01f,Mathf.Abs(dir.x)),(Screen.height*.5f-85)/Mathf.Max(.01f,Mathf.Abs(dir.y)));
                Vector2 pos=new Vector2(Screen.width*.5f,Screen.height*.5f)+dir*scale;
                GUI.color=a.role==SquadRole.Interceptor||a.role==SquadRole.Ambusher?new Color(1,.55f,.16f,.8f):new Color(.8f,.35f,1,.8f);
                GUI.DrawTexture(new Rect(pos.x-6,pos.y-6,12,12),Texture2D.whiteTexture);
                GUI.color=new Color(1,1,1,.65f);GUI.DrawTexture(new Rect(pos.x-2,pos.y-2,4,4),Texture2D.whiteTexture);shown++;
            }
            GUI.color=old;
        }
    }
}
