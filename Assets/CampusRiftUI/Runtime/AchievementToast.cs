using System.Collections.Generic;
using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.UI
{
    public sealed class AchievementToast:MonoBehaviour
    {
        readonly Queue<AchievementDefinition> queue=new Queue<AchievementDefinition>();UiKit kit;RectTransform card;float until;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Scene;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Scene;Create();}
        static void Scene(UnityEngine.SceneManagement.Scene s,UnityEngine.SceneManagement.LoadSceneMode m){Create();}
        static void Create(){var ui=FindAnyObjectByType<UIManager>();if(ui==null||FindAnyObjectByType<AchievementToast>()!=null)return;var canvas=ui.MainMenu!=null?ui.MainMenu.GetComponentInParent<Canvas>():FindAnyObjectByType<Canvas>();if(canvas==null)return;var go=new GameObject("Achievement Toast",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);go.AddComponent<AchievementToast>();}
        void Start(){kit=UiKit.Create();EndgameService.Unlocked+=Add;var r=(RectTransform)transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        void Add(AchievementDefinition a){queue.Enqueue(a);}
        void Update()
        {
            if(card!=null&&Time.unscaledTime>=until){Destroy(card.gameObject);card=null;}
            if(card!=null||queue.Count==0||kit==null)return;
            var a=queue.Dequeue();var frame=kit.Fit(transform,"Toast frame");card=frame;
            var p=kit.Panel(frame,"Comic achievement",610,22,700,146,true);
            kit.Text(p,LevelHUD.Vietnamese?"THÀNH TỰU ĐÃ MỞ":"ACHIEVEMENT UNLOCKED",25,18,650,40,23,ComicTheme.Navy);
            kit.Text(p,LevelHUD.Vietnamese?a.nameVN:a.nameEN,25,65,650,54,34,ComicTheme.Navy);until=Time.unscaledTime+3.5f;transform.SetAsLastSibling();
        }
        void OnDestroy(){EndgameService.Unlocked-=Add;}
    }
}
